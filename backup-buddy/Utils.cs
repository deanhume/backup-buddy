using HtmlAgilityPack;

public class Utils
{
    /// <summary>
    /// Converts HTML content to Markdown format.
    /// Extracts main content area and removes unnecessary elements.
    /// </summary>
    /// <param name="html">The HTML content to convert.</param>
    /// <returns>Markdown formatted string.</returns>
    public static string HtmlToMarkdown(string html)
    {
        // Load HTML document
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        // Try to extract main content area using common selectors
        // Falls back to body if no main content container is found
        var contentNode = htmlDoc.DocumentNode.SelectSingleNode("//article")
            ?? htmlDoc.DocumentNode.SelectSingleNode("//main")
            ?? htmlDoc.DocumentNode.SelectSingleNode("//*[@class='post-content']")
            ?? htmlDoc.DocumentNode.SelectSingleNode("//*[@class='content']")
            ?? htmlDoc.DocumentNode.SelectSingleNode("//*[@id='content']")
            ?? htmlDoc.DocumentNode.SelectSingleNode("//body");

        // Remove unnecessary elements that don't belong in content
        var nodesToRemove = contentNode?.SelectNodes(".//script|.//style|.//nav|.//footer|.//header")?.ToList();
        if (nodesToRemove != null)
        {
            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }
        }

        var contentHtml = contentNode?.InnerHtml ?? html;

        // Configure converter for better markdown output
        var config = new ReverseMarkdown.Config
        {
            UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.Bypass,
            GithubFlavored = true,
            RemoveComments = true,
            SmartHrefHandling = true
        };

        var converter = new ReverseMarkdown.Converter(config);
        var markdown = converter.Convert(contentHtml);

        return markdown.Trim() + Environment.NewLine;
    }

    /// <summary>
    /// Creates a file path from a URL by extracting the last segment or using "index" if empty.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static string CreateFilePath(string url)
    {
        var uri = new Uri(url);
        var pathSegments = uri.AbsolutePath.Trim('/').Split('/');
        var fileName = pathSegments.Length > 0 && !string.IsNullOrEmpty(pathSegments[^1])
            ? pathSegments[^1]
            : "index";
        return fileName;
    }

    /// <summary>
    /// Sanitizes the file name by replacing invalid characters.
    /// </summary>
    /// <param name="fileName">The filename</param>
    /// <returns></returns>
    public static string SanitizeFileName(string fileName)
    {
        // Sanitize filename to remove invalid characters
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName;
    }

    /// <summary>
    /// Creates a socket HTTP Handler and forces IPV4 instead of 6.
    /// </summary>
    /// <returns>A socket HTTP Handler.</returns>
    private static SocketsHttpHandler CreateSocketsHttpHandler()
    {
        // IPv6 can cause delays on some Windows systems
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new System.Net.Sockets.Socket(
                    System.Net.Sockets.AddressFamily.InterNetwork, // Force IPv4
                    System.Net.Sockets.SocketType.Stream,
                    System.Net.Sockets.ProtocolType.Tcp);

                socket.NoDelay = true;

                try
                {
                    await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                    return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };
        return handler;
    }

    /// <summary>
    /// Creates and configures an HttpClient instance.
    /// </summary>
    /// <returns>An HttpClient instance.</returns>
    public static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient(CreateSocketsHttpHandler());
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        httpClient.DefaultRequestHeaders.ConnectionClose = false; // Keep connection alive for reuse
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        return httpClient;
    }
}