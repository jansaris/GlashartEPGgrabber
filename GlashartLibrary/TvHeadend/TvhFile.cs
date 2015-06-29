using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GlashartLibrary.TvHeadend
{
    public abstract class TvhFile
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TvhFile));

        [JsonIgnore]
        public string Id { get; private set; }

        [JsonIgnore]
        public State State { get; private set; }

        [JsonIgnore]
        public virtual string CreateUrl { get { return string.Empty; } }
        [JsonIgnore]
        public virtual object CreateData { get { return string.Empty; } }

        [JsonIgnore] private string _originalJson;

        /*Tvheadend extra properties*/
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        protected TvhFile()
        {
            Id = Guid.NewGuid().ToString("N");
            _originalJson = string.Empty;
            State = State.New;
        }

        protected static T LoadFromFile<T>(string filename) where T : TvhFile
        {
            try
            {
                var json = File.ReadAllText(filename);
                var tvhFile = JsonConvert.DeserializeObject<T>(json);
                tvhFile._originalJson = json;
                tvhFile.Id = tvhFile.ExtractId(filename);
                tvhFile.State = State.Loaded;
                return tvhFile;
            }
            catch (Exception ex)
            {
                Logger.Error(ex,"Failed to load {0} from file {1}", typeof(T).Name, filename);
                return null;
            }
        }

        protected void SaveToFile(string filename)
        {
            try
            {
                var json = TvhJsonConvert.Serialize(this);
                Logger.DebugFormat("Generated json: {0} for {1}", json, filename);

                if (json == _originalJson)
                {
                    Logger.DebugFormat("No changes made to object, don't save to file {0}", filename);
                    return;
                }

                State = File.Exists(filename) ? State.Updated : State.Created;
                if (!PostOnUrl())
                {
                    File.WriteAllText(filename, json);
                    Logger.DebugFormat("Written json to file {0} ({1})", filename, State);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save {0} to file {1}", GetType().Name, filename);
            }
        }

        private bool PostOnUrl()
        {
            if (State != State.Created) return false;
            if (string.IsNullOrWhiteSpace(CreateUrl)) return false;
            var comm = new TvhWebCommunication();
            comm.Create(CreateUrl, CreateData);
            Logger.InfoFormat("Created {0} on tvheadend using web. Give it 5 sec to initialize", GetType().Name);
            comm.WaitUntilScanCompleted();
            return true;
        }

        protected virtual string ExtractId(string filename)
        {
            return filename.Split(Path.DirectorySeparatorChar).Last();
        }

        protected void RemoveFromFile(string file)
        {
            var fileinfo = new FileInfo(file);
            var folder = fileinfo.Directory;
            if (fileinfo.Exists)
            {
                Logger.DebugFormat("Remove file {0} for {1} {2}", file, GetType().Name, Id);
                fileinfo.Delete();
            }
            if (folder != null && folder.Exists && !folder.EnumerateFiles().Any())
            {
                Logger.DebugFormat("Remove empty folder {0} for {1} {2}", folder, GetType().Name, Id);
                folder.Delete(true);
            }
            State = State.Removed;
        }
    }
}