<!--
	README: Price-Watcher
	- Vietnamese README with animated badges and GIFs for a polished GitHub-first presentation
	- Keep this file short and actionable while showcasing features and quick start
-->

# Price-Watcher

_Công cụ so sánh giá thông minh — tìm sản phẩm bằng Link hoặc Hình ảnh_

[![Build Status](https://github.com/nhotungdo/Price-Watcher/actions/workflows/dotnet.yml/badge.svg)](https://github.com/nhotungdo/Price-Watcher/actions)
![GitHub last commit](https://img.shields.io/github/last-commit/nhotungdo/Price-Watcher)
![GitHub repo size](https://img.shields.io/github/repo-size/nhotungdo/Price-Watcher)

---

<!-- Hero / animated typing -->

![Typing SVG](https://readme-typing-svg.herokuapp.com?font=Rubik&size=24&pause=1000&color=2F80ED&center=true&vCenter=true&width=780&height=48&lines=Search+by+Link+or+Image;Smart+shopping+with+Price+Watcher;Find+best+price+&+history)

<!-- A small product animation (replace link with your app's demo GIF) -->

![Demo](https://media.giphy.com/media/3oEjI6SIIHBdRxXI40/giphy.gif)

---

## Mô tả

Price-Watcher là ứng dụng web giúp người dùng tìm các sản phẩm tương tự trên nhiều sàn thương mại điện tử và đề xuất mức giá tốt nhất. Người dùng có thể dán URL sản phẩm hoặc tải lên ảnh, hệ thống sẽ chuẩn hóa đầu vào, thu thập dữ liệu và đưa ra gợi ý nhanh.

## Tính năng chính

- Tìm kiếm theo URL sản phẩm (Shopee, Lazada, Tiki)
- Tìm kiếm theo Ảnh (image search, stub)
- Máy gợi ý: lọc outliers, tính điểm theo giá/ship/rating/tiêu đề
- Lưu lịch sử tìm kiếm và snapshot giá
- Thông báo Telegram (tùy chọn)

---

## Cài đặt

Yêu cầu: [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

```powershell
cd PriceWatcher
dotnet restore
dotnet ef database update  # nếu dùng EF Migrations
dotnet run --launch-profile http
```

Mở trình duyệt tới `http://localhost:5000`.

---

## Cấu hình

Thêm secrets vào `appsettings.json` (hoặc `appsettings.Development.json` khi chạy local):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=PriceWatcherDB;Trusted_Connection=True;Trust Server Certificate=True"
  },
  "Authentication": {
    "Google": {
      "ClientId": "<GOOGLE_CLIENT_ID>",
      "ClientSecret": "<GOOGLE_CLIENT_SECRET>"
    }
  },
  "Telegram": {
    "BotToken": "<BOT_TOKEN>",
    "AdminChatId": "<ADMIN_CHAT_ID>"
  },
  "Recommendation": {
    "WeightPrice": 0.7,
    "WeightRating": 0.2,
    "WeightShipping": 0.1,
    "TrustedShopSalesThreshold": 50
  }
}
```

Khuyến nghị dùng [dotnet user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) hoặc biến môi trường cho secrets.

---

## Cách sử dụng

1. Truy cập trang chính
2. Dán URL sản phẩm hoặc tải lên ảnh
3. Bấm “Phân tích giá” và xem Top đề xuất theo từng sàn

API nội bộ:

- `POST /search/submit` — tạo job tìm kiếm
  - Body mẫu:
    ```json
    { "userId": 0, "url": "https://shopee.vn/..." }
    ```
- `GET /search/status/{searchId}` — xem trạng thái và kết quả

---

## Phát triển & Test

```powershell
dotnet test PriceWatcher/PriceWatcher.Tests
```

Các thành phần chính: `Program.cs` (DI & middleware), `SearchController` (API), `SearchProcessingService` (xử lý), `RecommendationService` (gợi ý).

---

## Công nghệ sử dụng

- .NET 8, ASP.NET Core, Razor Pages
- Entity Framework Core (SQL Server)
- Polly (retry cho HTTP client)
- Telegram.Bot
- xUnit cho unit tests

---

## Badges

- Build: GitHub Actions (.NET)
- Last commit, repo size: shields.io

---

## Đóng góp

1. Fork repo
2. Tạo branch: `feature/<ten-tinh-nang>`
3. Viết test cho logic mới
4. Gửi PR kèm mô tả chi tiết

---

## Giấy phép

Chưa có file license trong repo. Nếu muốn dùng MIT, tạo file `LICENSE` với nội dung MIT và cập nhật badge tương ứng.

---

## 💡 Tips cuối

- Dùng GIF kích thước nhỏ (< 1-2MB) để README load nhanh
- Dùng `shields.io` cho badges realtime
- Đặt GIF demo trong thư mục `assets/` để dễ thay thế

---

_Cảm ơn bạn đã dùng Price-Watcher! Nếu cần README chuyển sang English hoặc thêm GIF cụ thể, cho mình biết nhé._

---

## 🚀 Roadmap tóm tắt — 3 ngày phát triển (Day 1 → Day 3)

Dưới đây là bản tóm tắt ngắn gọn về lộ trình 3 ngày mà team đã thực hiện (mục tiêu: nền tảng, xử lý input & recommendation). Mục này giúp reviewer hiểu nhanh scope của milestone.

### ✅ DAY 1 — Nền tảng & Authentication

- Khởi tạo solution, cấu trúc module `Domain/`, `Infrastructure/`, `Application/`, `Web/`, thêm NuGet quan trọng như Google Auth, EF Core, Telegram.
- Google OAuth hoàn chỉnh — cấu hình Google Cloud Console, routes `/auth/google` + callback `/signin-google`, logic tạo/update user.
- Telegram notification — setup bot, `ITelegramNotifier`, gửi message mỗi lần user login.

Deliverables:

- Google OAuth hoạt động
- DB User + EF Migration
- Telegram message khi login

### ✅ DAY 2 — Xử lý Link, Ảnh & Scraper

- `POST /search/submit` + `LinkProcessor` để chuẩn hóa URL, detect marketplace (Shopee/Lazada/Tiki).
- Upload ảnh + image-search stub (`IImageSearchService`) để trả keywords/response; validate image <= 8MB.
- Mock scrapers (`IShopeeScraper`, `ILazadaScraper`, `ITikiScraper`) trả list sản phẩm mẫu.

Deliverables:

- Input URL & Upload ảnh hoạt động
- Image-search stub chạy được
- Mock scrapers có dữ liệu mẫu

### ✅ DAY 3 — Recommendation Engine, Lịch sử & UI

- `RecommendationService` — gom nhóm, lọc outliers, sắp xếp theo `price + shipping`, gắn label (Best Price, Trusted Shop).
- Lưu `SearchHistory` và giới hạn 50 per user, API `GET /history`, `DELETE /history/{id}`.
- UI hiển thị kết quả với Thumbnail, Price, Shop, Rating, Labels, test end-to-end.

Deliverables:

- Recommendation Engine trả Top 3
- Lưu và load lịch sử
- UI hiển thị kết quả & full E2E test

---

Nếu anh muốn mình đưa phần Roadmap này lên một file `ROADMAP.md` riêng hoặc thêm checklists cho PRs, mình có thể làm tiếp.
