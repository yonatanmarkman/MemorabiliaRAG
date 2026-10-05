using Microsoft.AspNetCore.Mvc;
using MemorabiliaRAG.Core.Embeddings;
using MemorabiliaRAG.Core.Generation;
using MemorabiliaRAG.Core.Models;
using MemorabiliaRAG.Core.Retrieval;

namespace MemorabiliaRAG.Api.Controllers;

[ApiController]
[Route("MemorabiliaRAG")]
public class MemorabiliaController : ControllerBase
{
    private readonly List<EmbeddedItem> _embeddedItems;
    private readonly HybridSearch _hybridSearch;
    private readonly EmbeddingService _embeddingService;
    private readonly AnswerGenerator _answerGenerator;

    public MemorabiliaController(
        List<EmbeddedItem> embeddedItems,
        HybridSearch hybridSearch,
        EmbeddingService embeddingService,
        AnswerGenerator answerGenerator)
    {
        _embeddedItems = embeddedItems;
        _hybridSearch = hybridSearch;
        _embeddingService = embeddingService;
        _answerGenerator = answerGenerator;
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "ok" });
    }

    [HttpGet("items")]
    public IActionResult GetItems()
    {
        List<MemorabiliaItem> items = _embeddedItems
                                    .Select(e => e.Item)
                                    .ToList();
        return Ok(items);
    }

    [HttpGet("items/{id}")]
    public IActionResult GetItem(string id)
    {
        MemorabiliaItem? item = _embeddedItems
            .FirstOrDefault(e => e.Item.Id == id)?.Item;
        
        return item is not null 
            ? Ok(item) 
            : NotFound();
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question cannot be empty.");
        }

        List<EmbeddedItem> topMatches = await _hybridSearch.FindTopMatchesAsync(
            request.Question,
            _embeddedItems,
            _embeddingService);

        List<string> matchesList = new List<string>();

        if (topMatches.Count == 0)
        {
            return Ok(new AskResponse(
                "No relevant items found in your collection for that question.",
                matchesList));
        }

        string answer = await _answerGenerator
            .GenerateAnswerAsync(request.Question, topMatches);
        matchesList = topMatches
            .Select(m => m.Item.ItemDescription)
            .ToList();

        return Ok(new AskResponse(
            answer,
            matchesList));
    }
}

// ---- Request/response records ----
public record AskRequest(string Question);
public record AskResponse(string Answer, List<string> RetrievedCandidates);
