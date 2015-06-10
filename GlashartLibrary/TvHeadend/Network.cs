﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using Newtonsoft.Json;

namespace GlashartLibrary.TvHeadend
{
    public class Network : TvhFile
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
            var folder = Path.Combine(GetFolder(tvhFolder), Id);
            if (!Directory.Exists(folder))
            {
                Logger.DebugFormat("Folder doesn't exist, create {0}", folder);
                Directory.CreateDirectory(folder);
            }

            SaveToFile(GetFileName(folder), this);
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
            Logger.InfoFormat("Read network from {0}", folder);
            var config = GetFileName(folder);
            if (!File.Exists(config))
            {
                Logger.WarnFormat("Network config file ({0}) doesn't exist", config);
                return null;
            }

            var network = LoadFromFile<Network>(config, folder.Split(Path.DirectorySeparatorChar).Last());
            if(network != null) ReadMuxes(network, GetMuxFolder(folder));
            return network;
        }

        private static void ReadMuxes(Network network, string networkFolder)
        {
            Logger.DebugFormat("Read muxes for network {0} ({1}) from disk", network.Id, network.networkname);
            network.Muxes.AddRange(
                Directory.EnumerateDirectories(networkFolder)
                         .Select(Mux.ReadFromDisk)
                         .Where(mux => mux != null)
            );
        }

        private static string GetFolder(string tvhFolder)
        {
            return Path.Combine(tvhFolder, "input", "iptv", "networks");
        }

        private static string GetFileName(string networkFolder)
        {
            return Path.Combine(networkFolder, "config");
        }

        private static string GetMuxFolder(string folder)
        {
            return Path.Combine(folder, "muxes");
        }
    }
}
