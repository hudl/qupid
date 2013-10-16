namespace Qupid.Compile
{
    public class QueryError
    {
        public Severity Severity { get; set; }

        public string Message { get; set; }

        public int Line { get; set; }

        public int Character { get; set; }

    }
}
