using System;
using System.Collections.Generic;

namespace Qupid.AST
{
    public class PropertyReference
    {
        private static Dictionary<string, PropertyReference> KnownReferences = new Dictionary<string,PropertyReference>();

        public readonly string Collection;
        public readonly string Path;
        public readonly bool IsAggregate;
        public readonly AggregateTypes AggregateType;
        public readonly int Line;
        public readonly int Character;

        public string Alias { get; set; }
        public string AnalyzedName { get; set; }

        public PropertyReference(string col, string path, int line, int charPositionInLine)
        {
            Line = line;
            Character = charPositionInLine;
            Collection = col;
            Path = path;

            IsAggregate = false;
            if (path.EndsWith("COUNT", StringComparison.Ordinal))
            {
                IsAggregate = true;
                AggregateType = AggregateTypes.Count;
                Alias = col + "_count";
            }
            else if (path.EndsWith("SUM", StringComparison.Ordinal))
            {
                IsAggregate = true;
                AggregateType = AggregateTypes.Sum;
                Alias = col + "_sum";
            }
            else if (path.EndsWith("AVG", StringComparison.Ordinal))
            {
                IsAggregate = true;
                AggregateType = AggregateTypes.Average;
                Alias = col + "_avg";
            }
        }

        public static PropertyReference GetReference(string col, string path, int line, int charPositionInLine)
        {
            var agg = col + "." + path;
            if (KnownReferences.ContainsKey(agg))
            {
                return KnownReferences[agg];
            }

            var propRef = new PropertyReference(col, path, line, charPositionInLine);
            KnownReferences.Add(agg, propRef);
            return propRef;
        }

        public override string ToString()
        {
            return Collection + "." + Path;
        }
    }
}
