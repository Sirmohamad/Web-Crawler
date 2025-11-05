using System;
using System.Collections.Generic;
using System.Linq;

namespace WebCrawler.Models;

public class PageNode
{
    public required string Url { get; set; }
    public int Depth { get; set; }
    public DateTime CrawledAt { get; set; }
    public PageNode? Parent { get; set; }
    public List<PageNode> Children { get; set; } = new();
    public string? Content { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();

    public int TotalDescendants => Children.Sum(child => 1 + child.TotalDescendants);

    /// <summary>
    /// Extract full path from root to this node
    /// </summary>
    public List<PageNode> GetFullPath()
    {
        var path = new List<PageNode>();
        var current = this;
        
        while (current != null)
        {
            path.Add(current);
            current = current.Parent;
        }
        
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Build folder name based on full tree path
    /// </summary>
    public string GetFolderPath()
    {
        var path = GetFullPath();
        var segments = path.Select(node =>
        {
            try
            {
                var uri = new Uri(node.Url);
                var host = uri.Host.Replace(".", "_");
                var urlPath = uri.AbsolutePath.TrimStart('/').Replace("/", "_");
                
                // Limit length
                if (urlPath.Length > 30)
                {
                    urlPath = urlPath.Substring(0, 30);
                }
                
                return string.IsNullOrEmpty(urlPath) ? host : $"{host}_{urlPath}";
            }
            catch
            {
                return "unknown";
            }
        }).ToList();
        
        // Return full path with / separator for tree structure
        return string.Join("/", segments);
    }
    
    /// <summary>
    /// Get parent URL (page from which we came to this page)
    /// </summary>
    public string? GetParentUrl()
    {
        return Parent?.Url;
    }
}
