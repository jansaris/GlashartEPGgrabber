using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlashartLibrary.TvHeadend.Web;
using log4net;
using Newtonsoft.Json;

namespace GlashartLibrary.TvHeadend
{
    public class Network : TvhObject
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Network));

        [JsonIgnore]
        public List<Mux> Muxes { get; set; }

        /*TvHeadend properties*/
        public int? priority { get; set; }
        public int? spriority { get; set; }
        public int? max_streams { get; set; }
        public int? max_bandwidth { get; set; }
        public int? max_timeout { get; set; }
        public string networkname { get; set; }
        public int? nid { get; set; }
        public bool? autodiscovery { get; set; }
        public bool? skipinitscan { get; set; }
        public bool? idlescan { get; set; }
        public bool? sid_chnum { get; set; }
        public bool? ignore_chnum { get; set; }
        public int? satip_source { get; set; }
        public bool? localtime { get; set; }

        public Network()
        {
            Muxes = new List<Mux>();

            priority = 1;
            spriority = 1;
            max_streams = 0;
            max_bandwidth = 0;
            max_timeout = 15;
            nid = 0;
            autodiscovery = true;
            skipinitscan = true;
            idlescan = false;
            sid_chnum = false;
            ignore_chnum = false;
            satip_source = 0;
            localtime = false;
        }

        public void SaveToDisk(string tvhFolder)
        {
            var folder = Path.Combine(GetFolder(tvhFolder), uuid);
            if (!Directory.Exists(folder))
            {
                Logger.DebugFormat("Folder doesn't exist, create {0}", folder);
                Directory.CreateDirectory(folder);
            }

            SaveToFile(GetFileName(folder));
            Muxes.ForEach(m => m.SaveToDisk(GetMuxFolder(folder)));
        }

        public static List<Network> ReadFromDisk(string tvhFolder)
        {
            var networks = new List<Network>();
            var networksFolder = GetFolder(tvhFolder);
            if (!Directory.Exists(networksFolder))
            {
                Logger.WarnFormat("Directory {0} doesn't exist", networksFolder);
                return networks;
            }

            networks.AddRange(
                Directory.EnumerateDirectories(networksFolder)
                         .Select(ReadNetworkFromFolder)
                         .Where(network => network != null)
            );

            return networks;
        }

        private static Network ReadNetworkFromFolder(string folder)
        {
            Logger.DebugFormat("Read network from {0}", folder);
            var config = GetFileName(folder);
            if (!File.Exists(config))
            {
                Logger.WarnFormat("Network config file ({0}) doesn't exist", config);
                return null;
            }

            var network = LoadFromFile<Network>(config);
            if(network != null) ReadMuxes(network, GetMuxFolder(folder));
            return network;
        }

        private static void ReadMuxes(Network network, string muxesFolder)
        {
            Logger.DebugFormat("Read muxes for network {0} ({1}) from disk", network.uuid, network.networkname);
            if (!Directory.Exists(muxesFolder))
            {
                Logger.DebugFormat("The folder {0} doesn't exist, skip reading muxes for network {1}", muxesFolder, network.networkname);
                return;
            }
            network.Muxes.AddRange(
                Directory.EnumerateDirectories(muxesFolder)
                         .Select(Mux.ReadFromDisk)
                         .Where(mux => mux != null)
            );
            network.Muxes.ForEach(m => m.network_uuid = network.uuid);
        }

        private static string GetFolder(string tvhFolder)
        {
            return Path.Combine(tvhFolder, "input", "iptv", "networks");
        }

        private static string GetFileName(string networkFolder)
        {
            return Path.Combine(networkFolder, "config");
        }

        public override Urls Urls
        {
            get
            {
                return new Urls
                {
                    List = "/api/mpegts/network/grid",
                    Create = "/api/mpegts/network/create"
                };
            }
        }

        protected override string ExtractId(string filename)
        {
            var folder = Path.GetDirectoryName(filename);
            return folder != null
                ? folder.Split(Path.DirectorySeparatorChar).Last()
                : base.ExtractId(filename);
        }

        private static string GetMuxFolder(string folder)
        {
            return Path.Combine(folder, "muxes");
        }

        public void Remove(Mux mux, string tvhFolder)
        {
            if (mux == null)
            {
                Logger.Error("Can't remove an empty mux");
                return;
            }
            if (!Muxes.Contains(mux))
            {
                Logger.ErrorFormat("Can't mux {0} because it doesn't belong to network {1}", mux.uuid, uuid);
                return;
            }
            var networksFolder = GetFolder(tvhFolder);
            var thisNetworkFolder = Path.Combine(networksFolder, uuid);
            mux.Remove(GetMuxFolder(thisNetworkFolder));
        }
    }
}
