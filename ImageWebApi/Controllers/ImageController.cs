using System.IO;
using System;
using ImageWebApi.Libs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;

namespace ImageWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly string _OriginalBaseImageDir;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        public ImageController(IWebHostEnvironment env, IConfiguration configuration)
        {
            _OriginalBaseImageDir = Path.Combine(env.ContentRootPath, configuration["ImageApiSetting:ImageRootDir"]);
            _configuration = configuration;
            _environment = env;
        }

        [HttpGet("{project}/{filename}")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetAsync([FromRoute] string project, [FromRoute] string filename)
        {
            if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(filename)) return await GetDefaultImageAsync();
            project = project.ToLower();
            filename = filename.ToLower();
            var imagePath = Path.Combine(_OriginalBaseImageDir, project, filename);
            if (System.IO.File.Exists(imagePath) == false) return await GetDefaultImageAsync();
            return await GetSpecialImageAsync(imagePath, filename);
        }

        [HttpGet("{project}/{width}x{height}/{filename}")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetAsync([FromRoute] string project, [FromRoute] string width, [FromRoute] string height, [FromRoute] string filename)
        {
            if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(filename)) return await GetDefaultImageAsync();
            project = project.ToLower();
            filename = filename.ToLower();
            var imageOriginalPath = Path.Combine(_OriginalBaseImageDir, project, filename);
            if (System.IO.File.Exists(imageOriginalPath) == false) return await GetDefaultImageAsync();
            if (int.TryParse(width, out int _width) == false || int.TryParse(height, out int _height) == false) return await GetDefaultImageAsync();

            string destinationDir = Path.Combine(_environment.ContentRootPath, _configuration["ImageApiSetting:ImageCacheRootDir"], project, $"{_width}x{_height}");
            string imageDestinationPath = Path.Combine(destinationDir, filename);
            if (System.IO.File.Exists(imageDestinationPath))
            {
                new FileExtensionContentTypeProvider().TryGetContentType(filename, out string contentType);
                return File(await System.IO.File.ReadAllBytesAsync(imageDestinationPath), contentType ?? "application/octet-stream");
            }
            if (Directory.Exists(destinationDir) == false) Directory.CreateDirectory(destinationDir);

            ImageCompress imgCompress = ImageCompress.GetImageCompressObject;
            imgCompress.GetImage = new System.Drawing.Bitmap(imageOriginalPath);
            imgCompress.Height = _height;
            imgCompress.Width = _width;
            imgCompress.Save(filename, destinationDir);

            if (System.IO.File.Exists(imageDestinationPath) == false) return await GetDefaultImageAsync();
            return await GetSpecialImageAsync(imageDestinationPath, filename);
        }

        [NonAction]
        private async Task<IActionResult> GetDefaultImageAsync()
        {
            return File(await System.IO.File.ReadAllBytesAsync(Path.Combine(_OriginalBaseImageDir, _configuration["ImageApiSetting:DefaultImage"])), _configuration["ImageApiSetting:DefaultImageMimeType"]);
        }

        [NonAction]
        private async Task<IActionResult> GetSpecialImageAsync(string imageFullPath, string filename)
        {
            new FileExtensionContentTypeProvider().TryGetContentType(filename, out string contentType);
            return File(await System.IO.File.ReadAllBytesAsync(imageFullPath), contentType ?? "application/octet-stream");
        }
    }
}
