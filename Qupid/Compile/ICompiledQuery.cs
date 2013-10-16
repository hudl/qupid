using System.Collections.Generic;

namespace Qupid.Compile
{
    public interface ICompiledQuery
    {
        string GetMongoQuery();

        bool HasErrors();
        bool HasWarnings();

        IEnumerable<QueryError> GetErrors();
    }
}
