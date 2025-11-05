using System;
using System.Linq;
using System.Threading.Tasks;
using WebCrawler.Models;
using WebCrawler.Services;

namespace WebCrawler;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("===== Web Crawler with .NET =====\n");
        Console.WriteLine("✨ All settings are configured automatically!");
        Console.WriteLine("📝 Enter URL (or press Enter to use default)\n");

        // Get input link
        Console.Write("🌐 Enter input link: ");
        string startUrl = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrEmpty(startUrl))
        {
            startUrl = "https://example.com";
            Console.WriteLine($"✓ Using default URL: {startUrl}");
        }

        Console.WriteLine("\n⚙️  Get optional settings (or press Enter to use defaults):\n");

        Console.Write("📍 Limit to a specific section by ID? (optional): ");
        var sectionId = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(sectionId))
            sectionId = null;

        Console.Write("📊 Maximum crawling depth (default: 10): ");
        var maxDepthInput = Console.ReadLine()?.Trim();
        var maxDepth = int.TryParse(maxDepthInput, out var depth) ? depth : 10;

        Console.WriteLine("\n✨ Starting crawling with the following settings:");
        Console.WriteLine($"   📌 URL: {startUrl}");
        if (!string.IsNullOrEmpty(sectionId))
            Console.WriteLine($"   📌 Section ID: #{sectionId}");
        Console.WriteLine($"   📌 Max Depth: {maxDepth}");
        Console.WriteLine($"   📌 Download PDF: ✅");
        Console.WriteLine($"   📌 Download Word: ✅");
        Console.WriteLine($"   📌 Download Excel: ✅");
        Console.WriteLine($"   📌 Download PowerPoint: ✅");
        Console.WriteLine($"   📌 Download Images: ❌ (disabled)");
        Console.WriteLine($"   📌 Download Videos: ❌ (disabled)");
        Console.WriteLine($"   📌 Download Audio: ❌ (disabled)");
        Console.WriteLine($"   📌 Storage Folder: downloads/");
        Console.WriteLine();

        // Create config with default settings
        var config = new CrawlerConfig
        {
            StartUrl = startUrl,
            SectionId = sectionId,
            MaxDepth = maxDepth,
            DelayMs = 500,  // Half a second delay between requests
            // Use static ID list
            TargetElementIds = TargetElementIds.IsEnabled ? TargetElementIds.Ids : null
            // All selectors are configured by default
        };

        // Create and start crawling
        var crawler = new WebCrawlerService(config);
        
        crawler.UrlVisited += (sender, url) =>
        {
            Console.WriteLine($"✓ {url}");
        };

        crawler.ProgressUpdated += (sender, e) =>
        {
            Console.WriteLine($"  → Status: {e.ProcessedLinks}/{e.TotalLinks} links processed at depth {e.Depth}");
        };

        var rootNode = await crawler.StartCrawlingAsync();

        // Display crawled tree
        Console.WriteLine("\n===== Crawled Tree =====");
        PrintNodeTree(rootNode, 0);

        Console.WriteLine("\n===== Final Report =====");
        Console.WriteLine($"📄 Total pages: {CountTotalNodes(rootNode)}");
        Console.WriteLine($"📊 Tree depth: {GetMaxDepth(rootNode)}");
        Console.WriteLine($"💾 Files in folder: downloads/");

        Console.WriteLine("\n✅ Crawling completed successfully!");
        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }

    static void PrintNodeTree(PageNode node, int indent)
    {
        var indentStr = new string(' ', indent * 2);
        Console.WriteLine($"{indentStr}📄 {node.Url}");
        
        foreach (var child in node.Children)
        {
            PrintNodeTree(child, indent + 1);
        }
    }

    static int CountTotalNodes(PageNode node)
    {
        return 1 + node.Children.Sum(child => CountTotalNodes(child));
    }

    static int GetMaxDepth(PageNode node)
    {
        if (!node.Children.Any())
            return node.Depth;
        
        return node.Children.Max(child => GetMaxDepth(child));
    }
}
