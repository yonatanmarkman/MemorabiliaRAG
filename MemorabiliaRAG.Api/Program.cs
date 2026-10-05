using MemorabiliaRAG.Api.Middleware;
using MemorabiliaRAG.Core.Embeddings;
using MemorabiliaRAG.Core.Generation;
using MemorabiliaRAG.Core.Models;
using MemorabiliaRAG.Core.Retrieval;

namespace MemorabiliaRAG.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // ---- Configuration ----
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);
        string openAiKey = builder.Configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI API key not found in configuration.");

        // ---- Register singletons ----
        builder.Services.AddSingleton(new EmbeddingService(openAiKey));
        builder.Services.AddSingleton(new AnswerGenerator(openAiKey));
        builder.Services.AddSingleton<HybridSearch>();
        builder.Services.AddControllers();

        // ---- Global exception handling ----
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        // Paths
        const string xlsxFilePath = @"..\DataSets\Yony_s_autograph_collection.xlsx";
        const string cacheFilePath = @"..\DataSets\embeddings_cache.json";

        // The embedded item list needs async loading — can't just "new" it inline.
        // Load it here, synchronously, at startup, then register the already-built result:

        // ---- Load the embeddings cache once, at startup ----
        EmbeddingCache cache = new EmbeddingCache(cacheFilePath);
        List<EmbeddedItem> embeddedItems = cache.Exists()
            ? await cache.LoadAsync()
            : throw new InvalidOperationException("No embeddings cache found — run the console app first to generate one.");

        builder.Services.AddSingleton(embeddedItems);

        WebApplication app = builder.Build();

        app.UseExceptionHandler();

        app.MapControllers();

        app.Run();
    }
}
