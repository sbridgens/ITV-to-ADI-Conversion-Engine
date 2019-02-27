using System;
using System.Collections.Generic;

namespace ITV2ADI_Engine
{
    public partial class FieldMappings
    {
        public int Id { get; set; }
        public string AdiAppType { get; set; }
        public string AdiElement { get; set; }
        public string ItvElement { get; set; }
        public bool IsTitleMetadata { get; set; }
    }
}
