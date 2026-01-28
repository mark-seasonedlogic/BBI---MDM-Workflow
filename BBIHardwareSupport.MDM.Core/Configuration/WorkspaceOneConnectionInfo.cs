using BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration
{
    public sealed record WorkspaceOneConnectionInfo
    {
        public WorkspaceOneEnvironment Environment { get; init; }
        public string BaseUri { get; init; } = default!;

        // This is what WS1 expects in the header "aw-tenant-code"
        public string AwTenantCode { get; init; } = default!;

        // Optional compatibility alias if old code references TenantId
        public string TenantId => AwTenantCode;
    }

}
