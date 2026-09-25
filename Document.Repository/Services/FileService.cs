namespace Document.Repository.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FileService> _logger;
        
        // Maximum file size: 10MB for images, 50MB for PDFs
        private const long MaxImageSize = 10 * 1024 * 1024;
        private const long MaxDocumentSize = 50 * 1024 * 1024;

        private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
        {
            { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
            { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }
        };

        public FileService(IWebHostEnvironment webHostEnvironment, ILogger<FileService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName, string[] allowedExtensions)
        {
            // Check if the file is valid
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is not valid.");
            }

            // Validate file size
            long maxSize = allowedExtensions.Contains(".pdf") ? MaxDocumentSize : MaxImageSize;
            if (file.Length > maxSize)
            {
                throw new ArgumentException($"File size exceeds maximum allowed size of {maxSize / (1024 * 1024)}MB.");
            }

            // Validate the file extension
            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Invalid file type. Allowed extensions are: " + string.Join(", ", allowedExtensions));
            }

            // Validate file signature (magic bytes)
            if (!await IsValidFileSignature(file, fileExtension))
            {
                _logger.LogWarning("File upload rejected: Invalid file signature for {Extension}", fileExtension);
                throw new ArgumentException("File content does not match its extension. Possible security threat.");
            }

            // Sanitize filename - remove any path characters
            string safeFileName = Path.GetFileName(file.FileName);
            
            // Generate a unique file name
            string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, folderName);

            // Ensure the directory exists
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Prevent path traversal
            if (!filePath.StartsWith(_webHostEnvironment.WebRootPath))
            {
                throw new ArgumentException("Invalid file path.");
            }

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(fileStream);
            }

            _logger.LogInformation("File uploaded successfully: {FileName}", uniqueFileName);

            // Return the relative path for database storage
            return Path.Combine(folderName, uniqueFileName).Replace("\\", "/");
        }

        private async Task<bool> IsValidFileSignature(IFormFile file, string extension)
        {
            if (!_fileSignatures.ContainsKey(extension))
            {
                return true; // If no signature defined, skip validation
            }

            using (var reader = new BinaryReader(file.OpenReadStream()))
            {
                var signatures = _fileSignatures[extension];
                var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));

                return signatures.Any(signature =>
                    headerBytes.Take(signature.Length).SequenceEqual(signature));
            }
        }


        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            if (Path.IsPathRooted(filePath))
            {
                throw new ArgumentException("Invalid file path.");
            }

            string root = Path.GetFullPath(_webHostEnvironment.WebRootPath);
            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(Path.Combine(root, filePath));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new ArgumentException("Invalid file path.", ex);
            }

            if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid file path.");
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
