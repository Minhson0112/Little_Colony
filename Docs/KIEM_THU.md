# Kết quả kiểm tra — 02/10/2026

## Cập nhật 0.3 — map rộng, trang trí và đường đi cư dân

- **65/65 kiểm tra domain đạt**: thêm góc xây mới `x=±18, z=±10`, năm loại trang trí, từ chối ngoài ranh/suối, di chuyển nguyên tử và lưu lại vị trí/góc xoay.
- Blender 5.2 xuất năm FBX có màu cùng `ArtSource/GardenDecor.blend`. Trong Unity và WebGL, biểu tượng lẫn mô hình ghế, đèn, bồn nước, cổng hoa và nấm hiện các vật liệu màu riêng.
- WebGL thử ở origin riêng `localhost:8081` để không thay tiến độ người chơi ở cổng 8080. Mua và đặt ghế gỗ trừ đúng 45 hạt; nhấn giữ/kéo hiện ba nút nổi; Hủy trả ghế về vị trí cũ; Xoay đổi 90° và xác nhận lưu vị trí mới. Tải lại vẫn giữ ghế ở vị trí/góc mới.
- Kéo đất trống cho thấy khu vườn phía đông mở rộng và đạo cụ ở rìa. Kiến đi qua cầu đến đống hạt trong lúc làm; ong được mở ở cấp 2, rời cửa tổ khi vừa giao việc, bay qua cầu đến luống hoa rồi về phía tổ. Chu kỳ vào cửa/ra cửa được xác định bằng trạng thái hoạt ảnh trong `VillageWorld.Animate`.
- Build WebGL cuối bằng Unity 6000.4.2f1: **Succeeded / 22.143.285 bytes**. Ảnh gameplay `Docs/gameplay-backyard-v03.png` và ảnh nút nổi `Docs/move-controls-v03.png` chụp trực tiếp từ bản WebGL, không phải render Blender.
- Trình duyệt bản build kiểm thử không ghi nhận console error/warning sau tải lại. Chưa đo FPS hoặc kiểm tra trên điện thoại.

---

## Cập nhật 0.2 — vườn sau nhà, nâng cấp và bố trí

- **55/55 kiểm tra domain đạt**. Thêm kiểm tra chuyển bản lưu v1→v2, sức chứa theo cấp, chi phí nâng cấp, giới hạn cấp 3, phân công thêm cư dân, di chuyển/đổi hướng nguyên tử, chặn chồng lấn, di chuyển với 0 hạt, giữ việc đang chạy và serialize cấp/vị trí/hướng.
- Build WebGL cuối: **Succeeded / 21.545.422 bytes**. Build bằng Editor đang mở, không thay thế scene người dùng đang chỉnh.
- Đã kiểm tra trong Editor: cả 6 FBX mới có vật liệu Standard màu cụ thể, không thiếu vật liệu. Đã sửa script Blender để màu Base Color được xuất đúng.
- Đã sửa lỗi WebGL stripping loại CapsuleCollider dùng gián tiếp bởi CreatePrimitive: thêm link.xml. Không xuất hiện lỗi console mới trong các lần tải lại bản sửa; hai lỗi của lần thử trước còn trong lịch sử log của tab.

Kiểm tra qua giao diện WebGL ở 1280×720:

| Thao tác | Quan sát |
|---|---|
| Đọc làng cũ | Giữ công trình, 2 kiến/1 ong và tiến độ đã có; chỉ chuyển trường dữ liệu mới |
| Nâng nhà kiến 1→2 | Trừ 60 hạt, sức chứa nhà thành 2, tổng đàn thành 3 kiến |
| Nâng nhà kiến 2→3 | Trừ 120 hạt, sức chứa thành 3, tổng đàn thành 4 kiến; mô hình đổi |
| Nhà cấp 3 | Nút nâng cấp bị vô hiệu hóa |
| Kéo bản xem trước xuống suối | Hiện màu đỏ, không thể xác nhận |
| Hủy di chuyển | Nhà ở nguyên chỗ, số hạt 135 giữ nguyên |
| Di chuyển và xoay 90° | Nhà chuyển sang ô mới, 3 cư dân đi theo, vẫn 135 hạt |
| Nâng tổ ong 1→2 | Trừ 80 hạt, tổng đàn thành 2 ong; mô hình tổ thay đổi |
| Thiếu tiền nâng tổ cấp 3 | Thông báo thiếu hạt, vẫn 55 hạt và 2 ong |
| Tải lại bản cuối | Giữ nhà cấp 3, góc 90°, vị trí mới, 4 kiến/2 ong và 33 hạt sau khi mua bánh |
| Màu và bố cục | Chậu/bình tưới/nhà có màu; mặt tiền hiện trong thumbnail; nút camera tách khỏi bảng quản lý |
| Chuyển động | Gợn nước, lá trôi, bướm, cỏ/cây và côn trùng thay đổi vị trí/tư thế giữa các khung hình |

Ảnh gameplay mới: `Docs/gameplay-backyard-v02.png`. Render riêng của asset Blender: `ArtSource/backyard-assets-preview.png` (không phải ảnh gameplay).

Giới hạn còn lại: chưa đo FPS trên nhiều cấu hình, chưa kiểm tra trình duyệt di động hoặc nhiều tab cùng lưu. Bản đồ vẫn dùng vùng xây hợp lệ cũ để giữ tương thích bản lưu; nền cảnh bên ngoài vùng này chỉ là trang trí.

---

## Hồ sơ kiểm tra bản 0.1

## Môi trường

- Windows, Unity 6000.4.2f1, WebGL Build Support.
- Blender 5.2.2 LTS sinh 9 FBX và file nguồn thành công.
- .NET SDK 8.0.425 cho kiểm tra domain.
- Unity WebGL chạy bằng HTTP localhost:8080 trong trình duyệt tích hợp Codex, viewport 1280×720.

