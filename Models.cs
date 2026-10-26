using System.Text.Json.Serialization;

namespace CellphoneSCrawler;

public sealed class ProductLink
{
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
}

public sealed class RawProduct
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("sku")] public string SourceSku { get; set; } = "";
    [JsonPropertyName("brand")] public string Brand { get; set; } = "";
    [JsonPropertyName("salePrice")] public string DisplayPrice { get; set; } = "";
    [JsonPropertyName("regularPrice")] public string ComparePrice { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("image")] public string ImageUrl { get; set; } = "";
    [JsonPropertyName("availability")] public string Availability { get; set; } = "";
    [JsonPropertyName("variantUrls")] public List<string>? VariantUrls { get; set; }
}

public sealed record Product(
    string Sku,
    string Name,
    string Brand,
    long SellingPrice,
    long? OriginalPrice,
    string Description,
    string ImageUrl,
    string ProductUrl,
    string Category,
    string Availability,
    string CrawledAtUtc);

public sealed record FailedUrl(string Url, string Reason);

public sealed class CrawlerOptions
{
    public int Target { get; set; } = 500;
    public int MaxLoadMore { get; set; } = 30;
    public int DelayMs { get; set; } = 1800;
    public bool Headful { get; set; }
    public bool Screenshots { get; set; }
    public string Output { get; set; } = "output/phones";
    public List<string> Categories { get; set; } = new()
    {
        // 1. Danh mục tổng hợp điện thoại
        "https://cellphones.com.vn/mobile.html",

        // 2. Danh mục điện thoại theo từng thương hiệu chính hãng trên CellphoneS
        "https://cellphones.com.vn/mobile/apple.html",
        "https://cellphones.com.vn/mobile/samsung.html",
        "https://cellphones.com.vn/mobile/xiaomi.html",
        "https://cellphones.com.vn/mobile/oppo.html",
        "https://cellphones.com.vn/mobile/vivo.html",
        "https://cellphones.com.vn/mobile/realme.html",
        "https://cellphones.com.vn/mobile/honor.html",
        "https://cellphones.com.vn/mobile/tecno.html",
        "https://cellphones.com.vn/mobile/infinix.html",
        "https://cellphones.com.vn/mobile/nokia.html",
        "https://cellphones.com.vn/mobile/nothing-phone.html",
        "https://cellphones.com.vn/mobile/nubia.html",
        "https://cellphones.com.vn/mobile/oneplus.html",
        "https://cellphones.com.vn/mobile/asus.html",
        "https://cellphones.com.vn/mobile/benco.html",
        "https://cellphones.com.vn/mobile/itel.html",
        "https://cellphones.com.vn/mobile/masstel.html",
        "https://cellphones.com.vn/mobile/sony.html",
        "https://cellphones.com.vn/mobile/tcl.html",
        "https://cellphones.com.vn/mobile/huawei.html",

        // 3. Danh mục nhóm nhu cầu & công nghệ điện thoại
        "https://cellphones.com.vn/mobile/dien-thoai-pho-thong.html",
        "https://cellphones.com.vn/mobile/dien-thoai-gaming.html",
        "https://cellphones.com.vn/mobile/dien-thoai-pin-trau.html",
        "https://cellphones.com.vn/mobile/dien-thoai-chup-anh-quay-phim.html",
        "https://cellphones.com.vn/mobile/ai.html"
    };

