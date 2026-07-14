namespace AIEventDiscovery.Services.Embeddings;

public interface IEmbeddingService
{
    /// <summary>
    /// Generates a semantic embedding vector for the given text.
    /// Uses the local all-MiniLM-L6-v2 ONNX model.
    /// </summary>
    float[] GenerateEmbedding(string text);
}
