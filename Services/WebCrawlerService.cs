using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using WebCrawler.Models;

namespace WebCrawler.Services;

public class WebCrawlerService
{
    private readonly HashSet<string> _visitedUrls = new();
    private readonly HashSet<string> _failedUrls = new();
    private readonly CrawlerConfig _config;
    private readonly FileDownloader _fileDownloader;

    public event EventHandler<CrawlProgressEventArgs>? ProgressUpdated;
    public event EventHandler<string>? UrlVisited;

    public WebCrawlerService(CrawlerConfig config)
    {
        _config = config;
        _fileDownloader = new FileDownloader(config);
    }

    /// <summary>
    /// Start crawling from the input page
    /// </summary>
    public async Task<PageNode> StartCrawlingAsync()
    {
        var rootNode = new PageNode
        {
            Url = _config.StartUrl,
            Depth = 0,
            CrawledAt = DateTime.Now
        };

        Console.WriteLine($"Starting crawling from: {_config.StartUrl}");
        await CrawlRecursiveAsync(rootNode, 0);
        
        Console.WriteLine($"\nCrawling completed!");
        Console.WriteLine($"Total pages: {_visitedUrls.Count}");
        Console.WriteLine($"Failed pages: {_failedUrls.Count}");
        
        return rootNode;
    }

