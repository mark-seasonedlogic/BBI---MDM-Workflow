using BBIHardwareSupport.MDM.Services.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services.Graph;

public sealed class FileJournalService : IJournalService
{
    private readonly string _root;

    public FileJournalService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _root = Path.Combine(appData, "BBIHardwareSupport", "MDMWorkflow", "_journal");
        Directory.CreateDirectory(_root);
    }

    public Task<string> StartOperationAsync(string title, CancellationToken ct)
    {
        var id = $"{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
        var hdr = new { id, title, started = DateTimeOffset.Now };
        File.WriteAllText(Path.Combine(_root, $"{id}.start.json"), JsonSerializer.Serialize(hdr, new JsonSerializerOptions { WriteIndented = true }));
        return Task.FromResult(id);
    }

    public Task AppendAsync(string operationId, object entry, CancellationToken ct)
    {
        var path = Path.Combine(_root, $"{operationId}.log.jsonl");
        File.AppendAllText(path, JsonSerializer.Serialize(entry) + Environment.NewLine);
        return Task.CompletedTask;
    }

    public Task CompleteAsync(string operationId, bool success, string? message, CancellationToken ct)
    {
        var tail = new { id = operationId, success, message, completed = DateTimeOffset.Now };
        File.WriteAllText(Path.Combine(_root, $"{operationId}.end.json"), JsonSerializer.Serialize(tail, new JsonSerializerOptions { WriteIndented = true }));
        return Task.CompletedTask;
    }
}

