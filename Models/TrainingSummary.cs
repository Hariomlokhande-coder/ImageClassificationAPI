namespace ImageClassificationAPI.Models
{
    public class TrainingSummary
    {
        public int ImageCount { get; set; }
        public int LabelCount { get; set; }
        public TimeSpan Duration { get; set; }
        public string Trainer { get; set; } = string.Empty;
        public double MicroAccuracy { get; set; }
        public double MacroAccuracy { get; set; }
        public double LogLoss { get; set; }
    }
}
