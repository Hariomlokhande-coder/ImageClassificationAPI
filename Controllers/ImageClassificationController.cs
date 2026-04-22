using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ImageClassificationAPI.Services;
using ImageClassificationAPI.Helpers;
using ImageClassificationAPI.Models;

namespace ImageClassificationAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ImageClassificationController : ControllerBase
    {
        private readonly ImageClassificationService _service;
        private readonly IWebHostEnvironment _env;

        public ImageClassificationController(ImageClassificationService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        [Authorize]
        [HttpPost("predict")]
        public IActionResult Predict(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid image file.");

            try
            {
                var path = ImageHelper.SaveImage(file, _env.ContentRootPath);
                var prediction = _service.Predict(path);
                var confidence = ImageClassificationService.GetPredictionConfidence(prediction.Score);

                return Ok(new
                {
                    Category = prediction.PredictLabel,
                    Confidence = (confidence * 100).ToString("0.##") + "%",
                    ImagePath = "/uploads/" + Path.GetFileName(path)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("train")]
        public IActionResult Train()
        {           
            try
            {
                var trainDataPath = Path.Combine(_env.ContentRootPath, "Dataset");

                if (!Directory.Exists(trainDataPath))
                {
                    return BadRequest("Training data directory not found in Dataset (project root).");
                }

                var summary = _service.TrainModel(trainDataPath);
                return Ok(new
                {
                    Message = "Training completed successfully.",
                    Trainer = summary.Trainer,
                    ImageCount = summary.ImageCount,
                    LabelCount = summary.LabelCount,
                    TrainingTime = summary.Duration.ToString(@"mm\:ss"),
                    MicroAccuracy = (summary.MicroAccuracy * 100).ToString("0.##") + "%",
                    MacroAccuracy = (summary.MacroAccuracy * 100).ToString("0.##") + "%",
                    LogLoss = summary.LogLoss.ToString("0.####")
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Training failed: " + ex.Message });
            }
        }

        [Authorize]
        [HttpGet("summary")]
        public IActionResult Summary()
        {
            var summary = _service.GetLastTrainingSummary();
            if (summary == null)
            {
                return Ok(new
                {
                    HasSummary = false
                });
            }

            return Ok(new
            {
                HasSummary = true,
                Trainer = summary.Trainer,
                ImageCount = summary.ImageCount,
                LabelCount = summary.LabelCount,
                TrainingTime = summary.Duration.ToString(@"mm\:ss"),
                MicroAccuracy = (summary.MicroAccuracy * 100).ToString("0.##") + "%",
                MacroAccuracy = (summary.MacroAccuracy * 100).ToString("0.##") + "%",
                LogLoss = summary.LogLoss.ToString("0.####")
            });
        }
    }
}
