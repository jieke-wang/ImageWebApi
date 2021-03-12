using System.IO;
using System;
using ImageWebApi.Libs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

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
        [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "md5" })]
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
        [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "md5", "bg", "q" })]
        public async Task<IActionResult> GetAsync([FromRoute] string project, [FromRoute] string width, [FromRoute] string height, [FromQuery] string bg, [FromQuery] string q, [FromRoute] string filename)
        {
            if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(filename)) return await GetDefaultImageAsync();
            project = project.ToLower();
            filename = filename.ToLower();
            var imageOriginalPath = Path.Combine(_OriginalBaseImageDir, project, filename);
            if (System.IO.File.Exists(imageOriginalPath) == false) return await GetDefaultImageAsync();
            if (int.TryParse(width, out int _width) == false || int.TryParse(height, out int _height) == false) return await GetDefaultImageAsync();

            string destinationDir = Path.Combine(_environment.ContentRootPath, _configuration["ImageApiSetting:ImageCacheRootDir"], project, $"{_width}x{_height}");
            string destinationFilename = FileHelper.ToValidFileName($"{bg}_{q}_{filename}");
            string imageDestinationPath = Path.Combine(destinationDir, destinationFilename);
            if (System.IO.File.Exists(imageDestinationPath))
            {
                new FileExtensionContentTypeProvider().TryGetContentType(filename, out string contentType);
                return File(await System.IO.File.ReadAllBytesAsync(imageDestinationPath), contentType ?? "application/octet-stream");
            }
            if (Directory.Exists(destinationDir) == false) Directory.CreateDirectory(destinationDir);

            ImageCompressV2 imgCompress = ImageCompressV2.GetImageCompressObject;
            imgCompress.GetImage = new System.Drawing.Bitmap(imageOriginalPath);
            imgCompress.Height = _height;
            imgCompress.Width = _width;
            imgCompress.BackgrouColor = bg;
            imgCompress.Quantity = q;
            imgCompress.Save(destinationFilename, destinationDir);

            if (System.IO.File.Exists(imageDestinationPath) == false) return await GetDefaultImageAsync();
            return await GetSpecialImageAsync(imageDestinationPath, destinationFilename);
        }

        [NonAction]
        private async Task<IActionResult> GetDefaultImageAsync()
        {
            Response.StatusCode = 404;
            //ControllerContext.HttpContext.Items.Add("NotFound", bool.TrueString);

            ControllerContext.HttpContext.Response.GetTypedHeaders().CacheControl =
            new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10),
            };
            ControllerContext.HttpContext.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
                new string[] { "Accept-Encoding" };

            return File(await System.IO.File.ReadAllBytesAsync(Path.Combine(_OriginalBaseImageDir, _configuration["ImageApiSetting:DefaultImage"])), _configuration["ImageApiSetting:DefaultImageMimeType"]);
        }

        [NonAction]
        private async Task<IActionResult> GetSpecialImageAsync(string imageFullPath, string filename)
        {
            new FileExtensionContentTypeProvider().TryGetContentType(filename, out string contentType);
            return File(await System.IO.File.ReadAllBytesAsync(imageFullPath), contentType ?? "application/octet-stream");
        }

        [HttpPost("{project}")]
        public async Task<IActionResult> PostAsync([FromRoute] string project, [FromForm] IFormFile file, [FromHeader] string token)
        {
            if (string.Equals(token, _configuration["ImageApiSetting:Token"]) == false) return Unauthorized();
            if (file == null || file.Length == 0) return BadRequest();

            string fileName = FileHelper.ToValidFileName(file.FileName);
            var imageDirPath = FileHelper.ToValidFilePath(Path.Combine(_OriginalBaseImageDir, project));
            var imagePath = Path.Combine(imageDirPath, fileName);
            if (System.IO.File.Exists(imagePath))
            {
                fileName = FileHelper.AddRandomToFilename(fileName);
                imagePath = Path.Combine(imageDirPath, fileName);
            }

            if (Directory.Exists(imageDirPath) == false) Directory.CreateDirectory(imageDirPath);

            using (FileStream fs = new FileStream(imagePath, FileMode.CreateNew, FileAccess.Write, FileShare.Delete))
            {
                await file.CopyToAsync(fs);
                await fs.FlushAsync();
            }

            string md5Hash = await FileHelper.GetHashFromFileAsync(imagePath, FileHelper.Algorithms.MD5);

            return new JsonResult(new
            {
                fileName,
                contentType = file.ContentType,
                size = file.Length,
                md5Hash
            });
        }

        [HttpPost("{project}/with_check_hash")]
        public async Task<IActionResult> PostWithCheckHashAsync([FromRoute] string project, [FromForm] IFormFile file, [FromHeader] string token, [FromHeader] string overwrite)
        {
            if (string.Equals(token, _configuration["ImageApiSetting:Token"]) == false) return Unauthorized();
            if (file == null || file.Length == 0) return BadRequest();

            string fileName = FileHelper.ToValidFileName(file.FileName);
            var imageDirPath = FileHelper.ToValidFilePath(Path.Combine(_OriginalBaseImageDir, project));
            var imagePath = Path.Combine(imageDirPath, fileName);
            bool needCheck = false;
            if (System.IO.File.Exists(imagePath))
            {
                needCheck = true;
            }

            if (Directory.Exists(imageDirPath) == false) Directory.CreateDirectory(imageDirPath);

            if (needCheck)
            {
                string tempFilename = Path.GetTempFileName();
                using (FileStream fs = new FileStream(tempFilename, FileMode.Truncate, FileAccess.Write, FileShare.Delete))
                {
                    await file.CopyToAsync(fs);
                    await fs.FlushAsync();
                }

                string oldHash = await FileHelper.GetHashFromFileAsync(imagePath, FileHelper.Algorithms.MD5);
                string newHash = await FileHelper.GetHashFromFileAsync(tempFilename, FileHelper.Algorithms.MD5);
                if (string.Equals(oldHash, newHash) == false)
                {
                    if (string.Equals(overwrite, "overwrite", StringComparison.OrdinalIgnoreCase) == false)
                    {
                        fileName = FileHelper.AddRandomToFilename(fileName);
                    }
                    imagePath = Path.Combine(imageDirPath, fileName);
                }

                System.IO.File.Move(tempFilename, imagePath, true);
            }
            else
            {
                using (FileStream fs = new FileStream(imagePath, FileMode.CreateNew, FileAccess.Write, FileShare.Delete))
                {
                    await file.CopyToAsync(fs);
                    await fs.FlushAsync();
                }
            }

            string md5Hash = await FileHelper.GetHashFromFileAsync(imagePath, FileHelper.Algorithms.MD5);

            return new JsonResult(new
            {
                fileName,
                contentType = file.ContentType,
                size = file.Length,
                md5Hash
            });
        }

        [HttpPost("{project}/remote")]
        public async Task<IActionResult> PostAsync([FromRoute] string project, [FromForm] string url, [FromServices] IHttpClientFactory httpClientFactory, [FromHeader] string token)
        {
            if (string.Equals(token, _configuration["ImageApiSetting:Token"]) == false) return Unauthorized();
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) == false) return BadRequest();

            string fileName = FileHelper.ToValidFileName(Path.GetFileName(url));
            var imageDirPath = FileHelper.ToValidFilePath(Path.Combine(_OriginalBaseImageDir, project));
            var imagePath = Path.Combine(imageDirPath, fileName);
            if (System.IO.File.Exists(imagePath))
            {
                fileName = FileHelper.AddRandomToFilename(fileName);
                imagePath = Path.Combine(imageDirPath, fileName);
            }

            int size;
            try
            {
                HttpClient httpClient = httpClientFactory.CreateClient("RemoteIamgePoolClient");
                using Stream stream = await httpClient.GetStreamAsync(uri);

                if (Directory.Exists(imageDirPath) == false) Directory.CreateDirectory(imageDirPath);
                using (FileStream fs = new FileStream(imagePath, FileMode.CreateNew, FileAccess.Write, FileShare.Delete))
                {
                    await stream.CopyToAsync(fs);
                    await fs.FlushAsync();
                    size = Convert.ToInt32(fs.Position);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

            string md5Hash = await FileHelper.GetHashFromFileAsync(imagePath, FileHelper.Algorithms.MD5);

            return new JsonResult(new
            {
                fileName,
                size,
                md5Hash
            });
        }

        [HttpPost("{project}/remote_with_check_hash")]
        public async Task<IActionResult> PostWithCheckHashAsync([FromRoute] string project, [FromForm] string url, [FromServices] IHttpClientFactory httpClientFactory, [FromHeader] string token, [FromHeader] string overwrite)
        {
            if (string.Equals(token, _configuration["ImageApiSetting:Token"]) == false) return Unauthorized();
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) == false) return BadRequest();

            string fileName = FileHelper.ToValidFileName(Path.GetFileName(url));
            var imageDirPath = FileHelper.ToValidFilePath(Path.Combine(_OriginalBaseImageDir, project));
            var imagePath = Path.Combine(imageDirPath, fileName);
            bool needCheck = false;
            if (System.IO.File.Exists(imagePath))
            {
                needCheck = true;
            }

            int size;
            try
            {
                HttpClient httpClient = httpClientFactory.CreateClient("RemoteIamgePoolClient");
                using Stream stream = await httpClient.GetStreamAsync(uri);

                if (Directory.Exists(imageDirPath) == false) Directory.CreateDirectory(imageDirPath);

                if (needCheck)
                {
                    string tempFilename = Path.GetTempFileName();
                    using (FileStream fs = new FileStream(tempFilename, FileMode.Truncate, FileAccess.Write, FileShare.Delete))
                    {
                        await stream.CopyToAsync(fs);
                        await fs.FlushAsync();
                        size = Convert.ToInt32(fs.Position);
                    }

                    string oldHash = await FileHelper.GetHashFromFileAsync(imagePath, FileHelper.Algorithms.MD5);
                    string newHash = await FileHelper.GetHashFromFileAsync(tempFilename, FileHelper.Algorithms.MD5);
                    if (string.Equals(oldHash, newHash) == false)
                    {
                        if (string.Equals(overwrite, "overwrite", StringComparison.OrdinalIgnoreCase) == false)
                        {
                            fileName = FileHelper.AddRandomToFilename(fileName);
                        }
                        imagePath = Path.Combine(imageDirPath, fileName);
                    }

                    System.IO.File.Move(tempFilename, imagePath, true);
                }
                else
                {
                    using (FileStream fs = new FileStream(imagePath, FileMode.CreateNew, FileAccess.Write, FileShare.Delete))
                    {
                        await stream.CopyToAsync(fs);
                        await fs.FlushAsync();
                        size = Convert.ToInt32(fs.Position);
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

            string md5Hash = await FileHelper.GetHashFromFileAsync(imagePath, FileHelper.Algorithms.MD5);

            return new JsonResult(new
            {
                fileName,
                size,
                md5Hash
            });
        }

        [HttpDelete("{project}/{filename}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] string project, [FromRoute] string filename, [FromHeader] string token)
        {
            if (string.Equals(token, _configuration["ImageApiSetting:Token"]) == false) return Unauthorized();
            if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(filename)) return BadRequest();

            project = project.ToLower();
            filename = filename.ToLower();
            var imagePath = Path.Combine(_OriginalBaseImageDir, project, filename);
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }

            string destinationDir = Path.Combine(_environment.ContentRootPath, _configuration["ImageApiSetting:ImageCacheRootDir"], project);
            var directories = Directory.GetDirectories(destinationDir);
            void DeleteFile(string directory)
            {
                foreach (var _filename in Directory.EnumerateFiles(directory, $"*{filename}", SearchOption.TopDirectoryOnly))
                {
                    //Console.WriteLine(filename);
                    System.IO.File.Delete(_filename);
                }
            }

            #region “Ï≤Ω
            Task[] tasks = new Task[directories.Length];
            for (int i = 0; i < directories.Length; i++)
            {
                string directory = directories[i];
                tasks[i] = Task.Run(() => DeleteFile(directory));
            }
            await Task.WhenAll(tasks);
            #endregion

            #region Õ¨≤Ω
            //for (int i = 0; i < directories.Length; i++)
            //{
            //    string directory = directories[i];
            //    DeleteFile(directory);
            //} 
            #endregion

            return new JsonResult(new
            {
                filename
            });
        }
    }
}
