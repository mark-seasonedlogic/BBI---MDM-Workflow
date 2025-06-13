using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Models
{
    public class RapidDrainEvent
    {
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
        public string SerialNumber { get; set; }
        public string Restaurant { get; set; }
        public string Model { get; set; }
        public DateTime EnrollmentDateTime { get; set; }
        public double Drain { get; set; }
        public double Hours { get; set; }
        public double DropPerHour { get; set; }
    }

}
