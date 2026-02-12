using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MesTechStok.Desktop.Services
{
    public class DocumentStorageService
    {
        private readonly string _baseFolder;

        public DocumentStorageService()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _baseFolder = Path.Combine(local, "MesTechStok", "Docs", "Customers");
            Directory.CreateDirectory(_baseFolder);
        }

        public string GetCustomerFolder(int customerId)
        {
            var dir = Path.Combine(_baseFolder, (customerId <= 0 ? "Temp" : customerId.ToString()));
            Directory.CreateDirectory(dir);
            return dir;
        }

        public async Task<string?> SaveAsync(int customerId, string sourcePathOrUrl)
        {
            try
            {
                var folder = GetCustomerFolder(customerId);
                string fileName = MakeSafeFileName(Path.GetFileName(sourcePathOrUrl));
                if (string.IsNullOrWhiteSpace(fileName)) fileName = $"doc_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
                var dest = Path.Combine(folder, fileName);

                if (Uri.TryCreate(sourcePathOrUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                    using var resp = await http.GetAsync(uri);
                    resp.EnsureSuccessStatusCode();
                    await using var fs = File.Open(dest, FileMode.Create, FileAccess.Write, FileShare.None);
                    await resp.Content.CopyToAsync(fs);
                }
                else
                {
                    if (File.Exists(sourcePathOrUrl))
                    {
                        File.Copy(sourcePathOrUrl, dest, overwrite: true);
                    }
                    else
                    {
                        // Metinsel link ise içeriğini .url olarak kaydedelim
                        await File.WriteAllTextAsync(dest + ".url", sourcePathOrUrl);
                        return dest + ".url";
                    }
                }
                return dest;
            }
            catch
            {
                return null;
            }
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }
    }
}


