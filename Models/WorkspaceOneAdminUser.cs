using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    public sealed class WorkspaceOneAdminUser
    {
        public int AdminId { get; init; }
        public string Uuid { get; init; } = "";
        public string UserName { get; init; } = "";
        public string FirstName { get; init; } = "";
        public string LastName { get; init; } = "";
        public string Email { get; init; } = "";
        public string LocationGroup { get; init; } = "";
        public string LocationGroupId { get; init; } = "";
        public string OrganizationGroupUuid { get; init; } = "";
        public string TimeZone { get; init; } = "";
        public string Locale { get; init; } = "";
        public DateTimeOffset? LastLoginTimeStamp { get; init; }
        public IReadOnlyList<WorkspaceOneRole> Roles { get; init; } = Array.Empty<WorkspaceOneRole>();
    }
}
