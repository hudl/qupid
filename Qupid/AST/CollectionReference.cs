

namespace Qupid.AST
{
    public class CollectionReference
    {
        public string Name { get; private set; }

        public CollectionReference(string name)
        {
            Name = name;
        }
    }
}
