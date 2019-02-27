using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ITV2ADI_Engine.ITV2ADI_Serialization;
using SCH_CONFIG;
using SCH_ITV;

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
        
        private string Padding { get { return new string('0', 16 - _Parser.ITV_PAID.Length); } }

        private XmlSerializerHelper<ADI> xmlSerializer;
        
        //private readonly MappingLookup TitleAppData = new MappingLookup();

        private ADI _ADI;
        private ADIAssetMetadataApp_Data _ADIAssetMetadataApp_Data;
        private ADIAssetAssetMetadataApp_Data _ADIAssetAssetMetadataApp_Data;

        ITV_Parser _Parser;
        
        public bool StartItvMapping()
        {
            ProcessITVFile();
            
            return true;
        }

        private void LoadAdiTemplate()
        {
            try
            {
                _ADI = new ADI();

                ADI_FILE = Properties.Resources.ADITemplate;
                xmlSerializer = new XmlSerializerHelper<ADI>();

                _ADI = xmlSerializer.Read(ADI_FILE);
            }
            catch (Exception LAT_EX)
            {
                log.Error($"Failed to Load ADI Template - {LAT_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {LAT_EX.StackTrace}");
            }
        }

        private void SetAppDataValue(string identifier, string newValue, bool isTitleMetadata = true)
        {
            if (!string.IsNullOrEmpty(newValue) && isTitleMetadata)
            {
                _ADI.Asset.Metadata.App_Data.Where(x => x.Name == identifier).FirstOrDefault().Value = newValue;
            }
            else
            {
                _ADI.Asset.Asset.Metadata.App_Data.Where(x => x.Name == identifier).FirstOrDefault().Value = newValue;
            }
        }
        

        private void SaveAdiFile(string AdiFileName)
        {
            try
            {
                xmlSerializer.Save(Path.Combine(ITV2ADI_CONFIG.TempWorkingDirectory, AdiFileName), _ADI);
            }
            catch (Exception SAF_EX)
            {
                log.Error($"Failed to Save ADI File - {SAF_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {SAF_EX.StackTrace}");
            }
        }

        private bool ProcessITVFile()
        {
            try
            {
                _Parser = new ITV_Parser();
                _Parser.ParseItvFile(ITV_FILE);
                _Parser.ProductsComponentMappingList = new Dictionary<string, string>();

                BuildProductLists();
                ProcessProducts();
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
            ///matches products to asset id (component id)
            foreach (var item in _Parser.ITV_Data.Sections["uid"].ToList())
            {
                //Console.WriteLine($"KeyName: {item.KeyName}, Value: {item.Value}");
                if (item.KeyName.StartsWith("p"))
                {
                    //get the compent section name matching the productid
                    var sectionID = $"component_{item.KeyName}";
                    //get the component id for the matching section name ie component_productid
                    try
                    {
                        foreach (var component in _Parser.ITV_Data.Sections[sectionID].ToList())
                        {
                            if (!string.IsNullOrEmpty(component.KeyName))
                            {
                                //key value 1 = mpg, 4 = trailer.
                                var ctype = component.Value == "0" ? "png" 
                                                                   : (component.Value == "1" 
                                                                                      ? "mpg" 
                                                                                      : "trailer"
                                                                     );
                                //get the component ID (asset section matching product) and adds the productid and component id to a dictionary
                                //in order of component name to avoid key exists errors as the product key is shared across components.
                                _Parser.ProductsComponentMappingList.Add($"{component.KeyName},{ctype}", item.KeyName);

                                log.Info($"Product ID: {item.KeyName} has Matching ComponentID: {component.KeyName},{ctype}");

                                _Parser.SECTION_ID = $"metadata_{item.KeyName}";
                                _Parser.ITV_PAID = Regex.Replace(_Parser.GET_ITV_VALUE("ProviderAssetId"), "[A-Za-z ]", "");
                                log.Info($"Current PAID Value for Product ID: {item.KeyName}: {_Parser.ITV_PAID}");

                                LoadAdiTemplate();
                                SetAmsSections();
                                SetProgramData();
                                SaveAdiFile($"ADITest_{_Parser.ITV_PAID}.xml");
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

        private void ProcessProducts()
        {
            //foreach (var productID in _Parser.ProductsComponentMappingList)
            //{
            //    //_Parser.SECTION_ID = $"metadata_{productID.Value}";
            //    //_Parser.ITV_PAID = Regex.Replace(_Parser.GET_ITV_VALUE("ProviderAssetId"), "[A-Za-z ]", "");
            //    //log.Info($"Current PAID Value for Product ID: {productID.Value}: {_Parser.ITV_PAID}");


            //    LoadAdiTemplate();
            //    SetAmsSections();
            //    SetProgramData();
            //    SaveAdiFile($"ADITest_{_Parser.ITV_PAID}.xml");
            //}
        }
    }
}
