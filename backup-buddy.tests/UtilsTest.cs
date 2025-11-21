public class BackupBuddyTests
{
    [Fact]
    public void SanitizeFileName_RemovesInvalidCharacters()
    {
        // Arrange
        string unsafeFileName = "test:file<name>with|invalid*chars?.txt";

        // Act
        string safeFileName = Utils.SanitizeFileName(unsafeFileName);

        // Assert
        Assert.DoesNotContain(':', safeFileName);
        Assert.DoesNotContain('<', safeFileName);
        Assert.DoesNotContain('>', safeFileName);
        Assert.DoesNotContain('|', safeFileName);
        Assert.DoesNotContain('*', safeFileName);
        Assert.DoesNotContain('?', safeFileName);
    }

    [Fact]
    public void CreateFilePath_ExtractsLastSegmentFromUrl()
    {
        // Arrange
        string url = "https://example.com/blog/my-awesome-post";
        
        // Act
        string fileName = Utils.CreateFilePath(url);
        
        // Assert
        Assert.Equal("my-awesome-post", fileName);
    }

    [Fact]
    public void CreateFilePath_ReturnsIndexForRootUrl()
    {
        // Arrange
        string url = "https://example.com/";
        
        // Act
        string fileName = Utils.CreateFilePath(url);
        
        // Assert
        Assert.Equal("index", fileName);
    }

    [Fact]
    public void CreateFilePath_HandlesMultiplePathSegments()
    {
        // Arrange
        string url = "https://example.com/blog/2024/01/my-post";
        
        // Act
        string fileName = Utils.CreateFilePath(url);
        
        // Assert
        Assert.Equal("my-post", fileName);
    }

    [Fact]
    public void CreateFilePath_HandlesUrlWithTrailingSlash()
    {
        // Arrange
        string url = "https://example.com/blog/my-post/";
        
        // Act
        string fileName = Utils.CreateFilePath(url);
        
        // Assert
        Assert.Equal("my-post", fileName);
    }

    [Fact]
    public void HtmlToMarkdown_ExtractsArticleContent()
    {
        // Arrange
        string html = @"
            <html>
                <body>
                    <article>
                        <h1>Test Article</h1>
                        <p>This is the main content.</p>
                    </article>
                </body>
            </html>";
        
        // Act
        string markdown = Utils.HtmlToMarkdown(html);
        
        // Assert
        Assert.Contains("Test Article", markdown);
        Assert.Contains("This is the main content", markdown);
    }

    [Fact]
    public void HtmlToMarkdown_RemovesScriptTags()
    {
        // Arrange
        string html = @"
            <html>
                <body>
                    <article>
                        <h1>Title</h1>
                        <script>alert('test');</script>
                        <p>Content</p>
                    </article>
                </body>
            </html>";
        
        // Act
        string markdown = Utils.HtmlToMarkdown(html);
        
        // Assert
        Assert.DoesNotContain("alert", markdown);
        Assert.DoesNotContain("script", markdown);
        Assert.Contains("Content", markdown);
    }

    [Fact]
    public void HtmlToMarkdown_RemovesNavigationElements()
    {
        // Arrange
        string html = @"
            <html>
                <body>
                    <nav>Navigation Menu</nav>
                    <article>
                        <h1>Article Title</h1>
                        <p>Article content</p>
                    </article>
                    <footer>Footer content</footer>
                </body>
            </html>";
        
        // Act
        string markdown = Utils.HtmlToMarkdown(html);
        
        // Assert
        Assert.DoesNotContain("Navigation Menu", markdown);
        Assert.DoesNotContain("Footer content", markdown);
        Assert.Contains("Article Title", markdown);
        Assert.Contains("Article content", markdown);
    }

    [Fact]
    public void HtmlToMarkdown_ExtractsMainTag()
    {
        // Arrange
        string html = @"
            <html>
                <body>
                    <header>Header</header>
                    <main>
                        <h1>Main Content</h1>
                        <p>This is in the main tag.</p>
                    </main>
                </body>
            </html>";
        
        // Act
        string markdown = Utils.HtmlToMarkdown(html);
        
        // Assert
        Assert.Contains("Main Content", markdown);
        Assert.Contains("This is in the main tag", markdown);
    }

    [Fact]
    public void HtmlToMarkdown_ConvertsLinksCorrectly()
    {
        // Arrange
        string html = @"
            <article>
                <p>Check out <a href='https://example.com'>this link</a>.</p>
            </article>";
        
        // Act
        string markdown = Utils.HtmlToMarkdown(html);
        
        // Assert
        Assert.Contains("this link", markdown);
        Assert.Contains("https://example.com", markdown);
    }

    [Fact]
    public void HtmlToMarkdown_ConvertsHeadingsCorrectly()
    {
        // Arrange
        string html = @"
            <article>
                <h1>Heading 1</h1>
                <h2>Heading 2</h2>
                <h3>Heading 3</h3>
            </article>";
        
        // Act
        string markdown = Utils.HtmlToMarkdown(html);
        
        // Assert
        Assert.Contains("Heading 1", markdown);
        Assert.Contains("Heading 2", markdown);
        Assert.Contains("Heading 3", markdown);
    }

    [Fact]
    public void HtmlToMarkdown_ExtractsContentByClass()
    {
        // Arrange
        string html = @"
            <html>
                <body>
                    <div class='sidebar'>Sidebar content</div>
                    <div class='post-content'>
                        <h1>Post Title</h1>
                        <p>Post body</p>
                    </div>
                </body>
            </html>";
        
        // Act
        string markdown = Utils.HtmlToMarkdown(html);
        
        // Assert
        Assert.Contains("Post Title", markdown);
        Assert.Contains("Post body", markdown);
    }

    [Fact]
    public void HtmlToMarkdown_FallsBackToBodyWhenNoContentContainer()
    {
        // Arrange
        string html = @"
            <html>
                <body>
                    <h1>Title in Body</h1>
                    <p>Content in body</p>
                </body>
            </html>";
        
        // Act
        string markdown = Utils.HtmlToMarkdown(html);
        
        // Assert
        Assert.Contains("Title in Body", markdown);
        Assert.Contains("Content in body", markdown);
    }
}