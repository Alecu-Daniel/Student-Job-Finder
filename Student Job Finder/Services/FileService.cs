namespace Student_Job_Finder.Services
{
    public class FileService
    {
        private readonly IWebHostEnvironment _env;
        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (file == null) return null;

            var folderPath = Path.Combine(_env.WebRootPath, "UserUploads",subFolder);
            if(!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        public void DeleteFile(string fileName, string subFolder)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            var filePath = Path.Combine(_env.WebRootPath, "UserUploads", subFolder, fileName);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }
}
