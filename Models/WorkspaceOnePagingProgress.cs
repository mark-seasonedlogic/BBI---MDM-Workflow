using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    public sealed class WorkspaceOnePagingProgress
    {
        public int CurrentPage { get; init; }          // 0-based
        public int PageSize { get; init; }             // may be 0 until discovered
        public int? TotalItems { get; init; }           // may be 0 until discovered
        public int ItemsLoaded { get; init; }
        public string? RequestUri { get; init; }

        public double? Percent =>
            (TotalItems > 0) ? (ItemsLoaded * 100.0 / TotalItems) : null;
    }

}
