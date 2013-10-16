using System.Collections.Generic;

namespace Qupid.AST
{
    public class WithClause
    {
        public string JoinOnTable { get; internal set; }
        public PropertyReference JoinProperty { get; internal set; }
        public List<PropertyReference> SelectedColumns { get; set; }

        public WithClause(string joinTable, PropertyReference joinProperty)
        {
            JoinOnTable = joinTable;
            JoinProperty = joinProperty;
        }
    }
}
