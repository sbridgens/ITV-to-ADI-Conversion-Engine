using System;
using System.Collections.Generic;

namespace ITV2ADI_Engine
{
    public partial class ReportClassMapping
    {
        public int Id { get; set; }
        public string ReportingClass { get; set; }
        public string ClassIncludes { get; set; }
        public string FolderLocation { get; set; }
        public string ShowType { get; set; }
    }
}
