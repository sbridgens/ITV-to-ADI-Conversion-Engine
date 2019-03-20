using System;
using System.Collections.Generic;
using System.Linq;
using SCH_ITV;
using SCH_IO;
using System.IO;
using SCH_CONFIG;
using ITV2ADI_Engine.ITV2ADI_Database;
using SCH_ADI;

namespace ITV2ADI_Engine.ITV2ADI_Workers
{
    public partial class MapITVtoADI
    {
        ITVConversionContext db;

        ITVConversionFunctions iTVConversion;

        private bool RejectIngest { get; set; }

        private bool IsUpdate { get; set; }

        private int ItvData_RowId { get; set; }

        private string ProgramTitle { get; set; }

        private string ProductId { get; set; }

        private string AssetId { get; set; }

        private string AssetType { get; set; }

        private DateTime LicenseStart { get; set; }

        private DateTime LicenseEnd { get; set; }

        private string ProviderName { get; set; }

        private string ProviderId { get; set; }

        private string MediaFileName { get; set; }

        private string MediaLocation { get; set; }

        private string MediaChecksum { get; set; }

        private DateTime Publication_Date { get; set; }

        private int? Version_Major { get; set; }

        private bool IsTVOD { get; set; }

        private bool SetAmsSections()
        {
            try
            {
                _Mapping.SetAMSClass();
                _Mapping.SetAMSPAID(ITV_Parser.Padding(_Parser.ITV_PAID), _Parser.ITV_PAID, true);
                _Mapping.SetAMSAssetName(ProgramTitle);
                _Mapping.SetAMSCreationDate(_Parser.GET_ITV_VALUE("Publication_Date"));
                _Mapping.SetAMSDescription(ProgramTitle);
                _Mapping.SetAMSProvider(ProviderName);
                _Mapping.SetAMSProviderId(ProviderId);
                _Mapping.SetAmsVersions(Convert.ToString(Version_Major));
                return true;
            }
            catch (Exception AMS_EX)
            {
                log.Error($"Failed to Map AMS Section - {AMS_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {AMS_EX.StackTrace}");

                return false;
            }
        }
 
        private bool CreateWorkingDirectory()
        {
            log.Info($"Creating Temp working Directory: {WorkingDirectory}");
            FileDirectoryOperations.CreateDirectory(WorkingDirectory);
            log.Info($"Creating Media Directory: {MediaDirectory}");
            FileDirectoryOperations.CreateDirectory(MediaDirectory);

            return Directory.Exists(WorkingDirectory);
        }

        private string ProcessServiceCode(string isadult)
        {
            IsTVOD = false;
            string result = _Mapping.GetPricePoint(_Parser.GET_ITV_VALUE("ServiceCode"));

            if(!string.IsNullOrEmpty(result))
            {
                IsTVOD = true;
                _Mapping.SetAmsProduct(_Mapping.GetTVODProductString(result, isadult));
            }

            return result;
        }

        private void CheckUpdate()
        {
            IsUpdate = false;
            RejectIngest = false;

            using (ITVConversionContext uDB = new ITVConversionContext())
            {
                var rowData = uDB.ItvConversionData.Where(p => p.Paid == _Parser.ITV_PAID)
                                                      .Select(pd => new
                                                      {
                                                          pd.Id,
                                                          pd.PublicationDate,
                                                          pd.VersionMajor
                                                      })
                                                      .FirstOrDefault();

                if(rowData != null)
                {

                    if(Publication_Date > rowData.PublicationDate)
                    {
                        log.Info("Package detected as an Update.");
                        ItvData_RowId = rowData.Id;
                        Version_Major = rowData.VersionMajor +1;
                        IsUpdate = true;
                        return;
                    }
                    else
                    {
                        log.Error($"Rejected: Ingest exists in the database however the publication_date is not greater than the existing ingest");
                        RejectIngest = true;
                        return;
                    }
                }
                else
                {
                    Version_Major = 1;
                }
            }
        }
        
        private void SeedItvData()
        {
            try
            {
                using (ITVConversionContext seedDb = new ITVConversionContext())
                {
                    seedDb.ItvConversionData.Add(new ItvConversionData
                    {
                        Paid = _Parser.ITV_PAID,
                        Title = ProgramTitle,
                        VersionMajor = Version_Major,
                        ProductId = ProductId,
                        ContentId = AssetId,
                        LicenseStartDate = LicenseStart,
                        LicenseEndDate = LicenseEnd,
                        ProviderName = ProviderName,
                        ProviderId = ProviderId,
                        OriginalItv = _Parser.ITV_Data.ToString(),
                        MediaFileName = MediaFileName,
                        MediaFileLocation = MediaLocation,
                        ProcessedDateTime = DateTime.Now,
                        PublicationDate = Publication_Date
                    });

                    int count = seedDb.SaveChanges();
                    ItvConversionData rowId = seedDb.ItvConversionData.Local.FirstOrDefault();
                    ItvData_RowId = rowId.Id;
                    log.Info($"{count} record(s) saved to the database with Row ID: {ItvData_RowId}");
                }
            }
            catch(Exception SID_EX)
            {
                throw new Exception($"Error encountered Seeding Data: {SID_EX.Message}");
            }
        }
        