## Đã đạt

- `dotnet run --project Tools/DomainChecks/DomainChecks.csproj`: **31/31 kiểm tra**.
- Bao gồm: vùng xây dựng, trùng vị trí, khóa cấp, phân bổ cư dân, làm đồng thời kiến/ong, đói/tạm dừng/cho ăn, thưởng một lần, bổ sung, nhiệm vụ, thời gian âm/NaN, vòng serialize dữ liệu và tiến độ rời game.
- Unity build cuối: `LITTLE_COLONY_BUILD: Succeeded / 20358564 bytes`; tiến trình thoát mã 0. Không có lỗi/cảnh báo trình biên dịch C# ở build cuối.
- Trình duyệt không ghi nhận console error/warning trong phiên kiểm tra cuối.

## Kiểm tra trực tiếp bản WebGL

| Thao tác | Kết quả quan sát |
|---|---|
| Kiến làm việc 15 giây | Hiện tiến độ, hoàn thành, thu +28 hạt và +12 XP |
| Mua đường | Trừ 8 hạt, thêm 90s năng lượng |
| Xây nhà nấm | Trừ 70 hạt, tăng từ một lên hai kiến |
| Nhận nhiệm vụ | +40 hạt và +15 XP; mở ong/hoa khi đạt cấp 2 |
| Xây tổ ong và hoa | Trừ đúng 95/45 hạt; xuất hiện ong và hoa |
| Tải lại trang | Giữ nguyên 180 hạt, 2 kiến, 1 ong, công trình và XP ở thời điểm kiểm tra |
| Tải lại khi ong đang làm | Công việc phục hồi, sau đó sẵn sàng thu hoạch |
| Thu mật | +38 hạt, +12 XP; tăng cấp tương ứng |
| Tưới hoa | Trừ 5 hạt và trở về trạng thái giao việc |
| Giúp bọ rùa | +12 hạt, +4 XP; bọ rùa biến mất rồi có thể quay lại |
| Chuỗi nhiệm vụ | Hoàn thành cả sáu bước |
| Năng lượng về 0 | Không thể bắt đầu công việc, có thông báo yêu cầu cho ăn |
| Mua bánh khi đói | Trừ 22 hạt, khôi phục 300s năng lượng |
| Tiếng Việt | Font Be Vietnam Pro hiển thị dấu đầy đủ trên WebGL |

Ảnh gameplay thực: `Docs/gameplay-webgl.png`. Bản làng trong trình duyệt đã được chơi để kiểm thử nên có tiến độ; Editor dùng bộ lưu riêng và sẽ bắt đầu làng mới.

## Lỗi phát hiện và đã sửa

- Font mặc định thiếu glyph tiếng Việt trên WebGL → nhúng Be Vietnam Pro theo OFL.
- Nền panel bị kéo méo → dùng viền 9-slice.
- Mặt cỏ che suối ở giữa cảnh → chỉnh cao độ mặt nước.
- Tên biến UI trùng Component.tag → đổi tên, build lại không còn cảnh báo C# đó.

## Giới hạn kiểm tra

Chưa kiểm tra Safari/Firefox, WebGL trên điện thoại, mất context đồ họa, nhiều tab cùng một bản lưu, độ bền lưu trữ dài hạn hoặc hiệu năng trên máy cấu hình thấp. Chưa có Unity PlayMode test tự động; 31 kiểm tra là kiểm tra domain độc lập, phần chơi WebGL được kiểm tra qua giao diện. Bản hiện tại phù hợp duyệt nguyên mẫu trên desktop, chưa phải bản phát hành.

## Lần kiểm tra 0.4 — 02/10/2026

- `dotnet run --project Tools/DomainChecks/DomainChecks.csproj`: **72/72 kiểm tra**. Bàn ghế, máy cắt cỏ, thân cây và đá đều chặn xây; di chuyển vào vùng bị chặn giữ nguyên tiền/vị trí. Bản lưu cũ có công trình ở vùng mới vẫn mở được và xoay tại chỗ.
- Build WebGL từ Unity Editor: `Succeeded: build`. Tải lại tại `localhost:8080` không có lỗi hoặc cảnh báo console.
- Quan sát trong WebGL viewport 1280×720: HUD trên cùng, nút Cửa hàng mở danh mục, chọn nhà nấm chuyển sang màn đặt công trình, hủy trở về làng. Bàn ghế/dù, máy cắt cỏ, thân cây đổ, đá, cỏ cao và suối đều hiển thị.
- Giao việc cho kiến sau khi cho ăn: thời gian và trạng thái cập nhật, kiến qua cầu rồi trở lại bên nhà; việc hoàn thành cho phép thu hoạch. Vật mang nhỏ ở độ zoom mặc định nên nên kiểm tra tiếp ở độ zoom lớn và trên thiết bị có độ phân giải cao.
- Ảnh gameplay WebGL mới: `Docs/gameplay-v04.png` (khác với bản render Blender).

## Lần kiểm tra giao diện 0.4.1 — 02/10/2026

- Đối chiếu trực tiếp ảnh gameplay gốc: HUD chỉ chiếm hai góc trên; cấp, năng lượng, kiến/ong ở trái, hạt sồi ở phải. Nút Cửa hàng dưới trái, thức ăn dưới phải, Sổ tay ở mép phải. Icon đều là hình tạo mới.
- Trong WebGL 1280×720, bấm lần lượt ba tab **Công trình**, **Làm việc**, **Trang trí** cho thấy đúng 2/2/6 vật phẩm, cùng tên, giá và trạng thái khóa cấp 2. Không đổi luật hoặc bản lưu.
- Build Unity WebGL thành công và không có lỗi console trong lần mở thử.

## Lần kiểm tra mở rộng 0.6 — 02/10/2026

