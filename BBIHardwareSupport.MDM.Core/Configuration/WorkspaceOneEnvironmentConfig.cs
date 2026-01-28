using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration
{
    public sealed class WorkspaceOneEnvironmentConfig
    {
        public string BaseUri { get; init; } = default!;
        public string TenantCode { get; init; } = default!;
    }
}

