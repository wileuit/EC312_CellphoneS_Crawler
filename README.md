# EC312 LAB 03 – CELLPHONES SMARTPHONE CRAWLER (C# + PLAYWRIGHT)

> **Dự án phục vụ:** Bài thực hành Lab 03 – Môn EC312 (Thương mại điện tử & Hệ thống thông tin).  
> **Ngôn ngữ & Công nghệ:** C# (.NET 8.0 SDK), Microsoft.Playwright, CSV WooCommerce Importer Schema.  
> **Website mục tiêu:** Danh mục **Điện thoại di động CellphoneS** (`https://cellphones.com.vn/mobile.html`).  
> **Mục tiêu thu thập:** Tối thiểu **500 sản phẩm điện thoại hợp lệ**, không trùng lặp, đầy đủ 6+ trường thông tin bắt buộc và lưu tại thư mục `output/phones/`.  
> **Bảo toàn dữ liệu cũ:** Bộ dữ liệu 154 sản phẩm tai nghe đã crawl trước đó được lưu trữ an toàn tại `output_backup_headphones/`.

---

## 1. Cơ chế khám phá danh mục & Tìm kiếm 500 sản phẩm thật

Để thu thập tối thiểu 500 điện thoại độc lập và đa dạng phiên bản cấu hình mà **tuyệt đối không tạo dữ liệu giả, không nhân bản dòng**, crawler áp dụng chiến lược đa tầng:

### A. Quét mạng lưới 26 danh mục điện thoại chính hãng đã xác minh (HTTP 200 OK)
Crawler duyệt qua danh mục tổng hợp và hơn 20 danh mục thương hiệu & phân khúc điện thoại có thật trên CellphoneS:
1. `https://cellphones.com.vn/mobile.html` (Trang tổng hợp hơn 900 điện thoại)
2. **Theo từng thương hiệu chính hãng:**
   - Apple iPhone: `https://cellphones.com.vn/mobile/apple.html`
   - Samsung: `https://cellphones.com.vn/mobile/samsung.html`
   - Xiaomi / POCO: `https://cellphones.com.vn/mobile/xiaomi.html`
   - OPPO: `https://cellphones.com.vn/mobile/oppo.html`
   - vivo: `https://cellphones.com.vn/mobile/vivo.html`
   - realme: `https://cellphones.com.vn/mobile/realme.html`
   - HONOR: `https://cellphones.com.vn/mobile/honor.html`
   - TECNO: `https://cellphones.com.vn/mobile/tecno.html`
   - Infinix: `https://cellphones.com.vn/mobile/infinix.html`
   - Nokia: `https://cellphones.com.vn/mobile/nokia.html`
   - Nothing Phone: `https://cellphones.com.vn/mobile/nothing-phone.html`
   - Nubia: `https://cellphones.com.vn/mobile/nubia.html`
   - OnePlus: `https://cellphones.com.vn/mobile/oneplus.html`
   - Asus (ROG Phone / Zenfone): `https://cellphones.com.vn/mobile/asus.html`
   - Benco: `https://cellphones.com.vn/mobile/benco.html`
   - itel: `https://cellphones.com.vn/mobile/itel.html`
   - Masstel: `https://cellphones.com.vn/mobile/masstel.html`
   - Sony: `https://cellphones.com.vn/mobile/sony.html`
   - TCL: `https://cellphones.com.vn/mobile/tcl.html`
   - Huawei: `https://cellphones.com.vn/mobile/huawei.html`
3. **Theo nhu cầu & công nghệ:**
   - Điện thoại phổ thông: `https://cellphones.com.vn/mobile/dien-thoai-pho-thong.html`
   - Điện thoại gaming: `https://cellphones.com.vn/mobile/dien-thoai-gaming.html`
   - Điện thoại pin trâu: `https://cellphones.com.vn/mobile/dien-thoai-pin-trau.html`
   - Điện thoại chụp ảnh / quay phim: `https://cellphones.com.vn/mobile/dien-thoai-chup-anh-quay-phim.html`
   - Điện thoại AI: `https://cellphones.com.vn/mobile/ai.html`

### B. Tự động hóa tương tác DOM (Xem thêm + Lazy-loading)
- Tự động cuộn chuột mô phỏng hành vi người dùng thật để kích hoạt nạp hình ảnh và dữ liệu lazy-load.
- Tự động phát hiện và click nút `Xem thêm [X] sản phẩm` (`.button__show-more-product`, `.btn-show-more`) để tải thêm liên tục các sản phẩm ẩn.

### C. Khám phá các phiên bản dung lượng & cấu hình thực tế (Linked Variants)
- Khi truy cập từng trang chi tiết sản phẩm, script tự động quét khối `<div class="list-linked">` (ví dụ: các phiên bản 128GB, 256GB, 512GB, 1TB) và đưa các URL phiên bản này vào hàng đợi để trích xuất đầy đủ.
- Nhờ vậy, crawler thu thập được các phiên bản thực tế của cùng một dòng máy với giá, thông số và SKU tương ứng từ website.

