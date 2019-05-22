using System;
using System.Collections.Generic;

namespace ITV2ADI_Engine.ITV2ADI_Database
{
    public partial class Itvfilter
    {
        public int Id { get; set; }
        public string MatchString { get; set; }
        public bool? DeleteOnMatch { get; set; }
        public string MoveOnMatchDirectory { get; set; }
        public bool Enabled { get; set; }
    }
}
