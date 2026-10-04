# Đăng nhập Discord và lưu game

## Đã triển khai

Nút Discord trên màn hình chào dùng OAuth authorization code ở backend .NET 8.
Backend lấy ID và tên từ Discord bằng scope `identify`, tạo player ID nội bộ
và cookie phiên HttpOnly. Client Secret và access token không được gửi cho Unity
hoặc lưu trong cookie. Luồng dùng tài khoản, cache và API lưu cloud hiện có.

Discord hoạt động độc lập với cấu hình Facebook. Cùng một ID bên ngoài ở hai
nhà cung cấp vẫn tạo hai tài khoản riêng; chưa có chức năng liên kết tài khoản.
Bản Facebook và bản khách giữ nguyên định dạng, khóa lưu và hành vi phục hồi.
Khi phiên hết hạn, nút đăng nhập lại dùng đúng nhà cung cấp của phiên cũ.

Trong màn hình chơi, game không hiển thị thanh save hoặc nút đăng xuất trên HUD.
Tiến độ tự lưu cục bộ mỗi 5 giây và đồng bộ cloud khoảng 30 giây; khi mất mạng,
game giữ cache và tự thử lại. Cài đặt chứa trạng thái đồng bộ, nút Đăng xuất và
các thao tác cần thiết khi phiên hết hạn hoặc bản lưu xung đột. Đăng xuất vẫn
đợi đồng bộ thành công trước khi đóng phiên.

## Cấu hình trên máy

1. Vào [Discord Developer Portal](https://discord.com/developers/applications),
   tạo hoặc chọn ứng dụng cho game. Trong OAuth2, thêm redirect URI chính xác:
   `https://localhost:7100/signin-discord`.
2. Lấy Client ID và Client Secret của ứng dụng rồi lưu bằng .NET User Secrets:

   ```powershell
   dotnet user-secrets set "Authentication:Discord:ClientId" "YOUR_CLIENT_ID" --project Backend/src/LittleColony.Api
   dotnet user-secrets set "Authentication:Discord:ClientSecret" "YOUR_CLIENT_SECRET" --project Backend/src/LittleColony.Api
   ```

   Dùng OAuth Client Secret, không dùng bot token. Không cần thêm bot vào server.
   Không đặt secret trong Unity, file cấu hình được chia sẻ hoặc chat.
3. Khởi động database và API. Nếu đã tin cậy chứng chỉ phát triển để dùng
   Facebook, dùng lại chứng chỉ đó. Nếu chưa, chạy `dotnet dev-certs https --trust`.

   ```powershell
   docker compose -f Backend/compose.yaml up -d
   dotnet run --project Backend/src/LittleColony.Api --launch-profile discord-local
   ```

4. Mở <https://localhost:7100>. `/api/session` phải có `discordEnabled: true`.
   Chọn **Continue with Discord**, cho phép trên Discord, quay lại game và chọn
   **Open my cloud garden**. Tài khoản mới sao chép làng khách tại origin hiện
   tại khi chưa có bản cloud; tài khoản cũ tải làng đã lưu của mình.

Callback phải khớp origin dùng để mở game. Khi dùng proxy HTTP cổng 8080,
khai báo thêm `http://localhost:8080/signin-discord` nếu Discord cho phép URI
phát triển đó. Proxy đã chuyển tiếp callback Discord; cần chạy lại Python server
sau khi cập nhật mã. Không đổi origin nếu muốn tiếp tục dùng bản khách cũ.

Luồng và scope dựa trên [tài liệu OAuth2 chính thức của Discord](https://github.com/discord/discord-api-docs/blob/main/developers/topics/oauth2.mdx).

## API và kiểm thử

- `GET /auth/discord`: bắt đầu OAuth, trả 503 `discord_not_configured` khi thiếu
  Client ID hoặc Client Secret.
- `GET /signin-discord`: callback do middleware kiểm tra state/correlation.
  Từ chối hoặc lỗi đăng nhập quay về `/?login=failed`.
- `GET /api/session`: thêm `discordEnabled` và `provider`; không trả token hay secret.
- `/api/save` và `/auth/logout`: cùng yêu cầu cookie và CSRF như Facebook.

Chạy bộ kiểm thử tại thư mục `Backend` với DynamoDB Local đang chạy:

```powershell
dotnet run --configuration Release --project tests/LittleColony.Api.Checks
```

Bộ kiểm thử chạy OAuth middleware, cookie và DynamoDB Local thật; phản hồi
HTTP của Discord/Facebook được giả lập chỉ trong test host. Bao gồm callback
giả mạo, trao đổi token, scope, tên hiển thị/fallback, CSRF, lưu riêng theo nhà
cung cấp, đăng xuất và tải lại bản lưu sau đăng nhập.

Đăng nhập Discord thật cần Client ID/Secret và redirect URI do chủ ứng dụng
cấu hình. Kiểm thử tự động không xác nhận cấu hình trên Discord Developer Portal.

## Kết quả kiểm tra — 04/10/2026

- Backend Release build: không có warning/error.
- 83 kiểm thử API đạt với DynamoDB Local thật, bao gồm Facebook và Discord.
  Docker Desktop gặp lỗi khởi động; lần kiểm thử dùng bản DynamoDB Local chính
  thức chạy bằng Java với database tạm trong bộ nhớ, không đổi volume Docker.
- 2.714 kiểm tra domain và bản dịch đạt.
- Unity 6000.4.2f1 build WebGL thành công; bản chơi tại `Builds/WebGL` đã cập nhật.
- Browser skill kiểm tra bản WebGL thật: nút Discord bỏ nhãn SOON khi được bật,
  bấm nút chuyển tới Discord OAuth với scope `identify` và callback cùng origin.
  Lần kiểm tra giao diện dùng thông tin ứng dụng giả, không đăng nhập tài khoản thật.
- 5 kiểm tra route proxy đạt; callback giả mạo qua cổng 8080 được backend từ chối.
- Đăng nhập bằng ứng dụng Discord thật vẫn cần cấu hình và kiểm tra thủ công.

## Cập nhật Cài đặt và tự lưu — 04/10/2026

- Bỏ thanh đồng bộ và nút đăng xuất khỏi màn hình chơi; chuyển thao tác tài khoản
  vào Cài đặt. Không còn nút lưu thủ công.
- Giữ tự lưu cục bộ mỗi 5 giây, cloud khoảng 30 giây và cơ chế retry/cache hiện có.
- Phiên hết hạn và xung đột bản lưu có thao tác phục hồi trong Cài đặt.
- 2.726 kiểm tra domain/bản dịch đạt; Unity WebGL build thành công.
- Kiểm tra Browser trên WebGL thật với backend fixture chỉ chạy loopback và dữ
  liệu tạm trong bộ nhớ: ghi nhận tự lưu định kỳ, ghi trước đăng xuất và gọi
  logout từ Cài đặt. Đã kiểm tra English/Tiếng Việt và phần âm thanh mở/đóng;
  không có lỗi console. Fixture này không dùng tài khoản hoặc dữ liệu server thật.
