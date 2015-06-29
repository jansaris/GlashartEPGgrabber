using System;
using System.Collections.Generic;
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
        public string provider { get; set; }
        public int? pmt { get; set; }
        public int? pcr { get; set; }
        public List<Stream> stream { get; set; }

        public Service(int sid)
        {
            this.sid = sid;
            dvb_servicetype = 0;
            enabled = true;
            created = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            last_seen = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            stream = new List<Stream>();
        }

        /// <summary>
        /// Don't use this constructor
        /// This constructor will be used by Newtonsoft.Json
        /// </summary>
        public Service() : this(1)
        {

        }

        public void AddVerimatrixStream()
        {
            if (stream.Any(s => s.IsVerimatrixStream()))
            {
                Logger.WarnFormat("Service {0} already contains a Verimatrix stream", svcname);
                return;
            }
            stream.Add(Stream.CreateVerimatrix());
            var video = Stream.CreateH264();
            pcr = video.pid;
            stream.Add(video);
            stream.Add(Stream.CreateTeletext());
            stream.Add(Stream.CreateAc3());
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
            if (State == State.Removed) return;

            if (!Directory.Exists(folder))
            {
                Logger.DebugFormat("Folder doesn't exist, create {0}", folder);
                Directory.CreateDirectory(folder);
            }

            var file = Path.Combine(folder, Id);
            SaveToFile(file);
        }

        public void Remove(string folder)
        {
            var file = Path.Combine(folder, Id);
            RemoveFromFile(file);
        }
    }
}