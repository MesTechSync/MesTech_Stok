using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MesTechStok.Desktop.Services
{
    public class ImageStorageService
    {
        private readonly string _baseFolder;
        private readonly ILogger<ImageStorageService>? _logger;
        private static readonly string[] AllowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tiff" };
        private static readonly string[] DangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".jar", ".dll", ".scr", ".com", ".pif" };
        private static readonly byte[][] ImageMagicNumbers = new[]
        {
            new byte[] { 0xFF, 0xD8, 0xFF }, // JPEG
            new byte[] { 0x89, 0x50, 0x4E, 0x47 }, // PNG
            new byte[] { 0x47, 0x49, 0x46, 0x38 }, // GIF
            new byte[] { 0x42, 0x4D }, // BMP
            new byte[] { 0x52, 0x49, 0x46, 0x46 }, // WEBP (starts with RIFF)
        };
        private const int MaxDownloadMb = 25; // SECURITY: Reduced from 50 to 25 MB
        private const int MaxDownloadPixels = 50000000; // SECURITY: Max 50MP (e.g., 7071x7071)
        private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(10); // SECURITY: Reduced from 15 to 10 seconds
        private static readonly string[] AllowedDomains = new[] { "imgur.com", "images.unsplash.com", "via.placeholder.com", "picsum.photos" }; // SECURITY: Whitelist

        public ImageStorageService(ILogger<ImageStorageService>? logger = null)
        {
            _logger = logger;
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _baseFolder = Path.Combine(local, "MesTechStok", "Images", "Products");
            Directory.CreateDirectory(_baseFolder);
        }

        public string GetBaseFolder() => _baseFolder;

        public string GetProductFolder(int productId)
        {
            // SECURITY: Validate product ID range
            if (productId <= 0 || productId > 2000000000) // Realistic max product ID
            {
                throw new ArgumentException($"Invalid product ID: {productId}");
            }

            var dir = Path.Combine(_baseFolder, productId.ToString());

            // SECURITY FIX: Enhanced path injection protection
            var fullPath = Path.GetFullPath(dir);
            var baseFullPath = Path.GetFullPath(_baseFolder);
            if (!fullPath.StartsWith(baseFullPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogWarning("Path injection attempt detected: {AttemptedPath}", dir);
                throw new UnauthorizedAccessException("Invalid path: Potential path injection detected");
            }

            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// SECURITY: Validates if URL is from trusted domain
        /// </summary>
        private static bool IsAllowedDomain(Uri uri)
        {
            var host = uri.Host.ToLowerInvariant();
            foreach (var allowedDomain in AllowedDomains)
            {
                if (host == allowedDomain || host.EndsWith($".{allowedDomain}"))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// SECURITY: Validates file extension against dangerous executables
        /// </summary>
        private static bool IsSafeFileExtension(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) return false;

            // Check if it's a dangerous extension
            foreach (var dangerousExt in DangerousExtensions)
            {
                if (ext == dangerousExt) return false;
            }

            // Check if it's an allowed image extension
            foreach (var allowedExt in AllowedImageExtensions)
            {
                if (ext == allowedExt) return true;
            }

            return false;
        }

        /// <summary>
        /// SECURITY: Validates file content by magic numbers (file signatures)
        /// </summary>
        private static bool IsValidImageFile(byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length < 4) return false;

            foreach (var magicNumber in ImageMagicNumbers)
            {
                if (fileBytes.Length >= magicNumber.Length)
                {
                    bool matches = true;
                    for (int i = 0; i < magicNumber.Length; i++)
                    {
                        if (fileBytes[i] != magicNumber[i])
                        {
                            matches = false;
                            break;
                        }
                    }
                    if (matches) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// SECURITY: Generate secure filename to prevent file system attacks
        /// </summary>
        private static string GenerateSecureFileName(string originalName)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(originalName + DateTime.UtcNow.Ticks));
            var hashString = Convert.ToBase64String(hash).Replace("/", "_").Replace("+", "-").Substring(0, 16);
            var ext = Path.GetExtension(originalName).ToLowerInvariant();
            return $"img_{hashString}{ext}";
        }

        public async Task<ImageSaveResult> SaveAsync(int productId, string sourcePathOrUrl)
        {
            var folder = GetProductFolder(productId);
            var secureFileName = GenerateSecureFileName($"product_{productId}_original.jpg");
            string originalPath = Path.Combine(folder, secureFileName);

            try
            {
                byte[] bytes;
                if (Uri.TryCreate(sourcePathOrUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    // SECURITY: Domain whitelist validation
                    if (!IsAllowedDomain(uri))
                    {
                        _logger?.LogWarning("Attempt to download from untrusted domain: {Domain}", uri.Host);
                        return new ImageSaveResult { Folder = folder, Error = "Domain not allowed" };
                    }

                    // SECURITY: URL path validation
                    if (!IsSafeFileExtension(uri.AbsolutePath))
                    {
                        _logger?.LogWarning("Unsafe file extension in URL: {Url}", sourcePathOrUrl);
                        return new ImageSaveResult { Folder = folder, Error = "Unsafe file extension" };
                    }

                    using var http = new HttpClient { Timeout = HttpTimeout };
                    // SECURITY: Add security headers
                    http.DefaultRequestHeaders.Add("User-Agent", "MesTechStok/1.0 ImageService");

                    using var resp = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                    resp.EnsureSuccessStatusCode();

                    var contentLength = resp.Content.Headers.ContentLength;
                    if (contentLength.HasValue && contentLength.Value > MaxDownloadMb * 1024L * 1024L)
                    {
                        _logger?.LogWarning("File too large: {Size} MB", contentLength.Value / (1024L * 1024L));
                        return new ImageSaveResult { Folder = folder, Error = "File too large" };
                    }

                    // SECURITY: Stream with size limit to prevent memory exhaustion
                    await using var stream = await resp.Content.ReadAsStreamAsync();
                    await using var ms = new MemoryStream();
                    var buffer = new byte[8192]; // Smaller buffer for better control
                    int read;
                    long total = 0;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        total += read;
                        if (total > MaxDownloadMb * 1024L * 1024L)
                        {
                            _logger?.LogWarning("Download size limit exceeded: {Size} bytes", total);
                            return new ImageSaveResult { Folder = folder, Error = "Download size limit exceeded" };
                        }
                        await ms.WriteAsync(buffer, 0, read);
                    }
                    bytes = ms.ToArray();
                }
                else
                {
                    // SECURITY: Local file validation
                    if (!IsSafeFileExtension(sourcePathOrUrl))
                    {
                        _logger?.LogWarning("Unsafe file extension: {FilePath}", sourcePathOrUrl);
                        return new ImageSaveResult { Folder = folder, Error = "Unsafe file extension" };
                    }

                    var fi = new FileInfo(sourcePathOrUrl);
                    if (!fi.Exists)
                    {
                        return new ImageSaveResult { Folder = folder, Error = "File not found" };
                    }

                    if (fi.Length > MaxDownloadMb * 1024L * 1024L)
                    {
                        _logger?.LogWarning("Local file too large: {Size} MB", fi.Length / (1024L * 1024L));
                        return new ImageSaveResult { Folder = folder, Error = "File too large" };
                    }

                    bytes = await File.ReadAllBytesAsync(sourcePathOrUrl);
                }

                // SECURITY: Validate file content by magic numbers
                if (!IsValidImageFile(bytes))
                {
                    _logger?.LogWarning("Invalid image file format detected for product {ProductId}", productId);
                    return new ImageSaveResult { Folder = folder, Error = "Invalid image format" };
                }

                // SECURITY: Validate image dimensions to prevent zip bomb attacks
                try
                {
                    using var ms = new MemoryStream(bytes);
                    var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    if (decoder.Frames.Count > 0)
                    {
                        var frame = decoder.Frames[0];
                        var totalPixels = (long)frame.PixelWidth * frame.PixelHeight;
                        if (totalPixels > MaxDownloadPixels)
                        {
                            _logger?.LogWarning("Image dimensions too large: {Width}x{Height} = {Pixels} pixels",
                                frame.PixelWidth, frame.PixelHeight, totalPixels);
                            return new ImageSaveResult { Folder = folder, Error = "Image dimensions too large" };
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error validating image dimensions for product {ProductId}", productId);
                    return new ImageSaveResult { Folder = folder, Error = "Image validation failed" };
                }

                await File.WriteAllBytesAsync(originalPath, bytes);

                // Derivates with secure filenames
                var thumb128 = Path.Combine(folder, GenerateSecureFileName($"product_{productId}_thumb128.jpg"));
                var thumb256 = Path.Combine(folder, GenerateSecureFileName($"product_{productId}_thumb256.jpg"));
                var preview768 = Path.Combine(folder, GenerateSecureFileName($"product_{productId}_preview768.jpg"));
                var full1200 = Path.Combine(folder, GenerateSecureFileName($"product_{productId}_full1200.jpg"));

                await SaveResizedJpegAsync(originalPath, thumb128, 128, 128, quality: 90);
                await SaveResizedJpegAsync(originalPath, thumb256, 256, 256, quality: 90);
                await SaveResizedJpegAsync(originalPath, preview768, 768, 768, quality: 92);
                await SaveFixedCanvasJpegAsync(originalPath, full1200, 1200, 1800, quality: 92);

                _logger?.LogInformation("Successfully processed image for product {ProductId}", productId);

                return new ImageSaveResult
                {
                    Folder = folder,
                    Original = originalPath,
                    Thumb128 = thumb128,
                    Thumb256 = thumb256,
                    Preview768 = preview768,
                    Full1200 = full1200
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ImageStorageService.SaveAsync failed for product {ProductId}: {Error}", productId, ex.Message);
                return new ImageSaveResult { Folder = folder, Error = ex.Message };
            }
        }

        private static async Task SaveResizedJpegAsync(string inputPath, string outputPath, int maxWidth, int maxHeight, int quality)
        {
            try
            {
                await Task.Run(() =>
                {
                    using var fs = File.OpenRead(inputPath);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = fs;
                    bmp.EndInit();
                    bmp.Freeze();

                    double scale = Math.Min((double)maxWidth / bmp.PixelWidth, (double)maxHeight / bmp.PixelHeight);
                    if (double.IsInfinity(scale) || double.IsNaN(scale) || scale > 1.0) scale = 1.0;

                    var tb = new TransformedBitmap(bmp, new ScaleTransform(scale, scale));
                    tb.Freeze();

                    var encoder = new JpegBitmapEncoder { QualityLevel = quality };
                    encoder.Frames.Add(BitmapFrame.Create(tb));
                    using var outFs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    encoder.Save(outFs);
                });
            }
            catch (Exception ex)
            {
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogError($"SaveResizedJpegAsync failed: {inputPath} -> {outputPath}: {ex.Message}", nameof(ImageStorageService)); } catch { }
                throw; // Re-throw to let calling method handle
            }
        }

        // Sabit kanvas: hedef boyutu tam doldurur (UniformToFill). Yatay/dikey fark etmeksizin çıktı aynı ölçü olur.
        private static async Task SaveFixedCanvasJpegAsync(string inputPath, string outputPath, int targetWidth, int targetHeight, int quality)
        {
            try
            {
                await Task.Run(() =>
                {
                    using var fs = File.OpenRead(inputPath);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = fs;
                    bmp.EndInit();
                    bmp.Freeze();

                    double scale = Math.Max((double)targetWidth / bmp.PixelWidth, (double)targetHeight / bmp.PixelHeight);
                    double scaledW = bmp.PixelWidth * scale;
                    double scaledH = bmp.PixelHeight * scale;
                    double offsetX = (targetWidth - scaledW) / 2.0;
                    double offsetY = (targetHeight - scaledH) / 2.0;

                    var dv = new System.Windows.Media.DrawingVisual();
                    using (var dc = dv.RenderOpen())
                    {
                        dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new System.Windows.Rect(0, 0, targetWidth, targetHeight));
                        dc.DrawImage(bmp, new System.Windows.Rect(offsetX, offsetY, scaledW, scaledH));
                    }
                    var rtb = new RenderTargetBitmap(targetWidth, targetHeight, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(dv);
                    rtb.Freeze();

                    var encoder = new JpegBitmapEncoder { QualityLevel = quality };
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using var outFs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    encoder.Save(outFs);
                });
            }
            catch (Exception ex)
            {
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogError($"SaveFixedCanvasJpegAsync failed: {inputPath} -> {outputPath}: {ex.Message}", nameof(ImageStorageService)); } catch { }
                throw; // Re-throw to let calling method handle
            }
        }
    }

    public class ImageSaveResult
    {
        public string Folder { get; set; } = string.Empty;
        public string? Original { get; set; }
        public string? Thumb128 { get; set; }
        public string? Thumb256 { get; set; }
        public string? Preview768 { get; set; }
        public string? Full1200 { get; set; }
        public string? Error { get; set; } // SECURITY: Error message for failed operations
    }
}


