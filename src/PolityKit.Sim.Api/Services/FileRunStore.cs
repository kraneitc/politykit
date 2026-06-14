using System.Text.Json;
using Microsoft.Extensions.Options;
using PolityKit.Sim.Api.Services.Models;

namespace PolityKit.Sim.Api.Services;

public sealed class FileRunStore : IRunStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly Lock _gate = new();
    private readonly string _directory;

    public FileRunStore(IOptions<RunStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _directory = Path.GetFullPath(options.Value.Directory);
        Directory.CreateDirectory(_directory);
    }

    public IReadOnlyList<StoredRun> List()
    {
        lock (_gate)
        {
            return Directory
                .EnumerateFiles(_directory, "*.json")
                .Select(TryReadRun)
                .OfType<StoredRun>()
                .OrderByDescending(run => run.CreatedAt)
                .ToArray();
        }
    }

    public StoredRun? Get(Guid id)
    {
        lock (_gate)
        {
            var path = GetRunPath(id);
            return File.Exists(path) ? ReadRun(path) : null;
        }
    }

    public StoredRun Add(StoredRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        lock (_gate)
        {
            var path = GetRunPath(run.Id);
            var temporaryPath = $"{path}.tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(run, JsonOptions));

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temporaryPath, path);
            return run;
        }
    }

    private string GetRunPath(Guid id)
    {
        return Path.Combine(_directory, $"{id:N}.json");
    }

    private static StoredRun? TryReadRun(string path)
    {
        try
        {
            return ReadRun(path);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static StoredRun? ReadRun(string path)
    {
        return JsonSerializer.Deserialize<StoredRun>(File.ReadAllText(path), JsonOptions);
    }
}
