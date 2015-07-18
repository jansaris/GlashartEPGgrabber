﻿using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GlashartLibrary.TvHeadend.Web
{
    public class TvhCommunication
    {
        private readonly string _hostAddress;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TvhCommunication));
        private const int ListCount = 50;

        public TvhCommunication(string hostAddress)
        {
            _hostAddress = hostAddress;
        }

        public TvhTable<T> GetTableResult<T>(string uri)
        {
            var current = 0;
            var table = GetTableResult<T>(uri, current, ListCount);
            if (table == null) return new TvhTable<T>();
            current += ListCount;
            while (current < table.total)
            {
                var newTable = GetTableResult<T>(uri, current, ListCount);
                table.entries.AddRange(newTable.entries);
                current += ListCount;
            }
            return table;
        }

        private TvhTable<T> GetTableResult<T>(string uri, int start, int count)
        {
            var query = string.Format("?start={0}&limit={1}", start, count);
            var sResult = GetData(string.Concat(uri,query));
            if (sResult == null) return default(TvhTable<T>);
            try
            {
                return JsonConvert.DeserializeObject<TvhTable<T>>(sResult,new JsonSerializerSettings{});
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Failed to convert the response into a table of {0}. Result: {1}", typeof(T).Name, sResult);
                return default(TvhTable<T>);
            }
        } 

        public T Post<T>(string uri, object data)
        {
            var sResult = PostData(uri, data);
            if (sResult == null) return default(T);
            try
            {
                return JsonConvert.DeserializeObject<T>(sResult);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Failed to convert the response {0}. Result: {1}", typeof(T).Name, sResult);
                return default(T);
            }
        }

        public void Post(string uri, object data)
        {
            PostData(uri, data);
        }

        private string PostData(string uri, object data)
        {
            try
            {
                using (var client = CreateWebClient())
                {
                    var uploadData = ConvertToQueryString(data);
                    var url = string.Concat("http://", _hostAddress, uri);
                    var result = client.UploadString(url, "POST", uploadData);
                    Logger.DebugFormat("Posted object on {0} with result {1}", url, result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("Failed to post data to tvheadend ({0}) at {1}", _hostAddress, uri);
                Logger.Error(message, ex);
                return null;
            }
        }

        private string GetData(string uri)
        {
            try
            {
                using (var client = CreateWebClient())
                {
                    var url = string.Concat("http://", _hostAddress, uri);
                    var result = client.DownloadString(url);
                    Logger.DebugFormat("Get from {0} with result {1}", url, result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("Failed to get data from tvheadend ({0}) at {1}", _hostAddress, uri);
                Logger.Error(message, ex);
                return null;
            }
        }

        private WebClient CreateWebClient()
        {
            var webClient = new WebClient();

            webClient.Headers.Add("Host", _hostAddress);
            webClient.Headers.Add("Origin", _hostAddress);
            webClient.Headers.Add("X-Requested-With", "XMLHttpRequest");
            webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.130 Safari/537.36");
            webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            webClient.Headers.Add("referer", string.Concat("http://", _hostAddress, "/extjs.html"));
            webClient.Headers.Add("Accept-Encoding", "gzip, deflate");
            webClient.Headers.Add("Accept-Language", "nl-NL,nl;q=0.8,en-US;q=0.6,en;q=0.4");
            return webClient;

            /*
                Host: _host
                Proxy-Connection: keep-alive
                Content-Length: 464
                Origin: _host
                X-Requested-With: XMLHttpRequest
                User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.130 Safari/537.36
                Content-Type: application/x-www-form-urlencoded; charset=UTF-8
                Accept: \*\/\*
                Referer: http:// _host /extjs.html
                Accept-Encoding: gzip, deflate
                Accept-Language: nl-NL,nl;q=0.8,en-US;q=0.6,en;q=0.4
             */
        }

        private static string ConvertToQueryString(object data)
        {
            var json = TvhJsonConvert.Serialize(data);

            var jObj = (JObject)JsonConvert.DeserializeObject(json);
            var query = String.Join("&",
                jObj.Children().Cast<JProperty>()
                    .Select(jp => jp.Name + "=" + HttpUtility.UrlEncode(jp.Value.ToString())));
            return query;
        }
    }
}