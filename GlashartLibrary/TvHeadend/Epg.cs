using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GlashartLibrary.TvHeadend
{
    public class Epg : TvhFile
    {
        /*TvHeadend properties*/
        public string name { get; set; }
        public string icon { get; set; }
        public List<string> channels { get; set; }

        /*Tvheadend extra properties*/
        [JsonExtensionData]
        public IDictionary<string, JToken> _additionalData;

        public Epg()
        {
            
        }
    }
}