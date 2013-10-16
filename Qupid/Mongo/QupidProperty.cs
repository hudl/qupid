using System.Collections.Generic;

namespace Qupid.Mongo
{
    public class QupidProperty
    {
        public string ShortName { get; set; }
        public string LongName { get; set; }
        public string Type { get; set; }
        public bool IsList { get; set; }
        public bool IsNullable { get; set; }
        public bool IsEnum { get; set; }
        public List<string> EnumValues { get; set; } 
        public bool HasSubProperties { get; set; }
        public List<QupidProperty> Properties { get; set; }
        public Dictionary<string, string> ShortToLongProperties { get; set; }
        public Dictionary<string, string> LongToShortProperties { get; set; }
    }
}