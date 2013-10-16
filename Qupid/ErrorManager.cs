using System.Collections.Generic;
using System.Linq;
using Qupid.Compile;

namespace Qupid
{
    public class ErrorManager
    {
        private readonly List<QueryError> _errors = new List<QueryError>();

        public void Add(QueryError error)
        {
            _errors.Add(error);
        }

        public void AddError(string message, int line = -1, int character = -1)
        {
            var isDuplicateError = false;
            if (line >= 0 && character >= 0)
            {
                isDuplicateError = _errors.Any(e => e.Line == line && e.Character == character);
            }
            if (!isDuplicateError)
            {
                Add(new QueryError
                    {
                        Message = message,
                        Line = line,
                        Character = character,
                        Severity = Severity.Error
                    });
            }
        }

        public void AddWarning(string message, int line = -1, int character = -1)
        {
            Add(new QueryError
                    {
                        Message = message,
                        Line = line,
                        Character = character,
                        Severity = Severity.Warning
                    });
        }

        /// <summary>
        /// Returns true if there are any issues with Error severity
        /// </summary>
        /// <returns></returns>
        public bool CanExecute()
        {
            return _errors.Count == 0 || _errors.All(e => e.Severity != Severity.Error);
        }

        public bool HasAnyIssues()
        {
            return _errors.Count > 0;
        }

        public IEnumerable<QueryError> GetErrors()
        {
            return _errors.ToList();
        }
    }
}
