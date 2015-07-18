using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlashartLibrary.TvHeadend.Web;
using log4net;

namespace GlashartLibrary.TvHeadend
{
    public class Channel : TvhObject
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Channel));

        /*TvHeadend properties*/
        public bool enabled { get; set; }
        public string name { get; set; }
        public int number { get; set; }
        public string icon { get; set; }
        public bool epgauto { get; set; }
        public int dvr_pre_time { get; set; }
        public int dvr_pst_time { get; set; }
        public List<string> services { get; set; }
        public List<string> tags { get; set; }
        public string bouquet { get; set; }

        public Channel()
        {
            name = string.Empty;
            number = -1;
            icon = string.Empty;
            enabled = true;
            epgauto = true;
            dvr_pre_time = 0;
            dvr_pst_time = 0;
            services = new List<string>();
            tags = new List<string>();
            bouquet = string.Empty;
        }

        public static List<Channel> ReadFromDisk(string tvhFolder)
        {
            var channels = new List<Channel>();
            var channelsFolder = GetFolder(tvhFolder);
            if (!Directory.Exists(channelsFolder))
            {
                Logger.WarnFormat("Directory {0} doesn't exist", channelsFolder);
                return channels;
            }

            channels.AddRange(Directory
                    .EnumerateFiles(channelsFolder)
                    .Select(LoadFromFile<Channel>)
                    .Where(channel => channel != null)
            );

            return channels;
        }

        public void SaveToDisk(string tvhFolder)
        {
            if (State == State.Removed) return;

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
            return Path.Combine(tvhFolder, "channel", "config");
        }

        public void AddTag(Tag tvhTag)
        {
            if (!tags.Contains(tvhTag.uuid))
            {
                tags.Add(tvhTag.uuid);
            }
        }

        public void AddService(Service service)
        {
            if (!services.Contains(service.uuid))
            {
                services.Add(service.uuid);
            }
        }

        public void Remove(string tvhFolder)
        {
            RemoveFromFile(Path.Combine(GetFolder(tvhFolder), uuid));
        }

        public override Urls Urls
        {
            get
            {
                return new Urls
                {
                    List = "/api/channel/grid",
                    Create = "/api/channel/create",
                };
            }
        }
    }
}