        private void UpdateItvData()
        {
            try
            {
                using (ITVConversionContext upDb = new ITVConversionContext())
                {
                    var entity = upDb.ItvConversionData.FirstOrDefault(i => i.Id == ItvData_RowId);
                    string adi = Path.Combine(WorkingDirectory, "ADI.xml");
                    _Mapping.LoadXDocument(adi);

                    if(!IsUpdate)
                    {
                        entity.IsTvod = IsTVOD;
                        entity.IsAdult = iTVConversion.IsAdult.ToLower() == "y" ? true : false;
                        entity.OriginalAdi = _Mapping.ADI_XDOC.ToString();
                        entity.MediaFileLocation = MediaLocation;
                        entity.MediaChecksum = MediaChecksum;
                    }
                    else
                    {
                        entity.VersionMajor = Version_Major;
                        entity.UpdateAdi = _Mapping.ADI_XDOC.ToString();
                        entity.UpdatedDateTime = DateTime.Now;
                        entity.UpdatedFileLocation = MediaLocation;
                        entity.UpdatedFileName = MediaFileName;
                        entity.UpdatedItv = _Parser.ITV_Data.ToString();
                        entity.UpdatedMediaChecksum = MediaChecksum;
                        entity.PublicationDate = Publication_Date;
                    }

                    int count = upDb.SaveChanges();
                    ItvConversionData rowId = upDb.ItvConversionData.Local.FirstOrDefault();
                    log.Info($"{count} record(s) updated in the database with Row ID: {ItvData_RowId}");
                }
            }
            catch(Exception UID_EX)
            {
                throw new Exception($"Error encountered Updating DB Data: {UID_EX.Message}");
            }
        }

        private bool PackageAndDeliverAsset()
        {
            try
            {
                ZipHandler zipHandler = new ZipHandler();
                string fname = _Parser.ITV_PAID;
                string package = $"{WorkingDirectory}.zip";
                string tmpPackage = Path.Combine(ITV2ADI_CONFIG.EnrichmentDirectory, $"{fname}.tmp");
                string finalPackage = Path.Combine(ITV2ADI_CONFIG.EnrichmentDirectory, $"{fname}.zip");

                if((File.Exists(package)) || (File.Exists(tmpPackage)))
                {
                    File.Delete(package);
                    File.Delete(tmpPackage);
                }
                log.Info("Starting Packaging and Delivery operations.");
                log.Info($"Packaging Source directory: {WorkingDirectory} to Zip Archive: {package}");
                zipHandler.CompressPackage(WorkingDirectory, package);
                log.Info($"Zip Archive: {package} created Successfully.");
                log.Info($"Moving: {package} to {tmpPackage}");
                FileDirectoryOperations.MoveFile(package, tmpPackage);
                log.Info($"Successfully Moved: {package} to {tmpPackage}");
                log.Info($"Moving tmp Package: {tmpPackage} to {finalPackage}");
                FileDirectoryOperations.MoveFile(tmpPackage, finalPackage);
                log.Info($"Successfully Moved: {tmpPackage} to {finalPackage}");

                log.Info("Updating Database with final data");
                UpdateItvData();
                log.Info("Starting Packaging and Delivery operations completed Successfully.");
                return true;
            }
            catch(Exception PADA_EX)
            {
                log.Error($"Failed to Package and Deliver Asset - {PADA_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {PADA_EX.StackTrace}");

                return false;
            }
        }
        
