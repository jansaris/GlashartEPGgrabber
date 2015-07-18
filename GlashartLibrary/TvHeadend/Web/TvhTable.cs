using System.Collections.Generic;

namespace GlashartLibrary.TvHeadend.Web
{
    public class TvhTable<T>
    {
        public List<T> entries { get; set; }
        public int total { get; set; }
    }
}