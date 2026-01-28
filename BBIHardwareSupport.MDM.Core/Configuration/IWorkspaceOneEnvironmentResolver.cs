using BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration
{
    internal interface IWorkspaceOneEnvironmentResolver
    {
        public abstract static WorkspaceOneConnectionInfo Resolve(WorkspaceOneEnvironment env);
    }
}
