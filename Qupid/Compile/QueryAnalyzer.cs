using Qupid.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using Qupid.Mongo;

namespace Qupid.Compile
{
    internal class QueryAnalyzer
    {
        private readonly QupidQuery _query;
        private readonly List<QupidCollection> _collections;
        private readonly Compiler _compiler;

        public QueryAnalyzer(Compiler compiler, QupidQuery root)
        {
            _query = root;
            _collections = compiler.Collections;
            _compiler = compiler;
        }

        public void Analyze()
        {
            var collection = _collections.SingleOrDefault(c => c.Name.Equals(_query.CollectionName, StringComparison.OrdinalIgnoreCase));
            if (collection == null)
            {
                Fail("Unknown collection: " + _query.CollectionName);
                return;
            }
            if (collection.Indices == null)
            {
                Fail("Invalid collection, found a null 'Indices' property");
                return;
            }

            _query.Collection = collection;

            AnalyzeSelectList(collection, _query.SelectProperties);
            AnalyzeWhereClause(collection, _query.WhereClauses);
            AnalyzeUnwindClause(collection, _query.UnwindClause);
            AnalyzeGroupByClause(collection, _query.GroupByClause);
            AnalyzeHavingClause(collection, _query.HavingClause);
            AnalyzeWithClause(collection, _query.WithClause);
        }

        private void AnalyzeUnwindClause(QupidCollection collection, UnwindClause unwindClause)
        {
            if (unwindClause != null)
            {
                var prop = unwindClause.Property;
                var shortName = collection.ConvertToShortPath(prop.Path);
                if (shortName == null)
                {
                    _compiler.ErrorManager.AddWarning("The 'unwind' property (" + prop.Path + ") is invalid.", prop.Line, prop.Character);
                }
                else
                {
                    prop.AnalyzedName = shortName;
                }
            }
        }

        private void AnalyzeSelectList(QupidCollection collection, PropertyList propertyList)
        {
            if (propertyList == null || propertyList.Properties == null)
            {
                return;
            }

            foreach (var prop in propertyList.Properties.ToArray())
            {
                if (!prop.Collection.Equals(collection.Name, StringComparison.OrdinalIgnoreCase))
                {
                    // skip over selected properties from "with" tables/collections
                    continue;
                }
                if (prop.IsAggregate)
                {
                    continue;
                }
                if (prop.Path == "*")
                {
                    // expand '*' property references to include everything within that nesting level
                    var expandedProperties = collection.GetAllReferencedProperties(prop.Path);
                    if (expandedProperties == null)
                    {
                        _compiler.ErrorManager.AddWarning("The 'select' property (" + prop.Path + ") is invalid.", prop.Line, prop.Character);
                        continue;
                    }

                    // add all expanded properties
                    propertyList.Properties.AddRange(expandedProperties.Select(qp => new PropertyReference(collection.Name, qp.LongName, prop.Line, prop.Character)
                        {
                            AnalyzedName = qp.ShortName,
                        }));

                    // pull out the '.*' property
                    propertyList.Properties.Remove(prop);
                    continue;
                }

                var shortName = collection.ConvertToShortPath(prop.Path);
                if (shortName == null)
                {
                    _compiler.ErrorManager.AddWarning("The 'select' property (" + prop.Path + ") is invalid.", prop.Line, prop.Character);
                    continue;
                }
                prop.AnalyzedName = shortName;
            }
        }

