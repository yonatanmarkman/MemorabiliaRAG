using MemorabiliaRAG.Core.Data;
using MemorabiliaRAG.Core.Embeddings;
using MemorabiliaRAG.Core.Generation;
using MemorabiliaRAG.Core.Models;
using MemorabiliaRAG.Core.Retrieval;

namespace MemorabiliaRAG;


public class Program
{
    public static async Task Main(string[] args)
    {
        // ---- Configuration ----
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string openAiKey = config["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI API key not found in configuration.");

        // ---- Paths ----
        const string xlsxFilePath = @"..\DataSets\Yony_s_autograph_collection.xlsx";
        const string cacheFilePath = @"..\DataSets\embeddings_cache.json";

        // ---- Load or generate embeddings ----
        var cache = new EmbeddingCache(cacheFilePath);
        var embeddingService = new EmbeddingService(openAiKey);
        List<EmbeddedItem> embeddedItems;

        if (cache.Exists())
        {
            Console.WriteLine("Loading cached embeddings...");
            embeddedItems = await cache.LoadAsync();
        }
        else
        {
            Console.WriteLine("Generating embeddings (first run)...");

            var loader = new ExcelItemLoader();
            List<MemorabiliaItem> items = loader.Load(xlsxFilePath);
            Console.WriteLine($"Loaded {items.Count} items from spreadsheet.");

            embeddedItems = await embeddingService.EmbedItemsAsync(
                items,
                progress: new Progress<int>(n =>
                    Console.WriteLine($"  embedded {n}/{items.Count}")));

            await cache.SaveAsync(embeddedItems);
            Console.WriteLine("Embeddings saved to cache.");
        }

        Console.WriteLine($"Ready with {embeddedItems.Count} embedded items.");

        // ---- Sanity check ----
        Console.WriteLine();
        Console.WriteLine("Sample item text:");
        Console.WriteLine(embeddedItems[0].Item.ToEmbeddingText());
        Console.WriteLine($"Vector length: {embeddedItems[0].Vector.Length}");

        // Wiring part
        var search = new HybridSearch();

        string[] testQuestions = {
            "What items do I have signed by Metallica?",
            "Top 5 items signed by Burton C. Bell",
            "What did I get from meet and greets?",
            "RotemSmotri items"
        };

        Console.WriteLine("\nKeywords from the test questions: ");
        foreach (var q in testQuestions)
        {
            var keywords = HybridSearch.ExtractKeywords(q);
            Console.WriteLine($"\"{q}\" -> [{string.Join(", ", keywords)}]");
        }

        Console.WriteLine();
        Console.Write("Ask a question about your collection? : ");

        string? question = Console.ReadLine()?.Trim();
        while (!string.IsNullOrEmpty(question))
        {
            if (!string.IsNullOrWhiteSpace(question))
            {
                float[] queryVector = await embeddingService.EmbedQueryAsync(question);
                List<EmbeddedItem> topMatches =
                    await search.FindTopMatchesAsync(question, embeddedItems, embeddingService);

                var answerGenerator = new AnswerGenerator(openAiKey);

                if (topMatches.Count == 0)
                {
                    Console.WriteLine("No relevant items found in your collection for that question.");
                }
                else
                {
                    Console.WriteLine("Top matches found: ");
                    foreach (EmbeddedItem item in topMatches)
                    {
                        Console.WriteLine(item.Item.ItemDescription);
                    }

                    Console.WriteLine("Thinking...");
                    string answer = await answerGenerator.GenerateAnswerAsync(question, topMatches);

                    Console.WriteLine();
                    Console.WriteLine("Answer:");
                    Console.WriteLine(answer);
                }
            }

            question = Console.ReadLine()!.Trim();
        }
    }
}
