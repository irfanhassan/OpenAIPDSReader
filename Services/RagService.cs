using OpenAI.Embeddings;
using System.ClientModel;

namespace OPenAIPDSQandA.Services;

/// <summary>
/// Retrieval-Augmented Generation service.
/// Splits a document into overlapping chunks, generates embeddings for each chunk
/// using batch API calls, and retrieves the most relevant chunks for a given query
/// using cosine similarity.
/// </summary>
public class RagService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly List<DocumentChunk> _index = new();

    // ~1 500 chars ≈ 375 tokens — keeps embeddings meaningful and context tight
    private const int ChunkSize    = 1_500;
    private const int ChunkOverlap = 250;
    private const int DefaultTopK  = 5;

    /// <summary>How many chunks to embed per API call (OpenAI allows up to 2 048).</summary>
    private const int EmbeddingBatchSize = 64;

    public int ChunkCount => _index.Count;

    public RagService(string apiKey, string embeddingModel = "text-embedding-3-small")
    {
        _embeddingClient = new EmbeddingClient(embeddingModel, new ApiKeyCredential(apiKey));
    }

    // -------------------------------------------------------------------------
    // Indexing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Chunks the document and embeds every chunk using batch API calls.
    /// Call this once after loading the PDS.
    /// </summary>
    public async Task BuildIndexAsync(
        string documentText,
        Action<int, int>? onProgress = null,
        CancellationToken ct = default)
    {
        _index.Clear();

        var chunks = ChunkDocument(documentText);
        int total  = chunks.Count;
        int done   = 0;

        // Embed in batches — one API call per batch instead of one per chunk
        for (int batchStart = 0; batchStart < total; batchStart += EmbeddingBatchSize)
        {
            ct.ThrowIfCancellationRequested();

            int batchEnd   = Math.Min(batchStart + EmbeddingBatchSize, total);
            var batch      = chunks.GetRange(batchStart, batchEnd - batchStart);
            var embeddings = await EmbedBatchAsync(batch, ct);

            for (int j = 0; j < batch.Count; j++)
            {
                _index.Add(new DocumentChunk(batch[j], embeddings[j], batchStart + j));
                onProgress?.Invoke(++done, total);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Retrieval
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the top-K most relevant chunks for the query, in document order.
    /// </summary>
    public async Task<string[]> RetrieveAsync(
        string query,
        int topK = DefaultTopK,
        CancellationToken ct = default)
    {
        if (_index.Count == 0)
            return [];

        var queryEmbedding = await EmbedSingleAsync(query, ct);

        return _index
            .Select(c => (chunk: c, score: CosineSimilarity(queryEmbedding, c.Embedding)))
            .OrderByDescending(x => x.score)
            .Take(topK)
            .OrderBy(x => x.chunk.Index)   // restore document reading order
            .Select(x => x.chunk.Text)
            .ToArray();
    }

    /// <summary>
    /// Returns the top-K most relevant chunks together with their similarity scores.
    /// </summary>
    public async Task<(string Text, float Score)[]> RetrieveWithScoresAsync(
        string query,
        int topK = DefaultTopK,
        CancellationToken ct = default)
    {
        if (_index.Count == 0)
            return [];

        var queryEmbedding = await EmbedSingleAsync(query, ct);

        return _index
            .Select(c => (chunk: c, score: CosineSimilarity(queryEmbedding, c.Embedding)))
            .OrderByDescending(x => x.score)
            .Take(topK)
            .OrderBy(x => x.chunk.Index)   // restore document reading order
            .Select(x => (x.chunk.Text, x.score))
            .ToArray();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<float[]> EmbedSingleAsync(string text, CancellationToken ct)
    {
        var result = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return result.Value.ToFloats().ToArray();
    }

    private async Task<List<float[]>> EmbedBatchAsync(List<string> texts, CancellationToken ct)
    {
        var result = await _embeddingClient.GenerateEmbeddingsAsync(texts, cancellationToken: ct);
        // Results are returned in the same order as the inputs
        return result.Value
                     .OrderBy(e => e.Index)
                     .Select(e => e.ToFloats().ToArray())
                     .ToList();
    }

    private static List<string> ChunkDocument(string text)
    {
        var chunks = new List<string>();
        int start  = 0;

        while (start < text.Length)
        {
            int end = Math.Min(start + ChunkSize, text.Length);

            // Prefer splitting at a paragraph boundary to keep semantic units intact
            if (end < text.Length)
            {
                int searchFrom = Math.Max(start + ChunkSize / 2, 0);
                int searchLen  = end - searchFrom;

                int paraBreak = text.LastIndexOf("\n\n", end, searchLen);
                if (paraBreak > searchFrom)
                {
                    end = paraBreak + 2;
                }
                else
                {
                    int lineBreak = text.LastIndexOf('\n', end, Math.Min(searchLen, 200));
                    if (lineBreak > searchFrom)
                        end = lineBreak + 1;
                }
            }

            var chunk = text[start..end].Trim();
            if (chunk.Length > 50)
                chunks.Add(chunk);

            // Advance with overlap so context isn't lost at chunk boundaries
            start = Math.Max(start + 1, end - ChunkOverlap);
        }

        return chunks;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0f, magA = 0f, magB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot  += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        float denom = MathF.Sqrt(magA) * MathF.Sqrt(magB);
        return denom == 0f ? 0f : dot / denom;
    }
}

public record DocumentChunk(string Text, float[] Embedding, int Index);
