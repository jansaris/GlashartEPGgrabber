﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GlashartLibrary.TvHeadend
{
    public class Service : TvhFile
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Service));

        [JsonIgnore]
        public string Id { get; set; }

        /*TvHeadend properties*/
        public int? sid { get; set; }
        public string svcname { get; set; }
        public int? dvb_servicetype { get; set; }
        public int? created { get; set; }
        public int? last_seen { get; set; }
        public bool? enabled { get; set; }

        /*Tvheadend extra properties*/

        [JsonExtensionData]
        public IDictionary<string, JToken> _additionalData;

        public Service()
        {
            Id = Guid.NewGuid().ToString();
            sid = 1;
            dvb_servicetype = 1;
            enabled = true;
            created = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            last_seen = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            State = State.New;
        }

        public static Service ReadFromDisk(string file)
        {
            Logger.DebugFormat("Read service from {0}", file);
            if (!File.Exists(file))
            {
                Logger.WarnFormat("Service file ({0}) doesn't exist", file);
                return null;
            }

            try
            {
                var service = LoadFromFile<Service>(file);
                service.Id = file.Split(Path.DirectorySeparatorChar).Last();
                return service;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to read and parse service data from {0}", file);
                return null;
            }
        }

        public void SaveToDisk(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Logger.DebugFormat("Folder doesn't exist, create {0}", folder);
                Directory.CreateDirectory(folder);
            }

            var file = Path.Combine(folder, Id);

            var json = TvhJsonConvert.Serialize(this);
            Logger.DebugFormat("Generated json: {0} for {1}", json, file);

            State = File.Exists(file) ? State.Updated : State.Created;
            File.WriteAllText(file, json);
            Logger.DebugFormat("Written json to file {0} ({1})", file, State);
        }

        
    }
}