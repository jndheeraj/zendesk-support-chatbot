namespace KyrisCBL.Helpers
{
    public class CosineSimilarityHelper
    {
        public static double Calculate(List<float> a, List<float> b)
        {
            if (a == null || b == null || a.Count != b.Count)
                throw new ArgumentException("Embeddings must be non-null and of equal length.");

            double dot = 0.0;
            double magA = 0.0;
            double magB = 0.0;

            for (int i = 0; i < a.Count; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB) + 1e-10); // Add small epsilon to avoid divide-by-zero
        }
    }
}
