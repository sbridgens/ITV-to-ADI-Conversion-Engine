using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IniParser.Model;
using SCH_ADI;
using SCH_IO;
using SCH_CONFIG;
using SCH_ITV;
using ITV2ADI_Engine.ITV2ADI_Database;

namespace ITV2ADI_Engine.ITV2ADI_Workers
{
    public partial class MapITVtoADI
    {
        /// <summary>
        /// Intialize Log4net
        /// </summary>
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(MapITVtoADI));

        public string ADI_FILE { get; set; }

        public string ITV_FILE { get; set; }

        public bool B_IsSuccess { get; private set; }

        private string WorkingDirectory { get; set; }
        
        private string MediaDirectory { get; set; }

        private string WorkDirname { get; set; }
        
        private ITV_Parser _Parser;

        private ADI_Mapping _Mapping;
        
        public bool StartItvMapping()
        {
            ProcessITVFile();
            
            return true;
        }
        
        private void SetAppDataValue(string identifier, string newValue, bool isTitleMetadata = true)
        {
            if (!string.IsNullOrEmpty(newValue) && isTitleMetadata)
            {
                _Mapping.ADI_FILE.Asset.Metadata.App_Data.Where(x => x.Name == identifier).FirstOrDefault().Value = newValue;
            }
            else
            {
                _Mapping.ADI_FILE.Asset.Asset.Metadata.App_Data.Where(x => x.Name == identifier).FirstOrDefault().Value = newValue;
            }
        }
        
        private bool SaveAdiFile(string AdiFileName)
        {
            try
            {
                _Mapping.SaveAdi(Path.Combine(WorkingDirectory, AdiFileName));
                return true;
            }
            catch (Exception SAF_EX)
            {
                log.Error($"Failed to Save ADI File - {SAF_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {SAF_EX.StackTrace}");
                return false;
            }
        }

        private bool ProcessITVFile()
        {
            try
            {
                log.Info("Loading and parsing itv File");
                _Parser = new ITV_Parser();
                _Parser.ParseItvFile(ITV_FILE);
                _Parser.ProductsComponentMappingList = new Dictionary<string, string>();

                log.Info("ITV Successfully parsed");
                BuildProductLists();
                return true;
            }
            catch (Exception LIV_EX)
            {
                log.Error($"Failed to Load itv file: {ITV_FILE} - {LIV_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {LIV_EX.StackTrace}");

                return false;
            }
        }

