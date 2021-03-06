﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlashartLibrary.TvHeadend.Web;
using log4net;

namespace GlashartLibrary.TvHeadend
{
    public class Epg : TvhObject
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Epg));

        /*TvHeadend properties*/
        public string name { get; set; }
        public List<string> channels { get; set; }

        public Epg()
        {
            name = string.Empty;
            channels = new List<string>();
        }

        public void AddChannel(Channel channel)
        {
            if (channel == null) return;
            if (channels.Contains(channel.uuid)) return;

            channels.Add(channel.uuid);
        }

        public void RemoveChannel(Channel channel)
        {
            if (channel == null) return;
            channels.Remove(channel.uuid);
        }

        public static List<Epg> ReadFromDisk(string tvhFolder)
        {
            var folder = GetFolder(tvhFolder);
            Logger.DebugFormat("Read epg's from {0}", folder);
            if (!Directory.Exists(folder))
            {
                Logger.WarnFormat("Epg directory ({0}) doesn't exist", folder);
                return new List<Epg>();
            }

            return Directory.EnumerateFiles(folder)
                            .Select(LoadFromFile<Epg>)
                            .ToList();
        }

        public void SaveToDisk(string tvhFolder)
        {
            var folder = GetFolder(tvhFolder);
            if (!Directory.Exists(folder))
            {
                Logger.DebugFormat("Folder doesn't exist, create {0}", folder);
                Directory.CreateDirectory(folder);
            }

            var file = Path.Combine(folder, uuid);
            SaveToFile(file);
        }

        private static string GetFolder(string tvhFolder)
        {
            return Path.Combine(tvhFolder, "epggrab", "xmltv", "channels");
        }

        public override Urls Urls
        {
            get
            {
                return new Urls
                {
                    Create = string.Empty,
                    List = "/api/epggrab/channel/list",
                };
            }
        }
    }
}