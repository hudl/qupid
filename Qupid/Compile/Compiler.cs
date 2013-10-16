
using System;
using System.Collections.Generic;
using System.Linq;
using Qupid.AutoGen;
using Qupid.Mongo;

namespace Qupid.Compile
{
    public class Compiler
    {
        private const int DefaultMaxSize = 100000;

        public List<QupidCollection> Collections { get; private set; }
        public int MaxCollectionSizeWithNoIndex { get; set; }

        private readonly ICollectionFinder _collectionFinder;
        private readonly ErrorManager _errorManager;

        public ErrorManager ErrorManager { get { return _errorManager; }}

        public Compiler(ICollectionFinder collectionFinder)
            : this(collectionFinder, new ErrorManager())
        {
        }

        public Compiler(ICollectionFinder collectionFinder, ErrorManager errorManager)
        {
            MaxCollectionSizeWithNoIndex = DefaultMaxSize;
            _collectionFinder = collectionFinder;
            _errorManager = errorManager;
        }

        public ICompiledQuery Compile(string query)
        {
            if (Collections == null)
            {
                Collections = _collectionFinder.FindAllCollections().ToList();
            }

            try
            {
                var ast = QuerySyntaxParser.ParseString(query, _errorManager);
                if (!_errorManager.CanExecute())
                {
                    return ast;
                }

                new QueryAnalyzer(this, ast).Analyze();
                return ast;
            }
            catch (Exception e)
            {
                _errorManager.AddError("Error parsing your query. " + e.Message);
            }

            return null;
        }
    }
}