        private void BuildProductLists()
        {
            log.Info("Building product and component lists");
            string commentText = string.Empty;
            ///matches products to asset id (component id)
            foreach (KeyData item in _Parser.ITV_Data.Sections["uid"].ToList())
            {
                Enum.TryParse(item.Value[0].ToString(), true, out ITV_Parser.ApplicationType applicationType);

                if (applicationType == ITV_Parser.ApplicationType.asset)
                {
                    //get the compent section name matching the productid
                    string sectionID = $"component_{item.KeyName}";
                    try
                    {
                        var sectionList = _Parser.ITV_Data.Sections[sectionID].ToList();
                        foreach (KeyData component in _Parser.ITV_Data.Sections[sectionID].ToList())
                        {
                            try
                            {
                                ProcessProductList(item, component);
                            }
                            catch (Exception ProcessProductEx)
                            {
                                log.Error($"Caught Exception during Process of Product ID {item.KeyName}: {ProcessProductEx.Message}\r\n");

                                if (log.IsDebugEnabled)
                                    log.Debug($"Stack Trace: {ProcessProductEx.StackTrace}");

                                log.Error($"############### Packaging FAILED For Product ID: {item.KeyName} ###############\r\n");
                                continue;
                            }
                        }

                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
        }

        private void ProcessProductList(KeyData productData, KeyData ComponentData)
        {
            if (!string.IsNullOrEmpty(ComponentData.KeyName))
            {
                string ctype = _Parser.ComponentType(ComponentData.Value);

                if (ComponentData.Value == "1")
                {
                    if (SetRequiredPackageVars(productData.KeyName, ComponentData.KeyName, ctype))
                    {
                        string programName = _Parser.GET_ITV_VALUE("BillingName");

                        log.Info($"*************** Generating Package For PAID: { _Parser.ITV_PAID}, Program name: {programName} ***************\r\n");
                        log.Info($"Matching ComponentID: {ComponentData.KeyName},{ctype}");

                        LoadAdiTemplate();

                        if (StartProcessing())
                        {
                            log.Info($"All operations completed Successfully, removing temporary Files/Directories for PAID: {_Parser.ITV_PAID}");
                            log.Info($"***************Packaging FINISHED For PAID: { _Parser.ITV_PAID}, Program name: {programName} ***************\r\n");
                        }
                        else
                        {
                            log.Error("Failed during Conversion Process, the relevant error should be logged above for the problem area, check logs for errors and rectify.");
                            log.Info($"Removing Temp working directory: {WorkingDirectory}");
                            FileDirectoryOperations.DeleteDirectory(WorkingDirectory);
                            FileDirectoryOperations.ProcessITVFailure(ITV2ADI_CONFIG.FailedDirectory, WorkDirname, ITV_FILE);

                            using (ITVConversionContext db = new ITVConversionContext())
                            {
                                var rData = db.ItvConversionData.Where(i => i.Id == ItvData_RowId).FirstOrDefault();
                                db.Remove(rData);
                                db.SaveChanges();
                            }
                            log.Error($"***************Packaging FAILED For PAID: { _Parser.ITV_PAID}, Program name: {programName} ***************\r\n");
                        }
                    }
                }
            }
        }

        private void LoadAdiTemplate()
        {
            try
            {
                _Mapping = new ADI_Mapping();
                _Mapping.DeserializeAdi(Properties.Resources.ADITemplate);
            }
            catch (Exception LAT_EX)
            {
                log.Error($"Failed to Load ADI Template - {LAT_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {LAT_EX.StackTrace}");
            }
        }

        private bool StartProcessing()
        {
            log.Info("Starting Conversion of itv data to ADI");
            CheckUpdate();

            if (!RejectIngest)
            {
                return CreateWorkingDirectory() &&
                       SetAmsSections() &&
                       SetProgramData() &&
                       SetAssetData() &&
                       SaveAdiFile("ADI.xml") &&
                       PackageAndDeliverAsset();
            }
            else
            {
                return false;
            }
        }

        private bool SetRequiredPackageVars(string item_keyname, string comp_keyname, string ctype)
        {
            //Below items required earlier for reference and processing.
            log.Info("Setting packaging variables for use during workflow.");
            //get the component ID (asset section matching product) and adds the productid and component id to a dictionary
            //in order of component name to avoid key exists errors as the product key is shared across components.
            _Parser.ProductsComponentMappingList.Add($"{comp_keyname},{ctype}", item_keyname);
            _Parser.AssetSectionID = $"metadata_{comp_keyname}";
            _Parser.SECTION_ID = $"metadata_{item_keyname}";
            _Parser.ITV_PAID = Regex.Replace(_Parser.GET_ITV_VALUE("ProviderAssetId"), "[A-Za-z ]", "");

            if(_Parser.IsMovieContentType())
            {
                WorkDirname = $"{_Parser.ITV_PAID}_{ DateTime.Now.ToString("yyyyMMdd-HHmm")}";
                WorkingDirectory = Path.Combine(ITV2ADI_CONFIG.TempWorkingDirectory, WorkDirname);
                MediaDirectory = Path.Combine(WorkingDirectory, "media");

                ProgramTitle = _Parser.GET_ITV_VALUE("Title");
                ProductId = item_keyname;
                AssetId = comp_keyname;
                LicenseStart = Convert.ToDateTime(_Parser.GET_ITV_VALUE("ActivateTime"));
                LicenseEnd = Convert.ToDateTime(_Parser.GET_ITV_VALUE("DeactivateTime"));
                ProviderName = _Parser.GET_ITV_VALUE("Provider");
                ProviderId = _Parser.GET_ITV_VALUE("ProviderId");
                Publication_Date = Convert.ToDateTime(_Parser.GET_ITV_VALUE("Publication_Date"));

                MediaFileName = _Parser.GET_ASSET_DATA("FileName");
                ActiveDate = _Parser.GET_ASSET_DATA("ActiveDate");
                DeactiveDate = _Parser.GET_ASSET_DATA("DeactiveDate");

                if ((string.IsNullOrEmpty(ActiveDate)) || string.IsNullOrEmpty(DeactiveDate))
                {
                    log.Error($"Rejected: Source ITV does not contain one of the following mandatory fields: ActiveData, DeactiveDate at asset level");
                    return false;
                }
                else
                {
                    log.Info("Variables set Successfully");

                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
