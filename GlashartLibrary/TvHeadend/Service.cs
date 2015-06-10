using System;
using System.IO;
using System.Linq;
using log4net;

namespace GlashartLibrary.TvHeadend
{
    public class Service : TvhFile
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Service));

        /*TvHeadend properties*/
        public int? sid { get; set; }
        public string svcname { get; set; }
        public int? dvb_servicetype { get; set; }
        public int? created { get; set; }
        public int? last_seen { get; set; }
        public bool? enabled { get; set; }

        public Service()
        {
            sid = 1;
            dvb_servicetype = 1;
            enabled = true;
            created = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            last_seen = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static Service ReadFromDisk(string file)
        {
            Logger.DebugFormat("Read service from {0}", file);
            if (!File.Exists(file))
            {
                Logger.WarnFormat("Service file ({0}) doesn't exist", file);
                return null;
            }

            return LoadFromFile<Service>(file);
        }

        public void SaveToDisk(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Logger.DebugFormat("Folder doesn't exist, create {0}", folder);
                Directory.CreateDirectory(folder);
            }

            var file = Path.Combine(folder, Id);
            SaveToFile(file);
        }        
    }
}