        private bool SetProgramData()
        {
            try
            {
                iTVConversion = new ITVConversionFunctions();
                
                using (db = new ITVConversionContext())
                {
                    iTVConversion.Db = db;
                    bool B_IsFirst = true;

                    if (!IsUpdate)
                    {
                        SeedItvData();
                    }

                    foreach (var entry in db.FieldMappings.OrderBy(x => x.ItvElement))
                    {
                        var itvValue = "";

                        if (B_IsFirst)
                        {
                            //In place to get the showtype
                            var tmpVal = _Parser.GET_ITV_VALUE("ReportingClass");
                            iTVConversion.ParseReportingClass(tmpVal, true);
                            B_IsFirst = false;
                        }

                        var ValueParser = new Dictionary<string, Func<string>>()
                        {
                            { "none" , () => _Parser.GetNoneTypeValue(entry.ItvElement) },
                            { "BillingId",() =>  ITV_Parser.GetBillingId(_Parser.ITV_PAID)},
                            { "SummaryLong", () => _Mapping.ConcatTitleDataXmlValues(itvValue, _Parser.GET_ITV_VALUE("ContentGuidance")) },
                            { "Length", () => ITV_Parser.GetTimeSpan(entry.ItvElement, itvValue) },
                            { "RentalTime", () => _Parser.GetRentalTime(entry.ItvElement,itvValue, iTVConversion.IsMovie, iTVConversion.IsAdult) },
                            { "ReportingClass",() =>  iTVConversion.ParseReportingClass(itvValue, entry.IsTitleMetadata) },
                            { "ServiceCode",() => ProcessServiceCode(iTVConversion.IsAdult) },
                            { "HDContent", () => _Mapping.SetEncodingFormat(itvValue) },
                            { "CanBeSuspended",() =>  _Parser.CanBeSuspended(itvValue)},
                            { "Language", () =>  ITV_Parser.GetISO6391LanguageCode(itvValue) },
                            { "AnalogCopy", () => _Mapping.CGMSMapping(itvValue) }
                        };

                        itvValue = _Parser.GET_ITV_VALUE(entry.ItvElement);

                        if (ValueParser.ContainsKey(entry.ItvElement))
                        {
                            itvValue = ValueParser[entry.ItvElement]();
                        }

                        if (!string.IsNullOrEmpty(itvValue))
                        {
                            _Mapping.SetOrUpdateAdiValue(entry.AdiAppType,
                                                     entry.AdiElement,
                                                     itvValue,
                                                     entry.IsTitleMetadata);
                        }
                    }

                }
                return true;
            }
            catch (Exception SPD_EX)
            {
                log.Error($"Failed to Map Title MetaData - {SPD_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {SPD_EX.StackTrace}");

                return false;
            }
        }

        private bool SetAssetData()
        {
            try
            {
                log.Info("ITV Metadata conversion completed Successfully, starting media operations");

                using (ITVConversionContext mediaContext = new ITVConversionContext())
                {
                    MediaLocation = string.Empty;

                    foreach (var location in mediaContext.MediaLocations.ToList())
                    {
                        string FullAssetName = Path.Combine(location.MediaLocation, MediaFileName);

                        if(File.Exists(FullAssetName))
                        {
                            MediaLocation = location.MediaLocation;

                            string destFname = Path.Combine(MediaDirectory, MediaFileName);
                            MediaChecksum = VideoFileProperties.GetFileHash(FullAssetName);
                            string fsize = VideoFileProperties.GetFileSize(FullAssetName).ToString();

                            log.Info($"Source file Hash for {MediaFileName}: {MediaChecksum}");
                            log.Info($"Source file Size for {MediaFileName}: {fsize}");

                            _Mapping.SetContent(MediaFileName);
                            _Mapping.SetOrUpdateAdiValue("VOD", "Content_CheckSum", MediaChecksum, false);
                            _Mapping.SetOrUpdateAdiValue("VOD", "Content_FileSize", fsize, false);
                            //Change to move later on but confirm with dale
                            log.Info($"Copying Media File: {FullAssetName} to {MediaDirectory}");

                            FileDirectoryOperations.CopyFile(FullAssetName, destFname);
                            string postHash = VideoFileProperties.GetFileHash(destFname);

                            if (FileDirectoryOperations.ValidateFileTransferSuccess(MediaChecksum, postHash))
                            {
                                log.Info("Media metadata and operations completed successfully.");
                                return true;
                            }
                            else
                            {
                                throw new Exception($"File transfer of media file: {MediaFileName} failed, pre and post hashes do not match!");
                            }
                        }
                    }

                    if(IsUpdate && string.IsNullOrEmpty(MediaLocation))
                    {
                        log.Info("Update package does not have a media file, continuing with metadata package only.");
                        return true;
                    }
                }

                return false;
            }
            catch(Exception SAD_EX)
            {
                log.Error($"Failed to Map Asset Data - {SAD_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {SAD_EX.StackTrace}");

                return false;
            }
        }


        public void CleanUp()
        {
            try
            {
                FileDirectoryOperations.DeleteDirectory(WorkingDirectory);
            }
            catch (Exception CU_EX)
            {
                log.Error($"Failed During Cleanup - {CU_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {CU_EX.StackTrace}");

            }
        }
    }
}
