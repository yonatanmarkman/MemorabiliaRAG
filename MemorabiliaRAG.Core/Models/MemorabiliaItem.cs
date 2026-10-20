namespace MemorabiliaRAG.Core.Models;

public class MemorabiliaItem
{
    public required string Id { get; set; }
    public required string ItemDescription { get; set; }    // Raw "Item" column, e.g. "Fear Factory - Obsolete signed CD"
    public string? ItemSource { get; set; }                 // Source/Channel — eBay, Meet & Greet, artist's site, etc. (Nullable)
    public int? YearObtained { get; set; }                   // Always known since I know when I got each item :)
    public DateOnly? DateObtained { get; set; }              // Only when a precise date exists, usually for items I got signed in-person
    public string? Notes { get; set; }                      // Nullable

    public string ToEmbeddingText()
    {
        List<string> parts = [ItemDescription];

        if (!string.IsNullOrWhiteSpace(ItemSource)) 
            parts.Add($"Source: {ItemSource}");
        if (DateObtained.HasValue) 
            parts.Add($"Date obtained: {DateObtained:yyyy-MM-dd}");
        else if (YearObtained.HasValue) 
            parts.Add($"Year obtained: {YearObtained}");
        if (!string.IsNullOrWhiteSpace(Notes)) 
            parts.Add(Notes);
        
        return string.Join(". ", parts);
    }
}
