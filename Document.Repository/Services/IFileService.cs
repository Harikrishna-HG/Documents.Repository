namespace Document.Repository.Services
{
    public interface IFileService
    {
        public Task<string> SaveFileAsync(IFormFile file, string folderName, string[] allowedExtensions);
        public void DeleteFile(string filePath);
    }
}
