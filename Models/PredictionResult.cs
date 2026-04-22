namespace ImageClassificationAPI.Models
{
    public class PredictionResult
    {
        public string? PredictLabel { get; set; }
        public float[]? Score { get; set; }
    }
}
