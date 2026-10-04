# Đăng nhập Facebook và lưu game — local

## Đã triển khai

Unity WebGL → ASP.NET Core .NET 8 → DynamoDB Local. Facebook OAuth chạy ở
backend; Unity không giữ App Secret hay Facebook access token. Trình duyệt nhận
cookie phiên HttpOnly; API lấy player ID từ phiên đã xác thực. Mỗi tài khoản có
một bản lưu chính, với số phiên bản để tránh ghi đè giữa các thiết bị.

Hiện chưa có ứng dụng Facebook của chủ dự án, vì vậy nút Facebook chưa được
kích hoạt. Kiểm thử tự động dùng phản hồi Facebook giả lập bên trong chương
trình kiểm thử riêng, nhưng chạy middleware OAuth và DynamoDB Local thật.
Chưa xác nhận đăng nhập với tài khoản Facebook thật.

## Chạy ngay, chưa cần tài khoản AWS hoặc ứng dụng Facebook

Tại thư mục gốc `C:\Users\admin\Bug Village`:

```powershell
docker compose -f Backend/compose.yaml up -d
dotnet restore Backend/LittleColony.Backend.sln --locked-mode
dotnet run --project Backend/src/LittleColony.Api --launch-profile local
```

Mở <http://localhost:5100>. API phục vụ luôn bản Unity đã build trong
`Builds/WebGL`. `/health/ready` kiểm tra kết nối database;
`/api/session` trả về `facebookEnabled: false` khi chưa có cấu hình.
`/api/save` yêu cầu đăng nhập, không có endpoint đăng nhập giả trong ứng dụng.

Nếu cần build lại Unity, chạy ở terminal khác:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build-webgl.ps1
```

**Giữ bản lưu khách cũ:** dữ liệu trình duyệt thuộc từng origin (giao thức,
hostname, cổng). Bản lưu tại `http://localhost:8080` không tự xuất hiện tại
`http://localhost:5100` hoặc `https://localhost:7100`; dữ liệu cũ không bị xóa.
`Tools/serve-webgl.py` đã được bổ sung proxy `/api/`, `/auth/`, `/signin-facebook`
về API cổng 5100 để tiếp tục dùng origin 8080. Nếu máy chủ Python cũ đang chạy,
dừng bằng Ctrl+C trong terminal của nó rồi chạy lại:

```powershell
python Tools/serve-webgl.py
```

## Tạo ứng dụng Facebook và cấu hình

