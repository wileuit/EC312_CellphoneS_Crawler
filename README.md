# EC312 LAB 03 – CELLPHONES SMARTPHONE CRAWLER (C# + PLAYWRIGHT)

> **Môn học:** EC312 – Thương mại điện tử & Hệ thống thông tin  
> **Ngôn ngữ & Nền tảng:** C# (.NET 8.0 SDK), Microsoft.Playwright, WooCommerce CSV Importer Schema  
> **Website mục tiêu:** Danh mục **Điện thoại di động CellphoneS** (`https://cellphones.com.vn/mobile.html`)  
> **Kết quả thu thập thực tế:** **477 sản phẩm điện thoại di động chính hãng**, 100% dữ liệu thật, không trùng lặp, đầy đủ 6+ trường thông tin bắt buộc, lưu tại thư mục `output/phones/`  
> **Bảo toàn dữ liệu:** Bộ dữ liệu 154 sản phẩm tai nghe cào trước đó đã được sao lưu toàn vẹn tại `output_backup_headphones/`

---

## 1. Phân tích & Kế thừa từ TikiCrawler (Project mẫu của Giảng viên)

Dự án được xây dựng dựa trên việc phân tích kiến trúc dự án mẫu `TikiCrawler2026` của giảng viên, chọn lọc và tinh chỉnh các kỹ thuật cốt lõi để thích ứng tối ưu với nền tảng Single Page App (Nuxt.js / Vue.js) của CellphoneS:

| Kỹ thuật trong TikiCrawler | Triển khai & Cải tiến trong CellphoneS Crawler | Lợi ích thực tế đạt được |
|---|---|---|
| **Giả lập người dùng thật (User-Agent & Viewport)** | Cấu hình `BrowserNewContextOptions` với `UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36..."` và Viewport `1380x900`. | Tránh bị cơ chế chống bot của CellphoneS chặn; nhận đầy đủ DOM động và hình ảnh. |
| **Đồng bộ mạng ngầm (`NetworkIdle`)** | Kết hợp `GotoAsync(..., DOMContentLoaded)` và `WaitForLoadStateAsync(LoadState.NetworkIdle, 6000ms)` có bọc `try-catch`. | Đảm bảo Nuxt.js hydrat hóa xong các component sản phẩm và nút bấm tương tác trước khi đọc dữ liệu. |
| **Cuộn chuột từng bước kích hoạt Lazy-load** | Vòng lặp cuộn chuột gia số `window.scrollBy(0, 800)` kèm quãng nghỉ 350ms mô phỏng thao tác đọc lướt của con người. | Kích hoạt sự kiện `IntersectionObserver` nạp đầy đủ danh sách ảnh, thông số kỹ thuật và các khối card sản phẩm. |
| **Bóc tách đa tầng (Fallback Locators)** | Đa tầng trích xuất: JSON-LD Schema.org `Product` → OpenGraph Meta (`og:title`, `og:image`) → DOM Selector chuyên biệt (`.technical-content-item`, `.product__price--show`). | Dữ liệu không bị gián đoạn hay mất mát khi website thay đổi cấu trúc hiển thị hoặc cập nhật theme. |
| **Chịu lỗi độc lập (Fault Tolerance) & Auto-Save** | Bọc `try-catch` riêng biệt trên từng URL; ghi nhận URL lỗi/bỏ qua vào `skipped_urls.csv`; tự động lưu file kết quả mỗi 10 sản phẩm. | Quá trình thu thập an toàn, có thể ngắt bằng `Ctrl + C` bất kỳ lúc nào mà không bị mất dữ liệu đã cào. |
| **Mở rộng phân trang (Load-more Automation)** | Tự động phát hiện và click nút "Xem thêm ... sản phẩm" (`.button__show-more-product`, `.btn-show-more`). | Nạp liên tục hàng trăm sản phẩm ẩn trên cùng một trang danh mục mà không bị giới hạn ở 20-30 máy ban đầu. |

---

## 2. Chiến lược khám phá & Thu thập 477+ sản phẩm thật

Để đạt quy mô lớn mà **tuyệt đối không tạo dữ liệu giả, không nhân bản dòng**, crawler vận hành theo chiến lược 4 lớp:

