namespace ImageClassificationAPI.Helpers
{
    public static class ImageHelper
    {
        public static string SaveImage(IFormFile file, string rootPath)
        {
            var folder = Path.Combine(rootPath, "wwwroot", "uploads");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return filePath;
        }
    }
}