using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GlashartLibrary.TvHeadend
{
    public class Stream
    {
        public int pid { get; set; }
        public string type { get; set; }
        public int position { get; set; }

        /*Tvheadend extra properties*/
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public static Stream CreateVerimatrixStream()
        {
            var stream = new Stream
            {
                pid = 102,
                type = "CA",
                position = 262144,
                _additionalData = new Dictionary<string, JToken> {{"caidlist", JArray.Parse("[{ \"caid\": 22017 }]")}}
            };
            return stream;
        }

        public bool IsVerimatrixStream()
        {
            if (pid != 102) return false;
            if (type != "CA") return false;
            if (position != 262144) return false;
            if (!_additionalData.ContainsKey("caidlist")) return false;
            return true;
        }
    }
}