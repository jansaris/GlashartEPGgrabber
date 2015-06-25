using System;
using System.Collections.Generic;
using System.Linq;

namespace GlashartLibrary.TvHeadend
{
    public class SidGenerator
    {
        private readonly List<int> _alreadyRegisteredSids = new List<int>(); 
        private readonly Random _random = new Random();

        public int CreatePrimarySid()
        {
            return GetSid(1, 999);
        }

        public int CreateSecondarySid()
        {
            return GetSid(1000, 9999);
        }

        private int GetSid(int min, int max)
        {
            var sid = 0;
            for (var i = 0; i < 1000; i++) //Try for max 1000 times
            {
                sid = _random.Next(min, max);
                if(!_alreadyRegisteredSids.Contains(sid)) break; //If the sid is not in use, use this one
            }
            _alreadyRegisteredSids.Add(sid);
            return sid;
        }

        public void RegisterExistingSids(IEnumerable<Service> services)
        {
            _alreadyRegisteredSids.AddRange(
                services
                .Where(s => s.sid.HasValue)
                .Select(s => s.sid.Value)
            );
            _alreadyRegisteredSids.Sort();
        }
    }
}