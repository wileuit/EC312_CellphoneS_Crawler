using System.Text.Json;
using Microsoft.Playwright;

namespace CellphoneSCrawler;

public sealed class CrawlEngine
{
    private readonly CrawlerOptions _options;
    private readonly Dictionary<string, ProductLink> _links = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Product> _products = new();
    private readonly List<FailedUrl> _failed = new();
    private readonly HashSet<string> _seenSku = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CrawlEngine(CrawlerOptions options) => _options = options;

    public async Task RunAsync(CancellationToken ct)
    {
        var output = Path.GetFullPath(_options.Output);
        Directory.CreateDirectory(output);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !_options.Headful
        });

        // Kế thừa từ TikiCrawler: Thiết lập User-Agent và Viewport chuẩn người dùng thật để tránh bị hạn chế
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            Locale = "vi-VN",
            ViewportSize = new ViewportSize { Width = 1380, Height = 900 }
        });

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(35000);

        try
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("BƯỚC 1: THU THẬP LIÊN KẾT TỪ DANH MỤC ĐIỆN THOẠI CELLPHONES (TỔNG HỢP & THEO HÃNG)");
            Console.WriteLine("================================================================================");

            foreach (var category in _options.Categories)
            {
                ct.ThrowIfCancellationRequested();
                Console.WriteLine($"\n[Danh mục] {category}");

                try
                {
                    var response = await page.GotoAsync(category, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 45000
                    });

                    if (response?.Status is 403 or 429)
                        throw new BlockedSiteException($"Website giới hạn truy cập: HTTP {response.Status}");

                    // Chờ các kết nối mạng ngầm (NetworkIdle) để Nuxt SPA nạp xong phần lớn dữ liệu
                    try
                    {
                        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 6000 });
                    }
                    catch (TimeoutException) { /* Cho phép tiếp tục nếu có analytics/long-polling ngầm */ }

                    await Task.Delay(_options.DelayMs, ct);

                    if (_options.Screenshots && _options.Categories.IndexOf(category) == 0)
                    {
                        var shotPath = Path.Combine(output, "01_category_first.png");
                        await page.ScreenshotAsync(new PageScreenshotOptions { Path = shotPath, FullPage = false });
                        Console.WriteLine($"  [Ảnh chụp] Đã lưu minh chứng danh mục tại: {shotPath}");
                    }

                    var emptyClicks = 0;
                    for (var click = 0; click <= _options.MaxLoadMore; click++)
                    {
                        ct.ThrowIfCancellationRequested();

                        // Cuộn chuột từ từ mô phỏng người dùng và kích hoạt lazy-load
                        for (var s = 0; s < 3; s++)
                        {
                            await page.EvaluateAsync("() => { window.scrollBy(0, 800); return 1; }");
                            await Task.Delay(350, ct);
                        }

                        var before = _links.Count;
                        var script = ReadScript("discover-links.js");
                        var json = await page.EvaluateAsync<string>(script);
                        var items = JsonSerializer.Deserialize<ProductLink[]>(json ?? "[]", JsonOptions) ?? Array.Empty<ProductLink>();

                        foreach (var item in items)
                        {
                            var normalized = UrlRules.NormalizeProductUrl(item.Url);
                            if (normalized is not null)
                            {
                                _links.TryAdd(normalized, item);
                            }
                        }

                        var added = _links.Count - before;
                        Console.WriteLine($"  Lượt {click}: Đã tích lũy {_links.Count} URL hợp lệ (+{added} link mới)");

                        // Nếu đã đủ số lượng link gấp đôi mục tiêu thì có thể chuyển sang bước đọc chi tiết
                        if (_links.Count >= _options.Target * 2) break;
                        if (click == _options.MaxLoadMore) break;

                        // Nhấn nút Xem thêm sản phẩm
                        var clicked = await page.EvaluateAsync<bool>(ReadScript("click-load-more.js"));
                        if (!clicked) break;

                        if (added == 0) emptyClicks++; else emptyClicks = 0;
                        if (emptyClicks >= 3) break;

                        await Task.Delay(_options.DelayMs, ct);
                    }
                }
                catch (BlockedSiteException) { throw; }
                catch (Exception ex)
                {
                    _failed.Add(new FailedUrl(category, "Không tải được danh mục: " + ex.Message));
                    Console.WriteLine("  [Cảnh báo danh mục] " + ex.Message);
                }

                if (_links.Count >= _options.Target * 2) break;
            }

            if (_links.Count == 0)
            {
                Console.WriteLine("\n[CẢNH BÁO] Không tìm thấy link sản phẩm. Kiểm tra cấu trúc website. KHÔNG tạo dữ liệu giả.");
                return;
            }

            Console.WriteLine("\n================================================================================");
            Console.WriteLine($"BƯỚC 2: TRÍCH XUẤT CHI TIẾT TỐI ĐA {_options.Target} SẢN PHẨM TỪ {_links.Count} URL BAN ĐẦU");
            Console.WriteLine("================================================================================");

            // Sử dụng hàng đợi để xử lý tuần tự và bổ sung các phiên bản cấu hình/dung lượng thực tế
            var queue = new Queue<ProductLink>(_links.Values);
            var visited = new HashSet<string>(_links.Keys, StringComparer.OrdinalIgnoreCase);
            var index = 0;

            while (queue.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                if (_products.Count >= _options.Target) break;

                var item = queue.Dequeue();
                index++;

                try
                {
                    var response = await page.GotoAsync(item.Url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 40000
                    });

                    if (response?.Status is 403 or 429)
                        throw new BlockedSiteException($"HTTP {response.Status}, website giới hạn tần suất truy cập");
                    if (response?.Status is >= 400)
                        throw new InvalidOperationException($"HTTP {response.Status}");

                    // Cuộn nhẹ để kích hoạt tải thông số kỹ thuật và hình ảnh
                    await page.EvaluateAsync("() => { window.scrollBy(0, 600); return 1; }");
                    await Task.Delay(_options.DelayMs, ct);

                    var json = await page.EvaluateAsync<string>(ReadScript("extract-product.js"));
                    var raw = JsonSerializer.Deserialize<RawProduct>(json ?? "{}", JsonOptions);

                    // Khám phá các phiên bản dung lượng/cấu hình liên kết trực tiếp trên trang thật
                    if (raw?.VariantUrls is { Count: > 0 })
                    {
                        foreach (var vUrl in raw.VariantUrls)
                        {
                            var normalizedVar = UrlRules.NormalizeProductUrl(vUrl);
                            if (normalizedVar is not null && visited.Add(normalizedVar))
                            {
                                queue.Enqueue(new ProductLink { Url = normalizedVar, Title = "" });
                            }
                        }
                    }

                    var product = raw is null ? null : ProductCleaner.Clean(raw, item.Url);

                    if (product is null)
                    {
                        var reason = "Thiếu tên sản phẩm, không phải điện thoại hoặc không nhận dạng được giá hợp lệ (>= 50,000 VND)";
                        _failed.Add(new FailedUrl(item.Url, reason));
                        Console.WriteLine($"  [{index}] Bỏ qua: {reason} | URL: {item.Url}");
                        continue;
                    }

                    if (!_seenSku.Add(product.Sku))
                    {
                        var reason = $"SKU bị trùng ({product.Sku})";
                        _failed.Add(new FailedUrl(item.Url, reason));
                        Console.WriteLine($"  [{index}] Bỏ qua: {reason}");
                        continue;
                    }

                    _products.Add(product);
                    var saleInfo = product.OriginalPrice.HasValue
                        ? $"{product.SellingPrice:N0}đ (Giá gốc: {product.OriginalPrice.Value:N0}đ)"
                        : $"{product.SellingPrice:N0}đ";

                    Console.WriteLine($"  [{_products.Count}/{_options.Target}] OK: {product.Name} | {saleInfo} | Hãng: {(string.IsNullOrEmpty(product.Brand) ? "Chưa rõ" : product.Brand)}");

                    if (_options.Screenshots && _products.Count == 1)
                    {
                        var shotProduct = Path.Combine(output, "02_first_product.png");
                        await page.ScreenshotAsync(new PageScreenshotOptions { Path = shotProduct, FullPage = false });
                        Console.WriteLine($"  [Ảnh chụp] Đã lưu minh chứng sản phẩm đầu tiên tại: {shotProduct}");
                    }

                    // Tự động lưu định kỳ mỗi 10 sản phẩm
                    if (_products.Count % 10 == 0)
                    {
                        Save(output);
                        Console.WriteLine($"    -> [Đã lưu tự động]: {_products.Count} sản phẩm đã xuất CSV tại {output}");
                    }
                }
                catch (BlockedSiteException) { throw; }
                catch (Exception ex)
                {
                    _failed.Add(new FailedUrl(item.Url, ex.GetType().Name + ": " + ex.Message));
                    Console.WriteLine($"  [{index}] Lỗi kết nối/trích xuất: {ex.Message}");
                }
            }
        }
        finally
        {
            Save(output);
            await context.CloseAsync();

            Console.WriteLine("\n================================================================================");
            Console.WriteLine($"KẾT QUẢ THU THẬP DỮ LIỆU ĐIỆN THOẠI CELLPHONES:");
            Console.WriteLine($" - Số sản phẩm hợp lệ thu được : {_products.Count} / {_options.Target}");
            Console.WriteLine($" - Số URL bỏ qua hoặc gặp lỗi   : {_failed.Count}");
            Console.WriteLine($" - Thư mục lưu file kết quả      : {output}");
            Console.WriteLine($"   + {Path.Combine(output, "cellphones_raw_products.csv")}");
            Console.WriteLine($"   + {Path.Combine(output, "woocommerce_import.csv")}");
            Console.WriteLine($"   + {Path.Combine(output, "skipped_urls.csv")}");
            Console.WriteLine($"   + {Path.Combine(output, "run_summary.txt")}");
            Console.WriteLine("================================================================================");

            if (_products.Count < _options.Target)
            {
                Console.WriteLine($"[CHÚ Ý] Website cung cấp {_products.Count} sản phẩm độc lập (chưa đạt {_options.Target}). Đã ghi nhận số lượng thực tế, tuyệt đối không tạo dữ liệu giả.");
            }
        }
    }

    private static string ReadScript(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Scripts", name));
    private void Save(string output) => CsvExporter.Export(output, _products, _failed);
}

public sealed class BlockedSiteException : Exception
{
    public BlockedSiteException(string message) : base(message) { }
}