- `dotnet run --project Tools/DomainChecks/DomainChecks.csproj`: **96/96 kiểm tra**. Nhà cấp 1/2/3 chứa 2/3/4 cư dân; công trình mới xây/nâng cấp được; điểm làm việc cấp 3 dùng ba lao động và thưởng gấp ba; bản lưu v3 lên v4 giữ tiến độ; khi đang làm không thể nâng cấp làm thay đổi thưởng.
- Unity WebGL build: `Succeeded: build`. Mở lại bằng HTTP trong trình duyệt, không thấy cảnh báo hoặc lỗi console.
- Trực tiếp xây hang kiến mới, nâng lên cấp 2: số kiến tăng từ 4 lên 5 và mô hình thay đổi. Bãi cành nâng đến cấp 3, danh sách việc hiện thưởng theo ba lao động; bảng công trình không còn chồng chữ.
- Một bản lưu thử riêng có bốn ong: nâng hoa cúc lên cấp 3, giao việc 45 giây. UI báo **Ong rảnh 1/4**, **chỗ 3/3**, phần thưởng **225 hạt + 72 XP**; quan sát ong cùng đi giữa tổ, cầu và luống hoa. Mưa giảm còn xác suất 1/20 mỗi 30 giây, ban ngày 240 giây; kiểm tra biên thời tiết bằng DomainChecks.
- Ảnh gameplay Unity WebGL: `Docs/gameplay-v06.png` và `Docs/gameplay-workers-v06.png`. Đây là ảnh chụp game, không phải bản xem trước Blender.

## Sửa đường bay và cửa hàng sau 0.6 — 02/10/2026

- Ong dùng tuyến bay riêng, đi thẳng qua suối; kiến giữ đường qua cầu. Chuyển động cư dân theo chặng với tốc độ cố định, không nội suy vị trí từ thời gian còn lại của công việc.
- Ảnh thu nhỏ của Cửa hàng được chụp dưới ánh sáng ngày cố định. Thẻ nhà ghi cấp 1/2/3 chứa 2/3/4 kiến hoặc ong.
- DomainChecks: **96/96 đạt**; Unity WebGL build: **Succeeded**. Mở WebGL không có lỗi/cảnh báo console. Quan sát ba ong sau khi giao việc vẫn ở phía tổ rồi tiến dần qua suối tới luống hoa, thay vì xuất hiện tức thì tại điểm làm việc.
- Mở cửa hàng trong đêm: cả bốn mẫu nhà và sáu điểm làm việc đã mở khóa vẫn có ảnh sáng, tên, giá và nút Chọn rõ ràng. Ảnh gameplay: `Docs/shop-night-v061.png`.

## Lần kiểm tra nâng cấp và tổ mới 0.7 — 02/10/2026

- `dotnet run --project Tools/DomainChecks`: **105/105 kiểm tra**. Đồng hồ nâng cấp 25/50 giây lưu được, hoàn thành đúng một lần kể cả khi mở lại, không trừ tiền hai lần, không di chuyển hay giao việc trong lúc nâng cấp. Bản lưu v4 giữ thêm chỗ ong ở tổ nhỏ cũ; tổ nhỏ mới 1/2/3 ong, tổ đắt 2/3/4 ong.
- Blender 5.2 xuất sáu FBX màu cho lều lá kiến và tổ ong bông hoa, mỗi kiểu ba cấp. Cửa hàng WebGL hiện đủ sáu nhà trong hai hàng với tên, giá và sức chứa đúng từng cấp.
- Trong WebGL 1280×720, nâng nhà nấm: mô hình chuyển sang giàn giáo có bọ cánh cứng cầm búa chuyển động, thanh tiến độ chạy; sau 25 giây mô hình cấp 2 và kiến thứ ba xuất hiện. Không có lỗi/cảnh báo console.
- Đặt đèn vườn và đợi đến đêm: đèn chiếu vùng sáng vàng trên cỏ. Đã chỉnh tâm ánh sáng lên theo tọa độ thế giới sau lần quan sát đầu; build WebGL cuối `Succeeded: build`, mở lại và quan sát lúc đêm, không có lỗi/cảnh báo console.

## Lần kiểm tra thức ăn đặt trên đất 0.8 — 02/10/2026

- `dotnet run --project Tools/DomainChecks`: **118/118 kiểm tra**. Thức ăn chỉ đặt ở phía vườn, một món mỗi lần; bữa ăn tạm dừng công việc và năng lượng, lưu được giữa chừng, cộng năng lượng một lần khi xong rồi xóa vật thể. Ô đất cũ có thể xây lại. Bản lưu v5 lên v6 giữ tiền/công trình/việc.
- Blender 5.2 xuất `SugarCube.fbx` và `Cookie.fbx` có màu từ `ArtSource/VillageFood.blend`. Unity WebGL build `Succeeded: build`.
- Trong WebGL 1280×720, tab **Thức ăn** hiển thị hai món, mô hình, giá 8/22 hạt và năng lượng 90/300 giây; hai nút đồ ăn cũ ở góc dưới đã biến mất.
- Đặt đường ở vườn: ba kiến qua cầu và hai ong bay tới; hết bữa đường biến mất, thanh năng lượng tăng, cư dân trở về hoạt động trước. Đặt bánh quy vào đúng ô cũ: vùng đặt hợp lệ. Không có lỗi/cảnh báo console ở bước này.

## Sửa đường về sau bữa ăn 0.8.1 — 02/10/2026

- Khi bữa ăn kết thúc trước lúc một con kiến tới nơi, lộ trình mới có thể nối vị trí đang đi với đích bên kia suối. `FollowRoute` nay chèn hai điểm ở hai đầu cầu cho mọi đoạn qua suối chưa nằm trên làn cầu, kể cả đường vừa được tính lại giữa chuyến.

## Cân bằng kinh tế và mở khóa 0.9 — 02/10/2026

