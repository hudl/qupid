using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;
using Qupid.Mongo;

namespace Qupid.Tests
{
    public class CollectionFinderTest
    {
        private class FooDocument
        {
            [BsonId]
            public BsonObjectId FooId { get; set; }

            [BsonElement("n")]
            public string Name { get; set; }

            [BsonElement("dc")]
            public string DateCreated { get; set; }
        }

        private class FooCollectionFinder : ICollectionFinder
        {
            public IEnumerable<QupidCollection> FindAllCollections()
            {
                yield return new QupidCollection(typeof (FooDocument), "foo", "foo");
            }
        }

        [Fact]
        public void FindAllCollections_CustomFinder_Success()
        {
            var allCollections = new FooCollectionFinder().FindAllCollections().ToList();
            var collection = allCollections.First();

            Assert.Equal(1, allCollections.Count);
            Assert.Equal(3, collection.Properties.Count);
            Assert.Equal("n", collection.LongToShortProperties["Name"]);
            Assert.Equal("DateCreated", collection.ShortToLongProperties["dc"]);
        }
    }
}