    public static CrawlerOptions Parse(string[] args)
    {
        var o = new CrawlerOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string NeedValue()
            {
                if (i + 1 >= args.Length) throw new ArgumentException($"Thiếu giá trị cho {a}");
                return args[++i];
            }
            switch (a)
            {
                case "--target": o.Target = int.Parse(NeedValue()); break;
                case "--max-load-more": o.MaxLoadMore = int.Parse(NeedValue()); break;
                case "--delay": o.DelayMs = int.Parse(NeedValue()); break;
                case "--headful": o.Headful = true; break;
                case "--screenshots": o.Screenshots = true; break;
                case "--output": o.Output = NeedValue(); break;
                case "--category": o.Categories = new List<string> { NeedValue() }; break;
                case "--help": ShowHelp(); Environment.Exit(0); break;
                default: throw new ArgumentException($"Tham số không hỗ trợ: {a}. Chạy --help.");
            }
        }
        if (o.Target is < 1 or > 1000) throw new ArgumentException("--target phải trong khoảng 1..1000");
        if (o.MaxLoadMore is < 0 or > 100) throw new ArgumentException("--max-load-more phải trong khoảng 0..100");
        if (o.DelayMs is < 1000 or > 60000) throw new ArgumentException("--delay phải trong khoảng 1000..60000 ms");
        if (o.Categories.Any(u => !UrlRules.IsAllowedCategory(u))) throw new ArgumentException("--category phải là danh mục điện thoại công khai trên cellphones.com.vn, không có query");
        return o;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("CellphoneS Crawler | EC312 Lab 3 - Danh mục Điện thoại");
        Console.WriteLine("dotnet run -- --target 500 --headful --screenshots");
        Console.WriteLine("  --target N           Số sản phẩm cần crawl (mặc định 500)");
        Console.WriteLine("  --delay N            Giãn cách request tính bằng ms, tối thiểu 1000ms (mặc định 1800)");
        Console.WriteLine("  --max-load-more N    Số lượt bấm xem thêm mỗi danh mục (mặc định 30)");
        Console.WriteLine("  --category URL       Chỉ crawl URL danh mục điện thoại xác định");
        Console.WriteLine("  --headful            Hiển thị cửa sổ Chromium");
        Console.WriteLine("  --screenshots        Chụp ảnh danh mục và sản phẩm đầu tiên");
        Console.WriteLine("  --output DIR         Thư mục CSV/ảnh/log (mặc định output/phones)");
    }
}

public static class UrlRules
{
    private static readonly HashSet<string> ExcludedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "mobile", "tablet", "laptop", "phu-kien", "thiet-bi-am-thanh", "dong-ho-thong-minh",
        "nha-thong-minh", "do-gia-dung", "tivi", "may-tinh-de-ban", "man-hinh", "may-in",
        "dien-may", "hang-cu", "thu-cu-doi-moi", "sim-so-dep", "uu-dai", "tin-tuc",
        "sforum", "khuyen-mai", "cart", "order", "hoi-dap", "dich-vu", "chinh-sach", "lien-he"
    };

    public static bool IsAllowedCategory(string s)
    {
        if (!Uri.TryCreate(s, UriKind.Absolute, out var u)) return false;
        return u.Scheme == "https" && u.Host.Equals("cellphones.com.vn", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrEmpty(u.Query) && string.IsNullOrEmpty(u.Fragment)
            && (u.AbsolutePath.Equals("/mobile.html", StringComparison.OrdinalIgnoreCase)
                || u.AbsolutePath.StartsWith("/mobile/", StringComparison.OrdinalIgnoreCase))
            && u.AbsolutePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
    }

    // Trang sản phẩm CellphoneS có đường dẫn .html tại cấp gốc, ví dụ: /iphone-15.html hoặc /dien-thoai-samsung-galaxy-s24.html
    public static string? NormalizeProductUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        if (!Uri.TryCreate(input, UriKind.Absolute, out var u))
        {
            if (!Uri.TryCreate(new Uri("https://cellphones.com.vn"), input, out u)) return null;
        }

        if (u.Scheme != "https"
            || !u.Host.Equals("cellphones.com.vn", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(u.Query) || !u.AbsolutePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            || u.AbsolutePath.Count(c => c == '/') != 1) return null;

        var slug = u.AbsolutePath.Trim('/').Replace(".html", "");
        if (ExcludedSlugs.Contains(slug)) return null;

        return new UriBuilder(u) { Fragment = "", Query = "" }.Uri.AbsoluteUri;
    }
}
