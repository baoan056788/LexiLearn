# LexiLearn

LexiLearn là website học từ vựng tiếng Anh xây dựng bằng ASP.NET Core MVC và SQL Server. Ứng dụng hỗ trợ tạo bộ từ vựng, nhập từ file Excel, học bằng flashcard, làm quiz/test, chơi match game, theo dõi tiến độ học tập và quản trị dữ liệu người dùng.

## Tính năng chính

- Đăng ký, đăng nhập và phân quyền bằng cookie authentication.
- Tạo, sửa, xóa và sao chép bộ từ vựng.
- Nhập thẻ từ vựng từ file Excel.
- Khám phá bộ từ công khai.
- Học bằng flashcard và match game.
- Làm bài test trắc nghiệm, xem kết quả và lịch sử test.
- Lịch ôn thông minh theo spaced repetition cho từng thẻ và từng người dùng.
- Tra từ bằng Gemini AI, hỗ trợ nhập tiếng Anh hoặc tiếng Việt, trả về nghĩa, IPA, ví dụ, đồng nghĩa và trái nghĩa.
- Hỏi AI về từ vựng/ngữ pháp, lưu lịch sử hội thoại theo tài khoản và phát âm từ tiếng Anh bằng Web Speech API.
- Theo dõi tiến độ học tập, số thẻ đã học, điểm trung bình và streak.
- Khu vực quản trị cho người dùng, danh mục, bộ từ, phản hồi, thông báo và báo cáo.
- Tối ưu triển khai IIS bằng publish Release, response compression và ReadyToRun.

## Công nghệ sử dụng

- **Backend:** ASP.NET Core MVC (.NET 9)
- **ORM:** Entity Framework Core 9
- **Database:** Microsoft SQL Server
- **Authentication:** ASP.NET Core Cookie Authentication
- **Password hashing:** BCrypt.Net-Next
- **Excel import:** ExcelDataReader
- **AI features:** Google Gemini API cho tra từ/chat, Web Speech API cho phát âm trên trình duyệt
- **Learning algorithm:** Spaced repetition đơn giản dựa trên repetition count, interval days và ease factor
- **Frontend:** Razor Views, Bootstrap, jQuery, Font Awesome
- **Web server/deploy:** IIS + ASP.NET Core Hosting Bundle

## Cấu trúc thư mục

```text
LexiLearn/
├── Areas/Admin/          # Khu vực quản trị
├── Controllers/          # Controller MVC cho người dùng
├── Data/                 # AppDbContext và seed data
├── Migrations/           # EF Core migrations
├── Models/               # Entity database
├── Services/             # Business logic: auth, study, test, progress
├── ViewModels/           # Model trung gian cho view/form
├── Views/                # Razor views
├── wwwroot/              # Static files: css, js, lib, images
├── Program.cs            # Cấu hình ứng dụng, middleware, route, DI
├── appsettings.json      # Cấu hình connection string local
└── LexiLearn.csproj      # Project .NET
```

## Yêu cầu cài đặt

Cài các phần mềm sau:

- .NET SDK 9
- SQL Server 2019/2022 hoặc SQL Server Express
- SQL Server Management Studio (SSMS)
- IIS
- ASP.NET Core Hosting Bundle cho .NET 9 nếu chạy bằng IIS
- Git

Kiểm tra .NET:

```cmd
dotnet --version
```

## Cấu hình database

Mở `appsettings.json` và chỉnh connection string theo SQL Server trên máy:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=MYPC123\\MSSQLSERVER03;Database=LexiLearnDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

## Cấu hình Gemini API

Tính năng tra từ AI dùng Google Gemini API. Bạn có thể lấy API key miễn phí quota tại Google AI Studio:

```text
https://aistudio.google.com/app/apikey
```

Khuyến nghị đặt API key bằng biến môi trường để không lộ key trong Git:

```cmd
setx GEMINI_API_KEY "YOUR_GEMINI_API_KEY"
```

Sau khi chạy lệnh trên, mở lại terminal hoặc restart IIS để ứng dụng nhận biến môi trường mới.

Có thể cấu hình model trong `appsettings.json`:

```json
{
  "Gemini": {
    "Model": "gemini-2.5-flash"
  }
}
```

Không commit API key thật lên GitHub. Nếu cần cấu hình local bằng file, hãy dùng `appsettings.Local.json` hoặc biến môi trường.

Nếu dùng SQL Login:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=MYPC123\\MSSQLSERVER03;Database=LexiLearnDb;User Id=sa;Password=YOUR_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

## Tạo database

Chạy migration:

```cmd
cd /d D:\EL\LexiLearn
dotnet ef database update
```

Migration hiện tại tạo thêm bảng `CardReviews` để lưu lịch ôn từng thẻ:

