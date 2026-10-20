using MemorabiliaRAG.Core.Models;

namespace MemorabiliaRAG.Core.Retrieval;

public interface IReranker
{
    Task<List<EmbeddedItem>> RerankAsync(
        string question, List<EmbeddedItem> candidates, int topK);
}
