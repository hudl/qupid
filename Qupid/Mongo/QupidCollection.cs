using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;

namespace Qupid.Mongo
{
    public class QupidCollection
    {
        public string Name { get; set; }
        public string Database { get; set; }
        public List<QupidIndex> Indices { get; set; }
        public List<QupidProperty> Properties { get; set; }
        public Dictionary<string, string> ShortToLongProperties { get; set; }
        public Dictionary<string, string> LongToShortProperties { get; set; }
        public long NumberOfRows { get; set; }
        public Type BaseDataType { get; set; }

        public QupidCollection()
        {
        }

        public QupidCollection(Type baseType, string databaseName, string collectionName)
        {
            Name = collectionName;
            Database = databaseName;
            BaseDataType = baseType;

            Properties = InspectTypeForMongoAttributes(baseType);
            ShortToLongProperties = Properties.ToDictionary(p => p.ShortName, p => p.LongName);
            LongToShortProperties = Properties.ToDictionary(p => p.LongName, p => p.ShortName);

            Indices = new List<QupidIndex>();
        }

        public QupidProperty GetProperty(string longPath)
        {
            var strBld = new StringBuilder();

            string[] pieces = longPath.Split('.');
            var curQupidProp = (QupidProperty)null;
            var propertyContext = Properties;
            var conversionDict = LongToShortProperties;
            for (int depth = 0; depth < pieces.Length; depth++)
            {
                string result;
                if (!conversionDict.TryGetValue(pieces[depth], out result)) break;
                strBld.AppendFormat("{0}.", result);
                curQupidProp = propertyContext.FirstOrDefault(p => p.LongName.ToLowerInvariant().Equals(pieces[depth].ToLowerInvariant()));
                if (curQupidProp == null) break;
                if (curQupidProp.HasSubProperties)
                {
                    propertyContext = curQupidProp.Properties;
                    conversionDict = curQupidProp.LongToShortProperties;
                }
            }

            return curQupidProp;
        }

        public IEnumerable<QupidProperty> GetAllReferencedProperties(string longPath)
        {
            var propertyContext = Properties;
            foreach (var piece in longPath.Split('.'))
            {
                if (piece == "*")
                {
                    // we have reached the star piece, return all sub-properties of the current property context
                    return propertyContext.Where(qp => !qp.HasSubProperties);
                }

                var nestedProp = FetchProperty(propertyContext, piece);
                if (nestedProp == null)
                {
                    return null;
                }
                propertyContext = nestedProp.Properties;
            }

            // we shouldn't ever reach this case
            return null;
        }

        public string ConvertToShortPath(string longPath)
        {
            var strBld = new StringBuilder();

            string[] pieces = longPath.Split('.');
            var propertyContext = Properties;
            var conversionDict = LongToShortProperties;
            for (int depth = 0; depth < pieces.Length; depth++)
            {
                string result;
                if (!conversionDict.TryGetValue(pieces[depth], out result)) break;
                strBld.AppendFormat("{0}.", result);
                var curQupidProp = propertyContext.FirstOrDefault(p => p.LongName.ToLowerInvariant().Equals(pieces[depth].ToLowerInvariant()));
                if (curQupidProp == null) break;
                if (curQupidProp.HasSubProperties)
                {
                    propertyContext = curQupidProp.Properties;
                    conversionDict = curQupidProp.LongToShortProperties;
                }
            }

            if (strBld.Length > 0)
            {
                //if we built something - remove the trailing '.'
                strBld = strBld.Remove(strBld.Length - 1, 1);
            }
            else
            {
                // otherwise return null
                return null;
            }
            return strBld.ToString();
        }

        public string ConvertToLongPath(string shortPath)
        {

            StringBuilder strBld = new StringBuilder();

            string[] pieces = shortPath.Split('.');
            var propertyContext = Properties;
            var conversionDict = ShortToLongProperties;
            for (int depth = 0; depth < pieces.Length; depth++)
            {
                string result;
                if (!conversionDict.TryGetValue(pieces[depth], out result)) break;
                strBld.AppendFormat("{0}.", result);
                var curQupidProp = propertyContext.FirstOrDefault(p => p.ShortName.ToLowerInvariant().Equals(pieces[depth].ToLowerInvariant()));
                if (curQupidProp == null) break;
                if (curQupidProp.HasSubProperties)
                {
                    propertyContext = curQupidProp.Properties;
                    conversionDict = curQupidProp.ShortToLongProperties;
                }
            }
            //if we built something - remove the trailing '.'
            if (strBld.Length > 0)
            {
                strBld = strBld.Remove(strBld.Length - 1, 1);
            }
            else
            {
                return shortPath; //else just return the input
            }
            return strBld.ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is QupidCollection))
            {
                return false;
            }

            var qc = (QupidCollection) obj;
            return qc.Database.Equals(Database, StringComparison.OrdinalIgnoreCase) && qc.Name.Equals(Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Database.GetHashCode() + Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        protected List<QupidProperty> InspectTypeForMongoAttributes(Type baseDataType)
        {
            var properties = new List<QupidProperty>();
            foreach (var prop in baseDataType.GetProperties().OrderBy(p => p.Name))
            {
                var attr = prop.GetCustomAttributes(false).FirstOrDefault(p => p.GetType() == typeof(BsonElementAttribute) || p.GetType() == typeof(BsonIdAttribute));
                if (attr == null) continue;

                var newProp = new QupidProperty
                {
                    IsList = prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>),
                    IsNullable =
                        prop.PropertyType.IsGenericType &&
                        prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>),
                    IsEnum = prop.PropertyType.IsEnum,
                    LongName = prop.Name,
                    ShortName = (attr is BsonElementAttribute)
                                    ? ((BsonElementAttribute)attr).ElementName
                                    : "_id",
                };
                Type propType;
                if (newProp.IsList || newProp.IsNullable)
                {
                    propType = prop.PropertyType.GetGenericArguments()[0];
                }
                else
                {
                    propType = prop.PropertyType;
                }
                if (newProp.IsEnum)
                {
                    newProp.EnumValues = Enum.GetNames(propType).ToList();
                }
                newProp.Type = propType.Name;
                newProp.HasSubProperties = DetectSubElements(propType);

                if (newProp.HasSubProperties)
                {
                    newProp.Properties = InspectTypeForMongoAttributes(propType);
                    newProp.ShortToLongProperties = newProp.Properties.ToDictionary(p => p.ShortName, p => p.LongName);
                    newProp.LongToShortProperties = newProp.Properties.ToDictionary(p => p.LongName, p => p.ShortName);
                }

                properties.Add(newProp);
            }

            return properties;
        }

        private QupidProperty FetchProperty(IEnumerable<QupidProperty> props, string longName)
        {
            var fetchedProperty = props.FirstOrDefault(p => p.LongName.ToLowerInvariant().Equals(longName.ToLowerInvariant()));
            return fetchedProperty;
        }

        private static bool DetectSubElements(Type propType)
        {
            return propType.GetProperties().Any(p => p.GetCustomAttributes(false).Any(p2 => p2.GetType() == typeof(BsonElementAttribute)));
        }
    }
}
