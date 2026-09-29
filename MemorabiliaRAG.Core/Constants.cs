using OpenAI.Chat;

namespace MemorabiliaRAG.Core;

internal static class Constants
{
    public const int DefaultTopK = 5;

    /*
    Empirically calibrated against text-embedding-3-small on this dataset:
    relevant questions scored >= 0.41, unrelated questions scored 0.2-0.3.
    0.4 chosen as a safety margin below the observed relevant floor.
    */
    public const double MinSimilarity = 0.4;

    public const double MinCombinedScore = 0.4;
    public const double SemanticWeight = 0.7;
    public const double KeywordWeight = 0.3;

    public const string Model = "gpt-4o-mini";

    public const string PromptForOpenAI = 
        "You are an assistant that answers questions about the user's personal autograph/memorabilia collection. " +
        "Only use the numbered items listed below as context. If the context doesn't contain enough information " +
        "to answer, say so plainly rather than guessing or inventing details.\n\n" +
        "Some retrieved items may be irrelevant noise from similarity search — ignore any item that doesn't " +
        "actually answer the question, even if it was included in the list below.\n\n" +
        "After your answer, add a line starting with 'Sources used:' followed by the bracketed numbers " +
        "of ONLY the items you actually relied on (e.g. 'Sources used: [2], [4]'). If you used none, write 'Sources used: none'.";

    public static readonly ChatCompletionOptions ChatOptions = new ChatCompletionOptions
    {
        Temperature = 0.1f,       // how "creative"/random the output is
        MaxOutputTokenCount = 300 // caps response length
    };

    public static string GetUserChatMessage(string context, string question)
    {
        return $"Collection items:\n{context}\n\nQuestion: {question}";
    }

    public static readonly HashSet<string> StopWords = new()
    {
        "a", "an", "the", "of", "in", "on", "at", "to", "for", "from", "by", "with", "about",
        "what", "which", "who", "whom", "whose", "where", "when", "why", "how",
        "do", "does", "did", "is", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "i", "my", "me", "you", "your",
        "and", "or", "but", "if", "so", "as", "than",
        "item", "items", "signed", "top", "five", "5"
    };
}
