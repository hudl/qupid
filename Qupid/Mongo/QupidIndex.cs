using System.Collections.Generic;

namespace Qupid.Mongo
{
    public class QupidIndex
    {
        public string Name { get; set; }
        public List<string> ShortProperties { get; set; }
        public List<string> LongProperties { get; set; }

        public override string ToString()
        {
            return Name + " (" + string.Join(", ", LongProperties) + ")";
        }
    }
}