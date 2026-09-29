using System.Text.Json;
using MemorabiliaRAG.Core.Models;

namespace MemorabiliaRAG.Core.Embeddings;

public class EmbeddingCache
{
    private readonly string _cachePath;

    public EmbeddingCache(string cachePath)
    {
        _cachePath = cachePath;
    }

    public bool Exists() => File.Exists(_cachePath);

    public async Task SaveAsync(List<EmbeddedItem> items)
    {
        await using var stream = File.Create(_cachePath);
        await JsonSerializer.SerializeAsync(stream, items);
    }

    public async Task<List<EmbeddedItem>> LoadAsync()
    {
        await using var stream = File.OpenRead(_cachePath);
        return await JsonSerializer.DeserializeAsync<List<EmbeddedItem>>(stream)
            ?? throw new InvalidOperationException("Cache file was empty or corrupt.");
    }
}