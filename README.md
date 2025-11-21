# Backup Buddy
<img src="./logo.png" width=400>

A powerful .NET command-line tool that backs up websites by converting HTML pages to Markdown format with images. Perfect for archiving blogs, documentation sites, or any web content you want to preserve in a portable, readable format.

## Features

- 🗺️ **Sitemap Processing** - Automatically discovers and processes all URLs from a sitemap.xml file
- 📝 **HTML to Markdown Conversion** - Converts web pages to clean, readable Markdown format
- 🖼️ **Image Download** - Downloads and saves all images from each page
- ⚡ **Parallel Processing** - Fast concurrent downloads with configurable parallelism (default: 10 concurrent requests)
- 📁 **Organized Output** - Creates structured directories for each page with metadata
- 🎯 **Smart Content Extraction** - Focuses on main content, removing navigation, headers, and footers
- 🔄 **Connection Reuse** - Optimized HTTP client configuration for better performance
- 📊 **Progress Tracking** - Real-time progress updates during backup process

## Installation

### Build from Source

```bash
git clone https://github.com/yourusername/backup-buddy.git
cd backup-buddy
dotnet build
```

### Run Directly

```bash
dotnet run --project backup-buddy -- <sitemap-url>
```

### Create Standalone Executable

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

Replace `win-x64` with your target platform:
- `win-x64` - Windows 64-bit
- `linux-x64` - Linux 64-bit
- `osx-x64` - macOS 64-bit
- `osx-arm64` - macOS Apple Silicon

## Usage

### Basic Usage

```bash
backup-buddy <sitemap-url>
```

### Examples

```bash
# Backup a blog
backup-buddy https://example.com/sitemap.xml

# Backup WordPress site
backup-buddy https://myblog.wordpress.com/sitemap.xml
```

## Output Structure

The tool creates an `output` directory in the current working directory with the following structure:

```
output/
├── 1_page-name/
│   ├── page-name.md
│   ├── metadata.txt
│   └── images/
│       ├── image1.jpg
│       └── image2.png
├── 2_another-page/
│   ├── another-page.md
│   ├── metadata.txt
│   └── images/
│       └── photo.jpg
└── ...
```

Each page directory contains:
- **Markdown file** - The converted page content
- **metadata.txt** - Original URL and processing timestamp
- **images/** - All images from the page

## How It Works

1. **Sitemap Download** - Downloads and parses the sitemap.xml file
2. **URL Extraction** - Extracts all URLs from the sitemap
3. **Parallel Processing** - Processes multiple pages concurrently
4. **Content Extraction** - Extracts main content from HTML (article, main, or content sections)
5. **Markdown Conversion** - Converts HTML to GitHub-flavored Markdown
6. **Image Download** - Downloads all images and saves them locally
7. **File Organization** - Creates organized directories with metadata

## Limitations

- Only processes URLs listed in the sitemap
- Requires valid sitemap.xml format
- May not capture JavaScript-rendered content
- Image URLs must be accessible at processing time
- If processing very large sites, consider reducing the `maxParallelism` value to decrease memory usage.


## License

This project is open source and available under the [MIT License](LICENSE).

## Use Cases

- **Blog Archival** - Backup your blog posts before migrating platforms
- **Documentation Preservation** - Archive documentation sites for offline access
- **Content Backup** - Create portable backups of web content
- **Static Site Generation** - Convert dynamic sites to markdown for static site generators
- **Offline Reading** - Save web content for offline reading in Markdown format

## Future Enhancements

- [ ] Support for sitemap index files
- [ ] Custom output format options
- [ ] Progress bar visualization
- [ ] Video and other media download support

---

Made with ❤️ by [Dean Hume](https://www.deanhume.com)