- `dotnet run --project Tools/DomainChecks`: **54/54 kiểm tra đạt**. Kiểm tra việc 5/15/45 phút, phần thưởng theo công trình và lao động, XP tăng dần, khóa cấp 1–5, thời gian nâng cấp tăng, tạm dừng khi đói, di trú bản lưu v1/v6 giữ cấp đã đạt và thức ăn cũ giữ phần năng lượng đã mua.
- Unity WebGL build: `Succeeded: build` sau thay đổi cuối. Trên làng mới ở WebGL 1280×720: thanh cấp 1 `0/40`, năng lượng ban đầu gần `15:00`, cửa sổ đống hạt hiện đúng ba lựa chọn `5:00 / +46`, `15:00 / +125`, `45:00 / +330` và nâng điểm `70 hạt / 5:00`. Cửa hàng ở cấp 3 hiển thị giá công trình đã mở và nhãn `Mở cấp 4/5` cho mẫu chưa mở.

## Hàng rào tự nối 0.9.1 — 02/10/2026

- DomainChecks: **60/60 kiểm tra đạt**. Các đoạn hàng rào giá 14 hạt được đặt cạnh nhau theo hàng và góc; cùng ô bị từ chối; công trình thường vẫn không được chồng vào rào; di chuyển/xoay và lưu lại hoạt động.
- WebGL 1280×720: tab Trang trí hiện hàng rào với mô hình gỗ và giá. Đặt hai đoạn sát nhau, hai thanh nối liên tục; thêm các đoạn theo phương vuông góc, thanh rào đổi thành góc khi có hàng xóm mới.
- Unity WebGL build cuối `Succeeded: build`. Tải lại trang thử: bốn đoạn rào vẫn ở đúng vị trí và giữ hình nối; thẻ Trang trí hiện giá `14 hạt` cùng nhãn cấp gọn, không bị cắt chữ.

## Đặt công trình bằng nút nổi 0.9.3 — 02/10/2026

- Unity WebGL build `Succeeded: build`. Chọn Cỏ ba lá trong Cửa hàng: bản xem trước hiện trên đất, không còn thanh đặt ở dưới. Chạm ô đất hợp lệ: ba nút Hủy, ✓, Xoay xuất hiện phía trên bản xem trước.
- Bấm Hủy: không trừ hạt (194 giữ nguyên). Chọn lại, chạm ô đất và bấm ✓: xây xong, tiền giảm 18 hạt (194 → 176), XP tăng 5. Bảng thông tin công trình mở bình thường.

## Tỷ lệ kiến 0.9.4 — 02/10/2026

- Giảm riêng tỷ lệ mô hình kiến từ 0,75 xuống 0,65 (khoảng 13%); mô hình ong và tốc độ di chuyển giữ nguyên. Unity WebGL build `Succeeded: build`; mở lại game và kiểm tra trực quan kiến cạnh nhà, hàng rào và cầu.

## Nhịp chơi và kinh tế 0.10.0 — 03/10/2026

- `dotnet run --project Tools/DomainChecks/DomainChecks.csproj`: **83/83 kiểm tra đạt**.
- Kiểm tra ba thời lượng 15 phút/2 giờ/6 giờ, lợi nhuận ròng theo giờ giảm khi chọn chuyến dài (sau bổ sung và phân bổ chi phí bánh), thức ăn đủ cho chuyến dài, trần 12 giờ, mở ong cấp 5 và điểm kiến cấp 10.
- So sánh Advance từng giây với một lần offline cho cả ba chuyến, có mưa: hoàn thành giống nhau và năng lượng khớp. Kiểm tra lưu/đọc/thu hoạch việc dài.
- Chuyển save v1/v6/v7 lên v8: giữ tiền, cấp, XP, năng lượng, phần thưởng và thời gian còn lại của việc; thức ăn cũ giữ năng lượng đã mua; giữ quyền mua theo cấp cũ; migration lặp lại an toàn.
- Đã thử `Tools/build-webgl.ps1`, nhưng Unity 6000.4.2f1 thoát mã 1 ngay sau bước nhận project path, trước khi có kết quả biên dịch. Log tại `Logs/build-webgl.log` chưa ghi nguyên nhân cụ thể. Chưa xác nhận build WebGL hay kiểm tra trực quan UI giờ/phút/giây của bản này. Build hiện có không được coi là bản 0.10.0 đã xác minh.

## Nhà quả sồi và trang trí — 03/10/2026

- DomainChecks: **101/101 đạt**. Nhà mới thêm đúng cư dân theo ba cấp, nâng cấp, di chuyển/xoay, giới hạn bờ làng; ba món trang trí đặt được hai bờ, không chồng lấn hoặc nâng cấp, lưu/đọc được. ID cũ giữ nguyên, grandfather mở khóa không áp dụng món mới.
- Blender xuất đủ 6 FBX và lưu nguồn; kiểm tra ảnh dựng ba cấp nhà và ba món trang trí.
- `Tools/build-webgl.ps1`: **thành công**, xác nhận cả thay đổi kinh tế trước đó biên dịch được trong Unity.
- Trình duyệt 1280×720: cửa hàng cuộn xuống hiện Nhà quả sồi, hình đúng màu, giá 1.200, sức chứa 2/3/4. Tab Trang trí cuộn hiện Cây nhỏ/Cỏ cao/Ô nhỏ và đúng giá 85/24/65; không có lỗi/cảnh báo console được ghi nhận.
- Chưa thao tác mua công trình vào bản lưu hiện có trong trình duyệt; hành vi đặt/nâng cấp/lưu được kiểm bằng domain. Ảnh WebGL: `Docs/shop-acorn-garden-webgl.png`.

## Hoạt ảnh và lịch bọ rùa — 03/10/2026

