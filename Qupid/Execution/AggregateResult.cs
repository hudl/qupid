using System.Collections.Generic;
using Qupid.Compile;

namespace Qupid.Execution
{
    public class AggregateResult
    {
        public List<string> Columns { get; set; }
        public List<List<string>> Rows { get; set; }
        public List<QueryError> Errors { get; set; } 

        public AggregateResult()
        {
            Columns = new List<string>();Rows = new List<List<string>>();
            Errors = new List<QueryError>();
        }
    }
}