using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace AvecADeskApi.Helper
{
    public class FileUploadHelper
    {
        private readonly IWebHostEnvironment _environment;

        public FileUploadHelper(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string?> SaveFileAsync( IFormFile? file,string folderName)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var uploadFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "Upload", folderName);

            Directory.CreateDirectory(uploadFolder);

            var fileName = Path.GetFileName(file.FileName);

            var fullPath = Path.Combine(
                uploadFolder,
                fileName
            );

            await using var stream = new FileStream(
                fullPath,
                FileMode.Create
            );

            await file.CopyToAsync(stream);

            return $"wwwroot/Upload/{folderName}/{fileName}";
        }
    }
 }