- DomainChecks: **106/106 đạt**. Vị trí khách tránh suối/rìa bản đồ, nhà/điểm làm việc, đồ trang trí và đạo cụ cố định; chọn được đất trống cả hai bờ kể cả khi hết tiền.
- WebGL build thành công. Kiểm tra thực tế: bọ xuất hiện bên trái làng thay vì điểm cũ bên phải, có chân cựa và thân xoay; bấm Giúp khiến nhãn biến mất, thân lật và thưởng đổi 268 → 288 hạt, XP 289 → 292. Sau hoạt ảnh, bọ biến mất. Không ghi nhận lỗi/cảnh báo console.
- Lịch mới xét xác suất 20% mỗi 30 giây, bảo đảm thử xuất hiện sau tối đa 300 giây khi có đất trống. Không tích lũy lượt khi bọ đang chờ hoặc đang được cứu. Chưa đo phân bố thống kê nhiều lượt trong trình duyệt.
- Ảnh: `Docs/ladybug-upturned-webgl.png`, `Docs/ladybug-rescued-webgl.png`.

## Kiến sư tử và cổng — 03/10/2026

- DomainChecks: **225/225 đạt**. Thêm kiểm tra thưởng kiến sư tử không hoàn thành nhiệm vụ bọ rùa; cổng nối rào ở cả bốn hướng, đặt cổng trước/rào trước, từ chối rào chắn cửa và xoay gây chồng lấn, di chuyển và lưu/đọc. Kiểm tra hành lang xuyên cổng với rào hai bên và trụ vẫn chặn đường.
- Kiến sư tử đã kiểm tra trực tiếp WebGL: hố đất và mô hình có hàm hiển thị, nhãn đủ chữ; bấm đuổi khiến hố và con vật biến mất, hạt 288 → 313, XP 292 → 296, bọ rùa vẫn tồn tại độc lập. Không ghi nhận lỗi/cảnh báo console. Ảnh `Docs/ant-lion-webgl.png`.
- Cổng: xác minh hình học nối rào trong mã và logic đặt/tìm đường bằng DomainChecks; chưa quan sát trực tiếp cư dân đi xuyên cổng trong WebGL.
- Build WebGL cuối sau thay đổi lối đi cổng: thành công (Unity thoát mã 0).

## Refactor C# và tài liệu mã — 03/10/2026

- Phạm vi: 8 file C# thuộc Assets/Scripts, Assets/Editor và Tools/DomainChecks/Program.cs. Thêm doc block tiếng Anh cho 197 khai báo kiểu, class, constructor, method và local function. Local function dùng block comment để tránh cảnh báo XML documentation của compiler.
- Chuẩn hóa thụt dòng, ngoặc khối, câu lệnh riêng dòng, switch/array nhiều dòng, khai báo field riêng và cách xuống dòng điều kiện dài. Method dạng expression body được chuyển sang block; các helper dựng cảnh/UI/tìm đường được đổi sang tên mô tả rõ chức năng.
- Giữ tên class, namespace, trường serialized, property, API public, callback Unity, enum ID, hằng số, chuỗi UI tiếng Việt và thứ tự khởi tạo/thực thi. Không thay đổi save v8 hoặc luật gameplay.
- Đối chiếu bằng parser Roslyn: mọi token thực thi khớp bản trước sau khi áp dụng cùng bộ biến đổi bố cục và đổi tên helper private. Báo cáo tại `Logs/csharp-refactor-verification.txt`; bản trước refactor tại `Logs/csharp-refactor-baseline`.
- DomainChecks: **225/225 đạt**. Build WebGL lần đầu thành công; bản cuối được build lại sau lượt chỉnh định dạng.
- Thêm `.editorconfig` cho C#: UTF-8, bốn dấu cách, ngoặc dòng riêng và quy tắc định dạng nhất quán.
- Build WebGL cuối sau toàn bộ refactor: thành công, Unity thoát mã 0. Không ghi nhận lỗi hoặc cảnh báo C# trong log build.

## Bỏ thanh thông báo đáy màn hình — 03/10/2026

- Gỡ phần vẽ thanh thông báo `Game.Notice` khỏi `VillageUI.OnGUI`.
- Build WebGL thành công. Quan sát game 1280×720: phần giữa đáy màn hình đã trống, nút Cửa hàng và HUD hiển thị bình thường; không ghi nhận lỗi/cảnh báo console.
- Ảnh thực tế: `Docs/gameplay-no-bottom-status.png`.

## Cài đặt âm thanh và zoom bằng thao tác — 03/10/2026

- Thay nút góc nhìn và các nút zoom bằng nút Cài đặt. Nút Âm thanh mở/đóng thanh trượt âm lượng tổng 0–100%; áp dụng qua AudioListener cho nhạc nền và các lớp âm môi trường.
- `Tools/build-webgl.ps1`: **thành công**, Unity thoát mã 0. Không ghi nhận lỗi hoặc cảnh báo C# trong log build.
- WebGL 1280×720: mở Cài đặt → Âm thanh, kéo thanh trượt về 0%, 50%, 100%. Tải lại bản build cuối và mở lại bảng: mức 50% đã lưu vẫn được giữ. Sau kiểm tra, trả âm lượng về 100%.
- Khi bảng mở, bấm Cửa hàng hoặc lăn chuột trên thế giới không kích hoạt giao diện phía sau hoặc đổi camera. Đóng bảng và lăn chuột hai chiều thay đổi zoom bình thường. Không ghi nhận lỗi/cảnh báo console.
- Mobile dùng khoảng cách hai ngón để zoom, giữ giới hạn camera hiện có và bỏ qua thao tác chọn/kéo đến khi thả hết ngón. Canvas có `touch-action: none`. Chưa kiểm tra trực tiếp thao tác hai ngón trên thiết bị mobile.
- Ảnh thực tế: `Docs/settings-audio-webgl.png`. Không thay đổi luật domain hoặc định dạng bản lưu làng.

