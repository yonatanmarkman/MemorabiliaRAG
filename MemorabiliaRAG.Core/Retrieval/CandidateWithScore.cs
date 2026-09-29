using MemorabiliaRAG.Core.Models;

namespace MemorabiliaRAG.Core.Retrieval;

public class CandidateWithScore
{
    public required double SemanticScore { get; set; }
    public required EmbeddedItem Candidate { get; set; }
    public virtual double FinalScore
    { 
        get => SemanticScore; 
    }

    public override string ToString()
    {
        return Candidate.Item.ItemDescription;
    }
}

public class CandidateWithMultipleScores : CandidateWithScore
{
    public required double KeywordScore { get; set; }

    public double CalculateFinalScore()
    {
        return (SemanticScore * Constants.SemanticWeight)
             + (KeywordScore * Constants.KeywordWeight);
    }

    public override double FinalScore
    {
        get => CalculateFinalScore();
    }
}