# EC312 LAB 03 – DANH SÁCH VỊ TRÍ CHỤP ẢNH MINH CHỨNG BÁO CÁO

Tài liệu này hướng dẫn chi tiết các vị trí màn hình sinh viên cần chụp ảnh thực tế trên máy để đưa vào file Báo cáo Word Lab 3.

---

## Phần A – Thiết kế & Cài đặt Mã nguồn

- [ ] **Hình 1: Cấu trúc dự án trong Visual Studio Code**
  - **Vị trí chụp:** Cửa sổ VS Code mở thư mục `EC312_CellphoneS_Crawler`, mở rộng thanh Explorer hiển thị đầy đủ các thư mục `Scripts/`, `output/`, `Reference/`, `Docs/`, `.vscode/` và file `CellphoneSCrawler.csproj`.
  - **Mô tả báo cáo:** *Cấu trúc mã nguồn dự án CellphoneS Crawler bằng C# (.NET 8) và Microsoft.Playwright.*

- [ ] **Hình 2: Triển khai quét liên kết sản phẩm (`Scripts/discover-links.js`)**
  - **Vị trí chụp:** Trình soạn thảo VS Code mở file `Scripts/discover-links.js`.
  - **Mô tả báo cáo:** *Thuật toán JavaScript quét URL sản phẩm tai nghe hợp lệ từ DOM danh mục CellphoneS.*

- [ ] **Hình 3: Triển khai bóc tách chi tiết (`Scripts/extract-product.js`)**
  - **Vị trí chụp:** Trình soạn thảo VS Code mở file `Scripts/extract-product.js`.
  - **Mô tả báo cáo:** *Mã nguồn trích xuất dữ liệu đa tầng từ JSON-LD Schema.org, OpenGraph meta và bảng thông số kỹ thuật.*

- [ ] **Hình 4: Triển khai ánh xạ chuẩn WooCommerce (`CsvExporter.cs`)**
  - **Vị trí chụp:** Trình soạn thảo VS Code mở file `CsvExporter.cs`.
  - **Mô tả báo cáo:** *Mã nguồn chuẩn hóa trường dữ liệu và xuất CSV tương thích WooCommerce Importer (Published=-1).*

---

## Phần B – Vận hành & Thu thập Dữ liệu Thực tế

- [ ] **Hình 5: Trình duyệt Chromium mở trang danh mục Tai nghe CellphoneS**
  - **Vị trí chụp:** Cửa sổ trình duyệt Chromium đang truy cập `https://cellphones.com.vn/thiet-bi-am-thanh/tai-nghe.html` (hoặc mở file ảnh có sẵn `output/01_category_first.png`).
  - **Mô tả báo cáo:** *Trình duyệt Playwright Chromium truy cập danh mục tai nghe CellphoneS và thực hiện cuộn lazy-load.*

- [ ] **Hình 6: Terminal PowerShell đang chạy quá trình thu thập**
  - **Vị trí chụp:** Cửa sổ Terminal hiển thị lệnh chạy `dotnet run -- --target 100 --headful --screenshots` và tiến trình đang bóc tách từng sản phẩm `[X/100] OK: ...`.
  - **Mô tả báo cáo:** *Tiến trình thu thập dữ liệu hiển thị thông tin sản phẩm và trạng thái tự động lưu trên Terminal.*

- [ ] **Hình 7: Kết thúc quá trình crawl thành công 100 sản phẩm**
  - **Vị trí chụp:** Terminal dừng lại ở bảng thống kê kết thúc: `Số sản phẩm hợp lệ thu được: 100 / 100`, `Số URL bỏ qua hoặc gặp lỗi: 0` và nội dung file `output/run_summary.txt`.
  - **Mô tả báo cáo:** *Kết quả crawl thực tế hoàn thành 100 sản phẩm không lỗi và lưu trữ tại thư mục output/.*