## Mapping Việt–Anh và chọn ngôn ngữ — 03/10/2026

- Gom đủ **246/246 chuỗi Việt gốc** trong C# vào `Assets/Scripts/Localization/I18n.cs`, kể cả thông báo, lỗi domain, tên đối tượng trong scene và tên sản phẩm. Thêm nhãn Việt không dấu, nhãn cài đặt ngôn ngữ, template chứa số liệu và 8 key cho trang tải WebGL: tổng **301 entry Việt–Anh**. Audit không phát hiện key thiếu hoặc chuỗi Việt còn nằm ngoài catalog trong C#; báo cáo tại `Logs/i18n-extraction-verification.txt`.
- `dotnet run --project Tools/DomainChecks/DomainChecks.csproj`: **1.436/1.436 đạt**, gồm 225 kiểm tra domain cũ và 1.211 kiểm tra i18n. Kiểm tra bản dịch hai ngôn ngữ, placeholder khớp nhau, câu ghép với số liệu, tên loài trong câu, đổi ngược ngôn ngữ, giữ lỗi domain chuẩn và JSON bản lưu không thay đổi, fallback ngôn ngữ không hợp lệ.
- Build WebGL cuối sau chỉnh khung lỗi: **thành công**, Unity thoát mã 0; không ghi nhận lỗi/cảnh báo C#.
- WebGL 1280×720: bấm Ngôn ngữ trong Cài đặt chuyển Việt → Anh → Việt. HUD, trạng thái ngày/đêm, bảng âm lượng, cửa hàng, sức chứa/giá, sổ tay và thông tin nhà đổi đồng bộ. Điều chỉnh caption và vùng tên nhà để không cắt chữ tiếng Anh.
- Tải lại ở cả hai ngôn ngữ: màn hình tải và giao diện dùng đúng lựa chọn đã lưu. Tiêu đề trang, thuộc tính `lang` và nhãn truy cập của canvas đổi theo lựa chọn. `i18n-web.json` được sinh từ catalog chính trong build callback, không có bảng dịch riêng cần chỉnh.
- Thử xem trước Cỏ ba lá trên suối: lỗi tiếng Anh hiển thị đủ hai dòng; Hủy không trừ hạt (313 giữ nguyên), XP giữ 296/340. Cuối lượt kiểm tra trả về tiếng Việt và giữ âm lượng 100%. Không ghi nhận lỗi/cảnh báo console. Chưa kiểm tra trực tiếp UI trên điện thoại thật.
- Ảnh: `Docs/settings-language-vi.png`, `Docs/settings-language-en.png`, `Docs/shop-i18n-en.png`, `Docs/placement-i18n-en.png`.

## Tiếng Anh mặc định — 03/10/2026

- Đặt tiếng Anh làm mặc định của i18n, PlayerPrefs và trang tải WebGL khi chưa lưu lựa chọn; ngôn ngữ không hợp lệ cũng dùng tiếng Anh. Giữ nguyên ID ngôn ngữ và lựa chọn hợp lệ đã lưu.
- DomainChecks và i18n: **1.437/1.437 đạt**, gồm kiểm tra mặc định tiếng Anh và fallback mới. Build WebGL thành công, không ghi nhận lỗi/cảnh báo C#.
- Mở bản build ở `127.0.0.1:8080`: màn hình tải, HUD, nút Settings/Shop/Journal và nhãn công việc hiển thị tiếng Anh. Không thay đổi bản lưu làng để kiểm tra ngôn ngữ.

## Mở rộng ngang và viền thân cây — 03/10/2026

- DomainChecks: **1.737/1.737 đạt**, thêm tám kịch bản về mép xây mới `x=±30`, chiều sâu giữ `z=±10`, giới hạn ngoài map, quy tắc hai bờ/suối, di chuyển công trình cũ sang vùng mới, lưu/đọc v8 và khoảng an toàn xuất hiện khách.
- Build WebGL cuối qua Unity Editor: `Succeeded: build`; không ghi nhận lỗi/cảnh báo C# trong log.
- Kiểm tra WebGL ở 1280×720: thấy dãy thân cây lớn có rêu quây phía dưới, suối vẫn thông; thu nhỏ và kéo camera tới mép phải mới, hàng rào và đạo cụ rìa hiện đúng. Sau khi kéo rộng nền, góc nhìn xa ở mép phải được phủ kín đất, không còn khoảng trống ngoài terrain. Console không ghi nhận lỗi/cảnh báo.
- Giữ công trình và tài nguyên bản lưu hiện có; không xác nhận mua/di chuyển công trình khi kiểm tra hình ảnh. Đặt và lưu tại tọa độ mới đã được kiểm bằng domain; chưa kiểm trực tiếp cư dân đi từ nhà ở mép mới trong WebGL.
- Ảnh gameplay Unity WebGL: `Docs/gameplay-expanded-map-webgl.png`.

## Thu hẹp zoom ra tối đa — 03/10/2026

- Giảm `MaxCameraZoom` từ 17 xuống 9,7; zoom mặc định cũng là 9,7. Tâm nhìn mặc định và phím Home dùng `z=2,1` để khung hình tới hàng rào trên và khoảng nửa thân cây dưới.
- Unity WebGL build: `Succeeded: build`; không ghi nhận lỗi/cảnh báo C# trong log.
- Quan sát WebGL 1280×720 sau tải bản mới: hàng rào nằm sát mép trên, chỉ phần trên của thân cây viền dưới xuất hiện. Cuộn zoom ra thêm tám lần: khung hình giữ nguyên ở giới hạn. Console không ghi nhận lỗi/cảnh báo.
- Ảnh: `Docs/gameplay-camera-limit-webgl.png`. Không đổi luật domain hoặc bản lưu làng.

## Hàng chậu hoa chia vườn và cổng giữa — 03/10/2026

