namespace BLL.Helper
{
    public static class Upload
    {
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public static async Task<string> UploadFile(string FolderName, IFormFile File)
        {
            try
            {
                // Validation
                if (File == null || File.Length == 0)
                    return "ERROR: File is empty";

                if (File.Length > MaxFileSize)
                    return $"ERROR: File size exceeds {MaxFileSize / (1024 * 1024)}MB limit";

                var extension = Path.GetExtension(File.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                    return $"ERROR: Invalid file type. Allowed: {string.Join(", ", AllowedImageExtensions)}";

                // 1) Get Directory
                string FolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", FolderName);

                // Create directory if not exists
                Directory.CreateDirectory(FolderPath);

                // 2) Get File Name - secure it
                string FileName = Guid.NewGuid() + extension; // Use only extension, not full filename

                // 3) Merge Path with File Name
                string FinalPath = Path.Combine(FolderPath, FileName);

                // 4) Save File Asynchronously
                await using (var Stream = new FileStream(FinalPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await File.CopyToAsync(Stream);
                }

                return FileName;
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        public static async Task<string> RemoveFile(string folderName, string fileName)
        {
            try
            {
                // Security: Validate filename to prevent path traversal
                if (string.IsNullOrWhiteSpace(fileName) ||
                    fileName.Contains("..") ||
                    fileName.Contains("/") ||
                    fileName.Contains("\\"))
                {
                    return "ERROR: Invalid filename";
                }

                // Combine the correct path to the file
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderName, fileName);

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    return "File Deleted Successfully";
                }

                return "File Not Found";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        // Helper method to check if result is error
        public static bool IsError(string result)
        {
            return result.StartsWith("ERROR:");
        }
    }
}