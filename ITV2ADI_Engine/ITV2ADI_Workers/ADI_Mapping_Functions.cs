using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ITV2ADI_Engine.ITV2ADI_Workers
{
    public partial class MapITVtoADI
    {
        ITVConversionContext db;

        private bool isShowType { get; set; }

        #region AMSData

        private void SetAMSClass()
        {
            _ADI.Metadata.AMS.Asset_Class = "package";
            _ADI.Asset.Metadata.AMS.Asset_Class = "title";
            _ADI.Asset.Asset.Metadata.AMS.Asset_Class = "movie";
        }

        private void SetAMSPAID()
        {
            _ADI.Metadata.AMS.Asset_ID = $"PAID{Padding}{_Parser.ITV_PAID}";
            _ADI.Asset.Metadata.AMS.Asset_ID = $"TITL{Padding}{_Parser.ITV_PAID}";
            _ADI.Asset.Asset.Metadata.AMS.Asset_ID = $"ASST{Padding}{_Parser.ITV_PAID}";
        }

        private void SetAMSAssetName()
        {
            string title = _Parser.GET_ITV_VALUE("Title");
            _ADI.Metadata.AMS.Asset_Name = title;
            _ADI.Asset.Metadata.AMS.Asset_Name = title;
            _ADI.Asset.Asset.Metadata.AMS.Asset_Name = title;
        }

        private void SetAMSCreationDate()
        {
            string CDate = _Parser.GET_ITV_VALUE("Publication_Date");
            _ADI.Metadata.AMS.Creation_Date = CDate;
            _ADI.Asset.Metadata.AMS.Creation_Date = CDate;
            _ADI.Asset.Asset.Metadata.AMS.Creation_Date = CDate;
        }

        private void SetAMSDescription()
        {
            string AmsDesc = _Parser.GET_ITV_VALUE("Title");
            _ADI.Metadata.AMS.Description = AmsDesc;
            _ADI.Asset.Metadata.AMS.Description = $"{AmsDesc} Title";
            _ADI.Asset.Asset.Metadata.AMS.Description = $"{AmsDesc} Content";
        }

        private void SetAMSProvider()
        {
            string provider = _Parser.GET_ITV_VALUE("Provider");
            _ADI.Metadata.AMS.Provider = provider;
            _ADI.Asset.Metadata.AMS.Provider = provider;
            _ADI.Asset.Asset.Metadata.AMS.Provider = provider;
        }

        private void SetAMSProviderId()
        {
            string providerId = _Parser.GET_ITV_VALUE("ProviderId");
            _ADI.Metadata.AMS.Provider_ID = providerId;
            _ADI.Asset.Metadata.AMS.Provider_ID = providerId;
            _ADI.Asset.Asset.Metadata.AMS.Provider_ID = providerId;
        }

        private void SetAmsVersions()
        {
            //Version major
            _ADI.Metadata.AMS.Version_Major = "1";
            _ADI.Asset.Metadata.AMS.Version_Major = "1";
            _ADI.Asset.Asset.Metadata.AMS.Version_Major = "1";

            //Version minor
            _ADI.Metadata.AMS.Version_Minor = "0";
            _ADI.Asset.Metadata.AMS.Version_Minor = "0";
            _ADI.Asset.Asset.Metadata.AMS.Version_Minor = "0";
        }

        private bool SetAmsSections()
        {
            try
            {
                SetAMSClass();
                SetAMSPAID();
                SetAMSAssetName();
                SetAMSCreationDate();
                SetAMSDescription();
                SetAMSProvider();
                SetAMSProviderId();
                SetAmsVersions();

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

        #endregion

        #region TitleData

        private void AddNewAssetMetadata_App_Data_Node(string appType, string NameValue, string ValueContents)
        {
            _ADIAssetMetadataApp_Data = new ADIAssetMetadataApp_Data
            {
                App = appType,
                Name = NameValue,
                Value = ValueContents
            };

            _ADI.Asset.Metadata.App_Data.Add(_ADIAssetMetadataApp_Data);
        }

        private void AddNewAssetAssetMetadata_App_Data_Node(string appType, string NameValue, string ValueContents)
        {
            _ADIAssetAssetMetadataApp_Data = new ADIAssetAssetMetadataApp_Data
            {
                App = appType,
                Name = NameValue,
                Value = ValueContents
            };

            _ADI.Asset.Asset.Metadata.App_Data.Add(_ADIAssetAssetMetadataApp_Data);
        }

        private void SetOrUpdateAdiValue(string appType, string attributeName, string itvValue, bool isTitleMetadata = true)
        {
            //linq returns a bool if the attribute exists in the adi if true then update the value
            //if false we fall back to the else if and check for a null, if null do nothing else
            //add the new node to the adi and set the correct values.
            if(isTitleMetadata)
            {

                if (_ADI.Asset.Metadata.App_Data.Any(x => x.Name == attributeName))
                {
                    SetAppDataValue(attributeName, itvValue, isTitleMetadata);
                }
                else if (!string.IsNullOrEmpty(itvValue))
                {
                    AddNewAssetMetadata_App_Data_Node(appType, attributeName, itvValue);
                }
            }
            else
            {
                if(_ADI.Asset.Asset.Metadata.App_Data.Any(x => x.Name == attributeName))
                {
                    SetAppDataValue(attributeName, itvValue, isTitleMetadata);
                }
                else if (!string.IsNullOrEmpty(itvValue))
                {
                    AddNewAssetAssetMetadata_App_Data_Node(appType, attributeName, itvValue);
                }
            }
        }
        
        private string ParseReportClassIncludes(string ReportingClass)
        {
            var includes = db.ReportClassMapping.Where(r => ReportingClass.ToLower().Contains(r.ReportingClass.ToLower()))
                                                   .Select(r => new
                                                   {
                                                       r.ClassIncludes,
                                                       r.ReportingClass,
                                                       r.FolderLocation,
                                                       r.ShowType
                                                   })
                                                   .FirstOrDefault();

            
            List<string> incList = includes.ClassIncludes?.Split(',').ToList();

            if(!string.IsNullOrEmpty(includes.ClassIncludes))
            {
                int count = incList.Count;

                foreach (var include in incList)
                {
                    if (ReportingClass.ToLower() == $"{includes.ReportingClass} {include}".ToLower())
                    {
                        if (isShowType)
                        {
                            return includes.ShowType;
                        }
                        else
                        {
                            return includes.FolderLocation;
                        }
                    }

                    count--;
                    //If zero then the class is a part of excludes list ie cutv asset so we can assume here 
                    //that we matched no kids based include categories and can return the location associated
                    //with cutv that has no include classes.
                    if (count == 0)
                    {
                        return db.ReportClassMapping.Where(r => ReportingClass.ToLower().Contains(r.ReportingClass.ToLower()) &&
                                                                   r.ClassIncludes == null)
                                                       .Select(f => f.FolderLocation)
                                                       .FirstOrDefault().ToString();
                    }
                }
            }
            else if(isShowType)
            {
                return includes.ShowType;
            }
            
            return "";
        }

        private string ParseReportingClass(string ReportingClass)
        {
            var result = db.ReportClassMapping.Where(r => r.ReportingClass.ToLower() == ReportingClass.ToLower()
                                                      && r.ClassIncludes == null)
                                             .Select(f => f.FolderLocation).FirstOrDefault();

            if (!string.IsNullOrEmpty(result) && !isShowType)
            {
                return result;
            }
            else
            {
                return ParseReportClassIncludes(ReportingClass);
            }
        }
        
        private bool SetProgramData()
        {
            try
            {
                ITVConversionFunctions iTVConversion = new ITVConversionFunctions();

                var blah = iTVConversion.GetBillingId(_Parser.ITV_PAID);

                using (db = new ITVConversionContext())
                {
                    foreach (var entry in db.FieldMappings)
                    {
                        log.Debug($"{entry.AdiElement}, {entry.ItvElement}");
                        //add further logic for folder location items and any other conversions.
                        // add logic for reporting class = adult then add the node else ignore
                        var itvValue = _Parser.GET_ITV_VALUE(entry.ItvElement);
                        
                        switch(entry.ItvElement)
                        {
                            case "none":
                                SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, entry.ItvElement);
                                break;
                            case "BillingId":
                                var billingId = iTVConversion.GetBillingId(_Parser.ITV_PAID);
                                SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, billingId);
                                break;
                            case "Length":
                            case "RentalTime":
                                var timespan = "";
                                if(entry.ItvElement == "RentalTime")
                                {
                                    timespan = TimeSpan.FromSeconds(Convert.ToDouble(itvValue)).ToString(@"hh\:mm\:ss");
                                }
                                else
                                {
                                    timespan = TimeSpan.FromMinutes(Convert.ToDouble(itvValue)).ToString(@"hh\:mm\:ss");
                                }
                                SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, timespan);
                                break;
                            case "ReportingClass":
                                if(entry.AdiElement.Equals("Show_Type"))
                                {
                                    isShowType = true;
                                }
                                var repclass = ParseReportingClass(itvValue);
                                SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, repclass);
                                break;
                            case "ServiceCode":
                                //if tvod process this, needs discussion
                                var test = itvValue.Split(',');
                                break;
                            case "HDContent":
                                var hdValue = itvValue.ToLower() == "yes" ? "Y" : "N"; 
                                SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, hdValue, entry.IsTitleMetadata);
                                break;
                            case "CanBeSuspended":
                                if(itvValue.ToLower() == "yes")
                                    SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, itvValue, entry.IsTitleMetadata);
                                break;
                            case "Language":
                                var culture = CultureInfo.GetCultures(CultureTypes.AllCultures)
                                                         .Where(n => n.EnglishName.ToLower() == itvValue.ToLower())
                                                         .Select(n=> n.Name)
                                                         .FirstOrDefault();
                                SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, culture, entry.IsTitleMetadata);
                                break;
                            default:
                                SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, itvValue, entry.IsTitleMetadata);
                                break;
                        }
                    }
                }
                

                return true;
            }
            catch (Exception SPD_EX)
            {
                log.Error($"Failed to Map Title Data - {SPD_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {SPD_EX.StackTrace}");

                return false;
            }
        }

        #endregion

        #region AssetData
        

        #endregion
    }
}
