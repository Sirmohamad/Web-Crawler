using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using WebCrawler.Models;

namespace WebCrawler.Services;

public class FileDownloader
{
    private readonly HttpClient _httpClient;
    private readonly CrawlerConfig _config;
    private readonly HashSet<string> _downloadedHashes = new();

    public FileDownloader(CrawlerConfig config)
    {
        _config = config;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        
        // Ensure download folder exists
        if (!Directory.Exists(_config.DownloadPath))
        {
            Directory.CreateDirectory(_config.DownloadPath);
        }
    }

    /// <summary>
    /// Download file based on URL
    /// </summary>
    public async Task<string?> DownloadFileAsync(string url, PageNode node)
    {
        try
        {
            var absoluteUrl = GetAbsoluteUrl(url, node.Url);
            
            if (!IsValidFileUrl(absoluteUrl))
                return null;

            var uri = new Uri(absoluteUrl);
            var fileName = Path.GetFileName(uri.LocalPath);
            
            // Build a unique name with folder based on tree structure
            var safeFileName = SanitizeFileName(fileName);
            var folderPath = node.GetFolderPath();
            
            var filePath = Path.Combine(_config.DownloadPath, "file_content", folderPath, safeFileName);

            // Ensure folder exists
            var folder = Path.GetDirectoryName(filePath);
            if (folder != null && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Check if file with name already exists
            if (File.Exists(filePath))
            {
                Console.WriteLine($"  ⏭️  File already downloaded: {safeFileName}");
                return filePath;
            }

            Console.WriteLine($"  📥 Downloading: {safeFileName}");

            using var response = await _httpClient.GetAsync(absoluteUrl);
            response.EnsureSuccessStatusCode();

            // Download file temporarily to check hash
            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            
            // Calculate hash
            var fileHash = CalculateFileHash(fileBytes);
            
            // Check for duplicate content
            if (_downloadedHashes.Contains(fileHash))
            {
                Console.WriteLine($"  ⚠️  Duplicate file detected - ignored: {safeFileName}");
                return null;
            }
            
            // Save hash
            _downloadedHashes.Add(fileHash);
            
            // Save file
            await File.WriteAllBytesAsync(filePath, fileBytes);

            Console.WriteLine($"  ✓ Download successful: {safeFileName}");
            return filePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error downloading {url}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Validate file URL
    /// </summary>
    private bool IsValidFileUrl(string url)
    {
        if (_config.DownloadImages && IsImageFile(url))
            return true;
        
        if (_config.DownloadPdfs && url.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return true;
        
        if (_config.DownloadWord && IsWordFile(url))
            return true;
        
        if (_config.DownloadExcel && IsExcelFile(url))
            return true;
        
        if (_config.DownloadPowerpoint && IsPowerpointFile(url))
            return true;
        
        if (_config.DownloadVideos && IsVideoFile(url))
            return true;
        
        if (_config.DownloadAudios && IsAudioFile(url))
            return true;

        return false;
    }

    /// <summary>
    /// Check if URL is an image file
    /// </summary>
    private bool IsImageFile(string url)
    {
        var extension = Path.GetExtension(url).ToLower();
        return extension == ".jpg" || extension == ".jpeg" || 
               extension == ".png" || extension == ".gif" || 
               extension == ".bmp" || extension == ".webp" || 
               extension == ".svg" || extension == ".ico";
    }

    /// <summary>
    /// Check if URL is a video file
    /// </summary>
    private bool IsVideoFile(string url)
    {
        var extension = Path.GetExtension(url).ToLower();
        return extension == ".mp4" || extension == ".avi" || 
               extension == ".mkv" || extension == ".mov" || 
               extension == ".wmv" || extension == ".flv";
    }

    /// <summary>
    /// Check if URL is an audio file
    /// </summary>
    private bool IsAudioFile(string url)
    {
        var extension = Path.GetExtension(url).ToLower();
        return extension == ".mp3" || extension == ".wav" || 
               extension == ".ogg" || extension == ".m4a" || 
               extension == ".flac";
    }

    /// <summary>
    /// Check if URL is a Word file
    /// </summary>
    private bool IsWordFile(string url)
    {
        var extension = Path.GetExtension(url).ToLower();
        return extension == ".doc" || extension == ".docx";
    }

    /// <summary>
    /// Check if URL is an Excel file
    /// </summary>
    private bool IsExcelFile(string url)
    {
        var extension = Path.GetExtension(url).ToLower();
        return extension == ".xls" || extension == ".xlsx" || extension == ".csv";
    }

    /// <summary>
    /// Check if URL is a PowerPoint file
    /// </summary>
    private bool IsPowerpointFile(string url)
    {
        var extension = Path.GetExtension(url).ToLower();
        return extension == ".ppt" || extension == ".pptx";
    }

    /// <summary>
    /// Convert relative URL to absolute
    /// </summary>
    private string GetAbsoluteUrl(string url, string baseUrl)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return uri.ToString();
        
        if (Uri.TryCreate(new Uri(baseUrl), url, out var absoluteUri))
            return absoluteUri.ToString();
        
        return url;
    }

    /// <summary>
    /// Calculate file hash to detect duplicates
    /// </summary>
    private string CalculateFileHash(byte[] fileBytes)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(fileBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Sanitize file name
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }

    /// <summary>
    /// Generate hash for URL
    /// </summary>
    private string GetUrlHash(string url)
    {
        return url.GetHashCode().ToString("X").ToLower();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