1. **Quét mạng lưới 26 danh mục điện thoại chính hãng (Đã xác minh HTTP 200 OK):**
   * **Danh mục tổng hợp:** `https://cellphones.com.vn/mobile.html` (chứa hơn 900 sản phẩm).
   * **Theo từng thương hiệu chính hãng:**
     * Apple iPhone: `https://cellphones.com.vn/mobile/apple.html`
     * Samsung Galaxy: `https://cellphones.com.vn/mobile/samsung.html`
     * Xiaomi / POCO: `https://cellphones.com.vn/mobile/xiaomi.html`
     * OPPO: `https://cellphones.com.vn/mobile/oppo.html`
     * vivo: `https://cellphones.com.vn/mobile/vivo.html`
     * realme: `https://cellphones.com.vn/mobile/realme.html`
     * HONOR: `https://cellphones.com.vn/mobile/honor.html`
     * TECNO: `https://cellphones.com.vn/mobile/tecno.html`
     * Infinix: `https://cellphones.com.vn/mobile/infinix.html`
     * Nokia: `https://cellphones.com.vn/mobile/nokia.html`
     * Nothing Phone: `https://cellphones.com.vn/mobile/nothing-phone.html`
     * Nubia: `https://cellphones.com.vn/mobile/nubia.html`
     * OnePlus: `https://cellphones.com.vn/mobile/oneplus.html`
     * Asus (ROG Phone / Zenfone): `https://cellphones.com.vn/mobile/asus.html`
     * Benco, itel, Masstel, Sony, TCL, Huawei...
   * **Theo phân khúc nhu cầu:** Điện thoại phổ thông, gaming, pin trâu, chụp ảnh, điện thoại AI.
2. **Khám phá các phiên bản cấu hình/dung lượng thực tế (Linked Variants):**
   * Khi mở từng trang sản phẩm, engine tự động quét khối `<div class="list-linked">` (ví dụ: các bản 128GB, 256GB, 512GB, 1TB) và nạp các URL phiên bản này vào hàng đợi `Queue<ProductLink>`.
   * Nhờ đó, các phiên bản cấu hình thực tế được ghi nhận độc lập với SKU, giá và thông số riêng của chính nó.
3. **Bộ lọc nghiêm ngặt loại trừ phụ kiện & sản phẩm khác:**
   * Sử dụng biểu thức chính quy (Regex) loại bỏ triệt để: tai nghe, ốp lưng, bao da, củ sạc, cáp sạc, pin dự phòng, kính cường lực, máy tính bảng (iPad, Galaxy Tab), laptop, đồng hồ thông minh (Apple Watch).
   * Chỉ chấp nhận sản phẩm thuộc nhóm điện thoại với mức giá hợp lệ (>= 50,000 VND).
4. **Chuẩn hóa & Chống trùng lặp (Deduplication):**
   * Chuẩn hóa URL (bỏ query parameter, hash fragment).
   * Tạo SKU duy nhất theo tiền tố `CPS-` kết hợp mã sản phẩm nguồn hoặc hàm băm SHA-256 URL.
   * `HashSet<string> _seenSku` đảm bảo mỗi dòng trong CSV là một sản phẩm hoàn toàn độc lập.

---

## 3. Cấu trúc thư mục dự án

```text
EC312_CellphoneS_Crawler/
├── .vscode/
│   ├── launch.json                      # Cấu hình Run & Debug 500 điện thoại trong VS Code
│   └── tasks.json                       # Tự động build trước khi debug
├── bin/ / obj/                          # Thư mục biên dịch .NET 8.0
├── Docs/
│   └── HUONG_DAN_MINH_CHUNG.md          # Danh mục vị trí chụp ảnh minh chứng cho Báo cáo Lab 3
├── Reference/
│   └── DataMapping_Thay.xlsx            # Bảng đối chiếu trường dữ liệu mẫu của giảng viên
├── Scripts/
│   ├── click-load-more.js               # Script click nút "Xem thêm sản phẩm" (.button__show-more-product)
│   ├── discover-links.js                # Script quét và lọc URL sản phẩm điện thoại từ danh mục
│   └── extract-product.js               # Script bóc tách JSON-LD, specs (RAM/ROM/Chip/Pin/Cam), màu sắc, variants
├── CellphoneSCrawler.cs                 # Engine Playwright quản lý hàng đợi và điều phối phiên crawl
├── CellphoneSCrawler.csproj             # Cấu hình dự án C# .NET 8 và Playwright package
├── CsvExporter.cs                       # Xuất CSV chuẩn 22 cột WooCommerce và CSV thô UTF-8 BOM
├── Models.cs                            # Model dữ liệu, 26 URL danh mục, chuẩn hóa URL, CLI parser
├── ProductCleaner.cs                    # Làm sạch dữ liệu, nhận diện 25+ hãng điện thoại, lọc phụ kiện
├── Program.cs                           # Entrypoint dòng lệnh CLI
├── run-windows.ps1                      # Script PowerShell 1-click tự động chạy crawler
├── output_backup_headphones/            # Dữ liệu 154 sản phẩm tai nghe đã sao lưu an toàn
└── output/
    └── phones/                          # Thư mục xuất dữ liệu điện thoại chính thức
        ├── 01_category_first.png        # Ảnh chụp minh chứng trang danh mục đầu tiên
        ├── 02_first_product.png         # Ảnh chụp minh chứng trang chi tiết sản phẩm đầu tiên
        ├── cellphones_raw_products.csv  # Dữ liệu thô 11 cột gốc trích xuất từ website
        ├── woocommerce_import.csv       # Dữ liệu chuẩn 22 cột import WooCommerce (KÈM URL ẢNH)
        ├── woocommerce_import_no_images.csv # File dự phòng chuẩn 22 cột (ĐỂ TRỐNG CỘT ẢNH)
        ├── skipped_urls.csv             # Danh sách URL bị bỏ qua/lỗi kèm nguyên nhân
        └── run_summary.txt              # Báo cáo tổng kết số lượng và chất lượng trường dữ liệu
```

