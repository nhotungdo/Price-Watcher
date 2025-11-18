<!--
	README: Price-Watcher
	- Vietnamese README with animated badges and GIFs for a polished GitHub-first presentation
	- Keep this file short and actionable while showcasing features and quick start
-->

# Price-Watcher

_Công cụ hỗ trợ mua sắm thông minh — tìm sản phẩm bằng Link hoặc bằng Hình ảnh_

<!-- Badges -->

[![Build Status](https://github.com/nhotungdo/Price-Watcher/actions/workflows/dotnet.yml/badge.svg)](https://github.com/nhotungdo/Price-Watcher/actions)  
![GitHub last commit](https://img.shields.io/github/last-commit/nhotungdo/Price-Watcher)  
![GitHub repo size](https://img.shields.io/github/repo-size/nhotungdo/Price-Watcher)  
![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)

---

<!-- Hero / animated typing -->

![Typing SVG](https://readme-typing-svg.herokuapp.com?font=Rubik&size=24&pause=1000&color=2F80ED&center=true&vCenter=true&width=780&height=48&lines=Search+by+Link+or+Image;Smart+shopping+with+Price+Watcher;Find+best+price+&+history)

<!-- A small product animation (replace link with your app's demo GIF) -->

![Demo](https://media.giphy.com/media/3oEjI6SIIHBdRxXI40/giphy.gif)

---

## ✨ Tổng quan

Price-Watcher là một web app giúp người dùng tìm kiếm sản phẩm nhanh chóng bằng URL sản phẩm hoặc bằng cách tải lên hình ảnh (image search). Ứng dụng theo dõi thay đổi giá, lưu lịch sử tìm kiếm và gửi thông báo khi có biến động giá quan trọng.

### ✅ Tính năng chính

- Tìm kiếm sản phẩm theo `URL`
- Tìm kiếm theo `Ảnh` (image search)
- Lưu lịch sử tìm kiếm (Search history)
- Lưu trữ ảnh và snapshot giá (Price snapshots)
- Gửi thông báo (Telegram/notification)
- Bảng điều khiển và UI đơn giản, responsive

---

## 💻 Cài đặt & Chạy nhanh

Yêu cầu: [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

Trên Windows PowerShell (từ thư mục gốc `PriceWatcher`):

```powershell
cd PriceWatcher
dotnet restore
dotnet ef database update  # nếu dùng EF Migrations (nếu chưa có DB hãy cập nhật connection string trong appsettings.json)
dotnet run
```

Mở trình duyệt tới `https://localhost:5001` (hoặc port hiển thị trong console).

---

## 🔐 Cấu hình OAuth & Telegram

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

- Để an toàn, dùng [dotnet user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) hoặc biến môi trường.
- `AdminChatId` lấy qua @userinfobot trên Telegram.
- Tham số Recommendation điều chỉnh thuật toán gợi ý sản phẩm.

---

## 🧭 Cách dùng nhanh — Ví dụ

1. Truy cập trang chính
2. Dán `URL` sản phẩm vào ô tìm kiếm — hoặc tải lên `Ảnh` — bấm `Search`
3. Chờ kết quả, chọn đề xuất và xem lịch sử / snapshot giá cũ

> Tip: Bạn có thể dùng extension/browser bookmark để nhanh chóng copy link sản phẩm.

---

## 🔧 Hướng dẫn phát triển

- Xem `Program.cs` để hiểu luồng khởi tạo (DI & middleware).
- Controllers: `SearchController`, `UsersController`, `AuthController`.
- Services: `SearchProcessingService`, `RecommendationService`, `UserService` và các interface trong `Services/Interfaces/`.

Thử local tests:

```powershell
dotnet test PriceWatcher/PriceWatcher.Tests
```

Các unit test hiện có:

- `LinkProcessorTests`: kiểm tra việc nhận diện nền tảng & productId từ URL
- `RecommendationServiceTests`: đảm bảo quy trình lọc, tính điểm & dán nhãn
- `SearchHistoryServiceTests`: xác nhận lịch sử tìm kiếm không vượt quá 50 bản ghi/user

---

## 🎨 Hướng dẫn thêm animation / GIF demo (gợi ý)

Bạn có thể thêm GIF demo (bước tìm kiếm -> trả kết quả) vào README để tăng tính trực quan. Một số công cụ hữu ích:

- LICEcap — quay GIF trực tiếp từ màn hình.
- Peek (Linux) hoặc ScreenToGif (Windows) — chỉnh sửa frame và xuất GIF.
- GIF optimization: `gifsicle` để nén GIF trước khi upload.

Gợi ý chèn GIF:

```md
![Demo Search](assets/demo-search.gif)
```

Tốt nhất upload `assets/demo-search.gif` trong repo rồi tham chiếu đường dẫn tương đối để đảm bảo hiển thị ổn định.

---

## 📦 Releases & Badges

Để thêm animation badges hoặc badges động, sử dụng `shields.io` & các service như `readme-typing-svg` hoặc animated SVG từ repo chủ:

- Typing effect: `https://readme-typing-svg.herokuapp.com`
- Animated SVG badges: `https://shields.io`

Ví dụ con fly-in badge:

```md
![GitHub last commit](https://img.shields.io/github/last-commit/nhotungdo/Price-Watcher)
```

---

## 🤝 Contributing

Rất hoan nghênh PR! Vui lòng:

1. Fork repo
2. Tạo branch mới: `feature/my-cool-feature`
3. Thêm test cho logic mới
4. Submit PR kèm mô tả thực thi

Bạn có thể thêm GIF demo cho tính năng mới trong `assets/` và cập nhật README để hiển thị.

---

## 📝 License

This project doesn't have a license file yet — nếu bạn muốn license MIT, hãy tạo file `LICENSE` với nội dung MIT.

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
