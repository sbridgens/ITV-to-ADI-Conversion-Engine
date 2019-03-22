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
        /// Returns True or False dependant on conversion of the itv file
        /// </summary>
        /// <returns></returns>
        public bool StartItvMapping()
        {
            try
            {
                B_IsSuccess = false;
                log.Info("Loading and parsing itv File");
                ITVPaser = new ITV_Parser();
                ITVPaser.ParseItvFile(ITV_FILE);
                ITVPaser.ProductsComponentMappingList = new Dictionary<string, string>();

                log.Info("ITV Successfully parsed");
                BuildProductLists();
                return B_IsSuccess;
            }
            catch (Exception SIM_EX)
            {
                log.Error($"Failed to MAP ITV file: {ITV_FILE} - {SIM_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {SIM_EX.StackTrace}");

                return false;
            }
        }

        /// <summary>
        /// Sets the correct adi entry for the mapped itv data
        /// </summary>
        /// <param name="identifier">XML Attribute name typically VOD</param>
        /// <param name="newValue">Parsed ITV Value</param>
        /// <param name="isTitleMetadata">Boolean to indicate if this is title metadata or asset metadata</param>
        private void SetAppDataValue(string identifier, string newValue, bool isTitleMetadata = true)
        {
            if (!string.IsNullOrEmpty(newValue) && isTitleMetadata)
            {
                AdiMapping.ADI_FILE.Asset.Metadata.App_Data.Where(x => x.Name == identifier).FirstOrDefault().Value = newValue;
            }
            else
            {
                AdiMapping.ADI_FILE.Asset.Asset.Metadata.App_Data.Where(x => x.Name == identifier).FirstOrDefault().Value = newValue;
            }
        }
        
        /// <summary>
        /// Function to save the New adi file
        /// </summary>
        /// <param name="AdiFileName">Adi filename Typically ADI.xml</param>
        /// <returns></returns>
        private bool SaveAdiFile(string AdiFileName)
        {
            try
            {
                AdiMapping.SaveAdi(Path.Combine(WorkingDirectory, AdiFileName));
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
        
        /// <summary>
        /// Function to iterate the data sections and identify the correct products and assets
        /// then triggers the processing of the products found.
        /// </summary>
        private void BuildProductLists()
        {
            log.Info("Building product and component lists");
            string commentText = string.Empty;
            ///matches products to asset id (component id)
            foreach (KeyData item in ITVPaser.ITV_Data.Sections["uid"].ToList())
            {
                Enum.TryParse(item.Value[0].ToString(), true, out ITV_Parser.ApplicationType applicationType);

                if (applicationType == ITV_Parser.ApplicationType.asset)
                {
                    //get the compent section name matching the productid
                    string sectionID = $"component_{item.KeyName}";
                    var sectionList = ITVPaser.ITV_Data.Sections[sectionID].ToList();

                    foreach (KeyData component in ITVPaser.ITV_Data.Sections[sectionID].ToList())
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
            }
        }

        /// <summary>
        /// Main Method for processing the product entries 1 by 1
        /// this will enure the required methods are invoked for parsing and packaging
        /// plus will process failed objects.
        /// </summary>
        /// <param name="productData"></param>
        /// <param name="ComponentData"></param>
        private void ProcessProductList(KeyData productData, KeyData ComponentData)
        {
            if (!string.IsNullOrEmpty(ComponentData.KeyName))
            {
                string ctype = ITVPaser.ComponentType(ComponentData.Value);

                if (ComponentData.Value == "1")
                {
                    log.Info("Setting packaging variables for use during workflow.");

                    if (SetRequiredPackageVars(productData.KeyName, ComponentData.KeyName, ctype))
                    {
                        log.Info("Variables set Successfully");
                        string programName = ITVPaser.GET_ITV_VALUE("Title");

                        log.Info($"*************** Generating Package For PAID: { ITVPaser.ITV_PAID}, Program name: {programName} ***************\r\n");
                        log.Info($"Matching ComponentID: {ComponentData.KeyName},{ctype}");

                        LoadAdiTemplate();

                        if (StartProcessing())
                        {
                            B_IsSuccess = true;
                            log.Info($"All operations completed Successfully, removing temporary Files/Directories for PAID: {ITVPaser.ITV_PAID}");
                            log.Info($"***************Packaging FINISHED For PAID: { ITVPaser.ITV_PAID}, Program name: {programName} ***************\r\n");
                        }
                        else
                        {
                            log.Error("Failed during Conversion Process, the relevant error should be logged above for the problem area, check logs for errors and rectify.");

                            if(Directory.Exists(WorkingDirectory))
                            {
                                log.Info($"Removing Temp working directory: {WorkingDirectory}");
                                FileDirectoryOperations.DeleteDirectory(WorkingDirectory);
                                FileDirectoryOperations.ProcessITVFailure(ITV2ADI_CONFIG.FailedDirectory, WorkDirname, ITV_FILE);
                            }

                            if(ItvData_RowId > 0)
                            {
                                using (ITVConversionContext db = new ITVConversionContext())
                                {
                                    var rData = db.ItvConversionData.Where(i => i.Id == ItvData_RowId).FirstOrDefault();
                                    db.Remove(rData);
                                    db.SaveChanges();
                                }
                            }
                            B_IsSuccess = false;
                            log.Error($"***************Packaging FAILED For PAID: { ITVPaser.ITV_PAID}, Program name: {programName} ***************\r\n");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ADI template with basic data used to deserialise adi data
        /// </summary>
        private void LoadAdiTemplate()
        {
            try
            {
                AdiMapping = new ADI_Mapping();
                AdiMapping.DeserializeAdi(Properties.Resources.ADITemplate);
            }
            catch (Exception LAT_EX)
            {
                log.Error($"Failed to Load ADI Template - {LAT_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {LAT_EX.StackTrace}");
            }
        }

        /// <summary>
        /// Function called by ProcessProductList this will execute
        /// the functions in turn and return a true/false based on execution
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Function in order to setout the workflows variables in order to reduce overhead later on
        /// </summary>
        /// <param name="item_keyname"></param>
        /// <param name="comp_keyname"></param>
        /// <param name="ctype"></param>
        /// <returns></returns>
        private bool SetRequiredPackageVars(string item_keyname, string comp_keyname, string ctype)
        {
            //Below items required earlier for reference and processing.
            //get the component ID (asset section matching product) and adds the productid and component id to a dictionary
            //in order of component name to avoid key exists errors as the product key is shared across components.
            ITVPaser.ProductsComponentMappingList.Add($"{comp_keyname},{ctype}", item_keyname);
            ITVPaser.AssetSectionID = $"metadata_{comp_keyname}";
            ITVPaser.SECTION_ID = $"metadata_{item_keyname}";
            ITVPaser.ITV_PAID = Regex.Replace(ITVPaser.GET_ITV_VALUE("ProviderAssetId"), "[A-Za-z ]", "");

            if(ITVPaser.IsMovieContentType())
            {
                WorkDirname = $"{ITVPaser.ITV_PAID}_{ DateTime.Now.ToString("yyyyMMdd-HHmm")}";
                WorkingDirectory = Path.Combine(ITV2ADI_CONFIG.TempWorkingDirectory, WorkDirname);
                MediaDirectory = Path.Combine(WorkingDirectory, "media");

                ProgramTitle = ITVPaser.GET_ITV_VALUE("Title");
                ProductId = item_keyname;
                AssetId = comp_keyname;
                LicenseStart = Convert.ToDateTime(ITVPaser.GET_ITV_VALUE("ActivateTime"));
                LicenseEnd = Convert.ToDateTime(ITVPaser.GET_ITV_VALUE("DeactivateTime"));
                ProviderName = ITVPaser.GET_ITV_VALUE("Provider");
                ProviderId = ITVPaser.GET_ITV_VALUE("ProviderId");
                Publication_Date = Convert.ToDateTime(ITVPaser.GET_ITV_VALUE("Publication_Date"));

                MediaFileName = ITVPaser.GET_ASSET_DATA("FileName");
                ActiveDate = ITVPaser.GET_ASSET_DATA("ActiveDate");
                DeactiveDate = ITVPaser.GET_ASSET_DATA("DeactiveDate");

                if ((string.IsNullOrEmpty(ActiveDate)) || string.IsNullOrEmpty(DeactiveDate))
                {
                    log.Error($"Rejected: Source ITV does not contain one of the following mandatory fields: ActiveData, DeactiveDate at asset level");
                    return false;
                }
                else
                {
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