---

## 4. Bảng ánh xạ dữ liệu WooCommerce (22 cột chuẩn `DataMapping_Thay.xlsx`)

| STT | Cột WooCommerce | Kiểu dữ liệu | Nguồn trích xuất từ CellphoneS | Ghi chú & Quy chuẩn Lab 03 |
|:---:|---|---|---|---|
| 1 | `Type` | Chuỗi | Cố định `simple` | Sản phẩm đơn lẻ tiêu chuẩn WooCommerce. |
| 2 | `SKU` | Chuỗi | JSON-LD SKU / Mã định danh nguồn | Mã định danh duy nhất có tiền tố `CPS-`. |
| 3 | `Name` | Chuỗi | Thẻ `h1`, JSON-LD `name` | Tên đầy đủ, đã làm sạch khoảng trắng thừa. |
| 4 | **`Published`** | Số nguyên | **`-1`** | **Bản nháp (Draft)** theo đúng yêu cầu đề bài. |
| 5 | `Is featured?` | Số nguyên | `0` | Không gắn nhãn sản phẩm nổi bật mặc định. |
| 6 | `Visibility in catalog` | Chuỗi | `visible` | Hiển thị sản phẩm trong danh mục cửa hàng. |
| 7 | `Short description` | Chuỗi | Đoạn mở đầu <= 220 ký tự | Tóm tắt tính năng chính của máy. |
| 8 | `Description` | Chuỗi văn bản | Mô tả + Bảng thông số chi tiết + Màu sắc | Bóc tách chi tiết: Hệ điều hành, Chipset, RAM, ROM, Màn hình, Camera sau/trước, Pin, Màu sắc. |
| 9 | `Tax status` | Chuỗi | `taxable` | Trạng thái tính thuế tiêu chuẩn. |
| 10 | `In stock?` | Số nguyên | `1` hoặc `0` | `1` nếu còn hàng (InStock), `0` nếu tạm hết hàng. |
| 11 | `Backorders allowed?`| Số nguyên | `0` | Không cho phép đặt trước khi hết hàng. |
| 12 | `Sold individually?` | Số nguyên | `0` | Cho phép khách mua số lượng nhiều máy cùng lúc. |
| 13 | `Allow customer reviews?`| Số nguyên | `1` | Cho phép khách hàng để lại đánh giá. |
| 14 | `Regular price` | Số nguyên | Giá niêm yết (VND) | Giá gốc trước khuyến mãi (không kèm chữ "đ"). |
| 15 | `Sale price` | Số nguyên | Giá bán ưu đãi (VND) | Giá bán hiện tại thực tế (không kèm chữ "đ"). |
| 16 | `Categories` | Chuỗi | `Điện thoại` | Phân cấp danh mục WooCommerce chuẩn. |
| 17 | `Images` | URL | CDN ảnh CellphoneS | URL ảnh sản phẩm độ phân giải cao (`cdn2.cellphones.com.vn`). |
| 18 | `Position` | Số nguyên | `0` | Thứ tự hiển thị mặc định. |
| 19 | `Attribute 1 name` | Chuỗi | `Thương hiệu` | Khai báo thuộc tính hãng cho WooCommerce. |
| 20 | `Attribute 1 value(s)` | Chuỗi | Hãng nhận diện tự động | Apple, Samsung, Xiaomi, OPPO, vivo, realme, v.v. |
| 21 | `Attribute 1 visible` | Số nguyên | `1` | Hiển thị thuộc tính trên trang sản phẩm. |
| 22 | `Attribute 1 global` | Số nguyên | `0` | Thuộc tính riêng biệt theo từng sản phẩm. |

---

## 5. Hướng dẫn vận hành Crawler trên Windows VS Code

Mở **Terminal trong VS Code** (`Ctrl + \``) tại thư mục `EC312_CellphoneS_Crawler`:

### Lựa chọn 1: Chạy có giao diện Chromium + chụp ảnh minh chứng (Khuyên dùng)
```powershell
dotnet run -c Debug -- --target 500 --output output/phones --headful --screenshots
```
*Trình duyệt sẽ hiển thị trực quan các thao tác cuộn chuột, click xem thêm và tự động lưu 2 ảnh minh chứng `01_category_first.png`, `02_first_product.png`.*

### Lựa chọn 2: Chạy chế độ ngầm (Headless) tốc độ cao
```powershell
dotnet run -c Debug -- --target 500 --output output/phones
```

### Lựa chọn 3: Sử dụng script PowerShell 1-click
```powershell
.\run-windows.ps1 -Target 500 -Headful -Screenshots
```

---

## 6. Hướng dẫn Import vào WooCommerce & Xử lý sự cố lỗi tải ảnh từ xa

### Quy trình Import chuẩn trên giao diện WordPress
1. Đăng nhập trang quản trị WordPress: `http://localhost:8080/wp-admin/` (hoặc cổng cấu hình local của bạn).
2. Chọn menu **Sản phẩm (Products)** → **Tất cả sản phẩm (All Products)** → Nhấn nút **Nhập (Import)**.
3. Chọn file dữ liệu:
   * **Cách 1 (Kèm ảnh đầy đủ):** Chọn [`output/phones/woocommerce_import.csv`](file:///c:/Users/Admin/Downloads/lab3/EC312_CellphoneS_Crawler/output/phones/woocommerce_import.csv)
   * **Cách 2 (Import siêu tốc - Không tải ảnh):** Chọn [`output/phones/woocommerce_import_no_images.csv`](file:///c:/Users/Admin/Downloads/lab3/EC312_CellphoneS_Crawler/output/phones/woocommerce_import_no_images.csv)
4. Tích chọn ô: **"Cập nhật các sản phẩm hiện có" (Update existing products)**.
5. Nhấn **Tiếp tục** → WooCommerce tự động khớp 100% 22 cột đã chuẩn hóa → Nhấn **Chạy trình nhập dữ liệu (Run the importer)**.
6. Xác nhận toàn bộ sản phẩm xuất hiện trong danh sách ở trạng thái **Bản nháp (Draft)**.

### Xử lý lỗi: *"Người dùng đã chặn các yêu cầu qua HTTP đến URL"*
Nếu import file có URL ảnh mà gặp lỗi toàn bộ sản phẩm thất bại với thông báo:
> `Lỗi khi lấy ảnh từ xa https://cdn2.cellphones.com.vn/... Người dùng đã chặn các yêu cầu qua HTTP đến URL.`

* **Nguyên nhân:** File `wp-config.php` của WordPress bật hằng số `WP_HTTP_BLOCK_EXTERNAL = true` nhưng danh sách cho phép `WP_ACCESSIBLE_HOSTS` chỉ có `localhost,127.0.0.1`.
* **Khắc phục chuẩn an toàn (Dùng Allowlist):** Mở file `wordpress/wp-config.php` và thêm domain CDN của CellphoneS vào `WP_ACCESSIBLE_HOSTS`:
  ```php
  define('WP_HTTP_BLOCK_EXTERNAL', true);
  define('WP_ACCESSIBLE_HOSTS', 'localhost,127.0.0.1,*.cellphones.com.vn,cellphones.com.vn');
  ```
* **Giải pháp thay thế tức thì:** Sử dụng file `woocommerce_import_no_images.csv` (đã chuẩn bị sẵn tại `output/phones/`), import sẽ thành công 100% trong vài giây mà không phụ thuộc vào kết nối mạng tải ảnh của máy chủ WordPress.

---

## 7. Báo cáo thống kê thực tế (`run_summary.txt`)

Trích xuất trực tiếp từ kết quả thu thập tại [`output/phones/run_summary.txt`](file:///c:/Users/Admin/Downloads/lab3/EC312_CellphoneS_Crawler/output/phones/run_summary.txt):

```text
EC312 LAB 03 - CELLPHONES CRAWLER (DANH MỤC ĐIỆN THOẠI)
Thời gian UTC: 2026-10-26T03:23:40.3000917Z
Số sản phẩm điện thoại thu được: 477
Có đủ 6 trường chính (Tên + Giá + Hình + Mô tả/Thông số + Hãng + URL): 477
Số URL bỏ qua/lỗi: 294
Danh mục mặc định trong WooCommerce: Điện thoại
Trạng thái Published trong WooCommerce: -1 (Nháp / Draft) theo đúng yêu cầu đề bài.
Ánh xạ cột đối chiếu hoàn chỉnh theo Reference/DataMapping_Thay.xlsx.
```
