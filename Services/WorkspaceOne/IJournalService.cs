using BBIHardwareSupport.MDM.Models.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace BBIHardwareSupport.MDM.Services.Graph;

public interface IJournalService
{
    Task<string> StartOperationAsync(string title, CancellationToken ct);
    Task AppendAsync(string operationId, object entry, CancellationToken ct);
    Task CompleteAsync(string operationId, bool success, string? message, CancellationToken ct);
}

