using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qupid.Compile;
using Qupid.Mongo;

namespace Qupid.AST
{
    public class QupidQuery : ICompiledQuery
    {
        public PropertyList SelectProperties { get; set; }
        public List<WhereClause> WhereClauses { get; set; }
        public GroupByClause GroupByClause { get; set; }
        public HavingClause HavingClause { get; set; }
        public UnwindClause UnwindClause { get; set; }
        public WithClause WithClause { get; set; }
        public string CollectionName { get; set; }
        public QupidCollection Collection { get; set; }

        public ErrorManager ErrorManager { get; set; }

        public QupidQuery(PropertyList pl, string collection, List<WhereClause> where, UnwindClause unwind, 
            GroupByClause groupBy, HavingClause have, WithClause with)
        {
            CollectionName = collection;
            SelectProperties = pl;
            WhereClauses = where;
            UnwindClause = unwind;
            GroupByClause = groupBy;
            HavingClause = have;
            WithClause = with;
        }

        public string GetMongoQuery()
        {
            return ToString();
        }

        public bool HasErrors()
        {
            return ErrorManager.GetErrors().Any(e => e.Severity == Severity.Error);
        }

        public bool HasWarnings()
        {
            return ErrorManager.GetErrors().Any(e => e.Severity == Severity.Warning);
        }

        public IEnumerable<QueryError> GetErrors()
        {
            return ErrorManager.GetErrors();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.Append("  aggregate:'");
            sb.Append(Collection.Name);
            sb.AppendLine("',");
            sb.AppendLine("  pipeline: [");
            
            if (WhereClauses != null && WhereClauses.Count > 0)
            {
                // just do one for now
                sb.Append("    ");
                if (WhereClauses.Count == 1)
                {
                    // just do a simple match
                    sb.Append("{$match: ");
                    sb.Append(WhereClauses[0]);
                    sb.Append("}");
                }
                else
                {
                    // we need to and-together multiple where clauses
                    sb.Append("{$match: {$and: [ ");
                    var isFirst = true;
                    foreach (var wc in WhereClauses)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                        sb.Append(wc);
                    }
                    sb.Append(" ]}}");
                }
                sb.AppendLine(",");
            }

            if (UnwindClause != null)
            {
                sb.Append("    ");
                sb.Append(UnwindClause);
                sb.AppendLine(",");
            }

            if (GroupByClause != null)
            {
                sb.Append("    ");
                sb.Append(GroupByClause);
                sb.AppendLine(",");

                // having clauses only apply when we already have a group by clause first
                if (HavingClause != null)
                {
                    sb.Append("    ");
                    sb.Append("{$match: ");
                    sb.Append(HavingClause);
                    sb.Append("}");
                }
            }
            else
            {
                // there is no group by clause, so we need to project to only get the columns specified
                sb.Append("    ");
                sb.Append("{$project:{ ");
                var isFirst = true;
                foreach (var item in SelectProperties.Properties.Where(p => !String.IsNullOrWhiteSpace(p.AnalyzedName)))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    sb.Append("'");
                    sb.Append(item.AnalyzedName);
                    sb.Append("':1");
                }
                sb.AppendLine(" }},");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            var result = sb.ToString();
            return result;
        }
    }
}
