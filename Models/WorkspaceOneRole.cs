using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    public sealed class WorkspaceOneRole
    {
        public int Id { get; init; }
        public string Uuid { get; init; } = "";
        public string Name { get; init; } = "";
        public bool IsActive { get; init; }
    }
}
