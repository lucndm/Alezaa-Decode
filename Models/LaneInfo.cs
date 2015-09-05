using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Alezza.Decode.Models
{
    [DebuggerDisplay("{Meta.Title}")]
    public class LaneInfo
    {
        public string Isbn { set; get; }
        public string Url { set; get; }

        [JsonProperty(PropertyName = "meta")]
        public MetaBook Meta { set; get; }

        [JsonProperty(PropertyName = "sections")]
        public IList<Section> Sections { set; get; }

      

    }

    [DebuggerDisplay("{Title}")]
    public class MetaBook
    {
        [JsonProperty(PropertyName = "publisher")]
        public string Publisher { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "cover")]
        public string Cover { get; set; }

        [JsonProperty(PropertyName = "hero")]
        public string Hero { get; set; }

        [JsonProperty(PropertyName = "subtitle")]
        public string Subtitle { get; set; }
    }

    [DebuggerDisplay("{Title}")]
    public class Section
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "subtitle")]
        public string Subtitle { get; set; }

        public string ShortName
        {
            get
            {
                return GetShortFileName(Name);
            }
        }

        public static string GetShortFileName(string fileName)
        {
            return (fileName.Contains(".") ? (DigestUtils.Base64ComputeMD5(fileName.Split(new char[] { '.' })[0]) + "." + fileName.Split(new char[] { '.' })[1]) : DigestUtils.Base64ComputeMD5(fileName));
        }
    }
}