1. Vào [Meta for Developers](https://developers.facebook.com/apps/), đăng ký
   tài khoản developer nếu được yêu cầu và tạo ứng dụng cho game. Chọn use case
   đăng nhập người dùng bằng Facebook, cấu hình nền tảng Web. Tên mục trong
   dashboard có thể thay đổi; dùng hướng dẫn trong dashboard hiện tại.
2. Bật Facebook Login cho Web và khai báo **Valid OAuth Redirect URI** chính xác:
   `https://localhost:7100/signin-facebook`. Backend chỉ xin `public_profile`
   để đọc ID và tên; không cần email cho luồng đã triển khai.
3. Lấy App ID và App Secret. Lưu trên máy bằng .NET User Secrets (project đã có
   UserSecretsId). Thay các giá trị mẫu, không gửi secret vào chat hoặc lưu vào
   Unity, source code, `appsettings.json` hay Git:

   ```powershell
   dotnet user-secrets set "Authentication:Facebook:AppId" "YOUR_APP_ID" --project Backend/src/LittleColony.Api
   dotnet user-secrets set "Authentication:Facebook:AppSecret" "YOUR_APP_SECRET" --project Backend/src/LittleColony.Api
   ```

4. Tạo/tin cậy chứng chỉ phát triển ASP.NET Core bằng lệnh dưới. Windows có thể
   hiện hộp thoại xác nhận tin cậy chứng chỉ; thao tác này do bạn thực hiện.

   ```powershell
   dotnet dev-certs https --trust
   dotnet run --project Backend/src/LittleColony.Api --launch-profile facebook-local
   ```

5. Mở **<https://localhost:7100>**. Kiểm tra `/api/session` có
   `facebookEnabled: true`, chọn Facebook, đăng nhập trên trang Facebook,
   quay lại game rồi chọn **Open my cloud garden**. Bản lưu khách ở origin
   này được sao chép cho tài khoản lần đầu nếu tài khoản chưa có bản cloud.
   Khi đã có bản cloud, game tải bản của tài khoản đó.
6. Thử bằng tài khoản có quyền thử nghiệm theo cấu hình ứng dụng Meta. Việc
   mở đăng nhập cho người dùng ngoài nhóm thử nghiệm cần hoàn thành các yêu
   cầu phát hành, quyền truy cập và chính sách mà Meta hiển thị cho ứng dụng.

URI HTTPS và cách lưu secret theo
[hướng dẫn Facebook Login của Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/facebook-logins?view=aspnetcore-8.0).
Không đổi `localhost` thành `127.0.0.1` giữa chừng: đó là origin khác và callback
phải khớp. Thiết lập HTTPS trực tiếp ở cổng 7100 là đường chạy được chuẩn bị cho
Facebook; proxy HTTP ở 8080 phục vụ phát triển và giữ dữ liệu khách cũ.

## Hành vi lưu game

- Chơi khách tiếp tục dùng khóa lưu cũ. Không gửi game của khách lên server.
- Đăng nhập xong cần chọn vào làng tài khoản; game tải cloud trước khi cho chơi.
- Game giữ nguyên JSON `VillageState` và chạy migration cũ khi tải. Cache cloud
  được tách theo player ID, không đè bản khách.
- Tự lưu cloud khoảng 30 giây và tự thử lại khi mất mạng; không cần nút lưu.
  Trạng thái đồng bộ và nút đăng xuất nằm trong Cài đặt, không hiện trên HUD.
  Bản cục bộ vẫn được lưu như trước. Nếu đóng trình duyệt trước khi request
  hoàn tất, tiến độ gần nhất chưa chắc đã có trên server; lần mở lại sẽ thử
  khôi phục từ cache của cùng tài khoản và origin.
- Mỗi lần ghi có `expectedRevision` và `requestId`. Gửi lại cùng request sau
  mất mạng không tạo thêm một revision khi snapshot đó vẫn là bản hiện hành.
- Nếu thiết bị khác đã ghi trước, server trả `409`; game dừng tự ghi đè và
  hiện nút dùng bản cloud. Lựa chọn đó giữ cache hiện tại ở khóa `.recovery`
  trước khi thay thế. Đây là bản phục hồi kỹ thuật, chưa có giao diện xuất/nhập.
- Đăng xuất cố lưu trước; nếu chưa đồng bộ hoặc đang xung đột, game giữ phiên
  và thông báo để tránh bỏ sót tiến độ. Phiên hết hạn có nút đăng nhập lại.
- Discord đã được triển khai độc lập; xem [DISCORD_CLOUD_SAVE.md](DISCORD_CLOUD_SAVE.md).

## Database và API

| Bảng | Khóa chính | Dữ liệu |
| --- | --- | --- |
| `LittleColonyPlayers` | `identityId` (String) | Hash ID Facebook → player ID nội bộ |
| `LittleColonySaves` | `playerId` (String) | JSON game, schemaVersion, revision, updatedAt, thông tin retry |

Chỉ môi trường Development với DynamoDB Local mới tự tạo bảng. Docker volume
giữ dữ liệu qua lần khởi động lại; không chạy `down --volumes` nếu muốn giữ data.

| Route | Chức năng |
| --- | --- |
| `GET /api/session` | Trạng thái đăng nhập, khả năng đăng nhập Facebook, CSRF token |
| `GET /auth/facebook` | Bắt đầu OAuth; 503 nếu thiếu cấu hình |
| `GET /signin-facebook` | Callback do middleware xử lý và kiểm tra correlation/state |
| `GET /api/save` | 200 bản lưu của mình; 404 nếu chưa có; 401 nếu chưa đăng nhập |
| `PUT /api/save` | Ghi có điều kiện; yêu cầu cookie + `X-CSRF-TOKEN`; 409 khi xung đột |
| `POST /auth/logout` | Xóa cookie phiên của trình duyệt, yêu cầu CSRF token |

Snapshot tối đa 320 KiB, request tối đa 350 KiB. Server kiểm tra envelope,
quyền sở hữu và số phiên bản; **chưa xác minh kinh tế game/chống gian lận**.

## Kiểm thử

```powershell
dotnet run --project Tools/DomainChecks/DomainChecks.csproj
Push-Location Backend
dotnet run --configuration Release --project tests/LittleColony.Api.Checks
Pop-Location
```

Các kiểm thử API tạo bảng tên ngẫu nhiên và chỉ xóa các bảng kiểm thử của chính
lần chạy đó. Không dùng dữ liệu người chơi. Bao gồm cookie/OAuth correlation,
CSRF, tách tài khoản, ghi đồng thời, retry, payload không hợp lệ và đăng xuất.

## Khi triển khai AWS

Backend này chưa được deploy. Bước tiếp theo là provision DynamoDB trên AWS,
cấu hình IAM role cho GetItem/PutItem và ListTables (health probe hiện tại),
HTTPS, secret qua Secrets Manager, Data Protection key ring bền vững/chia sẻ
giữa các instance, và proxy/load balancer đáng tin cậy với forwarded headers.
Giữ game/API cùng origin bằng reverse proxy hoặc CloudFront behaviors;
không bật cache cho `/api/*`, `/auth/*`, `/signin-facebook` và phải forward
cookie/query string cần thiết. Đăng ký callback production với Meta.
Chưa có cấu hình Lambda/container/IaC cho các bước này.
