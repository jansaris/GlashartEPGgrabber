using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GlashartLibrary.Settings;
using GlashartLibrary.TvHeadend.Web;
using log4net;

namespace GlashartLibrary.TvHeadend
{
    public enum State { New, Loaded, Created, Updated, Removed }

    public class TvhConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TvhConfiguration));
        

        private List<Network> _networks = new List<Network>();
        private List<Channel> _channels = new List<Channel>();
        private List<Tag> _tags = new List<Tag>();
        private List<Epg> _epgs = new List<Epg>(); 
        private string _tvhFolder = string.Empty;
        private string _defaultNetworkName = string.Empty;

        private Network DefaultNetwork
        {
            get
            {
                var network = _networks.FirstOrDefault(n => n.networkname.Equals(_defaultNetworkName, StringComparison.OrdinalIgnoreCase));
                return network ?? CreateNetwork(_defaultNetworkName);
            }
        }

        public static TvhConfiguration ReadFromDisk(string tvhFolder, string defaultNetworkName)
        {
            var config = new TvhConfiguration
            {
                _tvhFolder = tvhFolder,
                _defaultNetworkName = defaultNetworkName,
                _networks = Network.ReadFromDisk(tvhFolder),
                _channels = Channel.ReadFromDisk(tvhFolder),
                _tags = Tag.ReadFromDisk(tvhFolder),
                _epgs = Epg.ReadFromDisk(tvhFolder)
            };
            return config;
        }

        public static TvhConfiguration ReadFromWeb(ISettings settings)
        {
            var config = new TvhConfiguration
            {
                _defaultNetworkName = settings.TvheadendNetworkName,
                _networks = ReadFromWeb<Network>(settings.TvheadendHostAddress),
                _channels = ReadFromWeb<Channel>(settings.TvheadendHostAddress),
                _tags = ReadFromWeb<Tag>(settings.TvheadendHostAddress),
                _epgs = ReadFromWeb<Epg>(settings.TvheadendHostAddress)
                //TODO: Ik probeer de EPG tabel op te halen via het web
            };
            var muxes = ReadFromWeb<Mux>(settings.TvheadendHostAddress);
            var services = ReadFromWeb<Service>(settings.TvheadendHostAddress);
            muxes.ForEach(mux => mux.Services.AddRange(services.Where(service => service.multiplex_uuid == mux.uuid)));
            config._networks.ForEach(network => network.Muxes.AddRange(muxes.Where(mux => mux.network_uuid == network.uuid)));
            return config;
        }

        private static List<T> ReadFromWeb<T>(string hostAddress) where T : TvhObject, new()
        {
            Logger.DebugFormat("Read {0} from web at {1}", typeof(T).Name, hostAddress);
            var instance = new T();
            var web = new TvhCommunication(hostAddress);
            var result = web.GetTableResult<T>(instance.Urls.List);
            if (result == null)
            {
                Logger.WarnFormat("Received no awnser from the web interface at {0}", hostAddress);
                return new List<T>();
            }
            result.entries.ForEach(obj => obj.State = State.Loaded);
            Logger.InfoFormat("Read {0} {1} from the tvheadend web interface", result.total, typeof(T).Name);
            return result.entries;
        } 

        public void SaveToDisk()
        {
            _networks.ForEach(n => n.SaveToDisk(_tvhFolder));
            var muxes = _networks.SelectMany(n => n.Muxes).ToList();
            LogUpdates(_networks, "network(s)");
            LogUpdates(muxes, "mux(es)");
            LogUpdates(muxes.SelectMany(m => m.Services).ToList(), "service(s)");

            _channels.ForEach(n => n.SaveToDisk(_tvhFolder));
            LogUpdates(_channels, "channel(s)");

            _tags.ForEach(n => n.SaveToDisk(_tvhFolder));
            LogUpdates(_tags, "tag(s)");

            _epgs.ForEach(n => n.SaveToDisk(_tvhFolder));
            LogUpdates(_epgs, "epg(s)");
        }

        private void LogUpdates<T>(List<T> files, string name) where T : TvhObject
        {
            Logger.InfoFormat("Update {0} {1} on disk ({2} Created; {3} Updated; {4} Deleted)", 
                files.Count, name, 
                files.Count(n => n.State == State.Created), 
                files.Count(n => n.State == State.Updated), 
                files.Count(n => n.State == State.Removed));
        }
        
        public Mux ResolveMux(string name, int nrOfExtraServices)
        {
            var mux = _networks.SelectMany(n => n.Muxes).FirstOrDefault(m => m.iptv_sname == name);
            return mux ?? CreateMux(name);
        }

        public Channel ResolveChannel(string name)
        {
            var channel = _channels.FirstOrDefault(c => c.name == name);
            return channel ?? CreateChannel(name);
        }

        public Tag ResolveTag(string name)
        {
            var tag = _tags.FirstOrDefault(c => c.name == name);
            return tag ?? CreateTag(name);
        }

        public Epg FindEpg(string name)
        {
            return _epgs.FirstOrDefault(e => String.Compare(e.name, name, StringComparison.OrdinalIgnoreCase) == 0) ??
                   _epgs.FirstOrDefault(e => String.Compare(e.uuid, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private Tag CreateTag(string name)
        {
            Logger.InfoFormat("Create new TVH tag for {0}", name);
            var tag = new Tag { name = name };
            _tags.Add(tag);
            return tag;
        }

        private Channel CreateChannel(string name)
        {
            Logger.InfoFormat("Create new TVH channel for {0}", name);
            var channel = new Channel { name = name };
            _channels.Add(channel);
            return channel;
        }

        private Mux CreateMux(string name)
        {
            Logger.InfoFormat("Create new TVH mux with service for {0}", name);
            var mux = new Mux {network_uuid = DefaultNetwork.uuid};
            DefaultNetwork.Muxes.Add(mux);
            return mux;
        }

        private Network CreateNetwork(string name)
        {
            var network = new Network { networkname = name };
            _networks.Add(network);
            return network;
        }

        /// It will clean up the Tvh Configuration based on the channellist
        ///     - It will clean up all muxes and its services
        ///     - It will clean up all the channels
        ///     - It will remove the removed channels from the EPG files
        ///     - It will clean up all the tags
        public void CleanUp(List<string> channelList)
        {
            CleanUpMuxes(channelList);
            CleanUpChannels(channelList);
            CleanUpTags();
        }

        private void CleanUpMuxes(List<string> channelList)
        {
            foreach (var network in _networks)
            foreach (var mux in network.Muxes)
            {
                //If the service name of this mux is in the channellist than the mux is required
                if(channelList.Contains(mux.iptv_sname)) continue;
                network.Remove(mux, _tvhFolder);
            }
        }

        private void CleanUpChannels(List<string> channelList)
        {
            foreach (var channel in _channels)
            {
                //If the channel is in the list than it is required
                if(channelList.Contains(channel.name)) continue;
                channel.Remove(_tvhFolder);
                CleanUpEpg(channel);
            }
        }

        private void CleanUpEpg(Channel channel)
        {
            foreach (var epg in _epgs)
            {
                epg.RemoveChannel(channel);
            }
        }

        private void CleanUpTags()
        {
            foreach (var tag in _tags)
            {
                //If any active channel has this tag, than it is required
                if(_channels.Where(c => c.State != State.Removed).Any(c => c.tags.Contains(tag.uuid))) continue;
                tag.Remove(_tvhFolder);
            }
        }
    }
}
