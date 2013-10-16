using System;
using System.Text;

namespace Qupid.AST
{
    public class GroupByClause
    {
        public readonly PropertyReference Property;

        public PropertyReference AggregateByProperty { get; set; }
        public PropertyReference AggregationProperty { get; set; } 

        public GroupByClause(PropertyReference prop)
        {
            Property = prop;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{ $group: { _id:'$");
            sb.Append(AggregateByProperty.AnalyzedName);
            sb.Append("', ");
            sb.Append(AggregationProperty.Alias);
            sb.Append(": ");
            switch (AggregationProperty.AggregateType)
            {
                case AggregateTypes.Count:
                    sb.Append("{$sum:1}");
                    break;

                case AggregateTypes.Sum:
                    sb.Append("{$sum:'$");
                    sb.Append(AggregationProperty.AnalyzedName);
                    sb.Append("'}");
                    break;

                case AggregateTypes.Average:
                    sb.Append("{$avg:'$");
                    sb.Append(AggregationProperty.AnalyzedName);
                    sb.Append("'}");
                    break;

                default:
                    throw new Exception("Unsupported aggregation type: " + AggregationProperty.AggregateType);
            }
            sb.Append("} }");
            return sb.ToString();
        }
    }
}
