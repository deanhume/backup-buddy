using System.Xml.Linq;
using ReverseMarkdown;
using HtmlAgilityPack;

public class Program
{
    /// <summary>
    /// Main entry point for the blog backup tool.
    /// Processes a sitemap.xml file and downloads all URLs as markdown with images.
    /// </summary>
    /// <param name="args">Command line arguments (sitemap URL).</param>
    public static async Task Main(string[] args)
    {

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: backup-buddy <sitemap-url>");
            Console.WriteLine("Example: backup-buddy https://example.com/sitemap.xml");
            return;
        }

        string sitemapUrl = args[0];
        Console.WriteLine($"Processing sitemap: {sitemapUrl}");

        var httpClient = new HttpClient(CreateSocketsHttpHandler());
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        httpClient.DefaultRequestHeaders.ConnectionClose = false; // Keep connection alive for reuse

        try
        {
            // Download and parse sitemap
            Console.WriteLine("Downloading sitemap...");
            var startTime = DateTime.Now;

            // Use GetStreamAsync and parse directly from stream for better performance
            using var stream = await httpClient.GetStreamAsync(sitemapUrl);
            var sitemap = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"Sitemap downloaded in {elapsed:F2} seconds, parsing URLs...");

            // Extract all URLs from the sitemap
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var urls = sitemap.Descendants(ns + "url")
                .Select(u => u.Element(ns + "loc")?.Value)
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList();

            Console.WriteLine($"Found {urls.Count} URLs in sitemap");

            // Create output directory
            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
            Directory.CreateDirectory(outputDir);

            // Process URLs in parallel for faster processing
            var maxParallelism = 10; // Adjust based on your needs, larger websites might require higher values
            var semaphore = new SemaphoreSlim(maxParallelism);
            var processed = 0;
            var totalCount = urls.Count;

            var tasks = urls.Select(async (url, index) =>
            {
                if (url == null) return;

                await semaphore.WaitAsync(); // Limit concurrent requests
                try
                {
                    var currentIndex = Interlocked.Increment(ref processed);
                    Console.WriteLine($"[{currentIndex}/{totalCount}] Processing: {url}");

                    // Download HTML content
                    var html = await httpClient.GetStringAsync(url);

                    // Convert to Markdown
                    var markdown = HtmlToMarkdown(html);

                    // Create a safe filename from the URL
                    string fileName = CreateFilePath(url);

                    fileName = SanitizeFileName(fileName);

                    // Create directory structure for this URL
                    var urlDir = Path.Combine(outputDir, $"{index + 1}_{fileName}");
                    Directory.CreateDirectory(urlDir);

                    // Download images from the page
                    var imagesDir = Path.Combine(urlDir, "images");
                    Directory.CreateDirectory(imagesDir);
                    await DownloadImages(html, url, imagesDir, httpClient);

                    // Save markdown file
                    var markdownPath = Path.Combine(urlDir, $"{fileName}.md");
                    await File.WriteAllTextAsync(markdownPath, markdown);

                    // Save metadata file with original URL and timestamp
                    var metadataPath = Path.Combine(urlDir, "metadata.txt");
                    await File.WriteAllTextAsync(metadataPath, $"Original URL: {url}\nProcessed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    Console.WriteLine($"  ✓ Saved to: {urlDir}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Error processing {url}: {ex.Message}");
                }
                finally
                {
                    semaphore.Release(); // Allow next request to proceed
                }
            });

            await Task.WhenAll(tasks);

            Console.WriteLine($"\nCompleted! Processed {processed} URLs.");
            Console.WriteLine($"Output directory: {outputDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
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
    /// Downloads the images from the given URL.
    /// </summary>
    /// <param name="html">The HTML page.</param>
    /// <param name="pageUrl">The URL of the page.</param>
    /// <param name="imagesDir">The directory to save images to.</param>
    /// <param name="httpClient">The httpClient object.</param>
    /// <returns></returns>
    public static async Task DownloadImages(string html, string pageUrl, string imagesDir, HttpClient httpClient)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var imageNodes = htmlDoc.DocumentNode.SelectNodes("//img[@src]");
        if (imageNodes == null || !imageNodes.Any())
        {
            return;
        }

        var baseUri = new Uri(pageUrl);
        var downloadedCount = 0;

        // Download images in parallel
        var imageTasks = imageNodes.Select(async (imgNode, index) =>
        {
            try
            {
                var src = imgNode.GetAttributeValue("src", "");
                if (string.IsNullOrEmpty(src)) return false;

                // Convert relative URLs to absolute
                Uri? imageUri = null;
                if (!Uri.TryCreate(src, UriKind.Absolute, out imageUri))
                {
                    if (!Uri.TryCreate(baseUri, src, out imageUri))
                    {
                        return false;
                    }
                }

                // Download image
                var imageBytes = await httpClient.GetByteArrayAsync(imageUri);

                // Create safe filename from URL
                var imageName = Path.GetFileName(imageUri.LocalPath);
                if (string.IsNullOrEmpty(imageName))
                {
                    imageName = $"image_{index}.jpg";
                }

                // Sanitize filename
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    imageName = imageName.Replace(c, '_');
                }

                var imagePath = Path.Combine(imagesDir, imageName);
                await File.WriteAllBytesAsync(imagePath, imageBytes);
                return true;
            }
            catch (Exception ex)
            {
                // Continue on error for individual images
                Console.WriteLine($"    ⚠ Could not download image: {ex.Message}");
                return false;
            }
        });

        var results = await Task.WhenAll(imageTasks);
        downloadedCount = results.Count(r => r);

        if (downloadedCount > 0)
        {
            Console.WriteLine($"  ✓ Downloaded {downloadedCount} images");
        }
    }
}