    /// <summary>
    /// Recursive crawling of pages
    /// </summary>
    private async Task CrawlRecursiveAsync(PageNode node, int depth)
    {
        if (depth >= _config.MaxDepth)
        {
            Console.WriteLine($"Reached maximum depth: {depth}");
            return;
        }

        // Normalize URL
        var normalizedUrl = NormalizeUrl(node.Url);
        
        if (_visitedUrls.Contains(normalizedUrl))
        {
            Console.WriteLine($"{new string(' ', depth * 2)}⏭️  Duplicate URL - skipped: {node.Url}");
            return;
        }

        try
        {
            Console.WriteLine($"{new string(' ', depth * 2)}[{depth}] Crawling: {node.Url}");
            
            _visitedUrls.Add(normalizedUrl);
            node.CrawledAt = DateTime.Now;

            // Download page content
            var html = await DownloadPageAsync(node.Url);
            if (string.IsNullOrEmpty(html))
            {
                _failedUrls.Add(node.Url);
                OnUrlVisited($"[Failed] {node.Url}");
                return;
            }

            node.Content = html;

            // Extract text content (only for first page if SectionId is provided)
            bool useSection = depth == 0 && !string.IsNullOrEmpty(_config.SectionId);
            await ExtractAndSaveContentAsync(html, node, useSection);

            // Download files (only for first page if SectionId is provided)
            await DownloadFilesFromPageAsync(html, node, useSection);

            // Extract links (only for first page if SectionId is provided)
            var links = await ExtractLinksAsync(html, node.Url, useSection);
            
            OnUrlVisited($"[Success] {node.Url} - {links.Count} links found");

            // If no links exist, we've reached a final page
            if (!links.Any())
            {
                Console.WriteLine($"{new string(' ', depth * 2)}📄 Final page: {node.Url}");
                return;
            }

            // Remove duplicate links and normalize
            var uniqueLinks = links
                .Select(link => NormalizeUrl(link))
                .Where(normalizedLink => !_visitedUrls.Contains(normalizedLink))
                .ToList();
            
            Console.WriteLine($"{new string(' ', depth * 2)}   📊 {links.Count} links found, {links.Count - uniqueLinks.Count} duplicates, {uniqueLinks.Count} new");

            // Recursively crawl each link
            foreach (var normalizedLink in uniqueLinks)
            {
                var childNode = new PageNode
                {
                    Url = normalizedLink,
                    Depth = depth + 1,
                    Parent = node
                };

                node.Children.Add(childNode);
                OnProgressUpdated(node, uniqueLinks.Count, node.Children.Count);

                await Task.Delay(_config.DelayMs);
                await CrawlRecursiveAsync(childNode, depth + 1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error crawling {node.Url}: {ex.Message}");
            _failedUrls.Add(node.Url);
        }
    }

    /// <summary>
    /// Download content of a page
    /// </summary>
    private async Task<string?> DownloadPageAsync(string url)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading {url}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extract links from HTML content
    /// </summary>
    private async Task<List<string>> ExtractLinksAsync(string html, string baseUrl, bool useSection = false)
    {
        var links = new List<string>();
        
        try
        {
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(req => req.Content(html));
            
            // If should be limited to section
            IElement? searchElement = null;
            if (useSection && !string.IsNullOrEmpty(_config.SectionId))
            {
                var section = document.QuerySelector($"#{_config.SectionId}");
                if (section != null)
                {
                    searchElement = section;
                    Console.WriteLine($"  📍 Limiting crawling to section: #{_config.SectionId}");
                }
                else
                {
                    Console.WriteLine($"  ⚠️  Section with ID '#{_config.SectionId}' not found");
                }
            }
            
            // If TargetElementIds is defined, only extract from those elements
            var linkElements = new List<IElement>();
            
            if (searchElement != null)
            {
                // If searchElement (limited section) exists
                linkElements.AddRange(searchElement.QuerySelectorAll(_config.LinkSelector));
            }
            else if (_config.TargetElementIds != null && _config.TargetElementIds.Any())
            {
                // If ID list is defined, only extract from those elements
                foreach (var elementId in _config.TargetElementIds)
                {
                    var targetElement = document.QuerySelector($"#{elementId}");
                    if (targetElement != null)
                    {
                        linkElements.AddRange(targetElement.QuerySelectorAll(_config.LinkSelector));
                        Console.WriteLine($"  ✅ Element with ID '{elementId}' found");
                    }
                    else
                    {
                        Console.WriteLine($"  ⚠️  Element with ID '{elementId}' not found");
                    }
                }
            }
            else
            {
                // Normal mode: from entire document
                linkElements.AddRange(document.QuerySelectorAll(_config.LinkSelector));
            }
            
            foreach (var element in linkElements)
            {
                var href = element.GetAttribute("href");
                if (string.IsNullOrEmpty(href)) continue;

                var absoluteUrl = GetAbsoluteUrl(baseUrl, href);
                
                // Validate URL
                if (IsValidUrl(absoluteUrl))
                {
                    // Check if only same domain should be crawled
                    if (_config.OnlySameDomain)
                    {
                        if (!IsSameDomain(_config.StartUrl, absoluteUrl))
                        {
                            Console.WriteLine($"  ⏭️  Link outside domain ignored: {absoluteUrl}");
                            continue;
                        }
                    }
                    
                    links.Add(absoluteUrl);
                }
            }

            // Extract list items
            var listItems = searchElement != null
                ? searchElement.QuerySelectorAll(_config.ItemListSelector)
                : document.QuerySelectorAll(_config.ItemListSelector);
            foreach (var listItem in listItems)
            {
                var itemElements = listItem.QuerySelectorAll(_config.ItemSelector);
                
                foreach (var itemElement in itemElements)
                {
                    var linkElement = itemElement.QuerySelector("a");
                    if (linkElement == null) continue;

                    var href = linkElement.GetAttribute("href");
                    if (string.IsNullOrEmpty(href)) continue;

                    var absoluteUrl = GetAbsoluteUrl(baseUrl, href);
                    
                    if (IsValidUrl(absoluteUrl))
                    {
                        // Check if only same domain should be crawled
                        if (_config.OnlySameDomain)
                        {
                            if (!IsSameDomain(_config.StartUrl, absoluteUrl))
                            {
                                continue;
                            }
                        }
                        
                        links.Add(absoluteUrl);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting links: {ex.Message}");
        }

        return links.Distinct().ToList();
    }

    /// <summary>
    /// Convert relative URL to absolute
    /// </summary>
    private string GetAbsoluteUrl(string baseUrl, string relativeUrl)
    {
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) &&
            Uri.TryCreate(baseUri, relativeUrl, out var absoluteUri))
        {
            return NormalizeUrl(absoluteUri.ToString());
        }
        return relativeUrl;
    }

    /// <summary>
    /// Normalize URL to prevent duplicates
    /// </summary>
    private string NormalizeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            
            // Build URL without query string and fragment
            var normalized = new UriBuilder
            {
                Scheme = uri.Scheme.ToLower(),
                Host = uri.Host.ToLower(),
                Port = uri.Port == 80 || uri.Port == 443 ? -1 : uri.Port,
                Path = uri.AbsolutePath
            }.Uri.ToString();
            
            // Remove trailing slash (except for root)
            if (normalized.EndsWith("/") && normalized.Length > uri.Scheme.Length + uri.Host.Length + 3)
            {
                normalized = normalized.TrimEnd('/');
            }
            
            return normalized.ToLower();
        }
        catch
        {
            return url.ToLower();
        }
    }

    /// <summary>
    /// Validate URL
    /// </summary>
    private bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Check if two URLs are in the same domain
    /// </summary>
    private bool IsSameDomain(string url1, string url2)
    {
        try
        {
            var uri1 = new Uri(url1);
            var uri2 = new Uri(url2);
            
            // Compare domains
            return uri1.Host.Equals(uri2.Host, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Build folder name from URL for organization
    /// </summary>
    private string GetUrlFolderName(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.Replace(".", "_");
            var path = uri.AbsolutePath.TrimStart('/').Replace("/", "_");
            
            // If path is empty (root)
            if (string.IsNullOrEmpty(path))
            {
                return host;
            }
            
            // If path is too long, shorten it
            if (path.Length > 50)
            {
                path = path.Substring(0, 50) + "_" + GetUrlHash(url);
            }
            
            return $"{host}_{path}";
        }
        catch
        {
            return "unknown";
        }
    }

    /// <summary>
    /// Extract and save text content
    /// </summary>
    private async Task ExtractAndSaveContentAsync(string html, PageNode node, bool useSection = false)
    {
        try
        {
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(req => req.Content(html));
            
            // If should be limited to section
            IElement? searchElement = null;
            if (useSection && !string.IsNullOrEmpty(_config.SectionId))
            {
                var section = document.QuerySelector($"#{_config.SectionId}");
                if (section != null)
                {
                    searchElement = section;
                }
            }
            
            // Extract text content using selector
            var contentElements = searchElement != null
                ? searchElement.QuerySelectorAll(_config.ContentSelector)
                : document.QuerySelectorAll(_config.ContentSelector);
            
            var textContent = new List<string>();
            foreach (var element in contentElements)
            {
                var text = element.TextContent?.Trim();
                if (!string.IsNullOrEmpty(text) && text.Length > 10) // Only useful texts
                {
                    textContent.Add(text);
                }
            }

            if (!textContent.Any())
                return;

            // Build folder path based on tree structure
            var folderPath = node.GetFolderPath();
            
            // Save extracted content
            var fileName = $"content_{GetUrlHash(node.Url)}.txt";
            var filePath = Path.Combine(_config.DownloadPath, "text_content", folderPath, fileName);
            
            // Create folder
            var folder = Path.GetDirectoryName(filePath);
            if (folder != null && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            await File.WriteAllTextAsync(filePath, string.Join("\n\n", textContent));
            Console.WriteLine($"  💾 Text content saved: {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting content: {ex.Message}");
        }
    }

    /// <summary>
    /// Download files from page
    /// </summary>
    private async Task DownloadFilesFromPageAsync(string html, PageNode node, bool useSection = false)
    {
        try
        {
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(req => req.Content(html));
            
            // If should be limited to section
            IElement? searchElement = null;
            if (useSection && !string.IsNullOrEmpty(_config.SectionId))
            {
                var section = document.QuerySelector($"#{_config.SectionId}");
                if (section != null)
                {
                    searchElement = section;
                }
            }
            
            // Find all image tags (only if enabled)
            if (_config.DownloadImages)
            {
                var images = searchElement != null
                    ? searchElement.QuerySelectorAll("img[src]")
                    : document.QuerySelectorAll("img[src]");
                foreach (var img in images)
                {
                    var src = img.GetAttribute("src");
                    if (!string.IsNullOrEmpty(src))
                    {
                        await _fileDownloader.DownloadFileAsync(src, node);
                    }
                }
            }

            // Find all links to PDF, Word, Excel, PowerPoint files
            var links = searchElement != null
                ? searchElement.QuerySelectorAll("a[href]")
                : document.QuerySelectorAll("a[href]");
            foreach (var link in links)
            {
                var href = link.GetAttribute("href");
                if (!string.IsNullOrEmpty(href))
                {
                    var fileUrl = GetAbsoluteUrl(node.Url, href);
                    var ext = Path.GetExtension(fileUrl).ToLower();
                    
                    // PDF
                    if (_config.DownloadPdfs && fileUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        await _fileDownloader.DownloadFileAsync(fileUrl, node);
                        continue;
                    }
                    
                    // Word
                    if (_config.DownloadWord && (ext == ".doc" || ext == ".docx"))
                    {
                        await _fileDownloader.DownloadFileAsync(fileUrl, node);
                        continue;
                    }
                    
                    // Excel
                    if (_config.DownloadExcel && (ext == ".xls" || ext == ".xlsx" || ext == ".csv"))
                    {
                        await _fileDownloader.DownloadFileAsync(fileUrl, node);
                        continue;
                    }
                    
                    // PowerPoint
                    if (_config.DownloadPowerpoint && (ext == ".ppt" || ext == ".pptx"))
                    {
                        await _fileDownloader.DownloadFileAsync(fileUrl, node);
                        continue;
                    }
                    
                    // Video
                    if (_config.DownloadVideos)
                    {
                        if (ext == ".mp4" || ext == ".avi" || ext == ".mkv" || ext == ".mov")
                        {
                            await _fileDownloader.DownloadFileAsync(fileUrl, node);
                            continue;
                        }
                    }
                    
                    // Audio
                    if (_config.DownloadAudios)
                    {
                        if (ext == ".mp3" || ext == ".wav" || ext == ".ogg" || ext == ".m4a")
                        {
                            await _fileDownloader.DownloadFileAsync(fileUrl, node);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading files: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate hash for URL
    /// </summary>
    private string GetUrlHash(string url)
    {
        return url.GetHashCode().ToString("X").ToLower();
    }

    private void OnProgressUpdated(PageNode node, int totalLinks, int processedLinks)
    {
        ProgressUpdated?.Invoke(this, new CrawlProgressEventArgs
        {
            CurrentUrl = node.Url,
            TotalLinks = totalLinks,
            ProcessedLinks = processedLinks,
            Depth = node.Depth
        });
    }

    private void OnUrlVisited(string url)
    {
        UrlVisited?.Invoke(this, url);
    }
}

public class CrawlProgressEventArgs : EventArgs
{
    public required string CurrentUrl { get; set; }
    public int TotalLinks { get; set; }
    public int ProcessedLinks { get; set; }
    public int Depth { get; set; }
}