### D. Bộ lọc chặt chẽ loại trừ phụ kiện & sản phẩm khác
- Loại trừ triệt để tai nghe, ốp lưng, bao da, củ sạc, cáp sạc, pin dự phòng, kính cường lực, máy tính bảng (iPad, Galaxy Tab), laptop, đồng hồ thông minh (Apple Watch) hoặc thẻ nhớ.
- Chỉ nhận các sản phẩm thuộc phân nhóm điện thoại với giá bán thực tế >= 50,000 VND.

---

## 2. Cấu trúc thư mục dự án

```text
EC312_CellphoneS_Crawler/
├── .vscode/
│   ├── launch.json                   # Cấu hình Run & Debug 500 điện thoại trong VS Code
│   └── tasks.json                    # Tự động build trước khi chạy
├── bin/ / obj/                       # Thư mục build .NET 8.0
├── Docs/
│   └── HUONG_DAN_MINH_CHUNG.md       # Danh mục các màn hình cần chụp minh chứng Lab 3
├── Reference/
│   └── DataMapping_Thay.xlsx         # Bảng đối chiếu trường dữ liệu mẫu của giảng viên
├── Scripts/
│   ├── click-load-more.js            # Click nút Xem thêm sản phẩm (.button__show-more-product)
│   ├── discover-links.js             # Quét và lọc URL sản phẩm điện thoại từ danh mục
│   └── extract-product.js            # Bóc tách JSON-LD, specs (RAM/ROM/Chip/Pin/Cam), màu sắc, variants
├── CellphoneSCrawler.cs              # Engine Playwright quản lý hàng đợi và điều phối crawl
├── CellphoneSCrawler.csproj          # File cấu hình .NET 8 và Playwright package
├── CsvExporter.cs                    # Xuất CSV chuẩn 22 cột WooCommerce và CSV thô UTF-8 BOM
├── Models.cs                         # Models dữ liệu, danh mục 26 URL, chuẩn hóa URL, CLI parser
├── ProductCleaner.cs                 # Làm sạch dữ liệu, nhận diện hãng điện thoại, lọc phụ kiện
├── Program.cs                        # Điểm khởi chạy CLI
├── run-windows.ps1                   # Script PowerShell 1-click tự động chạy 500 sản phẩm
├── output_backup_headphones/         # Dữ liệu 154 sản phẩm tai nghe đã sao lưu an toàn
└── output/
    └── phones/                       # Thư mục xuất dữ liệu điện thoại Lab 3
        ├── 01_category_first.png     # Ảnh chụp minh chứng danh mục đầu tiên (nếu bật --screenshots)
        ├── 02_first_product.png      # Ảnh chụp minh chứng sản phẩm đầu tiên (nếu bật --screenshots)
        ├── cellphones_raw_products.csv # Dữ liệu thô 11 cột điện thoại
        ├── woocommerce_import.csv    # Dữ liệu chuẩn hóa 22 cột import WooCommerce
        ├── skipped_urls.csv          # Danh sách URL bị bỏ qua/lỗi kèm nguyên nhân
        └── run_summary.txt           # Thống kê tổng kết phiên chạy
```

---

## 3. Bảng ánh xạ dữ liệu WooCommerce (22 cột)

