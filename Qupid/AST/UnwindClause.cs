using System.Text;

namespace Qupid.AST
{
    public class UnwindClause
    {
        public readonly PropertyReference Property;

        public UnwindClause(PropertyReference prop)
        {
            Property = prop;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{$unwind : '$");
            sb.Append(Property.AnalyzedName);
            sb.Append("'}");

            return sb.ToString();
        }
    }
}
