using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SCH_ITV;
using SCH_ADI;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ITV2ADI_Engine.ITV2ADI_Workers
{
    public partial class MapITVtoADI
    {
        ITVConversionContext db;

        private bool SetAmsSections()
        {
            try
            {
                _Mapping.SetAMSClass();
                _Mapping.SetAMSPAID(ITV_Parser.Padding(_Parser.ITV_PAID), _Parser.ITV_PAID);
                _Mapping.SetAMSAssetName(_Parser.GET_ITV_VALUE("Title"));
                _Mapping.SetAMSCreationDate(_Parser.GET_ITV_VALUE("Publication_Date"));
                _Mapping.SetAMSDescription(_Parser.GET_ITV_VALUE("Title"));
                _Mapping.SetAMSProvider(_Parser.GET_ITV_VALUE("Provider"));
                _Mapping.SetAMSProviderId(_Parser.GET_ITV_VALUE("ProviderId"));
                _Mapping.SetAmsVersions();

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
        
        
        private bool SetProgramData()
        {
            try
            {
                ITVConversionFunctions iTVConversion = new ITVConversionFunctions();
                
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
                                _Mapping.SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, entry.ItvElement);
                                break;
                            case "BillingId":
                                _Mapping.SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, ITV_Parser.GetBillingId(_Parser.ITV_PAID));
                                break;
                            case "Length":
                            case "RentalTime":
                                _Mapping.SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, ITV_Parser.GetTimeSpan(entry.ItvElement, itvValue));
                                break;
                            case "ReportingClass":
                                if(entry.AdiElement.Equals("Show_Type"))
                                {
                                    ADI_Mapping.IsShowType = true;
                                }
                                iTVConversion.Db = db;
                                _Mapping.SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, iTVConversion.ParseReportingClass(itvValue, ADI_Mapping.IsShowType));
                                break;
                            case "ServiceCode":
                                //if tvod process this, needs discussion
                                var test = itvValue.Split(',');
                                break;
                            case "HDContent":
                                _Mapping.SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, ITV_Parser.GetHdValue(itvValue), entry.IsTitleMetadata);
                                break;
                            case "CanBeSuspended":
                                if(itvValue.ToLower() == "yes")
                                    _Mapping.SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, itvValue, entry.IsTitleMetadata);
                                break;
                            case "Language":
                                _Mapping.SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, ITV_Parser.GetISO6391LanguageCode(itvValue), entry.IsTitleMetadata);
                                break;
                            default:
                                _Mapping.SetOrUpdateAdiValue(entry.AdiAppType, entry.AdiElement, itvValue, entry.IsTitleMetadata);
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
    }
}
