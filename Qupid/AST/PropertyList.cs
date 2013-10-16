
using System.Collections.Generic;

namespace Qupid.AST
{
    public class PropertyList
    {
        public List<PropertyReference> Properties { get; private set; }

        public PropertyList()
        {
            Properties = new List<PropertyReference>();
        }

        public void Add(PropertyReference reference)
        {
            Properties.Add(reference);
        }
    }
}
