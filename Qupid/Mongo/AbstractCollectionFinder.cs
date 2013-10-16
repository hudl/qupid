using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace Qupid.Mongo
{
    public abstract class AbstractCollectionFinder : ICollectionFinder
    {
        public abstract IEnumerable<QupidCollection> FindAllCollections();

        protected List<QupidProperty> InspectTypeForMongoAttributes(Type baseDataType)
        {
            var properties = new List<QupidProperty>();
            foreach (var prop in baseDataType.GetProperties().OrderBy(p => p.Name))
            {
                var attr = prop.GetCustomAttributes(false).FirstOrDefault(p => p.GetType() == typeof(BsonElementAttribute) || p.GetType() == typeof(BsonIdAttribute));
                if (attr == null) continue;

                var newProp = new QupidProperty
                    {
                        IsList = prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof (List<>),
                        IsNullable =
                            prop.PropertyType.IsGenericType &&
                            prop.PropertyType.GetGenericTypeDefinition() == typeof (Nullable<>),
                        IsEnum = prop.PropertyType.IsEnum,
                        LongName = prop.Name,
                        ShortName = (attr is BsonElementAttribute)
                                        ? ((BsonElementAttribute) attr).ElementName
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

        private static bool DetectSubElements(Type propType)
        {
            return propType.GetProperties().Any(p => p.GetCustomAttributes(false).Any(p2 => p2.GetType() == typeof (BsonElementAttribute)));
        }
    }
}
