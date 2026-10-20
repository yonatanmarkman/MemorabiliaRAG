using System.Text.Json;
using OpenAI.Chat;
using MemorabiliaRAG.Core.Models;

namespace MemorabiliaRAG.Core.Retrieval;

public class RerankerLLM : IReranker
{
    private readonly ChatClient _client;
    private readonly ChatCompletionOptions _options;
    private readonly Action<string>? _onFallback;

    public RerankerLLM(string apiKey, string model = Constants.Model, Action<string>? onFallback = null)
    {
        _client = new ChatClient(model, apiKey);
        _options = new ChatCompletionOptions
        {
            Temperature = 0f,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };
        _onFallback = onFallback;
    }

    public async Task<List<EmbeddedItem>> RerankAsync(
        string question, List<EmbeddedItem> candidates, int topK)
    {
        if (candidates.Count == 0) return candidates; // nothing to judge, skip the LLM call

        string numbered = string.Join("\n",
            candidates.Select((c, i) => $"[{i + 1}] {c.Item.ToEmbeddingText()}"));

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                Constants.PromptForReranker),
            new UserChatMessage($"Question: {question}\n\nCandidate items:\n{numbered}")
        };

        ChatCompletion completion = await _client.CompleteChatAsync(messages, _options);

        try
        {
            var indices = ParseRelevantIndices(completion.Content[0].Text, candidates.Count);
            return indices.Select(i => candidates[i - 1]).Take(topK).ToList();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            // Malformed model output: degrade to the pre-rerank behavior, but say so.
            _onFallback?.Invoke($"Reranker output could not be parsed ({ex.GetType().Name}); using retrieval order.");
            return candidates.Take(topK).ToList();
        }
    }

    private static List<int> ParseRelevantIndices(string json, int max)
    {
        using var doc = JsonDocument.Parse(json);
        var result = new List<int>();
        foreach (var el in doc.RootElement.GetProperty("relevant").EnumerateArray())
        {
            if (el.TryGetInt32(out int n) && n >= 1 && n <= max && !result.Contains(n))
                result.Add(n);
        }
        return result;
    }
}