| Cột WooCommerce | Kiểu dữ liệu | Nguồn trích xuất từ CellphoneS | Ghi chú & Quy chuẩn |
|---|---|---|---|
| `Type` | `simple` | Giá trị cố định | Sản phẩm đơn lẻ WooCommerce. |
| `SKU` | Chuỗi | JSON-LD SKU / Mã định danh CellphoneS | Định danh duy nhất có tiền tố `CPS-`. |
| `Name` | Chuỗi | Tên điện thoại đầy đủ (`h1`, JSON-LD) | Ví dụ: `iPhone 15 128GB \| Chính hãng VN/A`. |
| `Published` | `-1` | Giá trị cố định | **Bản nháp (Draft)** theo yêu cầu đề bài. |
| `Is featured?` | `0` | Giá trị cố định | Không ghim nổi bật. |
| `Visibility in catalog` | `visible` | Giá trị cố định | Hiển thị trong danh mục cửa hàng. |
| `Short description` | Chuỗi | Đoạn mở đầu <= 220 ký tự | Tóm tắt tính năng chính của máy. |
| `Description` | Chuỗi HTML/Text | Mô tả + Bảng thông số chi tiết + Màu sắc | Bóc tách đầy đủ: Hệ điều hành, Chipset, RAM, ROM, Màn hình, Camera sau/trước, Pin, Màu sắc. |
| `Tax status` | `taxable` | Giá trị cố định | Trạng thái thuế chuẩn. |
| `In stock?` | `1` hoặc `0` | Trạng thái tồn kho | `1` nếu còn hàng (InStock), `0` nếu hết hàng (OutOfStock). |
| `Backorders allowed?`| `0` | Giá trị cố định | Không cho đặt trước khi hết hàng. |
| `Sold individually?` | `0` | Giá trị cố định | Cho phép mua số lượng tùy chọn. |
| `Allow customer reviews?`| `1` | Giá trị cố định | Bật đánh giá sản phẩm. |
| `Regular price` | Số nguyên | Giá niêm yết (VND) | Giá gốc trước khuyến mãi (không kèm chữ "đ"). |
| `Sale price` | Số nguyên | Giá bán ưu đãi (VND) | Giá bán hiện tại thực tế (không kèm chữ "đ"). |
| `Categories` | `Điện thoại` | Danh mục mặc định | Đã chuyển chuẩn thành `Điện thoại`. |
| `Images` | URL | CDN ảnh CellphoneS | URL ảnh độ phân giải cao (`cdn2.cellphones.com.vn`). |
| `Position` | `0` | Giá trị cố định | Vị trí mặc định. |
| `Attribute 1 name` | `Thương hiệu` | Cột thuộc tính | Khai báo thuộc tính hãng. |
| `Attribute 1 value(s)` | Chuỗi | Hãng nhận diện tự động | Apple, Samsung, Xiaomi, OPPO, vivo, realme, v.v. |
| `Attribute 1 visible` | `1` | Giá trị cố định | Hiển thị thương hiệu trên trang sản phẩm. |
| `Attribute 1 global` | `0` | Giá trị cố định | Thuộc tính riêng cho từng sản phẩm. |

---

## 4. Lệnh Terminal để tự chạy Crawl 500 sản phẩm

Bạn có thể tự mở **Terminal trong VS Code** (`Ctrl + \``) tại thư mục `EC312_CellphoneS_Crawler` và chạy một trong các lệnh dưới đây:

### Lệnh 1: Chạy crawl 500 sản phẩm điện thoại kèm hiển thị trình duyệt và chụp ảnh minh chứng (Khuyên dùng cho Lab 3)
```powershell
dotnet run -c Debug -- --target 500 --output output/phones --headful --screenshots
```
*Lệnh này sẽ mở cửa sổ Chromium để bạn quan sát quá trình cuộn trang, tự động lưu ảnh minh chứng `01_category_first.png` và `02_first_product.png` vào `output/phones/`.*

### Lệnh 2: Chạy ở chế độ ngầm (Headless) tốc độ cao
```powershell
dotnet run -c Debug -- --target 500 --output output/phones
```

### Lệnh 3: Dùng script PowerShell 1-click
```powershell
.\run-windows.ps1 -Target 500 -Headful -Screenshots
```

---

## 5. Vị trí file kết quả đầu ra

Sau khi chạy, dữ liệu hoàn chỉnh sẽ được lưu tại:
* **Dữ liệu WooCommerce sẵn sàng import:**
  [`output/phones/woocommerce_import.csv`](file:///c:/Users/Admin/Downloads/lab3/EC312_CellphoneS_Crawler/output/phones/woocommerce_import.csv)
* **Dữ liệu thô gốc trích xuất từ website:**
  [`output/phones/cellphones_raw_products.csv`](file:///c:/Users/Admin/Downloads/lab3/EC312_CellphoneS_Crawler/output/phones/cellphones_raw_products.csv)
* **Danh sách URL bị bỏ qua/lỗi (nếu có):**
  [`output/phones/skipped_urls.csv`](file:///c:/Users/Admin/Downloads/lab3/EC312_CellphoneS_Crawler/output/phones/skipped_urls.csv)
* **Báo cáo tổng kết số lượng & trường dữ liệu:**
  [`output/phones/run_summary.txt`](file:///c:/Users/Admin/Downloads/lab3/EC312_CellphoneS_Crawler/output/phones/run_summary.txt)
* **Ảnh chụp minh chứng danh mục:**
  `output/phones/01_category_first.png`
* **Ảnh chụp minh chứng sản phẩm đầu tiên:**
  `output/phones/02_first_product.png`

---

## 6. Hướng dẫn Import vào WooCommerce

1. Mở trang quản trị WordPress: `http://localhost/.../wp-admin/`.
2. Chọn **Sản phẩm (Products)** → **Tất cả sản phẩm (All Products)** → Nhấn **Nhập (Import)**.
3. Chọn file `output/phones/woocommerce_import.csv`.
4. Nhấn **Tiếp tục**. WooCommerce sẽ tự động ánh xạ 100% 22 cột đã chuẩn hóa.
5. Nhấn **Chạy trình nhập dữ liệu (Run the importer)**.
6. Xác nhận các sản phẩm điện thoại hiển thị đúng giá, hãng, ảnh và ở trạng thái **Bản nháp (Draft - Published = -1)**.
