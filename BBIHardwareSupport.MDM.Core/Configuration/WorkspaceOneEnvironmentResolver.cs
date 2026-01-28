using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration
{
    public class WorkspaceOneEnvironmentResolver:IWorkspaceOneEnvironmentResolver
    {
        public static WorkspaceOneConnectionInfo Resolve(WorkspaceOneEnvironment env)
        {
            return env switch
            {
                WorkspaceOneEnvironment.Production => new WorkspaceOneConnectionInfo
                {
                    Environment = WorkspaceOneEnvironment.Production,
                    BaseUri = Environment.GetEnvironmentVariable("WS1_BASEURI")
                             ?? throw new InvalidOperationException("WS1_BASEURI not set"),
                    AwTenantCode = Environment.GetEnvironmentVariable("WS1TenantCode")
                             ?? throw new InvalidOperationException("WS1TenantCode not set"),
                },

                WorkspaceOneEnvironment.QA => new WorkspaceOneConnectionInfo
                {
                    Environment = WorkspaceOneEnvironment.QA,
                    BaseUri = Environment.GetEnvironmentVariable("WS1_QA_BASEURI")
                             ?? throw new InvalidOperationException("WS1_QA_BASEURI not set"),
                    AwTenantCode = Environment.GetEnvironmentVariable("WS1_QA_TenantCode")
                             ?? throw new InvalidOperationException("WS1_QA_TenantCode not set"),
                },

                _ => throw new ArgumentOutOfRangeException(nameof(env), env, null)
            };
        }

    }

}