- DomainChecks: **1.871/1.871 đạt**. Thêm 134 kiểm tra cho đất xây hai phía hàng chậu, vùng cấm đặt trên chậu/cổng, vị trí khách, các ô chậu và trụ vòm, hành lang liên tục qua cổng, giao tuyến hai hướng, chuyến trong cùng ô vườn và đường chéo đi đúng cửa. Kiểm tra save v8 giữ công trình cũ trùng hàng chậu, xoay tại chỗ và di chuyển ra ngoài.
- `GardenDivider` chia sẻ hình học giữa renderer, luật đặt và dẫn đường. `FollowRoute` chèn hai điểm qua cổng cho cả kiến/ong khi đoạn đường vượt hàng chậu ngoài cửa; bỏ waypoint trung gian của đường bay nằm trong hàng chậu, giữ đích cuối của công trình cũ. Ong dùng độ cao 0,72 khi tiếp cận cổng.
- Unity WebGL build cuối: `Succeeded: build`; không ghi nhận lỗi/cảnh báo C# trong log.
- WebGL 1280×720: kéo camera sang bờ phải, quan sát đủ hàng chậu hồng/tím/vàng, vòm giữa xoay 60° và lối đá qua cổng. Tảng đá cũ cạnh cửa đã dời ra rìa phải. Giữ zoom tối đa và hàng thân cây dưới. Console không ghi nhận lỗi/cảnh báo.
- Không mua hoặc di chuyển công trình trong bản lưu hiện có. Làng quan sát có năng lượng 0 và chưa có ong; chưa kiểm trực tiếp chuyến kiến/ong qua cổng trong WebGL. Quy tắc giao tuyến, lối trống và vòng lưu/đọc đã được kiểm bằng domain.
- Ảnh gameplay Unity WebGL: `Docs/gameplay-garden-divider-webgl.png`.

## Hàng cây mục khu nhà và lối đá hai bờ — 03/10/2026

- DomainChecks: **1.993/1.993 đạt**, gồm 122 kiểm tra bổ sung cho hai khu nhà, vùng đặt công trình và xuất hiện khách, hình học sáu khúc cây mục, hành lang đi bộ liên tục qua khoảng mở, giao tuyến qua cả hai hàng ngăn và tương thích bản lưu v8.
- Bên trái có sáu khúc cây mục tại `x=-18`, chia khu nhà thành hai phần. Theo điều chỉnh mới, bỏ cả hai cổng vòm ở hàng cây mục và hàng chậu hoa; mỗi khoảng mở tại `z=0` dùng năm phiến đá thấp, rộng thông thoáng cho kiến và ong.
- Lưới tìm đường đi bộ tránh khúc cây mục. Dẫn đường chung chèn các điểm qua khoảng mở theo thứ tự hàng ngăn gần nhất; giữ đường nối cầu và khả năng di chuyển công trình cũ ra khỏi hàng ngăn.
- Unity WebGL build cuối: `Succeeded: build`; không ghi nhận lỗi/cảnh báo C# trong log.
- Quan sát WebGL 1280×720 ở cả hai bờ: hàng cây mục có vỏ nứt, ruột rỗng, rêu và nấm; hàng chậu hoa giữ nguyên. Hai khoảng mở chỉ có đá, không còn vòm. Console không ghi nhận lỗi/cảnh báo.
- Không mua hoặc di chuyển công trình trong bản lưu hiện có. Chưa quan sát trực tiếp chuyến kiến/ong qua khoảng mở mới trong WebGL; kết nối và dẫn đường đã được kiểm bằng domain.
- Ảnh gameplay Unity WebGL: `Docs/gameplay-housing-log-divider-webgl.png`, `Docs/gameplay-garden-stone-passage-webgl.png`.

## Mua đất trong tab Mở rộng — 03/10/2026

- DomainChecks: **2.578/2.578 đạt**. Kiểm tra 252 ô mỗi bờ đều khóa trước mua, không đặt/chuyển công trình hoặc xuất hiện khách; mua độc lập giá 5.000/7.500 hạt, đủ đúng giá, thiếu tiền, ID sai, mua lặp, lưu/đọc một và hai vùng đã mở. Mua không đổi công trình, ID, XP hoặc năng lượng.
- Bản lưu v9: kiểm tra chuyển từ bản cũ có đất ngoài đã sử dụng ở từng bờ và bản chưa sử dụng, giữ tiền và hợp đồng làm việc đang chạy. Chỉ vùng đã có công trình được giữ mở; chuyển đổi lặp không cấp thêm đất.
- Unity WebGL build cuối: `Succeeded: build`, 41.431.778 byte. Lượt đầu bị lệch layout class giữa assembly Editor cũ và player mới khi Editor đang chạy game; sau nhập lại thư mục script và biên dịch, build thành công. Không ghi nhận lỗi/cảnh báo C# hoặc lỗi shader.
- Quan sát gameplay WebGL ở cả hai bờ: nền và cỏ của vùng ngoài tối rõ, hàng ngăn/lối đá giữ nguyên, ổ khóa cùng giá mua nằm giữa vùng. Bấm giá trên đất khu nhà mở đúng tab Mở rộng, không kéo camera. Kiểm tra thêm bằng tab localhost riêng khi người dùng đang chơi trên 127.0.0.1.
- Tab Mở rộng có hai thẻ, mô tả công dụng, giá, trạng thái khóa và số hạt còn thiếu. Kiểm tra tiếng Việt và tiếng Anh ở 1280×720; bấm nút mua bị vô hiệu hóa khi thiếu tiền không đổi số dư. Không ghi nhận lỗi/cảnh báo console.
- Chưa mua thành công trực tiếp trong bản lưu người dùng vì không đủ tiền; giao dịch thành công, quyền sở hữu sau tải lại và khả năng xây sau mua được kiểm bằng domain. Giữ công trình, không mua hay chuyển công trình trong quá trình kiểm tra. Editor đã được trả về chế độ chơi.
- Ảnh gameplay Unity WebGL: `Docs/shop-expansion-en-webgl.png`, `Docs/shop-expansion-vi-webgl.png`, `Docs/locked-housing-expansion-webgl.png`, `Docs/locked-garden-expansion-webgl.png`.

