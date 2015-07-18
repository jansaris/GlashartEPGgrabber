using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlashartLibrary.TvHeadend.Web;
using log4net;
using Newtonsoft.Json;

namespace GlashartLibrary.TvHeadend
{
    public class Mux : TvhObject
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Mux));

        public override string CreateUrl
        {
            get { return "/api/mpegts/network/mux_create"; }
        }

        public override Urls Urls
        {
            get
            {
                return new Urls
                {
                    List = "/api/mpegts/mux/grid",
                    Create = "/api/mpegts/network/mux_create"
                };
            }
        }

        public override object CreateData
        {
            get
            {
                return new
                {
                    uuid = network_uuid,
                    conf = this
                };
            }
        }

        [JsonIgnore]
        public List<Service> Services { get; set; }

        /*TvHeadend properties*/
        public string network_uuid { get; set; }
        public string iptv_url { get; set; }
        public string iptv_interface { get; set; }
        public string iptv_muxname { get; set; }
        public string iptv_sname { get; set; }
        public bool? iptv_atsc { get; set; }
        public bool? iptv_respawn { get; set; }
        public bool? enabled { get; set; }
        public int? epg { get; set; }
        public int? onid { get; set; }
        public int? tsid { get; set; }
        public int? scan_result { get; set; }
        public int? scan_state { get; set; }
        public int? pmt_06_ac3 { get; set; }
        
        public Mux()
        {
            Services = new List<Service>();

            iptv_atsc = false;
            iptv_respawn = false;
            enabled = true;
            epg = 1;
            onid = 0;
            tsid = 0;
            scan_result = 0;
            pmt_06_ac3 = 0;
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

            var folder = Path.Combine(networkFolder, uuid);
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
            Logger.DebugFormat("Read services for mux {0} ({1}) from disk", mux.uuid, mux.iptv_url);
            if (!Directory.Exists(folder))
            {
                Logger.DebugFormat("The folder {0} doesn't exist, skip reading services for mux {1}", folder, mux.iptv_muxname);
                return;
            }
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
            return Services.OrderBy(s => s.sid).FirstOrDefault(s => s.svcname.Contains(name));
        }

        public void Remove(string networkFolder)
        {
            var folder = Path.Combine(networkFolder, uuid);
            Services.ForEach(s => s.Remove(GetServicesFolder(folder)));
            var file = GetFileName(folder);
            RemoveFromFile(file);
        }
    }
}