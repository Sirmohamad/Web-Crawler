namespace WebCrawler;

/// <summary>
/// Static list of element IDs that should be crawled
/// These IDs are taken from www.intamedia.ir/terms
/// </summary>
public static class TargetElementIds
{
    /// <summary>
    /// IDs of elements in the page that should be crawled
    /// You can modify this list based on your needs
    /// </summary>
    public static List<string> Ids = new List<string>
    {
        "dnn_ContentPane",
        "LabelMainContent",
        "DNN_Documents",
        "dnn_ctr4044_View_ctl00_ctl00_LiveTabsiMod4045",
        "dnn_ctr3992_View_ctl00_ctl00_LiveTabsiMod3987",
        "dnn_ctr4046_View_ctl00_ctl00_LiveTabsiMod4051",
        "dnn_TopPane"
    };
    
    /// <summary>
    /// Is this feature enabled?
    /// If false, the entire page will be crawled
    /// </summary>
    public static bool IsEnabled = true;
}