## Phủ tối toàn vùng và sửa khởi tạo Unity Editor — 03/10/2026

- Giá đất hiện tại: khu nhà **10.000 hạt**, khu vườn **15.000 hạt**. DomainChecks sau đổi giá: **2.578/2.578 đạt**. Vùng tối phủ từ hàng ngăn tới rìa ngoài `x=±34`, toàn chiều sâu `z=±13,6`; cả nền, cỏ và đạo cụ đều được làm tối, khôi phục màu khi mua đất.
- Sửa lỗi `MaterialPropertyBlock` được tạo trong field initializer của MonoBehaviour, khiến Unity Editor không dựng được thế giới. Chuyển khởi tạo sang `BackyardEnvironment.Create`, sau khi component đã được thêm trên luồng chính. Không đổi luật gameplay hoặc định dạng bản lưu.
- Thêm `Assets/Editor/RuntimeCreationChecks.cs`: dựng thế giới trong Play Mode thật, đồng bộ làng mới, chạy 60 khung hoạt ảnh, đồng bộ hai vùng đã mở rồi thoát Play Mode. Làng kiểm tra độc lập và không truy cập PlayerPrefs của người chơi. **PASS**, Unity thoát mã 0; kết quả tại `Logs/runtime-creation-result.txt`, log tại `Logs/runtime-creation-check.log`.
- Build WebGL bản sửa: **Succeeded**, 41.441.219 byte; không ghi nhận lỗi/cảnh báo C# hoặc lỗi shader trong `Logs/build-webgl.log`.
- Mở scene Main và chạy trực tiếp trong Unity Editor: thế giới, hoạt ảnh cư dân và cửa hàng hiển thị bình thường. Tab Mở rộng hiện đúng giá 10.000/15.000; không mua đất khi kiểm tra. Bản lưu hiện có giữ 741 hạt, 13 kiến và 7 ong. Log `Logs/unity-play-verification.log` không ghi nhận exception hoặc lỗi/cảnh báo C#. Editor được để lại ở Play Mode của scene Main.

## Đom đóm báo điểm làm việc sẵn sàng — 03/10/2026

- Bỏ toàn bộ bảng trạng thái nổi của điểm làm việc và vùng chặn chuột tương ứng. Chọn trực tiếp công trình vẫn mở bảng chi tiết để giao việc, thu hoạch, bổ sung và xem thời gian.
- `WorkSiteFireflies` tạo sáu đom đóm bằng billboard, bay quanh phía trên mô hình và sáng nhẹ theo nhịp. Theo điều chỉnh tiếp theo, hiện ở trạng thái Idle/Ready/Empty, gồm giao việc, thu hoạch, tưới hoa và xếp lại vật liệu; ẩn khi Working (kể cả đói) hoặc đang nâng cấp. Mô hình cũng phát sáng nhẹ theo chu kỳ khoảng 3 giây bằng bản sao material riêng, giữ màu và material nguồn, khôi phục màu ngay khi công việc bắt đầu. Hiệu ứng đi theo công trình, được tạo lại khi đổi mô hình cấp và giải phóng cùng công trình; không thêm dữ liệu vào bản lưu hoặc đổi luật domain. Bật module Particle System có sẵn của Unity và dùng shader tự viết, không thêm asset bên thứ ba.
- Mở rộng kiểm tra Play Mode độc lập: cả sáu loại điểm làm việc, chuyển trạng thái trực tiếp không cần dựng lại, chuyển động đom đóm, nâng cấp tới cấp 3 và thay đổi vị trí. Kiểm tra trạng thái active, số hạt được render, emission tăng giảm theo thời gian, khôi phục màu gốc và material nguồn không bị thay đổi; tiếp tục chạy 60 khung hoạt ảnh và kiểm tra teardown. **PASS**, không có exception hoặc lỗi/cảnh báo C# trong `Logs/runtime-fireflies-check.log`.
- Build WebGL cuối gồm highlight: **Succeeded**, 43.108.737 byte, Unity thoát mã 0. Giữ shader variant emission dùng lúc chạy trong `Assets/Resources/WorkSiteGlowVariants.shadervariants`, tránh mất hiệu ứng trong bản WebGL. Đã sửa tên biến shader trùng từ khóa HLSL trong lượt build đầu; log build cuối không có lỗi shader hoặc lỗi/cảnh báo C#.
- Quan sát trực tiếp scene Main trong Unity: bảng Working/thời gian không còn trên các điểm làm việc; đom đóm hiển thị trên điểm sẵn sàng. Chọn Twig yard đang làm việc vẫn thấy thời gian và phần thưởng trong bảng chi tiết. Không giao việc, thu hoạch hoặc mua công trình trong lượt kiểm tra; giữ bản lưu người chơi. `Logs/unity-fireflies-verification.log` không ghi nhận exception hoặc lỗi shader/C#. Editor để lại ở Play Mode.
- Sau bổ sung highlight và trạng thái cần tưới/xếp lại, mở lại Main: các hoa sẵn sàng có ánh sáng nhẹ trên mô hình và đom đóm phía trên; điểm đang làm việc giữ hình ảnh bình thường. Bản lưu hiện tại vẫn có 4.610 hạt, 13 kiến và 7 ong; log cuối không có exception hoặc lỗi shader/C#. Trạng thái cần tưới/xếp lại và việc tắt hiệu ứng sau tương tác đã được kiểm bằng kịch bản Play Mode độc lập, không thao tác tài nguyên người chơi.
