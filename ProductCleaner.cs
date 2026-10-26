using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CellphoneSCrawler;

public static class ProductCleaner
{
    // Danh sách các thương hiệu điện thoại chính hãng phân phối tại CellphoneS
    private static readonly string[] KnownBrands = {
        "Apple", "Samsung", "Xiaomi", "OPPO", "vivo", "realme", "HONOR", "TECNO",
        "Infinix", "Nokia", "Nothing", "Nubia", "OnePlus", "Asus", "Benco", "Huawei",
        "Itel", "Masstel", "Meizu", "Sony", "TCL", "POCO", "ZTE", "Motorola", "Google"
    };

    // Các từ khóa nhận diện phụ kiện, máy tính bảng, tai nghe... cần loại trừ khỏi nhóm điện thoại
    private static readonly Regex NonPhonePattern = new(
        @"(tai\s*nghe|airpods|earbuds|headphone|buds|headset|freebuds|earphone|" +
        @"ốp\s*lưng|bao\s*da|cường\s*lực|dán\s*màn\s*hình|dán\s*lưng|kính\s*bảo\s*vệ|" +
        @"củ\s*sạc|cốc\s*sạc|cáp\s*sạc|dây\s*sạc|sạc\s*nhanh|sạc\s*dự\s*phòng|pin\s*dự\s*phòng|" +
        @"đế\s*sạc|giá\s*đỡ|gậy\s*chụp|thẻ\s*nhớ|sim\s*4g|sim\s*5g|bút\s*cảm\s*ứng|adapter|" +
        @"ipad|galaxy\s*tab|máy\s*tính\s*bảng|xiaomi\s*pad|oppo\s*pad|lenovo\s*tab|" +
        @"macbook|laptop|thinkpad|apple\s*watch|galaxy\s*watch|smartwatch|đồng\s*hồ|" +
        @"loa\s*bluetooth|chuột|bàn\s*phím|camera\s*hành\s*trình)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static Product? Clean(RawProduct raw, string productUrl)
    {
        var name = NormalizeText(raw.Name);
        if (name.Length < 6) return null;

        // Kiểm tra loại trừ nếu lọt sản phẩm phụ kiện, tai nghe, máy tính bảng...
        if (NonPhonePattern.IsMatch(name) || NonPhonePattern.IsMatch(productUrl))
        {
            return null;
        }

        // Giá điện thoại hợp lệ tối thiểu từ 50,000 VND (điện thoại phổ thông) đến 200,000,000 VND
        if (!long.TryParse(raw.DisplayPrice, NumberStyles.None, CultureInfo.InvariantCulture, out var current) || current < 50000)
            return null;

        long? original = null;
        if (long.TryParse(raw.ComparePrice, NumberStyles.None, CultureInfo.InvariantCulture, out var old)
            && old > current) original = old;

        var img = NormalizeImageUrl(raw.ImageUrl, productUrl);
        var brand = NormalizeText(raw.Brand);
        if (string.IsNullOrEmpty(brand) || brand.Equals("cellphones", StringComparison.OrdinalIgnoreCase))
        {
            brand = DetectBrand(name, productUrl);
        }

        var sku = NormalizeText(raw.SourceSku);
        if (string.IsNullOrEmpty(sku))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(productUrl.ToLowerInvariant()));
            sku = "CPS-" + Convert.ToHexString(hash)[..12];
        }
        else
        {
            sku = "CPS-" + new string(sku.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_').ToArray());
            if (sku.Length > 70) sku = sku[..70];
            if (sku == "CPS-")
            {
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(productUrl.ToLowerInvariant()));
                sku = "CPS-" + Convert.ToHexString(hash)[..12];
            }
        }

        var description = NormalizeDescription(raw.Description);
        if (description.Length > 7000) description = description[..7000];

        // Chuẩn danh mục sản phẩm: "Điện thoại" theo yêu cầu Lab 3
        var category = "Điện thoại";

        return new Product(
            sku,
            name,
            brand,
            current,
            original,
            description,
            img,
            productUrl,
            category,
            NormalizeText(raw.Availability),
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
    }

    public static string NormalizeText(string? input)
    {
        var t = WebUtility.HtmlDecode(input ?? "").Replace('\u00a0', ' ');
        return string.Join(' ', t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    public static string NormalizeDescription(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var decoded = WebUtility.HtmlDecode(input).Replace('\u00a0', ' ');
        var lines = decoded.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(NormalizeText)
                           .Where(l => !string.IsNullOrEmpty(l));
        return string.Join("\n", lines);
    }

    private static string NormalizeImageUrl(string? image, string pageUrl)
    {
        image = (image ?? "").Trim();
        if (image.StartsWith("//")) image = "https:" + image;
        if (!Uri.TryCreate(image, UriKind.Absolute, out var uri) &&
            !Uri.TryCreate(new Uri(pageUrl), image, out uri)) return "";
        return uri is not null && (uri.Scheme is "http" or "https") ? uri.AbsoluteUri : "";
    }

    public static string DetectBrand(string name, string url)
    {
        var lower = (name + " " + url).ToLowerInvariant();

        // 1. Nhận diện các dòng đặc thù
        if (lower.Contains("iphone") || lower.Contains("apple")) return "Apple";
        if (lower.Contains("galaxy") || lower.Contains("samsung")) return "Samsung";
        if (lower.Contains("redmi") || lower.Contains("poco") || lower.Contains("xiaomi")) return "Xiaomi";
        if (lower.Contains("rog phone") || lower.Contains("zenfone") || lower.Contains("asus")) return "Asus";
        if (lower.Contains("nothing phone") || lower.Contains("nothing")) return "Nothing";
        if (lower.Contains("realme")) return "realme";
        if (lower.Contains("oppo")) return "OPPO";
        if (lower.Contains("vivo")) return "vivo";
        if (lower.Contains("honor")) return "HONOR";
        if (lower.Contains("tecno")) return "TECNO";
        if (lower.Contains("infinix")) return "Infinix";
        if (lower.Contains("nokia")) return "Nokia";
        if (lower.Contains("nubia")) return "Nubia";
        if (lower.Contains("oneplus")) return "OnePlus";
        if (lower.Contains("benco")) return "Benco";
        if (lower.Contains("itel")) return "Itel";
        if (lower.Contains("masstel")) return "Masstel";
        if (lower.Contains("meizu")) return "Meizu";
        if (lower.Contains("sony")) return "Sony";
        if (lower.Contains("tcl")) return "TCL";
        if (lower.Contains("huawei")) return "Huawei";

        // 2. Đối chiếu danh sách tổng
        foreach (var brand in KnownBrands)
        {
            if (name.Contains(brand, StringComparison.OrdinalIgnoreCase))
                return brand;
        }

        return "";
    }
}
