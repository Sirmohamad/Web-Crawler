using System;

namespace WebCrawler.Models;

public class CrawlerConfig
{
    /// <summary>
    /// Input link to start crawling
    /// </summary>
    public required string StartUrl { get; set; }

    /// <summary>
    /// Section ID to limit crawling to a specific section (optional)
    /// Only applied to the first page
    /// </summary>
    public string? SectionId { get; set; } = null;

    /// <summary>
    /// List of element IDs that should be crawled (optional)
    /// If set, only links inside these elements will be extracted
    /// </summary>
    public List<string>? TargetElementIds { get; set; } = null;

    /// <summary>
    /// CSS selector for finding next page links
    /// </summary>
    public string LinkSelector { get; set; } = "a[href], a.next, a.page-link, .pagination a";

    /// <summary>
    /// CSS selector for finding item lists
    /// </summary>
    public string ItemListSelector { get; set; } = "ul, ol, .items, .list, .content-list, article, .post, .product-list";

    /// <summary>
    /// CSS selector for finding each item inside the list
    /// </summary>
    public string ItemSelector { get; set; } = "li, .item, .entry, .post-item, .product";

    /// <summary>
    /// CSS selector for extracting text content
    /// </summary>
    public string ContentSelector { get; set; } = "p, div.content, article, .post-content, .entry-content, main";

    /// <summary>
    /// Download image files
    /// </summary>
    public bool DownloadImages { get; set; } = false;

    /// <summary>
    /// Download PDF files
    /// </summary>
    public bool DownloadPdfs { get; set; } = true;

    /// <summary>
    /// Download Word files
    /// </summary>
    public bool DownloadWord { get; set; } = true;

    /// <summary>
    /// Download Excel files
    /// </summary>
    public bool DownloadExcel { get; set; } = true;

    /// <summary>
    /// Download PowerPoint files
    /// </summary>
    public bool DownloadPowerpoint { get; set; } = true;

    /// <summary>
    /// Download video files
    /// </summary>
    public bool DownloadVideos { get; set; } = false;

    /// <summary>
    /// Download audio files
    /// </summary>
    public bool DownloadAudios { get; set; } = false;

    /// <summary>
    /// Should only crawl links from the same domain?
    /// </summary>
    public bool OnlySameDomain { get; set; } = false;

    /// <summary>
    /// Maximum crawling depth
    /// </summary>
    public int MaxDepth { get; set; } = 10;

    /// <summary>
    /// Delay between requests (milliseconds)
    /// Recommendation: 100-500 milliseconds for normal websites
    /// </summary>
    public int DelayMs { get; set; } = 500;

    /// <summary>
    /// Path to save downloaded files
    /// </summary>
    public string DownloadPath { get; set; } = "downloads";
}
