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
        
        public bool ItvFailure { get; set; }
        /// <summary>
        /// Returns True or False dependant on conversion of the itv file
        /// </summary>
        /// <returns></returns>
        public bool StartItvMapping()
        {
            try
            {
                ItvFailure = false;
                B_IsSuccess = false;
                log.Info("Loading and parsing itv File");
                ITVParser = new ITV_Parser();
                ITVParser.ParseItvFile(ITV_FILE);
                if(FilterItvFile())
                {
                    ITVParser.ProductsComponentMappingList = new Dictionary<string, string>();
                    log.Info("ITV Successfully parsed");
                    BuildProductLists();
                }
                
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
        
        private bool FilterItvFile()
        {
            try
            {
                using (ITVConversionContext conversionContext = new ITVConversionContext())
                {
                    var itvFilters = conversionContext.Itvfilter.ToList();
                    var fileLines = File.ReadAllLines(ITV_FILE);

                    foreach (Itvfilter filter in itvFilters)
                    {
                        if(filter.Enabled == true)
                        {
                            int filterLength = 0;
                            //string regexPattern = $"(?i)({filter.MatchString})".Replace("\\","\\\\");
                            string searchString = filter.MatchString.Replace("\\", "\\\\");

                            if (searchString.Contains("="))
                            {
                                var oldSearch = searchString.Length;
                                searchString = searchString.Contains(" = ")
                                             ? $"(?i)(?m)(?<!\\S)({searchString})$"
                                             : $"(?i)(?m)(?<!\\S)({searchString.Replace("=", " = ")})$";

                                filterLength = filter.MatchString.Length + (searchString.Length - oldSearch);
                            }
                            foreach(var line in fileLines)
                            {
                                Match match = Regex.Match(line, searchString);
                                if (match.Success)
                                {
                                    log.Warn($"Failing ingest for {ITV_FILE} due to matching filter string");

                                    if (filter.DeleteOnMatch == true)
                                    {
                                        log.Warn($"Delete on match is TRUE, deleting source itv file: {ITV_FILE}");

                                        File.Delete(ITV_FILE);

                                        if (!File.Exists(ITV_FILE))
                                        {
                                            log.Info($"File: {ITV_FILE} successfully deleted.");
                                        }
                                        else
                                        {
                                            log.Error($"Failed to delete source itv file: {ITV_FILE}");
                                        }
                                    }
                                    else
                                    {
                                        log.Warn($"Delete on match is FALSE, moving file {ITV_FILE} to {filter.MoveOnMatchDirectory}");
                                        string destfile = Path.Combine(filter.MoveOnMatchDirectory, Path.GetFileName(ITV_FILE));
                                        FileDirectoryOperations.MoveFile(ITV_FILE, destfile);
                                        if(File.Exists(destfile))
                                        {
                                            log.Info($"Successfully moved source itv file: {ITV_FILE} to {destfile}");
                                        }
                                        else
                                        {
                                            log.Warn($"Failed to move source itv file {ITV_FILE} to {destfile} check logs.");
                                        }
                                    }
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch(Exception FIFEX)
            {
                log.Error($"Failed Filtering ITV File: {ITV_FILE} - {FIFEX.Message}");
                if (FIFEX.InnerException != null)
                    log.Debug($"Inner exception: {FIFEX.InnerException.Message}");

                return false;
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
                if (IsUpdate)
                {
                    AdiMapping.RemoveUpdateAssetSection();
                }
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
            foreach (KeyData item in ITVParser.ITV_Data.Sections["uid"].ToList())
            {
                Enum.TryParse(item.Value[0].ToString(), true, out ITV_Parser.ApplicationType applicationType);

                if (applicationType == ITV_Parser.ApplicationType.asset)
                {
                    //get the compent section name matching the productid
                    string sectionID = $"component_{item.KeyName}";
                    var sectionList = ITVParser.ITV_Data.Sections[sectionID].ToList();

                    foreach (KeyData component in ITVParser.ITV_Data.Sections[sectionID].ToList())
                    {
                        try
                        {
                            ProcessProductList(item, component);
                        }
                        catch (Exception ProcessProductEx)
                        {
                            log.Error($"Caught Exception during Process of Product ID {item.KeyName}: {ProcessProductEx.Message}\r\n");

                            log.Error($"############### Packaging FAILED For Product ID: {item.KeyName} ###############\r\n");
                            break; 
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
                string ctype = ITVParser.ComponentType(ComponentData.Value);

                if (ComponentData.Value == "1")
                {
                    log.Info("Setting packaging variables for use during workflow.");

                    if (SetRequiredPackageVars(productData.KeyName, ComponentData.KeyName, ctype))
                    {
                        log.Info("Variables set Successfully");
                        string programName = ITVParser.GET_ITV_VALUE("Title");

                        log.Info($"*************** Generating Package For PAID: { ITVParser.ITV_PAID}, Program name: {programName} ***************\r\n");
                        log.Info($"Matching ComponentID: {ComponentData.KeyName},{ctype}");

                        LoadAdiTemplate();

                        if (StartProcessing())
                        {
                            B_IsSuccess = true;
                            CleanUp();
                            log.Info($"All operations completed Successfully, removing temporary Files/Directories for PAID: {ITVParser.ITV_PAID}");
                            log.Info($"***************Packaging FINISHED For PAID: { ITVParser.ITV_PAID}, Program name: {programName} ***************\r\n");
                        }
                        else
                        {
                            log.Error("Failed during Conversion Process, the relevant error should be logged above for the problem area, check logs for errors and rectify.");
                            FileDirectoryOperations.ProcessITVFailure(ITV2ADI_CONFIG.FailedDirectory, WorkDirname, ITV_FILE);
                            ItvFailure = true;

                            if (Directory.Exists(WorkingDirectory))
                            {
                                CleanUp();
                            }

                            if(ItvData_RowId > 0)
                            {
                                using (ITVConversionContext db = new ITVConversionContext())
                                {
                                    var rData = db.ItvConversionData.Where(i => i.Id == ItvData_RowId).FirstOrDefault();
                                    if(rData != null)
                                    {
                                        db.Remove(rData);
                                        db.SaveChanges();
                                    }
                                }
                            }
                            B_IsSuccess = false;
                            log.Error($"***************Packaging FAILED For PAID: { ITVParser.ITV_PAID}, Program name: {programName} ***************\r\n");
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
                       SetAmsData() &&
                       SetAssetData() &&
                       SetProgramData() &&
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
            ITVParser.ProductsComponentMappingList.Add($"{comp_keyname},{ctype}", item_keyname);
            ITVParser.AssetSectionID = $"metadata_{comp_keyname}";
            ITVParser.SECTION_ID = $"metadata_{item_keyname}";
            //ITVPaser.ITV_PAID = Regex.Replace(ITVPaser.GET_ITV_VALUE("ProviderAssetId"), "[A-Za-z ]", "");
            ITVParser.ITV_PAID = ITVParser.GET_ITV_VALUE("ProviderAssetId");


            ///Set the directory name for the working directory needed later during parsing as a single entity
            WorkDirname = $"{ITVParser.ITV_PAID}_{ DateTime.Now.ToString("yyyyMMdd-HHmm")}";
            ///Full working directory path
            WorkingDirectory = Path.Combine(ITV2ADI_CONFIG.TempWorkingDirectory, WorkDirname);
            ///Media directory for placement of the required video assets
            MediaDirectory = Path.Combine(WorkingDirectory, "media");
            ///Correct program title
            ProgramTitle = ITVParser.GET_ITV_VALUE("Title");
            ///Correct ITV Product ID
            ProductId = item_keyname;
            ///Correct ITV Asset ide
            AssetId = comp_keyname;
            ///License start and end date required for mapping and requires a particular format
            LicenseStart = Convert.ToDateTime(ITVParser.GET_ITV_VALUE("ActivateTime"));
            LicenseEnd = Convert.ToDateTime(ITVParser.GET_ITV_VALUE("DeactivateTime"));
            ///Provider Name and ID for use in mapping and logging
            ProviderName = ITVParser.GET_ITV_VALUE("Provider");
            ProviderId = ITVParser.GET_ITV_VALUE("ProviderId");
            ///Publication data required to determine if the package is an update or initial ingest
            Publication_Date = Convert.ToDateTime(ITVParser.GET_ITV_VALUE("Publication_Date"));
            ///Physical asset file name
            MediaFileName = ITVParser.GET_ASSET_DATA("FileName");
            ///Physical asset active date
            ActiveDate = ITVParser.GET_ASSET_DATA("ActiveDate");
            ///physical asset deactive date
            DeactiveDate = ITVParser.GET_ASSET_DATA("DeactiveDate");

            if(ITVParser.IsMovieContentType())
            {
                if ((string.IsNullOrEmpty(ActiveDate)) || (string.IsNullOrEmpty(DeactiveDate)))
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
