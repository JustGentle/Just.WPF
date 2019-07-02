using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Just.WPF.Views.MongoDBTool
{
    [BsonIgnoreExtraElements]
    public class CacheSysProfileMode
    {
        public ObjectId Id { get; set; }
        public string Mode { get; set; }

        public string Item { get; set; }

        public string Display { get; set; }

        public string DefaultValue { get; set; }

        public string ItemValue { get; set; }

        public string Hints { get; set; }

        public int ValueType { get; set; }

        public string Category { get; set; }

        public bool Visible { get; set; }

        public EnumValues EnumValue { get; set; }
        
        public BsonDocument DynamicValue { get; set; }
    }
    public class EnumValues
    {
        public bool Multiselect { get; set; }

        public List<EnumValue> EnumValueCollection { get; set; }
    }

    public class EnumValue
    {
        public string Display { get; set; }
        public string Value { get; set; }

    }
}
