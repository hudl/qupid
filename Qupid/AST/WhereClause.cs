
namespace Qupid.AST
{
    public class WhereClause : ComparatorClause
    {
        public BooleanOperand BooleanOperand { get; private set; }

        public WhereClause(BooleanOperand boolOp, PropertyReference prop, Comparison comp, object literal)
            : base(prop, comp, literal)
        {
            BooleanOperand = boolOp;
        }

        public WhereClause(PropertyReference prop, Comparison comp, object literal)
            : base(prop, comp, literal)
        {
        }

        protected override string GetComparator()
        {
            return Property.AnalyzedName;
        }
    }
}
