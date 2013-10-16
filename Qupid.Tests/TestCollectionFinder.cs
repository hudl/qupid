using System.Collections.Generic;
using Qupid.Mongo;

namespace Qupid.Tests
{
    class TestCollectionFinder : ICollectionFinder
    {
        public IEnumerable<QupidCollection> FindAllCollections()
        {
            yield return new QupidCollection
                {
                    Database = "foo",
                    Name = "foo",
                    Properties = new List<QupidProperty>
                        {
                            new QupidProperty
                                {
                                    HasSubProperties = false,
                                    LongName = "FooId",
                                    ShortName = "_id",
                                    Type = "BsonObjectId",
                                },
                            new QupidProperty
                                {
                                    HasSubProperties = false,
                                    LongName = "Name",
                                    ShortName = "n",
                                    Type = "string",
                                },
                            new QupidProperty
                                {
                                    HasSubProperties = false,
                                    LongName = "DateCreated",
                                    ShortName = "dc",
                                    Type = "DateTime",
                                }
                        },
                    Indices = new List<QupidIndex>(),
                    LongToShortProperties = new Dictionary<string, string>
                        {
                            {"FooId", "_id"},
                            {"Name", "n"},
                            {"DateCreated", "dc"},
                        },
                    ShortToLongProperties = new Dictionary<string, string>
                        {
                            {"_id", "FooId"},
                            {"n", "Name"},
                            {"dc", "DateCreated"},
                        },
                };
        }
    }
}
