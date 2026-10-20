using OpenAI.Embeddings;
using MemorabiliaRAG.Core.Models;
using System.ClientModel;
using MemorabiliaRAG.Core.Data;

namespace MemorabiliaRAG.Core.Embeddings;

public static class EmbeddingMethods
{
    public static async Task<List<EmbeddedItem>> EmbedItemsAsync(
        string xlsxFilePath, 
        EmbeddingCache cache, 
        EmbeddingService embeddingService)
    {
        var loader = new ExcelItemLoader();
        List<MemorabiliaItem> items = loader.Load(xlsxFilePath);

        List<EmbeddedItem> embeddedItems = await embeddingService.EmbedItemsAsync(
            items,
            progress: new Progress<int>(n =>
                Console.WriteLine($"  embedded {n}/{items.Count}")));

        await cache.SaveAsync(embeddedItems);
        return embeddedItems;
    }
}