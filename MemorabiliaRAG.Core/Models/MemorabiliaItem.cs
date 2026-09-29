namespace MemorabiliaRAG.Core.Models;

public class MemorabiliaItem
{
    public required string Id { get; set; }
    public required string ItemDescription { get; set; }    // Raw "Item" column, e.g. "Fear Factory - Obsolete signed CD"
    public string? ObtainedAt { get; set; }                 // Source/Channel — eBay, Meet & Greet, artist's site, etc. (Nullable)
    public int? SigningYear { get; set; }                   // Always known when signing is known
    public DateOnly? SigningDate { get; set; }              // Only when a precise date exists
    public string? Notes { get; set; }                      // Nullable

    public string ToEmbeddingText()
    {
        List<string> parts = [ItemDescription];

        if (!string.IsNullOrWhiteSpace(ObtainedAt)) 
            parts.Add($"Obtained at: {ObtainedAt}");
        if (SigningDate.HasValue) 
            parts.Add($"Signed: {SigningDate:yyyy-MM-dd}");
        else if (SigningYear.HasValue) 
            parts.Add($"Signed in {SigningYear}");
        if (!string.IsNullOrWhiteSpace(Notes)) 
            parts.Add(Notes);
        
        return string.Join(". ", parts);
    }
}
