using OpenAI.Embeddings;
using MemorabiliaRAG.Core.Models;
using System.ClientModel;

namespace MemorabiliaRAG.Core.Embeddings;

public class EmbeddingService
{
    private readonly EmbeddingClient _client;
    private const int BatchSize = 100;

    public EmbeddingService(string apiKey)
    {
        _client = new EmbeddingClient("text-embedding-3-small", apiKey);
    }

    public async Task<List<EmbeddedItem>> EmbedItemsAsync(
        List<MemorabiliaItem> items,
        IProgress<int>? progress = null)
    {
        List<EmbeddedItem> results = [];

        for (int start = 0; start < items.Count; start += BatchSize)
        {
            List<MemorabiliaItem> batch = [.. items
                .Skip(start)
                .Take(BatchSize)];
            List<string> texts = [.. batch.Select(i => i.ToEmbeddingText())];

            OpenAIEmbeddingCollection embeddings
                 = await _client.GenerateEmbeddingsAsync(texts);

            for (int i = 0; i < batch.Count; i++)
            {
                results.Add(new EmbeddedItem
                {
                    Item = batch[i],
                    Vector = embeddings[i]
                        .ToFloats()
                        .ToArray()
                });
            }

            progress?.Report(Math.Min(start + BatchSize, items.Count));
        }

        return results;
    }

    public async Task<float[]> EmbedQueryAsync(string query)
    {
        ClientResult<OpenAIEmbedding> result
             = await _client.GenerateEmbeddingAsync(query);
        return result.Value.ToFloats().ToArray();
    }
}