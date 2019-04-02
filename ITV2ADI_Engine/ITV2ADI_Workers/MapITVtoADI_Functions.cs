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
        /// <summary>
        /// Updates the ADI file AMS Sections
        /// </summary>
        /// <returns></returns>
        private bool SetAmsData()
        {
            try
            {
                AdiMapping.SetAMSClass();
                AdiMapping.SetAMSPAID(ITV_Parser.Padding(ITVPaser.ITV_PAID), ITVPaser.ITV_PAID, true);
                AdiMapping.SetAMSAssetName(ProgramTitle);
                AdiMapping.SetAMSCreationDate(ITVPaser.GET_ITV_VALUE("Publication_Date"));
                AdiMapping.SetAMSDescription(ProgramTitle);
                AdiMapping.SetAMSProvider(ProviderName);
                AdiMapping.SetAMSProviderId(ProviderId);
                AdiMapping.SetAmsVersions(Convert.ToString(Version_Major));
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
 
        /// <summary>
        /// Creates the Temp working directory
        /// </summary>
        /// <returns></returns>
        private bool CreateWorkingDirectory()
        {
            log.Info($"Creating Temp working Directory: {WorkingDirectory}");
            FileDirectoryOperations.CreateDirectory(WorkingDirectory);
            log.Info($"Creating Media Directory: {MediaDirectory}");
            FileDirectoryOperations.CreateDirectory(MediaDirectory);

            return Directory.Exists(WorkingDirectory);
        }
        
        /// <summary>
        /// Function to determine if the package is an update or full ingest
        /// </summary>
        private void CheckUpdate()
        {
            IsUpdate = false;
            RejectIngest = false;

            using (ITVConversionContext uDB = new ITVConversionContext())
            {
                var rowData = uDB.ItvConversionData.Where(p => p.Paid == ITVPaser.ITV_PAID)
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
        
        /// <summary>
        /// Seeds the data early on as some parts are used for lookups / updating
        /// Failed ingest will have the row data deleted
        /// </summary>
        private void SeedItvData()
        {
            try
            {
                using (ITVConversionContext seedDb = new ITVConversionContext())
                {
                    seedDb.ItvConversionData.Add(new ItvConversionData
                    {
                        Paid = ITVPaser.ITV_PAID,
                        Title = ProgramTitle,
                        VersionMajor = Version_Major,
                        LicenseStartDate = LicenseStart,
                        LicenseEndDate = LicenseEnd,
                        ProviderName = ProviderName,
                        ProviderId = ProviderId,
                        OriginalItv = ITVPaser.ITV_Data.ToString(),
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
        
        /// <summary>
        /// Function to update the db data upon successful parsing of the package
        /// </summary>
        private void UpdateItvData()
        {
            try
            {
                using (ITVConversionContext upDb = new ITVConversionContext())
                {
                    var entity = upDb.ItvConversionData.FirstOrDefault(i => i.Id == ItvData_RowId);
                    string adi = Path.Combine(WorkingDirectory, "ADI.xml");
                    AdiMapping.LoadXDocument(adi);

                    if(!IsUpdate)
                    {
                        entity.IsTvod = IsTVOD;
                        entity.IsAdult = iTVConversion.IsAdult.ToLower() == "y" ? true : false;
                        entity.OriginalAdi = AdiMapping.ADI_XDOC.ToString();
                        entity.MediaFileLocation = MediaLocation;
                        entity.MediaChecksum = MediaChecksum;
                    }
                    else
                    {
                        entity.VersionMajor = Version_Major;
                        entity.UpdateAdi = AdiMapping.ADI_XDOC.ToString();
                        entity.UpdatedDateTime = DateTime.Now;
                        entity.UpdatedFileLocation = MediaLocation;
                        entity.UpdatedFileName = MediaFileName;
                        entity.UpdatedItv = ITVPaser.ITV_Data.ToString();
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

        /// <summary>
        /// Function that iterates the mappings table in the database and ensures the correct adi fields
        /// are set with the mapped data, also the valueparser dictionary allows func calls for fields that require
        /// further parsing outside of a one to one mapping
        /// </summary>
        /// <returns></returns>
        private bool SetProgramData()
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

                    try
                    {
                        var itvValue = "";
                        bool IsMandatoryField = entry.IsMandatoryField;

                        if (B_IsFirst)
                        {
                            //In place to get the showtype
                            var tmpVal = ITVPaser.GET_ITV_VALUE("ReportingClass");
                            iTVConversion.ParseReportingClass(tmpVal, true);
                            B_IsFirst = false;
                        }

                        var ValueParser = new Dictionary<string, Func<string>>()
                        {
                            { "none" , () => ITVPaser.GetNoneTypeValue(entry.ItvElement) },
                            { "BillingId",() =>  ITV_Parser.GetBillingId(ITVPaser.ITV_PAID)},
                            { "SummaryLong", () => AdiMapping.ConcatTitleDataXmlValues(itvValue, ITVPaser.GET_ITV_VALUE("ContentGuidance")) },
                            { "Length", () => ITV_Parser.GetTimeSpan(entry.ItvElement, itvValue) },
                            { "RentalTime", () => ITVPaser.GetRentalTime(entry.ItvElement,itvValue, iTVConversion.IsMovie, iTVConversion.IsAdult) },
                            { "ReportingClass",() =>  iTVConversion.ParseReportingClass(itvValue, entry.IsTitleMetadata) },
                            { "ServiceCode",() => AdiMapping.ProcessServiceCode(iTVConversion.IsAdult,  ITVPaser.GET_ITV_VALUE("ServiceCode")) },
                            { "HDContent", () => AdiMapping.SetEncodingFormat(itvValue) },
                            { "CanBeSuspended",() =>  ITVPaser.CanBeSuspended(itvValue)},
                            { "Language", () =>  ITV_Parser.GetISO6391LanguageCode(itvValue) },
                            { "AnalogCopy", () => AdiMapping.CGMSMapping(itvValue) }
                        };

                        itvValue = ITVPaser.GET_ITV_VALUE(entry.ItvElement);

                        if (ValueParser.ContainsKey(entry.ItvElement))
                        {
                            itvValue = ValueParser[entry.ItvElement]();
                        }

                        if (!string.IsNullOrEmpty(itvValue))
                        {
                            AdiMapping.SetOrUpdateAdiValue(entry.AdiAppType,
                                                     entry.AdiElement,
                                                     itvValue,
                                                     entry.IsTitleMetadata);
                        }
                        else if (entry.IsMandatoryField && string.IsNullOrEmpty(itvValue))
                        {
                            log.Error($"Rejected: Mandatory itv Field: {entry.ItvElement} not Found in the source ITV File.");
                            return false;
                        }
                    }
                    catch (Exception SPD_EX)
                    {
                        log.Error($"Failed while Mapping Title MetaData itv value: {entry.ItvElement} to Adi value: {entry.AdiElement} - {SPD_EX.Message}");
                        if (log.IsDebugEnabled)
                            log.Debug($"STACK TRACE: {SPD_EX.StackTrace}");

                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Function to update the asset metadata section
        /// </summary>
        /// <returns></returns>
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

                        if (File.Exists(FullAssetName))
                        {
                            //set media location used in later logging and calls
                            MediaLocation = location.MediaLocation;
                            //set the bool delete from source object
                            DeleteFromSource = location.DeleteFromSource;
                            log.Info($"Source Media found in location: {MediaLocation} and DeleteFromSource Flag is: {DeleteFromSource}");
                            //Change to move later on but confirm with dale
                            log.Info($"Copying Media File: {FullAssetName} to {MediaDirectory}");

                            //create the destination filename
                            string destFname = Path.Combine(MediaDirectory, MediaFileName);
                            //Begin the file movement and file operations
                            if (FileDirectoryOperations.CopyFile(FullAssetName, destFname))
                            {
                                log.Info($"Media file successfully copied, obtaining file hash for file: {destFname}");
                                MediaChecksum = VideoFileProperties.GetFileHash(destFname);
                                log.Info($"Source file Hash for {MediaFileName}: {MediaChecksum}");
                                string fsize = VideoFileProperties.GetFileSize(destFname).ToString();
                                log.Info($"Source file Size for {destFname}: {fsize}");

                                AdiMapping.SetContent(MediaFileName);
                                AdiMapping.SetOrUpdateAdiValue("VOD", "Content_CheckSum", MediaChecksum, false);
                                AdiMapping.SetOrUpdateAdiValue("VOD", "Content_FileSize", fsize, false);
                                log.Info("Media metadata and operations completed successfully.");
                                return true;
                            }
                            else
                            {
                                throw new Exception($"File transfer of media file: {MediaFileName} failed, pre and post hashes do not match!");
                            }
                        }
                    }

                    if (IsUpdate && string.IsNullOrEmpty(MediaLocation))
                    {
                        log.Info("Update package does not have a media file, continuing with metadata package only.");
                        return true;
                    }
                }
                log.Error($"Media file: {MediaFileName} was not found in the media locations configured, failing ingest.");
                return false;
            }
            catch (Exception SAD_EX)
            {
                log.Error($"Failed to Map Asset Data - {SAD_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {SAD_EX.StackTrace}");

                return false;
            }
        }

        /// <summary>
        /// Final function to package and deliver to the enhancement software input directory
        /// If the item is tvod the package is delivered with a prefix of tvod.
        /// </summary>
        /// <returns></returns>
        private bool PackageAndDeliverAsset()
        {
            try
            {
                using (ZipHandler ziphandler = new ZipHandler())
                {
                    string fname = IsTVOD ? $"TVOD_{WorkDirname}" : WorkDirname;
                    string package = $"{WorkingDirectory}.zip";
                    string tmpPackage = Path.Combine(ITV2ADI_CONFIG.EnrichmentDirectory, $"{fname}.tmp");
                    string finalPackage = Path.Combine(ITV2ADI_CONFIG.EnrichmentDirectory, $"{fname}.zip");

                    if ((File.Exists(package)) || (File.Exists(tmpPackage)))
                    {
                        File.Delete(package);
                        File.Delete(tmpPackage);
                    }
                    log.Info("Starting Packaging and Delivery operations.");
                    log.Info($"Packaging Source directory: {WorkingDirectory} to Zip Archive: {package}");
                    ///Compress Package
                    ziphandler.CompressPackage(WorkingDirectory, package);

                    log.Info($"Zip Archive: {package} created Successfully.");
                    log.Info($"Moving: {package} to {tmpPackage}");
                    ///Move package to to destination as a .tmp file extension to prevent enrichment picking it up
                    FileDirectoryOperations.MoveFile(package, tmpPackage);

                    log.Info($"Successfully Moved: {package} to {tmpPackage}");
                    log.Info($"Moving tmp Package: {tmpPackage} to {finalPackage}");
                    ///rename the package from .tmp to .zip
                    FileDirectoryOperations.MoveFile(tmpPackage, finalPackage);

                    log.Info($"Successfully Moved: {tmpPackage} to {finalPackage}");
                    log.Info("Updating Database with final data");
                    ///add remaining data to the database for later use
                    UpdateItvData();

                    ///if the config value DeleteFromSource = true delete the source media
                    if (DeleteFromSource)
                    {
                        FileDirectoryOperations.DeleteSourceMedia(MediaLocation, MediaFileName);
                    }

                    log.Info("Starting Packaging and Delivery operations completed Successfully.");
                    return true;
                }
            }
            catch(Exception PADA_EX)
            {
                log.Error($"Failed to Package and Deliver Asset - {PADA_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {PADA_EX.StackTrace}");

                return false;
            }
        }
        
        /// <summary>
        /// removes the working directory and tmp files.
        /// </summary>
        public void CleanUp()
        {
            try
            {
                if(WorkingDirectory != null && Directory.Exists(WorkingDirectory))
                {
                    if(FileDirectoryOperations.DeleteDirectory(WorkingDirectory))
                    {
                        log.Info($"Cleanup of Working directory: {WorkingDirectory} Successful");
                    }
                    else
                    {
                        throw new Exception($"Working directory was not removed, please remove manually");
                    }
                }
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
