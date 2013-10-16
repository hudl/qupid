using System.Collections.Generic;
using Qupid.AST;
using Qupid.Execution;
using Qupid.Mongo;

namespace Qupid.Plugin
{
    /// <summary>
    /// Allows for basic joining across collections or even across separate databases. The join is supported via the 
    /// 'WITH' keyword and is purely done in-memory (so be careful about the size of your joins).
    /// 
    /// Syntax:
    /// WITH TableOrCollectionName ON MatchingIdColumn
    /// 
    /// Where TableOrCollectionName is contained in one of the provided IQupidJoinPlugin instances provided to
    /// the QueryExecutor and MatchingIdColumn references the column/property name that will line up with the
    /// _id property of the collection being queried (in the example below that would be Users._id)
    /// 
    /// Example query:
    /// SELECT Users.*, UserSettings.*
    /// FROM Users
    /// WHERE Users.LastLoggedIn > '2013-01-01'
    /// WITH UserSettings ON UserSettings.UserId
    /// </summary>
    public interface IQupidJoinPlugin
    {
        bool IsColumnSupported(string columnName);
        bool IsCollectionSupported(string collectionName);
        void VerifySelectedColumns(ErrorManager errorManager);
        void Init(QupidCollection collection, PropertyReference joinPropery, List<string> selectedColumns);

        AggregateResult RunJoin(AggregateResult inputResults);
    }
}
