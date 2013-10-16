using System.Text;

namespace Qupid.AST
{
    public abstract class ComparatorClause
    {
        public PropertyReference Property { get; private set; }
        public Comparison Comparison { get; private set; }
        public object LiteralValue { get; private set; }

        public string AnalyzedValue { get; set; }

        protected ComparatorClause(PropertyReference prop, Comparison comp, object literal)
        {
            Property = prop;
            Comparison = comp;
            LiteralValue = literal;
        }

        protected abstract string GetComparator();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{'");
            sb.Append(GetComparator());
            sb.Append("':");
            
            switch (Comparison)
            {
                case Comparison.Equals:
                    sb.Append(AnalyzedValue);
                    break;

                case Comparison.NotEquals:
                    AppendOperator(sb, "$ne");
                    break;

                case Comparison.LessThan:
                    AppendOperator(sb, "$lt");
                    break;

                case Comparison.LessThanEquals:
                    AppendOperator(sb, "$lte");
                    break;

                case Comparison.GreaterThan:
                    AppendOperator(sb, "$gt");
                    break;

                case Comparison.GreaterThanEquals:
                    AppendOperator(sb, "$gte");
                    break;
            }

            sb.Append("}");
            return sb.ToString();
        }

        private void AppendOperator(StringBuilder sb, string op)
        {
            sb.Append("{");
            sb.Append(op);
            sb.Append(":");
            sb.Append(AnalyzedValue);
            sb.Append("}");
        }
    }
}
