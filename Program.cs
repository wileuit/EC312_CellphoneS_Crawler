using CellphoneSCrawler;

try
{
    var options = CrawlerOptions.Parse(args);
    using var cancel = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancel.Cancel(); };
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    Console.WriteLine("EC312 – CELLPHONES SMARTPHONE CRAWLER (C# + PLAYWRIGHT)");
    Console.WriteLine($"Mục tiêu: {options.Target} sản phẩm điện thoại | Đầu ra: {options.Output} | Chế độ: {(options.Headful ? "Hiển thị browser" : "Headless")}");
    await new CrawlEngine(options).RunAsync(cancel.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Đã dừng bởi người dùng. CSV thu được trước đó vẫn lưu tại thư mục đầu ra.");
}
catch (BlockedSiteException ex)
{
    Console.Error.WriteLine("Website từ chối/giới hạn: " + ex.Message + ". Không dùng kỹ thuật vượt chặn.");
    Environment.ExitCode = 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Lỗi: " + ex.Message);
    Environment.ExitCode = 1;
}
