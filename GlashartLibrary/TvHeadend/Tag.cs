using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace GlashartLibrary.TvHeadend
{
    public class Tag : TvhFile
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Tag));

        /*TvHeadend properties*/
        public bool enabled { get; set; }
        public int index { get; set; }
        public string name { get; set; }
        public bool @internal { get; set; }
        public bool @private { get; set; }
        public string icon { get; set; }
        public bool titled_icon { get; set; }
        public string comment { get; set; }

        public Tag()
        {
            enabled = true;
            index = -1;
            name = string.Empty;
            @internal = false;
            @private = false;
            icon = string.Empty;
            titled_icon = false;
            comment = string.Empty;
        }

        public static List<Tag> ReadFromDisk(string tvhFolder)
        {
            var channels = new List<Tag>();
            var channelsFolder = GetFolder(tvhFolder);
            if (!Directory.Exists(channelsFolder))
            {
                Logger.WarnFormat("Directory {0} doesn't exist", channelsFolder);
                return channels;
            }

            channels.AddRange(
                Directory.EnumerateFiles(channelsFolder)
                         .Select(ReadChannelFromFile)
                         .Where(channel => channel != null)
            );

            return channels;
        }

        public void SaveToDisk(string tvhFolder)
        {
            var folder = GetFolder(tvhFolder);
            if (!Directory.Exists(folder))
            {
                Logger.DebugFormat("Folder doesn't exist, create {0}", folder);
                Directory.CreateDirectory(folder);
            }

            var file = Path.Combine(folder, Id);
            SaveToFile(file);
        }

        private static Tag ReadChannelFromFile(string file)
        {
            return LoadFromFile<Tag>(file);
        }

        private static string GetFolder(string tvhFolder)
        {
            return Path.Combine(tvhFolder, "channel", "tag");
        }

        public void Remove(string tvhFolder)
        {
            RemoveFromFile(Path.Combine(GetFolder(tvhFolder), Id));
        }
    }
}