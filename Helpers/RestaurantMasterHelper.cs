using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.IntuneConfigManager.Models;
using System.IO;
using Microsoft.Graph.Models;
using System.Formats.Asn1;
using Windows.Devices.Usb;
using CsvHelper;
using CsvHelper.Configuration;
using NLog;
using NLog.LayoutRenderers.Wrappers;
namespace BBIHardwareSupport.MDM.IntuneConfigManager.Helpers
{
    public class RestaurantMasterHelper
    {
        private readonly ILogger Logger;
        public RestaurantMasterHelper(ILogger logger)
        {
            Logger = logger;
        }
        public Dictionary<string, RestaurantMasterInfo> LoadCorpRestaurantTimeZones(string csvPath)
        {
            var restaurants = new Dictionary<string, RestaurantMasterInfo>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var reader = new StreamReader(csvPath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)))
                {
                    var records = csv.GetRecords<dynamic>();
                    

                    foreach (var record in records)
                    {
                        var name = record.RSTRNT_LNG_NAME ?? record.RSTRNT_COMMON_NAME ?? record.RSTRNT_LEGAL_NAME;
                        if (string.IsNullOrEmpty(name))
                        {
                            Logger.Warn("Skipping record due to missing restaurant name: {0}",record.CONCEPT_RSTRNT_CD);
                            continue;
                        }
                        
                        var deviceTimezone = record.TIME_ZN_NAME;
                        if (string.IsNullOrEmpty(deviceTimezone))
                        {
                            Logger.Warn("Skipping record due to missing timezone name: {0}", record.CONCEPT_RSTRNT_CD);
                            continue;
                        }
                        var concept = record.CONCEPT_RSTRNT_CD;
                        if (string.IsNullOrEmpty(concept))
                        {
                            Logger.Warn("Skipping record due to missing concept id : {0}",name);
                            continue;
                        }

                        var storeCD = concept.Substring(0, concept.IndexOf("_"));
                        string storeAbbr = String.Empty;
                        switch(storeCD)
                        {
                            case "1":
                                storeAbbr = "OBS";
                                break;
                            case "4":
                                storeAbbr = "FLM";
                                break;
                            case "6":
                                storeAbbr = "BFG";
                                break;
                            case "7":
                                storeAbbr = "CIG";
                                break;
                            default:
                                
                                break;


                        }
                        if (string.IsNullOrEmpty(storeAbbr))
                        {
                            Logger.Warn("Skipping record with CD ID: {0}", concept);
                            continue;
                        }
                        var conceptInfo = concept.Split('_');
                        if (conceptInfo.Length != 2)
                        {
                            Logger.Warn("Skipping record with invalid concept format: {0}", concept);
                            continue;
                        }

                        string storeNumber = conceptInfo[1].PadLeft(4,'0');

                        var fullStoreId = String.Format("{0}{1}",storeAbbr,storeNumber);
                        restaurants.Add(fullStoreId,new RestaurantMasterInfo
                        {
                            ConceptCode = storeAbbr,
                            TimeZoneName = deviceTimezone // Default to UTC if not specified
                        });
                        
                    }

                    Logger.Info($"Loaded {restaurants.Count} restaurants from CSV.");
                    return restaurants;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reading CSV: {ex.Message}");
                throw;
            }

        }
        public Dictionary<string, RestaurantMasterInfo> LoadNonCorpRestaurantTimeZones(string csvPath)
        {
            var restaurants = new Dictionary<string, RestaurantMasterInfo>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var reader = new StreamReader(csvPath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)))
                {
                    var records = csv.GetRecords<dynamic>();


                    foreach (var record in records)
                    {
                        var name = record.RSTRNT_LNG_NAME ?? record.RSTRNT_COMMON_NAME ?? record.RSTRNT_LEGAL_NAME;
                        if (string.IsNullOrEmpty(name))
                        {
                            Logger.Warn("Skipping record due to missing restaurant name.");
                            continue;
                        }

                        var deviceTimezone = record.TimeZoneTableCode_Name
;
                        if (string.IsNullOrEmpty(deviceTimezone))
                        {
                            Logger.Warn("Skipping record due to missing timezone name.");
                            continue;
                        }
                        var concept = record.CONCEPT_RSTRNT_CD;
                        if (string.IsNullOrEmpty(concept))
                        {
                            Logger.Warn("Skipping record due to missing concept id.");
                            continue;
                        }

                        var storeCD = concept.Substring(0, concept.IndexOf("_"));
                        string storeAbbr = String.Empty;
                        switch (storeCD)
                        {
                            case "1":
                                storeAbbr = "OBS";
                                break;
                            case "4":
                                storeAbbr = "FLM";
                                break;
                            case "6":
                                storeAbbr = "BFG";
                                break;
                            case "7":
                                storeAbbr = "CIG";
                                break;
                            default:

                                break;


                        }
                        if (string.IsNullOrEmpty(storeAbbr))
                        {
                            Logger.Warn("Skipping record with CD ID: {0}", concept);
                            continue;
                        }
                        var conceptInfo = concept.Split('_');
                        if (conceptInfo.Length != 2)
                        {
                            Logger.Warn("Skipping record with invalid concept format: {0}", concept);
                            continue;
                        }

                        string storeNumber = conceptInfo[1].PadLeft(4, '0');

                        var fullStoreId = String.Format("{0}{1}", storeAbbr, storeNumber);
                        restaurants.Add(fullStoreId, new RestaurantMasterInfo
                        {
                            ConceptCode = storeAbbr,
                            TimeZoneName = deviceTimezone // Default to UTC if not specified
                        });

                    }

                    Logger.Info($"Loaded {restaurants.Count} restaurants from CSV.");
                    return restaurants;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reading CSV: {ex.Message}");
                throw;
            }

        }
    }
}
