
namespace Qupid.AST
{
    public class HavingClause : ComparatorClause
    {
        public HavingClause(PropertyReference prop, Comparison comp, object literal)
            : base(prop, comp, literal)
        {
        }

        protected override string GetComparator()
        {
            return Property.Alias;
        }
    }
}
