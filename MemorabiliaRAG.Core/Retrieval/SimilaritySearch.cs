using MemorabiliaRAG.Core.Models;

namespace MemorabiliaRAG.Core.Retrieval;

public class SimilaritySearch
{
    public List<EmbeddedItem> FindTopMatches(
        float[] queryVector,
        List<EmbeddedItem> candidates,
        int topK = Constants.DefaultTopK,
        double minSimilarity = Constants.MinSimilarity)
    {
        IEnumerable<CandidateWithScore> res =
            candidates
            .Select(c => new CandidateWithScore()
            { 
                Candidate = c, 
                SemanticScore = CosineSimilarity(queryVector, c.Vector)
            })
            .OrderByDescending(x => x.FinalScore);

        return res
            .Where(x => x.FinalScore >= minSimilarity)
            .Take(topK)
            .Select(x => x.Candidate)
            .ToList();
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, magA = 0, magB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}