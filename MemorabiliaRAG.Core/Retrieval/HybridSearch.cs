using MemorabiliaRAG.Core.Embeddings;
using MemorabiliaRAG.Core.Models;

namespace MemorabiliaRAG.Core.Retrieval;

public class HybridSearch
{
    public List<EmbeddedItem> FindTopMatches(
        List<string> questionKeywords,
        float[] queryVector,
        List<EmbeddedItem> candidates,
        int topK = Constants.DefaultTopK,
        double minCombinedScore = Constants.MinCombinedScore)
    {

        IEnumerable<CandidateWithMultipleScores> res =
            candidates
            .Select(c => new CandidateWithMultipleScores()
            {
                Candidate = c,
                SemanticScore = CosineSimilarity(queryVector, c.Vector),
                KeywordScore = KeywordOverlapScore(questionKeywords, c.Item.ToEmbeddingText())
            })
            .Where(x => x.FinalScore >= minCombinedScore)
            .OrderByDescending(x => x.FinalScore);

        IEnumerable<CandidateWithMultipleScores> resBeforeReturn = res
            .Take(topK);

        return resBeforeReturn
            .Select(x => x.Candidate)
            .ToList();
    }
    
    public async Task<List<EmbeddedItem>> FindTopMatchesAsync(
        string question,
        List<EmbeddedItem> candidates,
        EmbeddingService embeddingService,
        int topK = Constants.DefaultTopK)
    {
        List<string> subQueries = SplitIntoSubQueries(question);
        var allMatches = new Dictionary<string, EmbeddedItem>();

        foreach (string subQuery in subQueries)
        {
            List<string> subQueryKeyWords = ExtractKeywords(subQuery);

            string cleanSubQuery = subQueryKeyWords.Count > 0 
                                    ? string.Join(" ", subQueryKeyWords) 
                                    : subQuery;

            float[] subVector = await embeddingService
                            .EmbedQueryAsync(cleanSubQuery);

            List<EmbeddedItem> subMatches = FindTopMatches(
                subQueryKeyWords, 
                subVector, 
                candidates, 
                topK);
            
            foreach (EmbeddedItem m in subMatches)
                allMatches[m.Item.Id] = m;
        }

        return allMatches
            .Values
            .Take(topK * subQueries.Count)
            .ToList(); // cap sensibly
    }


    private static List<string> SplitIntoSubQueries(string question)
    {
        // crude but effective: split on " or " and " and "
        List<string> parts = System.Text.RegularExpressions.Regex
            .Split(question, @"\s+(or|and)\s+", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            .Where(p => !string.Equals(p, "or", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(p, "and", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        return parts.Count > 1 ? parts : new List<string> { question };
    }

    public static List<string> ExtractKeywords(string question)
    {
        HashSet<string> stopWords = Constants.StopWords;
        
        return question
            .Replace(".", "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('?', '.', ',').ToLowerInvariant())
            .Where(w => !stopWords.Contains(w) && w.Length > 2)
            .ToList();
    }

    private static double KeywordOverlapScore(List<string> keywords, string itemText)
    {
        if (keywords.Count == 0)
            return 0;
        
        string lowerText = itemText.ToLowerInvariant();
        
        
        // Split into actual words instead of checking raw substrings
        HashSet<string> textWords = System.Text.RegularExpressions.Regex
                                    .Matches(lowerText, @"\b[\w']+\b")
                                    .Select(m => m.Value)
                                    .ToHashSet();

        int matches = keywords.Count(k => textWords.Contains(k));
        double baseScore = (double)matches / keywords.Count;

        // Bonus: if multiple keywords appear, check they're adjacent (phrase match)
        if (keywords.Count > 1 && matches == keywords.Count)
        {
            string phrase = string.Join(" ", keywords);
            if (lowerText.Contains(phrase))
                return 1.0; // full phrase match — strongest possible signal

            return baseScore * 0.7; // all keywords present, but scattered, not adjacent
        }

        return baseScore;
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