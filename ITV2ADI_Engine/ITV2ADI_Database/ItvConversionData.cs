using System;
using System.Collections.Generic;

namespace ITV2ADI_Engine.ITV2ADI_Database
{
    public partial class ItvConversionData
    {
        public int Id { get; set; }
        public string Paid { get; set; }
        public string Title { get; set; }
        public bool? IsTvod { get; set; }
        public int? VersionMajor { get; set; }
        public bool? IsAdult { get; set; }
        public DateTime? PublicationDate { get; set; }
        public DateTime? LicenseStartDate { get; set; }
        public DateTime? LicenseEndDate { get; set; }
        public string ProviderName { get; set; }
        public string ProviderId { get; set; }
        public string OriginalItv { get; set; }
        public string OriginalAdi { get; set; }
        public string MediaFileName { get; set; }
        public string MediaFileLocation { get; set; }
        public string MediaChecksum { get; set; }
        public DateTime? ProcessedDateTime { get; set; }
        public string UpdatedItv { get; set; }
        public string UpdateAdi { get; set; }
        public string UpdatedFileName { get; set; }
        public string UpdatedFileLocation { get; set; }
        public string UpdatedMediaChecksum { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
    }
}
