using HtmlAgilityPack;

/// <summary>
/// Helper class for downloading media files (images and videos) from HTML pages.
/// </summary>
public class MediaDownloader
{
    /// <summary>
    /// Downloads the images from the given URL.
    /// </summary>
    /// <param name="html">The HTML page.</param>
    /// <param name="pageUrl">The URL of the page.</param>
    /// <param name="imagesDir">The directory to save images to.</param>
    /// <param name="httpClient">The httpClient object.</param>
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
                    // Try to detect extension from URL query parameters or default to .jpg
                    var extension = Path.GetExtension(imageUri.AbsoluteUri.Split('?')[0]);
                    if (string.IsNullOrEmpty(extension))
                    {
                        extension = ".jpg";
                    }
                    imageName = $"image_{index}{extension}";
                }
                else if (string.IsNullOrEmpty(Path.GetExtension(imageName)))
                {
                    // If filename exists but has no extension, try to detect from URL or default to .jpg
                    var extension = Path.GetExtension(imageUri.AbsoluteUri.Split('?')[0]);
                    if (string.IsNullOrEmpty(extension))
                    {
                        extension = ".jpg";
                    }
                    imageName = $"{imageName}{extension}";
                }

                // Sanitize filename
                imageName = Utils.SanitizeFileName(imageName);
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

    /// <summary>
    /// Downloads videos from the given URL.
    /// Supports video, source, and embed tags.
    /// </summary>
    /// <param name="html">The HTML page.</param>
    /// <param name="pageUrl">The URL of the page.</param>
    /// <param name="videosDir">The directory to save videos to.</param>
    /// <param name="httpClient">The httpClient object.</param>
    public static async Task DownloadVideos(string html, string pageUrl, string videosDir, HttpClient httpClient)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var videoUrls = new List<string>();
        var baseUri = new Uri(pageUrl);

        // Find video elements with src attribute
        var videoNodes = htmlDoc.DocumentNode.SelectNodes("//video[@src]");
        if (videoNodes != null)
        {
            videoUrls.AddRange(videoNodes.Select(v => v.GetAttributeValue("src", "")));
        }

        // Find source elements inside video tags
        var sourceNodes = htmlDoc.DocumentNode.SelectNodes("//video//source[@src]");
        if (sourceNodes != null)
        {
            videoUrls.AddRange(sourceNodes.Select(s => s.GetAttributeValue("src", "")));
        }

        // Find embed elements (for older video embeds)
        var embedNodes = htmlDoc.DocumentNode.SelectNodes("//embed[@src]");
        if (embedNodes != null)
        {
            var embedUrls = embedNodes
                .Select(e => e.GetAttributeValue("src", ""))
                .Where(src => !string.IsNullOrEmpty(src) && 
                    (src.Contains(".mp4") || src.Contains(".webm") || 
                     src.Contains(".ogg") || src.Contains(".mov")));
            videoUrls.AddRange(embedUrls);
        }

        // Remove empty URLs and duplicates
        videoUrls = videoUrls
            .Where(url => !string.IsNullOrEmpty(url))
            .Distinct()
            .ToList();

        if (!videoUrls.Any())
        {
            return;
        }

        var downloadedCount = 0;

        // Download videos in parallel
        var videoTasks = videoUrls.Select(async (src, index) =>
        {
            try
            {
                // Convert relative URLs to absolute
                Uri? videoUri = null;
                if (!Uri.TryCreate(src, UriKind.Absolute, out videoUri))
                {
                    if (!Uri.TryCreate(baseUri, src, out videoUri))
                    {
                        return false;
                    }
                }

                // Download video
                var videoBytes = await httpClient.GetByteArrayAsync(videoUri);

                // Create safe filename from URL
                var videoName = Path.GetFileName(videoUri.LocalPath);
                if (string.IsNullOrEmpty(videoName))
                {
                    // Try to detect extension from URL
                    var extension = Path.GetExtension(videoUri.AbsoluteUri.Split('?')[0]);
                    if (string.IsNullOrEmpty(extension))
                    {
                        extension = ".mp4"; // Default to mp4
                    }
                    videoName = $"video_{index}{extension}";
                }
                else if (string.IsNullOrEmpty(Path.GetExtension(videoName)))
                {
                    // If filename exists but has no extension
                    var extension = Path.GetExtension(videoUri.AbsoluteUri.Split('?')[0]);
                    if (string.IsNullOrEmpty(extension))
                    {
                        extension = ".mp4";
                    }
                    videoName = $"{videoName}{extension}";
                }

                // Sanitize filename
                videoName = Utils.SanitizeFileName(videoName);
                var videoPath = Path.Combine(videosDir, videoName);
                
                await File.WriteAllBytesAsync(videoPath, videoBytes);
                return true;
            }
            catch (Exception ex)
            {
                // Continue on error for individual videos
                Console.WriteLine($"    ⚠ Could not download video: {ex.Message}");
                return false;
            }
        });

        var results = await Task.WhenAll(videoTasks);
        downloadedCount = results.Count(r => r);

        if (downloadedCount > 0)
        {
            Console.WriteLine($"  ✓ Downloaded {downloadedCount} videos");
        }
    }
}