- [ ] **Hình 8: File dữ liệu thô `cellphones_raw_products.csv`**
  - **Vị trí chụp:** Mở file `output/cellphones_raw_products.csv` trong Microsoft Excel hoặc VS Code hiển thị đầy đủ các cột URL, SKU, Tên, Giá bán, Giá gốc, Mô tả, Hãng, Ảnh, Thời gian.
  - **Mô tả báo cáo:** *Dữ liệu thô thu thập từ CellphoneS gồm 100 sản phẩm với đầy đủ 6+ trường thông tin.*

---

## Phần C – Nhập dữ liệu vào WooCommerce

- [ ] **Hình 9: File `woocommerce_import.csv` theo chuẩn Importer**
  - **Vị trí chụp:** Mở file `output/woocommerce_import.csv` trong Microsoft Excel hiển thị các tiêu đề chuẩn: `Type`, `SKU`, `Name`, `Published` (-1), `Regular price`, `Sale price`, `Categories`, `Images`, `Attribute 1 name/value(s)`.
  - **Mô tả báo cáo:** *File CSV chuẩn hóa theo định dạng WooCommerce Product CSV Importer với trạng thái Draft.*

- [ ] **Hình 10: Màn hình Map CSV fields to products trong WordPress**
  - **Vị trí chụp:** Giao diện WordPress Admin → Sản phẩm → Nhập → Màn hình đối chiếu cột hiển thị 100% các cột được tự động khớp xanh.
  - **Mô tả báo cáo:** *Bước ánh xạ tự động các trường dữ liệu CSV vào thuộc tính sản phẩm WooCommerce.*

- [ ] **Hình 11: Thông báo nhập sản phẩm thành công**
  - **Vị trí chụp:** Màn hình WordPress hiển thị thông báo: *"Nhập thành công 100 sản phẩm"*.
  - **Mô tả báo cáo:** *Kết quả hoàn thành nhập 100 sản phẩm tai nghe vào cơ sở dữ liệu WooCommerce.*

- [ ] **Hình 12: Danh sách sản phẩm trong trang quản trị WooCommerce**
  - **Vị trí chụp:** WordPress Admin → Sản phẩm → Tất cả sản phẩm, hiển thị danh sách các sản phẩm vừa nhập với trạng thái **Bản nháp (Draft)**, hiển thị hình ảnh đại diện, giá bán VNĐ, giá khuyến mãi và danh mục `Thiết bị âm thanh > Tai nghe`.
  - **Mô tả báo cáo:** *Sản phẩm hiển thị đầy đủ ảnh, giá và thương hiệu ở trạng thái Bản nháp trong WooCommerce.*

---

## Đề cương tóm tắt cho Báo cáo Lab 3

1. **Tổng quan đề tài:** Giới thiệu bài toán thu thập dữ liệu e-commerce từ CellphoneS và tích hợp vào hệ thống WooCommerce.
2. **Kế thừa kỹ thuật từ TikiCrawler:**
   - Quản lý Context, User-Agent và Viewport chuẩn người dùng.
   - Cơ chế NetworkIdle kết hợp progressive scroll kích hoạt lazy-loading.
   - Xử lý bộ chọn dự phòng (JSON-LD + OpenGraph + DOM class).
   - Cơ chế xử lý lỗi độc lập không ngắt luồng.
3. **Mô hình kiến trúc Crawler:**
   - Trích xuất liên kết từ danh mục và click "Xem thêm".
   - Bóc tách chi tiết từng trang sản phẩm.
   - Làm sạch, phát hiện thương hiệu, định dạng số tiền VNĐ.
   - Xuất dữ liệu kép (Raw CSV & WooCommerce Import CSV).
4. **Kết quả đạt được:**
   - Đã thu thập thành công **100/100 sản phẩm thật**, không trùng lặp, đầy đủ hình ảnh, mô tả, giá gốc, giá khuyến mãi và thông số kỹ thuật.
   - Định dạng cột tương thích 100% với `DataMapping_Thay.xlsx`.
   - Trạng thái sản phẩm mặc định là Draft (`Published = -1`) an toàn cho cửa hàng.
