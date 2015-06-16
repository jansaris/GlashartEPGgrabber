using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using Newtonsoft.Json;

namespace GlashartLibrary.TvHeadend
{
    public class Mux : TvhFile
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Mux));

        [JsonIgnore]
        public List<Service> Services { get; set; }

        /*TvHeadend properties*/
        public string iptv_url { get; set; }
        public string iptv_interface { get; set; }
        public bool? iptv_atsc { get; set; }
        public bool? iptv_respawn { get; set; }
        public bool? enabled { get; set; }
        public int? epg { get; set; }
        public int? scan_result { get; set; }
        
        public Mux()
        {
            Services = new List<Service>();

            iptv_atsc = false;
            iptv_respawn = false;
            enabled = true;
            scan_result = 1;
            epg = 1;
        }

        public static Mux ReadFromDisk(string folder)
        {
            Logger.DebugFormat("Read mux from {0}", folder);
            var config = GetFileName(folder);
            if (!File.Exists(config))
            {
                Logger.WarnFormat("Mux config file ({0}) doesn't exist", config);
                return null;
            }

            var mux = LoadFromFile<Mux>(config);
            if(mux != null) ReadServices(mux, GetServicesFolder(folder));
            return mux;
        }

        public void SaveToDisk(string networkFolder)
        {
            if (State == State.Removed) return;

            var folder = Path.Combine(networkFolder, Id);
            if (!Directory.Exists(folder))
            {
                Logger.DebugFormat("Folder doesn't exist, create {0}", folder);
                Directory.CreateDirectory(folder);
            }

            SaveToFile(GetFileName(folder));
            Services.ForEach(s => s.SaveToDisk(GetServicesFolder(folder)));
        }

        private static void ReadServices(Mux mux, string folder)
        {
            Logger.DebugFormat("Read services for mux {0} ({1}) from disk", mux.Id, mux.iptv_url);
            mux.Services.AddRange(
                Directory.EnumerateFiles(folder)
                         .Select(Service.ReadFromDisk)
                         .Where(service => service != null)
            );
        }

        private static string GetFileName(string folder)
        {
            return Path.Combine(folder, "config");
        }

        private static string GetServicesFolder(string folder)
        {
            return Path.Combine(folder, "services");
        }

        protected override string ExtractId(string filename)
        {
            var folder = Path.GetDirectoryName(filename);
            return folder != null
                ? folder.Split(Path.DirectorySeparatorChar).Last()
                : base.ExtractId(filename);
        }

        public Service ResolveService(string name)
        {
            return Services.First(s => s.svcname == name);
        }

        public void Remove(string networkFolder)
        {
            var folder = Path.Combine(networkFolder, Id);
            Services.ForEach(s => s.Remove(GetServicesFolder(folder)));
            var file = GetFileName(folder);
            RemoveFromFile(file);
        }
    }
}