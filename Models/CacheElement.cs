using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Alezza.Decode.Models
{
    public class CacheElement
    {
        public string Key { set; get; }
        public byte[] Value { set; get; }

        public void MapValue(ref LaneInfo info)
        {
            var br = new BsonReader(new MemoryStream(Value));
            var serializer = new JsonSerializer();
            var laneInfo = serializer.Deserialize<RootObject>(br);
            info.Meta = laneInfo.Value.Meta;
            info.Sections = laneInfo.Value.Sections;
        }

        protected class RootObject
        {
            [JsonProperty(PropertyName = "value")]
            public ValueObject Value { get; set; }
        }
        protected class ValueObject
        {
            [JsonProperty(PropertyName = "meta")]
            public MetaBook Meta { set; get; }

            [JsonProperty(PropertyName = "sections")]
            public IList<Section> Sections { set; get; }
        }
    }
}
