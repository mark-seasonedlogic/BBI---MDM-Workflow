using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Models
{
    public class SimulationItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; } = "\uE7F8"; // default FontIcon
        public ICommand ExecuteCommand { get; set; }
        public string ImagePath { get; set; } = string.Empty; // Path to an image file if needed
    }
}
