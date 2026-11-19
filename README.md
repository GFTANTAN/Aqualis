# AquaMaterials.Api

Backend ASP.NET Core 8 dùng **Google Sheet "THEO DÕI VẬT TƯ AQUALIS"** làm database để tra cứu dữ liệu kho.

## Luồng tìm kiếm

| Bước | Endpoint | Mô tả |
|---|---|---|
| Option 1 | `GET /api/search/warehouse-types` | `XUATKHO`, `NHAPKHO`, `TONKHO` |
| Option 2 | `GET /api/search/customers` | Danh sách tên khách hàng (từ tab `DANHMUCHANG`) |
| Option 3 | `GET /api/search/products?customer=Duyên Thị` | Mã hàng + tên hàng của khách đó |
| Option 4 | `GET /api/search/data?type=XUATKHO&customer=Duyên Thị&itemCode=DT.EOEM1` | Số liệu cụ thể lấy từ sheet kho đã chọn |

Cấu trúc các tab trong sheet:

- `QUYCACHBAOBI`: danh mục khách hàng, mã hàng, mã bao bì (dùng cho Option 2 và 3)
- `NHAPKHO`: Số phiếu, Ngày nhập, Mã hàng, Tên hàng, ĐVT, SL nhập, Ghi chú, Nhà cung cấp, Loại nhập
- `XUATKHO`: Số phiếu, Ngày xuất, Mã hàng, Tên hàng, ĐVT, SL xuất, Bộ phận xuất hàng, Ghi chú
- `TONKHO`: STT, Mã hàng, Tên hàng, Nhóm, ĐVT, Tồn đầu, Nhập, Xuất, Tồn cuối, Ghi chú

Các tab kho không có cột khách hàng, nên API lọc theo khách bằng cách lấy các mã bao bì của khách trong `QUYCACHBAOBI`, rồi khớp với cột "Mã hàng" của tab kho.
Khi chọn 1 mã cụ thể (`itemCode`), kết quả có thêm `totals` (tổng SL nhập / SL xuất / tồn cuối).

Ví dụ: chọn `XUATKHO` ở Option 1 thì Option 4 đọc số liệu từ tab `XUATKHO`.
`itemCode` có thể bỏ trống để lấy toàn bộ mã vật tư của khách.

## Chạy

```bash
dotnet run
# mở http://localhost:5000/swagger (hoặc cổng hiển thị trong console)
```

Hoặc Docker: `docker build -t aqua-api . && docker run -p 8080:8080 aqua-api`

## Cấu hình (`appsettings.json`)

- `SpreadsheetId`: ID trong link Google Sheet.
- `Sheets`: tên các tab (phải đúng chính xác).
- `Headers`: danh sách tên cột (**bỏ dấu, viết thường**) để tự nhận diện cột mã vật tư, tên vật tư, khách hàng... Nếu tab XUATKHO/NHAPKHO/TONKHO đặt tên cột khác, thêm tên đó vào đây.
- `CacheSeconds`: thời gian cache dữ liệu (mặc định 60 giây).

## Yêu cầu

- Google Sheet phải bật chia sẻ **"Anyone with the link – Viewer"** (API đọc qua link CSV công khai, không cần API key).
- Dòng 1 của mỗi tab là dòng tiêu đề.
- Ô gộp (merged) ở các cột khách hàng/tên sản phẩm/mã hàng được tự điền xuống các dòng dưới.

## Dùng Google Sheets API chính thức (tuỳ chọn)

Nếu cần sheet private, thay phần `LoadAsync` trong `Services/GoogleSheetService.cs` bằng gọi
`https://sheets.googleapis.com/v4/spreadsheets/{id}/values/{sheet}?key=API_KEY` hoặc dùng Service Account.
