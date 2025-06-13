using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Models
{
    public class BatterySnapshot
    {
        public string SerialNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public double Battery { get; set; }
        public string Restaurant { get; set; }
        public string DeviceModel { get; set; }
        public DateTime EnrollmentDateTime { get; set; }

        public BatterySnapshot(string serialNumber, DateTime timestamp, double battery,
            string restaurant, string deviceModel, DateTime enrollmentDateTime)
        {
            SerialNumber = serialNumber;
            Timestamp = timestamp;
            Battery = battery;
            Restaurant = restaurant;
            DeviceModel = deviceModel;
            EnrollmentDateTime = enrollmentDateTime;
        }
    }
}

