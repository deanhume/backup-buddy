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

        try
        {
            // Download and parse sitemap
            Console.WriteLine("Downloading sitemap...");
            var startTime = DateTime.Now;

            // Use GetStreamAsync and parse directly from stream for better performance
            using var httpClient = Utils.CreateHttpClient();
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
                    var markdown = Utils.HtmlToMarkdown(html);

                    // Create a safe filename from the URL
                    string fileName = Utils.CreateFilePath(url);

                    fileName = Utils.SanitizeFileName(fileName);

                    // Create directory structure for this URL
                    var urlDir = Path.Combine(outputDir, $"{index + 1}_{fileName}");
                    Directory.CreateDirectory(urlDir);

                    // Download images from the page
                    var imagesDir = Path.Combine(urlDir, "images");
                    Directory.CreateDirectory(imagesDir);
                    await MediaDownloader.DownloadImages(html, url, imagesDir, httpClient);

                    // Download videos from the page
                    var videosDir = Path.Combine(urlDir, "videos");
                    Directory.CreateDirectory(videosDir);
                    await MediaDownloader.DownloadVideos(html, url, videosDir, httpClient);

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
}