using OpenAI.Chat;
using MemorabiliaRAG.Core.Models;

namespace MemorabiliaRAG.Core.Generation;

public class AnswerGenerator
{
    private readonly ChatClient _client;

    public AnswerGenerator(string apiKey)
    {
        _client = new ChatClient(Constants.Model, apiKey);
    }

    public async Task<string> GenerateAnswerAsync(string question, List<EmbeddedItem> retrievedItems)
    {
        string context = string.Join("\n", retrievedItems.Select(
            (r, i) => $"[{i + 1}] {r.Item.ToEmbeddingText()}"));

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                Constants.PromptForOpenAI),
            new UserChatMessage(
                Constants.GetUserChatMessage(context, question))
        };

        ChatCompletion completion = await _client.CompleteChatAsync(messages, Constants.ChatOptions);
        return completion.Content[0].Text;
    }
}