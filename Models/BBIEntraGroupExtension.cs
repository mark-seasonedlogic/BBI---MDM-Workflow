using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Models
{
    public class BBIEntraGroupExtension
    {
        public string? RestaurantCdId { get; set; }
        public string? BrandAbbreviation { get; set; }
        public string? RestaurantNumber { get; set; }
        public string? RestaurantName { get; set; }
        public string? RegionId { get; set; } // Optional/future
        public const string ExtensionId = "com.bbi.entra.group.metadata";
        public static string Prefix => $"extension_{ExtensionId}_";
        public  enum ConceptPrefix
        {
            OBS = 1,
            BFG = 6,
            CIG = 7
        }
    }

}
