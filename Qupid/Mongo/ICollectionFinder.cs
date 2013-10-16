using System.Collections.Generic;

namespace Qupid.Mongo
{
    public interface ICollectionFinder
    {
        IEnumerable<QupidCollection> FindAllCollections();
    }
}