        private void AnalyzeWithClause(QupidCollection collection, WithClause with)
        {
            if (with != null)
            {
                var withTable = with.JoinOnTable;
                var columns = _query.SelectProperties.Properties
                    .Where(p => p.Collection.Equals(withTable, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                with.SelectedColumns = columns;

                var shortName = collection.ConvertToShortPath(with.JoinProperty.Path);
                if (shortName == null)
                {
                    _compiler.ErrorManager.AddError("The 'with' property (" + with.JoinProperty.Path + ") is invalid.");
                    return;
                }
                with.JoinProperty.AnalyzedName = shortName;
                if (String.IsNullOrWhiteSpace(with.JoinProperty.Alias))
                {
                    with.JoinProperty.Alias = shortName;
                }
            }
        }

        private void AnalyzeGroupByClause(QupidCollection collection, GroupByClause group)
        {
            if (group != null)
            {
                if (group.Property.IsAggregate)
                {
                    _compiler.ErrorManager.AddError("Cannot 'group by' an aggregate property " + group.Property.Path);
                    return;
                }

                var shortName = collection.ConvertToShortPath(group.Property.Path);
                if (shortName == null)
                {
                    _compiler.ErrorManager.AddError("The 'group by' property (" + group.Property.Path + ") is invalid.");
                    return;
                }

                var dbProp = collection.GetProperty(group.Property.Path);
                if (dbProp.Type == "DateTime")
                {
                    _compiler.ErrorManager.AddWarning("You really want to group by a DateTime value? Really?");
                }

                group.Property.Alias = "_id";
                group.Property.AnalyzedName = shortName;
                group.AggregateByProperty = group.Property;
                if (_query.SelectProperties.Properties.Count(pr => pr.IsAggregate) > 1)
                {
                    Fail("You can only include one 'COUNT' or 'SUM' property in your 'select'");
                    return;
                }
                group.AggregationProperty = _query.SelectProperties.Properties.SingleOrDefault(pr => pr.IsAggregate);
                if (group.AggregationProperty == null)
                {
                    _compiler.ErrorManager.AddWarning("The aggregation property was not specified. Include a '.COUNT' or '.SUM' in your select clause");
                }
                else if (group.AggregationProperty.AggregateType != AggregateTypes.Count)
                {
                    var aggShortName = collection.ConvertToShortPath(group.AggregationProperty.Path);
                    if (aggShortName == null)
                    {
                        Fail("The Sum property is invalid - please append '.SUM' to the end of a valid numeric property name (ex: Donations.TotalAmount.SUM).");
                        return;
                    }
                    group.AggregationProperty.AnalyzedName = aggShortName;
                }

                //get only the first segment of the index (because the groupby must match that part currently)
                var indexFirstProperties = collection.Indices.Select(i => i.ShortProperties.First());
                var usesIndex = indexFirstProperties.Contains(shortName);
                if (!usesIndex && collection.NumberOfRows < _compiler.MaxCollectionSizeWithNoIndex)
                {
                    _compiler.ErrorManager.AddWarning("Group by (" + group.Property.Path + ") doesn't use an index. Running with caution");
                }
                else if (!usesIndex)
                {
                    Fail("Group by (" + group.Property.Path + ") doesn't use an index. Collection too large to allow");
                }
            }
        }

        private void AnalyzeHavingClause(QupidCollection collection, HavingClause having)
        {
            if (having != null)
            {
                var prop = having.Property;
                if (!prop.Collection.Equals(collection.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Fail("The 'having' property (" + prop.Path + ") is invalid.");
                    return;
                }

                having.AnalyzedValue = having.LiteralValue.ToString();
            }
        }

        private void AnalyzeWhereClause(QupidCollection collection, List<WhereClause> list)
        {
            if (list != null)
            {
                var indexFound = list.Count == 0; //if the list is empty - don't worry about indexes

                foreach (var where in list)
                {
                    var prop = where.Property;
                    if (!prop.Collection.Equals(collection.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        Fail("The 'where' property (" + prop.Path + ") is invalid.");
                        return;
                    }

                    var shortName = collection.ConvertToShortPath(prop.Path);
                    if (shortName == null)
                    {
                        _compiler.ErrorManager.AddError("The 'where' property (" + prop.Path + ") is invalid.");
                        return;
                    }
                    prop.AnalyzedName = shortName;

                    var dbProp = collection.GetProperty(prop.Path);
                    if (dbProp.Type == "DateTime")
                    {
                        where.AnalyzedValue = "new Date(" + where.LiteralValue + ")";
                    }
                    else if (dbProp.Type == "Boolean")
                    {
                        where.AnalyzedValue = (where.LiteralValue.ToString().Equals("1")) ? "true" : "false";
                    }
                    else if (dbProp.Type == "BsonObjectId")
                    {
                        where.AnalyzedValue = "ObjectId(" + where.LiteralValue + ")";
                    }
                    else
                    {
                        where.AnalyzedValue = where.LiteralValue.ToString();
                    }

                    // index checking
                    var usesIndex = collection.Indices.Select(i => i.ShortProperties.First()).Contains(shortName);
                    if (usesIndex)
                    {
                        indexFound = true;
                    }
                }

                //find the where clause(s) that don't use an index
                if (!indexFound)
                {
                    var smallCollection = collection.NumberOfRows < _compiler.MaxCollectionSizeWithNoIndex;
                    foreach (var where in list)
                    {
                        var shortName = collection.ConvertToShortPath(where.Property.Path);
                        if (shortName == null)
                        {
                            Fail("The 'where' property (" + where.Property.Path + ") is invalid.");
                            return;
                        }
                        var usesIndex = collection.Indices.Select(i => i.ShortProperties.First()).Contains(shortName);

                        if (!usesIndex && smallCollection)
                        {
                            _compiler.ErrorManager.AddWarning("Where clause (" + where.Property.Path + ") doesn't use an index. Proceed with caution");
                        }
                        else if (!usesIndex)
                        {
                            _compiler.ErrorManager.AddError("Where clause (" + where.Property.Path +
                                                            ") doesn't use an index. Collection too large to run un-indexed");
                        }
                    }
                }
            }
        }

        private void Fail(string message)
        {
            _compiler.ErrorManager.AddError(message, 0, 0);
        }
    }
}