- `UserId`, `CardId`: xác định thẻ của từng người học.
- `RepetitionCount`: số lần trả lời đúng liên tiếp.
- `IntervalDays`: số ngày giãn cách đến lần ôn sau.
- `EaseFactor`: độ dễ của thẻ, dùng để tăng/giảm interval.
- `DueAt`: ngày thẻ đến hạn ôn.

Ứng dụng cũng tạo thêm index cho các truy vấn thường dùng như danh sách bộ từ công khai, lịch sử test, phiên học, tiến độ và lịch ôn.

Tính năng AI chat tạo thêm bảng `AiConversations` và `AiMessages` để lưu lịch sử hỏi AI của từng người dùng.

Nếu máy chưa có `dotnet-ef`, cài bằng:

```cmd
dotnet tool install --global dotnet-ef
```

Seed data chỉ chạy tự động trong môi trường Development hoặc khi bật:

```json
{
  "SeedData": {
    "RunOnStartup": true
  }
}
```

## Chạy local bằng Kestrel

```cmd
cd /d D:\EL\LexiLearn
dotnet restore
dotnet build
dotnet run
```

Sau đó mở URL được terminal hiển thị, thường là:

```text
http://localhost:5000
```

## Publish Release

```cmd
cd /d D:\EL\LexiLearn
dotnet publish -c Release
```

Thư mục publish:

```text
D:\EL\LexiLearn\bin\Release\net9.0\win-x64\publish
```

## Chạy bằng IIS

1. Cài ASP.NET Core Hosting Bundle cho .NET 9.
2. Restart IIS:

```cmd
iisreset
```

3. Mở **Internet Information Services (IIS) Manager**.
4. Chọn **Sites** > **Add Website...**
5. Điền:

```text
Site name: LexiLearn
Physical path: D:\EL\LexiLearn\bin\Release\net9.0\win-x64\publish
Port: 8080
```

6. Vào **Application Pools** > chọn **LexiLearn** > **Basic Settings...**
7. Chỉnh:

```text
.NET CLR version: No Managed Code
Managed pipeline mode: Integrated
```

8. Recycle app pool và mở:

```text
http://localhost:8080
```

Nếu muốn dùng port 80:

```text
http://localhost
```

hãy đổi binding từ `8080` sang `80`. Không dùng port `1` vì Chrome chặn với lỗi `ERR_UNSAFE_PORT`.

## Cấp quyền SQL Server cho IIS App Pool

Nếu IIS báo lỗi 500 và log có dòng:

```text
Login failed for user 'IIS APPPOOL\LexiLearn'
```

hoặc:

```text
Cannot open database "LexiLearnDb" requested by the login
```

mở SSMS và chạy:

```sql
USE master;
GO

CREATE LOGIN [IIS APPPOOL\LexiLearn] FROM WINDOWS;
GO

USE LexiLearnDb;
GO

CREATE USER [IIS APPPOOL\LexiLearn] FOR LOGIN [IIS APPPOOL\LexiLearn];
GO

ALTER ROLE db_datareader ADD MEMBER [IIS APPPOOL\LexiLearn];
ALTER ROLE db_datawriter ADD MEMBER [IIS APPPOOL\LexiLearn];
GO
```

Nếu cần quyền migration/tạo bảng khi demo nhanh:

```sql
USE LexiLearnDb;
GO

ALTER ROLE db_owner ADD MEMBER [IIS APPPOOL\LexiLearn];
GO
```

Sau đó recycle app pool và restart site.

## Quy trình cập nhật code lên IIS

Mỗi lần sửa code:

```cmd
cd /d D:\EL\LexiLearn
dotnet build
dotnet publish -c Release
```

Nếu publish bị báo file đang bị khóa bởi `w3wp.exe`, tạo tạm file `app_offline.htm` trong thư mục publish, publish lại rồi xóa file đó:

```text
D:\EL\LexiLearn\bin\Release\net9.0\win-x64\publish\app_offline.htm
```

## Đẩy lên GitHub

Khởi tạo Git repo local:

```cmd
git init
git add .
git commit -m "Initial LexiLearn project"
```

Tạo repo mới trên GitHub, ví dụ `LexiLearn`, rồi liên kết remote:

```cmd
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/LexiLearn.git
git push -u origin main
```

## Ghi chú bảo mật

- Không commit mật khẩu SQL thật lên GitHub.
- Với môi trường production, nên dùng `appsettings.Production.json`, biến môi trường hoặc Secret Manager.
- Không nên để App Pool chạy bằng `LocalSystem`; hãy dùng `ApplicationPoolIdentity` và cấp quyền SQL đúng cho `IIS APPPOOL\LexiLearn`.
