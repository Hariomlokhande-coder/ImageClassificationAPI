using Microsoft.ML;
using ImageClassificationAPI.Models;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ImageClassificationAPI.Services
{
    public class ImageClassificationService
    {
        private const string TrainerName = "Fast image classifier (pixel features + SDCA)";
        private const int MaxImagesPerLabel = 400;
        private const int ImageWidth = 64;
        private const int ImageHeight = 64;

        private readonly MLContext _mlContext;
        private readonly SemaphoreSlim _trainingLock = new(1, 1);
        private ITransformer? _model;
        private TrainingSummary? _lastTrainingSummary;
        private readonly string _modelPath = Path.Combine(Directory.GetCurrentDirectory(), "MLModel", "image_model.zip");

        public ImageClassificationService()
        {
            _mlContext = new MLContext(seed: 1);
            Console.WriteLine($"{TrainerName} active.");

            if (File.Exists(_modelPath))
            {
                LoadModel();
                Console.WriteLine("Existing trained model loaded.");
            }
            else
            {
                Console.WriteLine("No trained model found yet.");
            }
        }

        private void LoadModel()
        {
            _model = _mlContext.Model.Load(_modelPath, out _);
        }

        public TrainingSummary? GetLastTrainingSummary()
        {
            return _lastTrainingSummary;
        }

        public TrainingSummary TrainModel(string trainDataPath)
        {
            if (!_trainingLock.Wait(0))
            {
                throw new InvalidOperationException("Training is already in progress.");
            }

            var stopwatch = Stopwatch.StartNew();
            Timer? progressTimer = null;

            try
            {
                var images = LoadImagesFromDirectory(trainDataPath)
                    .GroupBy(i => i.Label, StringComparer.OrdinalIgnoreCase)
                    .SelectMany(group => group
                        .Take(MaxImagesPerLabel))
                    .ToList();
                var imageCount = images.Count;
                var labelCount = images.Select(i => i.Label).Distinct(StringComparer.OrdinalIgnoreCase).Count();

                if (imageCount == 0)
                {
                    throw new InvalidOperationException("No supported images were found in the Dataset folder.");
                }

                if (labelCount < 2)
                {
                    throw new InvalidOperationException("Dataset must contain images in at least 2 label folders.");
                }

                Console.WriteLine($"Training images used: {imageCount}, Labels: {labelCount}, Max per label: {MaxImagesPerLabel}");

                var dataView = _mlContext.Data.LoadFromEnumerable(images);

                var pipeline = _mlContext.Transforms.LoadImages(
                        outputColumnName: "Image",
                        imageFolder: string.Empty,
                        inputColumnName: nameof(ImageData.ImagePath))
                    .Append(_mlContext.Transforms.ResizeImages(
                        outputColumnName: "Image",
                        imageWidth: ImageWidth,
                        imageHeight: ImageHeight))
                    .Append(_mlContext.Transforms.ExtractPixels(
                        outputColumnName: "Features",
                        inputColumnName: "Image",
                        interleavePixelColors: true,
                        scaleImage: 1f / 255f))
                    .Append(_mlContext.Transforms.Conversion.MapValueToKey(
                        outputColumnName: "LabelKey",
                        inputColumnName: nameof(ImageData.Label)))
                    .AppendCacheCheckpoint(_mlContext)

                    //model
                    .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                        labelColumnName: "LabelKey",
                        featureColumnName: "Features"))
                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue(
                        outputColumnName: nameof(PredictionResult.PredictLabel),
                        inputColumnName: "PredictedLabel"));

                Console.WriteLine("Training started...");
                progressTimer = new Timer(_ =>
                {
                    Console.Write($"\rTraining timer: {stopwatch.Elapsed:mm\\:ss}");
                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

                _model = pipeline.Fit(dataView);

                Console.WriteLine();
                Console.WriteLine("Training completed.");

                Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
                _mlContext.Model.Save(_model, dataView.Schema, _modelPath);

                var testPredictions = _model.Transform(dataView);
                var metrics = _mlContext.MulticlassClassification.Evaluate(
                    testPredictions,
                    labelColumnName: "LabelKey",
                    scoreColumnName: nameof(PredictionResult.Score));

                stopwatch.Stop();
                Console.WriteLine($"Training time: {stopwatch.Elapsed:mm\\:ss}");
                Console.WriteLine($"Training MicroAccuracy: {metrics.MicroAccuracy:P2}");
                Console.WriteLine($"Training MacroAccuracy: {metrics.MacroAccuracy:P2}");
                Console.WriteLine($"Training LogLoss: {metrics.LogLoss:0.####}");

                var summary = new TrainingSummary
                {
                    ImageCount = imageCount,
                    LabelCount = labelCount,
                    Duration = stopwatch.Elapsed,
                    Trainer = TrainerName,
                    MicroAccuracy = metrics.MicroAccuracy,
                    MacroAccuracy = metrics.MacroAccuracy,
                    LogLoss = metrics.LogLoss
                };

                _lastTrainingSummary = summary;
                return summary;
            }
            finally
            {
                progressTimer?.Dispose();
                Console.WriteLine();
                _trainingLock.Release();
            }
        }

        public PredictionResult Predict(string imagePath)
        {
            if (_model == null)
            {
                throw new InvalidOperationException("Model has not been trained or loaded yet.");
            }

            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                throw new FileNotFoundException("Prediction image was not found.", imagePath);
            }

            var imageData = new ImageData { ImagePath = imagePath };
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageData, PredictionResult>(_model);
            return predictionEngine.Predict(imageData);
        }

        public static float GetPredictionConfidence(float[]? scores)
        {
            if (scores == null || scores.Length == 0)
            {
                return 0f;
            }

            var maxScore = scores.Max();
            var stabilized = scores.Select(score => MathF.Exp(score - maxScore)).ToArray();
            var total = stabilized.Sum();

            if (total <= 0f)
            {
                return 0f;
            }

            return stabilized.Max() / total;
        }

        private IEnumerable<ImageData> LoadImagesFromDirectory(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return Enumerable.Empty<ImageData>();
            }

            var images = new List<ImageData>();
            var directories = Directory.GetDirectories(folder);

            foreach (var directory in directories)
            {
                var label = Path.GetFileName(directory);
                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                    .Where(IsSupportedImageFile);

                foreach (var file in files)
                {
                    images.Add(new ImageData
                    {
                        ImagePath = file,
                        Label = label
                    });
                }
            }

            return images;
        }

        private static bool IsSupportedImageFile(string path)
        {
            return path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase);
        }
    }
}
