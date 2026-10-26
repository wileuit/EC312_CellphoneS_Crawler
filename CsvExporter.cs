using System.Text;

namespace CellphoneSCrawler;

public static class CsvExporter
{
    private static readonly string[] RawHeaders = {
        "Product URL", "SKU", "Name", "Brand", "Selling price (VND)", "Original price (VND)",
        "Description", "Image URL", "Category", "Availability", "Crawled at UTC"
    };

    // Chuẩn WooCommerce Product CSV Import Schema, đối chiếu trực tiếp Reference/DataMapping_Thay.xlsx
    private static readonly string[] WooHeaders = {
        "Type", "SKU", "Name", "Published", "Is featured?", "Visibility in catalog", "Short description",
        "Description", "Tax status", "In stock?", "Backorders allowed?", "Sold individually?",
        "Allow customer reviews?", "Regular price", "Sale price", "Categories", "Images", "Position",
        "Attribute 1 name", "Attribute 1 value(s)", "Attribute 1 visible", "Attribute 1 global"
    };

    public static void Export(string dir, IReadOnlyList<Product> products, IReadOnlyList<FailedUrl> failures)
    {
        Directory.CreateDirectory(dir);

        // 1. File dữ liệu gốc thô
        WriteCsv(Path.Combine(dir, "cellphones_raw_products.csv"), RawHeaders, products.Select(p => new[] {
            p.ProductUrl,
            p.Sku,
            p.Name,
            p.Brand,
            p.SellingPrice.ToString(),
            p.OriginalPrice?.ToString() ?? "",
            p.Description,
            p.ImageUrl,
            p.Category,
            p.Availability,
            p.CrawledAtUtc
        }));

        // 2. File nhập trực tiếp WooCommerce (Trạng thái Published=-1 để lưu ở dạng Draft)
        WriteCsv(Path.Combine(dir, "woocommerce_import.csv"), WooHeaders, products.Select(p => new[] {
            "simple",                                            // Type
            p.Sku,                                              // SKU
            p.Name,                                             // Name
            "-1",                                               // Published (-1 = Bản nháp/Draft, không tự động công khai)
            "0",                                                // Is featured?
            "visible",                                          // Visibility in catalog
            GetShortDescription(p.Description),                 // Short description
            p.Description,                                      // Description (kèm thông số kỹ thuật)
            "taxable",                                          // Tax status
            p.Availability.Equals("OutOfStock", StringComparison.OrdinalIgnoreCase) ? "0" : "1", // In stock?
            "0",                                                // Backorders allowed?
            "0",                                                // Sold individually?
            "1",                                                // Allow customer reviews?
            (p.OriginalPrice ?? p.SellingPrice).ToString(),     // Regular price (VND)
            p.OriginalPrice.HasValue ? p.SellingPrice.ToString() : "", // Sale price (VND)
            p.Category,                                         // Categories
            p.ImageUrl,                                         // Images
            "0",                                                // Position
            string.IsNullOrWhiteSpace(p.Brand) ? "" : "Thương hiệu", // Attribute 1 name
            p.Brand,                                            // Attribute 1 value(s)
            string.IsNullOrWhiteSpace(p.Brand) ? "" : "1",      // Attribute 1 visible
            "0"                                                 // Attribute 1 global
        }));

        // 2b. File dự phòng không có URL ảnh để kiểm thử nhanh khi nguồn CDN gặp sự cố
        WriteCsv(Path.Combine(dir, "woocommerce_import_no_images.csv"), WooHeaders, products.Select(p => new[] {
            "simple",
            p.Sku,
            p.Name,
            "-1",
            "0",
            "visible",
            GetShortDescription(p.Description),
            p.Description,
            "taxable",
            p.Availability.Equals("OutOfStock", StringComparison.OrdinalIgnoreCase) ? "0" : "1",
            "0",
            "0",
            "1",
            (p.OriginalPrice ?? p.SellingPrice).ToString(),
            p.OriginalPrice.HasValue ? p.SellingPrice.ToString() : "",
            p.Category,
            "",                                                 // Images để trống
            "0",
            string.IsNullOrWhiteSpace(p.Brand) ? "" : "Thương hiệu",
            p.Brand,
            string.IsNullOrWhiteSpace(p.Brand) ? "" : "1",
            "0"
        }));

        // 3. File ghi nhận các URL lỗi hoặc bị bỏ qua
        WriteCsv(Path.Combine(dir, "skipped_urls.csv"), new[] { "URL", "Reason" },
            failures.Select(f => new[] { f.Url, f.Reason }));

        // 4. Báo cáo thống kê
        var complete = products.Count(p => p.Name.Length > 0 && p.SellingPrice > 0 &&
            p.ImageUrl.Length > 0 && p.Description.Length > 0 && p.Brand.Length > 0);
        File.WriteAllText(Path.Combine(dir, "run_summary.txt"),
            $"EC312 LAB 03 - CELLPHONES CRAWLER (DANH MỤC ĐIỆN THOẠI)\n" +
            $"Thời gian UTC: {DateTime.UtcNow:O}\n" +
            $"Số sản phẩm điện thoại thu được: {products.Count}\n" +
            $"Có đủ 6 trường chính (Tên + Giá + Hình + Mô tả/Thông số + Hãng + URL): {complete}\n" +
            $"Số URL bỏ qua/lỗi: {failures.Count}\n" +
            "Danh mục mặc định trong WooCommerce: Điện thoại\n" +
            "Trạng thái Published trong WooCommerce: -1 (Nháp / Draft) theo đúng yêu cầu đề bài.\n" +
            "Ánh xạ cột đối chiếu hoàn chỉnh theo Reference/DataMapping_Thay.xlsx.\n",
            new UTF8Encoding(true));
    }

    private static string GetShortDescription(string desc)
    {
        if (string.IsNullOrWhiteSpace(desc)) return "";
        var firstLine = desc.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? desc;
        return firstLine.Length > 220 ? firstLine[..220] + "..." : firstLine;
    }

    private static void WriteCsv(string path, IEnumerable<string> headers, IEnumerable<string[]> rows)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        writer.NewLine = "\r\n";
        writer.WriteLine(string.Join(",", headers.Select(Escape)));
        foreach (var row in rows) writer.WriteLine(string.Join(",", row.Select(Escape)));
    }

    private static string Escape(string? s)
    {
        s ??= "";
        // Chặn CSV formula injection khi mở bằng Excel
        if (s.Length > 0 && s[0] is '=' or '+' or '@' or '\t' or '\r') s = "'" + s;
        if (s.Contains('"') || s.Contains(',') || s.Contains('\r') || s.Contains('\n'))
            return '"' + s.Replace("\"", "\"\"") + '"';
        return s;
    }
}
