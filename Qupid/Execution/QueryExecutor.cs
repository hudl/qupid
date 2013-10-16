using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Qupid.AST;
using Qupid.Plugin;

namespace Qupid.Execution
{
    public class QueryExecutor
    {
        private readonly ErrorManager _errorManager;
        private readonly List<IQupidJoinPlugin> _joinPlugins;

        public QueryExecutor(ErrorManager errorManager)
            : this(errorManager, new List<IQupidJoinPlugin>())
        {
        }

        public QueryExecutor(ErrorManager errorManager, IEnumerable<IQupidJoinPlugin> plugins)
        {
            _errorManager = errorManager;
            _joinPlugins = plugins.ToList();
        }

        public AggregateResult Run(QupidQuery query, MongoDatabase db)
        {
            var aggResult = new AggregateResult();

            string queryText = null;
            try
            {
                queryText = query.GetMongoQuery();
            }
            catch (Exception e)
            {
                _errorManager.AddError(e.Message);
            }

            CommandDocument doc;
            try
            {
                doc = new CommandDocument(BsonDocument.Parse(queryText));
            }
            catch (Exception e)
            {
                _errorManager.AddError("Error parsing document. " + e.Message);
                return aggResult;
            }

            try
            {
                var result = db.RunCommand(doc);
                if (!result.Ok)
                {
                    _errorManager.AddError("Error running mongo command.");
                    return aggResult;
                }

                var resultArray = result.Response["result"].AsBsonArray;
                ParseResult(resultArray, query, aggResult);
            }
            catch (MongoCommandException mce)
            {
                _errorManager.AddError("Error running mongo command. " + mce.Message);
                return aggResult;
            }

            //if a group by - attempt to change the _id back to the group long name
            //note: this needs to be run before plugins run because if a plugin references the groupby
            //property it needs to be in full name form
            if (query.GroupByClause != null && aggResult.Columns.Count > 0)
            {
                var idCol = aggResult.Columns.FirstOrDefault(c => c.ToLowerInvariant().Equals("_id"));
                //the group by id column might already be converted by the runner if subobject
                if (idCol != null)
                {
                    var idColIdx = aggResult.Columns.IndexOf(idCol);
                    aggResult.Columns.RemoveAt(idColIdx);
                    aggResult.Columns.Insert(idColIdx, query.GroupByClause.AggregateByProperty.Path);    
                }
                
            }

            if (_joinPlugins != null)
            {
                foreach (var plugin in _joinPlugins)
                {
                    //do column error checking
                    plugin.VerifySelectedColumns(_errorManager);

                    //then run the plugin
                    aggResult = plugin.RunJoin(aggResult);
                }  
            }

            //try to convert short names into long names (it will just skip plugin columns since it can't find them)
            aggResult.Columns = aggResult.Columns.Select(c => query.Collection.ConvertToLongPath(c) ?? c).ToList();

            return aggResult;
        }

        private void ParseResult(BsonArray resultArray, QupidQuery query, AggregateResult results)
        {
            var firstDoc = resultArray.FirstOrDefault();
            if (firstDoc == null)
                return;

            var collectionSelectProperties = query.SelectProperties.Properties
                                                  .Where(p => p.Collection.Equals(query.CollectionName, StringComparison.OrdinalIgnoreCase))
                                                  .ToList();

            results.Columns = collectionSelectProperties.Select(s => !s.IsAggregate? s.Path:s.Alias).ToList();
            var shortList = collectionSelectProperties.Select(s =>  !s.IsAggregate? s.AnalyzedName: s.Alias).ToList();

            //TODO: I dont love this because it feels like a double conversion since we convert the _id back to its full name 
            //later - but it is a pretty cheap call
            if (query.GroupByClause != null)
            {
                //change the select property for the group by to _id so it will match mongo results
                shortList = shortList.Select(s => s.Equals(query.GroupByClause.AggregateByProperty.AnalyzedName) ? "_id" : s).ToList();
            }

            foreach (var curVal in resultArray)
            {
                var curDoc = curVal.AsBsonDocument;
                var row = shortList.Select(col => ExtractColumnValue(curDoc, col)).ToList();
                results.Rows.Add(row);
            }
        }

        private string ExtractColumnValue(BsonDocument curDoc, string col)
        {
            BsonValue val;
            //if not a nested property - just grab the BsonVal
            if (!col.Contains("."))
            {
                curDoc.TryGetValue(col, out val);
                if (val == null) return string.Empty;

                //if its an array - format it a little better
                if (val.IsBsonArray)
                {
                    return string.Join(",", ((BsonArray) val).Select(v => v.ToString()));
                }

                return val.ToString();
            }

            //if we are dealing with subobjects - then start the drill
            //because each layer could be an array we need to get recursive
            var curResult = new List<string>();
            DrillExtractProperty(curDoc, col, curResult);

            if (curResult.Count < 2)
            {
                return string.Join("", curResult); //just return the result - this handles 0 and 1 lengths well
            }
            //if array - format it better
            return "[" + string.Join(", ", curResult) + "]";
        }

        private void DrillExtractProperty(BsonDocument curDoc, string curColumnRemains, List<string> result)
        {
            //find the next piece
            string[] splitColumnRemains = curColumnRemains.Split('.');
            string startPiece = splitColumnRemains[0];

            BsonValue val;
            curDoc.TryGetValue(startPiece, out val);
            if (val == null) return; //if we couldn't find - unwind the stack

            if (val.IsBsonArray)
            {
                foreach (var item in val.AsBsonArray)
                {
                    //if the end of the property path - append value and unwind stack
                    if (splitColumnRemains.Length == 1)
                    {
                        result.Add(item.ToString());
                        return;
                    }
                    DrillExtractProperty(item.AsBsonDocument, string.Join(".", splitColumnRemains.Skip(1)), result);
                }
            }
            else
            {
                //if the end of the property path - append value and unwind stack
                if (splitColumnRemains.Length == 1)
                {
                    result.Add(val.ToString());
                    return;
                }
                DrillExtractProperty(val.AsBsonDocument, string.Join(".", splitColumnRemains.Skip(1)), result);
            }
        }
    }
}
