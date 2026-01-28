using BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration
{
    public class WorkspaceOneCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ApiKey { get; set; }
        public WorkspaceOneEnvironment Environment { get; set; }
    }
}
