namespace MemorabiliaRAG.Core.Models;

public class EmbeddedItem
{
    public required MemorabiliaItem Item { get; set; }
    public required float[] Vector { get; set; }
}