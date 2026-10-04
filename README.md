# Lá Nhỏ · Little Colony 0.10.0

Bản mẫu Unity 3D lấy cảm hứng từ vòng lặp quản lý của Bug Village, dùng asset mới tạo bằng Blender. Dự án được khởi tạo trực tiếp tại `C:\Users\admin\Bug Village`.

## Mở và chơi

Backend .NET chạy local nằm trong [`Backend/`](Backend/README.md), tách khỏi Unity client. Hướng dẫn khởi động API và DynamoDB Local ở README của backend.

- Unity **6000.4.2f1**, kèm WebGL Build Support.
- Unity Hub → Add project from disk → chọn thư mục này.
- Mở `Assets/Scenes/Main.unity`, nhấn Play.
- Màn hình chào có nền vườn minh họa, chọn English/Tiếng Việt và bật/tắt âm thanh. Chọn **Play as a guest / Chơi khách** để vào bản lưu hiện có. Discord/Facebook hỗ trợ đăng nhập và lưu cloud khi backend đã có cấu hình; xem [`Docs/LOGIN_SCREEN.md`](Docs/LOGIN_SCREEN.md) và [`Docs/DISCORD_CLOUD_SAVE.md`](Docs/DISCORD_CLOUD_SAVE.md).
- Nhạc nền từ `Assets/Resources/Audio/BackyardMusic.mp3` tự phát và lặp trong lúc chơi. Trên trình duyệt, hãy tương tác với trang để bật âm thanh nếu trình duyệt chặn tự phát.
- Âm đêm và âm mưa phát lặp trên hai lớp riêng bên trên nhạc nền. Khi trời vừa tối vừa mưa, cả ba lớp phát cùng lúc; mỗi lớp môi trường dịu dần khi điều kiện tương ứng kết thúc.
- Nếu cần tạo lại scene: menu **Little Colony → Prepare main scene**.
- Bản web: chạy `python Tools/serve-webgl.py`, mở [localhost:8080](http://localhost:8080).

## Điều khiển

Chọn **Giao việc** trên đống hạt, chọn chuyến 15 phút, 2 giờ hoặc 6 giờ, đợi và nhận thành quả. Nhận thưởng trong Sổ tay để tiếp tục hướng dẫn.

- Nhấn **Cửa hàng** ở góc dưới trái để mở danh mục; chọn công trình rồi chạm ô đất muốn xây. Ba nút nổi trên bản xem trước cho phép **Hủy**, **✓ Đặt** hoặc **Xoay**; không còn thanh xác nhận ở cuối màn hình. Viền xanh hợp lệ, đỏ bị chặn. Hủy/Esc không trừ hạt.
- Nhà ở bên trái suối; điểm làm việc ở bên phải. Có thể xây trong vùng rộng đến `x=±18, z=±10`, trừ dòng suối. Nhà cấp 1 đón hai cư dân.
- Đường và bánh ở tab **Thức ăn** trong Cửa hàng. Chọn món, đặt ở khu vườn bên phải suối rồi xác nhận; kiến và ong tạm ngừng lộ trình để tới ăn. Sau bữa, vật thể biến mất, ô đất trống lại và cư dân trở về công việc hoặc trạng thái trước đó. Đường thêm 2 giờ năng lượng, bánh thêm 8 giờ; hết năng lượng thì việc tạm dừng.
- Sau thu hoạch, bổ sung đống hạt hoặc tưới hoa với 7 hạt.
- Điểm làm việc có đom đóm bay phía trên và phát sáng nhấp nháy nhẹ khi sẵn sàng giao việc, thu hoạch, tưới hoa hoặc xếp lại vật liệu. Đang làm việc hoặc nâng cấp thì cả hai hiệu ứng tắt. Chạm công trình để thực hiện thao tác và xem thời gian trong bảng chi tiết; không còn bảng trạng thái nổi trên công trình.
- Kéo trên đất trống/WASD để dạo vườn. PC: lăn chuột để zoom. Mobile: chụm hai ngón để thu nhỏ, tách hai ngón để phóng to. Home về giữa, Esc hủy hoặc đóng Cài đặt.
- Bọ rùa xuất hiện định kỳ; chọn để giúp và nhận thưởng.
- Chọn nhà → **Nâng cấp**: bọ cánh cứng làm việc 15 phút để lên cấp 2 và 1 giờ để lên cấp 3; điểm làm việc cần 30 phút rồi 2 giờ. Đồng hồ vẫn chạy khi đóng game. Nhà kiến chứa 2/3/4 cư dân. Tổ ong nhỏ chứa 1/2/3 ong; tổ ong đèn lồng và tổ ong bông hoa chứa 2/3/4 ong. Tổ ong nhỏ cũ trong bản lưu vẫn giữ một chỗ ong bổ sung.
- **Nhấn giữ công trình khoảng 0,4 giây rồi kéo** đến ô mới. Ba nút ngay phía trên: **Hủy** trả về vị trí cũ, **✓ Đặt** xác nhận, **Xoay** quay 90°. Miễn phí, giữ nguyên cư dân và công việc. R xoay, Enter xác nhận, Esc hủy.
- Cửa hàng có năm tab: **Công trình** (ba kiểu nhà kiến, ba kiểu nhà ong), **Làm việc** (ba điểm cho kiến, ba loại hoa cho ong), **Trang trí**, **Thức ăn**, **Mở rộng**. Mỗi món có cấp mở khóa riêng từ 1 đến 12. Điểm làm việc nâng cấp được để 1/2/3 cư dân làm cùng lúc; phần thưởng tăng theo số lao động và loại điểm làm việc. Đèn vườn bật ánh sáng vàng khi trời tối. Đồ trang trí có thể đặt ở cả hai phía suối, trừ vùng đạo cụ cố định. Hàng rào gỗ trong tab Trang trí giá 14 hạt/đoạn: đặt ở các ô sát nhau để các thanh rào tự nối thành hàng, góc hoặc ngã ba.
- Khi đang làm việc, kiến và ong tới điểm thu thập; kiến cõng khúc gỗ, ong xách túi mật về qua hướng cửa tổ, chui vào rồi trở ra cho chuyến tiếp theo.
- Ong bay thẳng qua suối theo một cung bay, không đi qua cầu. Kiến vẫn dùng cầu. Cả hai di chuyển theo tốc độ cố định khi khám phá, làm việc và về tổ, không nhảy theo đồng hồ công việc.
- Mỗi thẻ nhà trong Cửa hàng ghi sức chứa theo cấp 1/2/3; ảnh công trình được tạo với ánh sáng cố định nên vẫn rõ khi mở cửa hàng lúc tối hoặc mưa.
- Một ngày trong game dài 240 giây, đêm dài 60 giây. Mưa có khoảng 1/20 xác suất trong mỗi khoảng 30 giây, có thể rơi lúc sáng hoặc tối. Cư dân rảnh khám phá quanh làng và tránh công trình; lúc tối họ về tổ, còn cư dân được giao việc vẫn có thể làm trong đêm khô ráo. Khi mưa, cả làng trú trong tổ và việc tạm dừng đến khi tạnh.

## Build và kiểm tra

Đóng Editor của dự án trước khi chạy build từ terminal:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/build-webgl.ps1
python Tools/serve-webgl.py
```

Hoặc dùng **Little Colony → Build WebGL** trong Editor. Kết quả tại `Builds/WebGL`; cần phục vụ bằng HTTP, không mở `index.html` bằng `file://`. Bản phát triển tắt nén để chạy trên HTTP server đơn giản. Script server chỉ nghe trên máy này, không công khai ra mạng.

Kiểm tra khởi tạo thế giới và 60 khung hoạt ảnh trong Play Mode bằng một Unity batch riêng, khi Editor dự án đã đóng:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.2f1\Editor\Unity.exe' -batchmode -nographics -projectPath (Get-Location).Path -executeMethod LittleColony.Editor.RuntimeCreationChecks.Run -logFile 'Logs/runtime-creation-check.log'
```

Không thêm `-quit`: kiểm tra tự thoát sau khi hoàn tất Play Mode và ghi kết quả vào `Logs/runtime-creation-result.txt`. Làng kiểm tra độc lập, không đọc hoặc ghi PlayerPrefs của người chơi.

Kiểm tra logic không cần mở Unity:

```powershell
dotnet run --project Tools/DomainChecks/DomainChecks.csproj
```

## Cấu trúc

| Thư mục | Vai trò |
|---|---|
| Assets/Scripts/Domain | Trạng thái làng, kinh tế, dân số, thời gian, nhiệm vụ; không phụ thuộc Unity |
| Assets/Scripts | Điều khiển trò chơi, dựng cảnh, giao diện tiếng Việt |
| Assets/Editor | Tạo scene, cấu hình và build WebGL |
| Assets/Resources/Models | Các FBX tự tạo bằng Blender, gồm nhà, điểm làm việc và đồ trang trí sân vườn |
| Assets/WebGLTemplates | Trang tải WebGL |
| ArtSource | File Blender và render tham khảo |
| Tools | Sinh asset, build, server, kiểm tra logic |
| Docs | Nghiên cứu, nguồn asset, kết quả kiểm tra |

Tiến độ lưu bằng PlayerPrefs, trên web được Unity lưu ở IndexedDB. Tự lưu mỗi 5 giây và sau thao tác thay đổi trạng thái. Khi trở lại, công việc chỉ tiến triển theo lượng năng lượng còn ở lần lưu. Xóa dữ liệu trang hoặc dùng một origin/trình duyệt khác sẽ không có bản lưu cũ. Không có đồng bộ giữa thiết bị.

Bản lưu cũ được chuyển sang kinh tế 0.9 mà không xóa tiền hay công việc đang chạy. XP tổng được quy đổi để giữ nguyên cấp đã đạt; thời gian, thưởng và XP của việc đã giao giữ nguyên. Bữa ăn đã mua giữ nguyên lượng năng lượng cũ. Giá và thời gian mới chỉ áp dụng cho giao dịch mới.

## Phạm vi 0.4

Đã có vòng lặp xây → giao việc → chờ → thu hoạch → cho ăn → mở ong/hoa, sáu nhiệm vụ và khách bọ rùa. Bản 0.2 đổi bối cảnh sang vườn sau nhà với hàng rào, hiên nhà, luống cây, chậu và bình tưới. Suối gợn chảy, lá trôi, cỏ/cây đung đưa, bướm bay và chân/cánh cư dân chuyển động. Giao diện có hình công trình, thanh thông tin gọn, sổ tay thu gọn và bảng quản lý nhà.

Nhà có ba cấp với mô hình riêng; hỗ trợ di chuyển và xoay bằng bản xem trước có xác nhận/hủy. Bản lưu 0.1 tự chuyển sang dữ liệu 0.2: nhà cũ cấp 1, hướng 0°, giữ hạt, XP, nhiệm vụ, công trình và việc đang làm. Một bản sao dữ liệu v1 được giữ trong PlayerPrefs trước khi chuyển đổi.

Giao diện 0.4 đưa cấp, năng lượng, hạt và dân số lên thanh trên; danh mục công trình chỉ xuất hiện khi mở Cửa hàng. Vườn có bàn ghế lớn dưới dù, máy cắt cỏ, thân cây đổ, cỏ cao và cụm đá. Đạo cụ lớn chặn xây trong vùng của chúng; công trình trong bản lưu cũ vẫn giữ vị trí.

Bản 0.4.1 thu HUD thành các thanh tối có icon ở hai góc trên, đặt Cửa hàng ở góc dưới trái, thức ăn ở góc dưới phải và gom điều khiển góc nhìn vào nút bánh răng. Sổ tay ở mép phải. Icon đều tự vẽ, không dùng ảnh của game gốc.

Bản 0.5 thêm ánh sáng sáng/tối, mưa, cư dân đi khám phá tránh công trình, đường về tổ khi trú mưa/tối và chân kiến có khớp chuyển động. Bản lưu v1/v2 được chuyển sang v3 để lưu đồng hồ thế giới và xác suất thời tiết; tiến độ làng và công việc cũ được giữ.

Bản 0.6 thêm hai kiểu nhà ở, bốn kiểu điểm làm việc mới, các mô hình riêng cho cấp của điểm làm việc, và cây nấm trên đá giữa suối. Nhà chứa 2/3/4 cư dân; điểm làm việc nhận 1/2/3 cư dân và thưởng theo số người tham gia. Ngày kéo dài 240 giây, xác suất mưa mỗi đoạn 30 giây giảm còn 1/20. Bản lưu v3 tự nâng lên v4, giữ công trình, tiền và việc đang chạy.

Bản 0.7 thêm lều lá kiến, tổ ong bông hoa, ánh sáng đèn vườn ban đêm và thời gian nâng cấp có bọ cánh cứng làm việc. Tổ ong nhỏ mới chứa 1/2/3 ong, loại đèn lồng và bông hoa chứa 2/3/4. Bản lưu v4 tự nâng lên v5, giữ số ong ở tổ nhỏ cũ và giữ nguyên công trình, tiền, việc đang chạy.

Bản 0.8 chuyển đường và bánh từ hai nút ngoài màn hình vào tab Thức ăn của Cửa hàng. Thức ăn là vật thể đặt trên đất, có thời gian dùng bữa, làm kiến/ong tạm ngừng đường đi và việc đang chạy, rồi tiếp tục sau khi ăn. Thức ăn tự biến mất và giải phóng ô đất. Bản lưu v5 tự nâng lên v6; bữa đang ăn được lưu cùng làng.

Bản 0.3 mở rộng vùng xây và trang trí vườn, thêm năm FBX màu tự tạo, thao tác nhấn giữ/kéo với nút nổi, và đường đi cư dân có nhặt vật, về đúng cửa, vào tổ rồi trở ra. Phiên bản save vẫn là v2 vì chỉ bổ sung loại công trình ở cuối enum và nới giới hạn vị trí; không làm mất bố trí cũ.

Đây là nền tảng chơi thử, chưa phải bản clone đầy đủ. Chưa có xóa công trình, thời gian xây, âm thanh, sự kiện mùa, bọ hôi/ant lion, cửa hàng lớn hoặc chế độ bạn bè. Hiện ưu tiên trình duyệt desktop ngang; bản cảm ứng cần một vòng thiết kế riêng.

Xem [nghiên cứu Bug Village](Docs/NGHIEN_CUU_BUG_VILLAGE.md), [nguồn asset](Docs/ASSETS.md) và [kiểm thử](Docs/KIEM_THU.md).

## Nhịp chơi và kinh tế 0.10.0

Làng mới bắt đầu với 1.000 hạt và 2 giờ năng lượng. Công việc kéo dài 15 phút / 2 giờ / 6 giờ, đống hạt cấp 1 trả 46 / 300 / 780 hạt và 18 / 100 / 240 XP. Trừ phí bổ sung 7 hạt, chuyến ngắn vẫn có thu nhập theo giờ cao hơn. Mưa và ăn tiếp tục tạm dừng việc, nên thời gian thực tế có thể dài hơn đồng hồ công việc. Đói giữ nguyên tiến độ.

Đường giá 18 hạt thêm 2 giờ; bánh giá 90 hạt thêm 8 giờ, cả hai mở ngay cấp 1 để chuẩn bị chuyến dài. Năng lượng tối đa 12 giờ, vẫn giảm theo thời gian kể cả khi rảnh hoặc mưa. Chuẩn bị đủ thức ăn trước khi rời làng; một bữa bánh đủ cho chuyến 6 giờ với khoảng dự phòng mưa thông thường.

Ong/hoa cúc và bãi cành mở cấp 5, oải hương cấp 9, máy gom hạt cấp 10; nội dung còn lại trải đến cấp 12. Nhà kiến giá 300/500/850; nhà ong 600/1.000/1.500; điểm kiến 100/350/900; hoa 200/650/1.200. Giá nâng cấp bằng ba lần bảng 0.9; đường cong XP và thưởng nhiệm vụ giữ nguyên.

Save v8 chuyển tự động từ v7, giữ nguyên tiền, XP, năng lượng, công trình, thời gian và thưởng của việc/nâng cấp/bữa ăn đang chạy. Những món đã mở theo cấp cũ vẫn mua được. Không cấp thêm tiền khởi đầu cho bản lưu cũ. Giới hạn tính thời gian offline vẫn là 24 giờ.

Thời gian đống nhỏ 15 phút/2 giờ/6 giờ, ong cấp 5, điểm kiến trung cấp 5/cao cấp 10 và hoa tiếp theo cấp 9 dựa trên hướng dẫn Gamezebo. Các mức giá, thưởng, thức ăn và nâng cấp là cân bằng riêng; không khẳng định trùng toàn bộ bảng kinh tế gốc. Chưa thêm tiền cao cấp.

## Nhà quả sồi và góc vườn mới

- Nhà quả sồi: 1.200 hạt, mở cấp 9, chứa 2/3/4 kiến. Hai lần nâng cấp giá 480/1.020 hạt, mất 15 phút/1 giờ; mô hình có phòng phụ theo cấp.
- Cây nhỏ: 85 hạt, cấp 3. Cỏ cao: 24 hạt, cấp 1. Ô nhỏ: 65 hạt, cấp 2. Đặt được ở hai phía suối, hỗ trợ di chuyển/xoay và chặn chồng lấn như đồ trang trí khác.
- Cửa hàng có thanh cuộn, giữ vị trí cuộn riêng cho từng tab. Cuộn xuống để thấy món cuối danh sách.
- Thêm enum ở cuối, không đổi ID cũ hoặc định dạng save v8. Quyền mở khóa cũ không tự mở các món mới.

## Bọ rùa cần giúp

Bọ rùa xuất hiện nằm ngửa, lắc thân và quẫy sáu chân. Chạm bọ hoặc nút Giúp để nhận thưởng một lần; bọ lật lại trong 0,85 giây, nhún chào rồi biến mất sau 2,4 giây.

Khi chưa có khách, mỗi 30 giây có 20% cơ hội xuất hiện; tối đa 5 phút sẽ thử xuất hiện để giữ đường cứu làng hết tiền. Nếu hết đất trống, thử lại ở lượt kế tiếp. Chọn ngẫu nhiên đất trống hai bờ, ưu tiên vùng đang nhìn thấy, tránh công trình/đạo cụ/suối và cách vị trí trước ít nhất 2,5 đơn vị. Chỉ có một bọ mỗi lần. Đồng hồ khách tính khi game chạy, không sinh thưởng offline; lịch và khách vẫn không được lưu, như cơ chế khách trước đây.

## Sự kiện kiến sư tử

Kiến sư tử trồi lên từ hố đất, nhấp nhô và đóng/mở hai hàm. Chạm nó hoặc nhãn “Đuổi kiến sư tử” để nhận 25 hạt và 4 XP một lần. Nó rụt xuống với bụi đất, miệng hố thu nhỏ rồi biến mất sau 1,6 giây. Không gây mất cư dân hoặc tài nguyên; không tính vào nhiệm vụ giúp bọ rùa.

Lịch riêng: xét 20% mỗi 30 giây, thử bắt buộc sau 5 phút nếu có đất trống. Tránh suối, công trình, đạo cụ cố định, vị trí lần trước và bọ rùa trong bán kính 3 đơn vị; ưu tiên vùng nhìn thấy. Tối đa một kiến sư tử, không sinh thêm trong lúc rụt xuống. Khách chỉ tồn tại trong phiên chơi, phần thưởng được lưu theo cơ chế hiện có. Đây là biến thể tự thiết kế lấy cảm hứng từ game gốc.

## Cổng nối hàng rào và lối đi

Hàng rào đặt sát hai trụ Cổng hoa sẽ nối vào cổng. Cổng xoay 0°/180° nối rào bên trái/phải; 90°/270° nối theo trục còn lại. Không đặt rào ngay giữa hoặc trước/sau cửa. Có thể đặt cổng cạnh rào có sẵn. Di chuyển cổng hoặc rào cập nhật lại các thanh nối; xoay cổng bị từ chối nếu hướng mới chồng lên rào đang có.

Phần tìm đường khi cư dân đi khám phá nhận diện hai trụ riêng, để trống lối giữa cổng cho cả kiến và ong. Rào sát cổng có vùng cản nhỏ hơn khoảng cách đặt công trình nên không bịt lối đi. Các đường bay làm việc của ong và lộ trình công việc hiện có giữ nguyên.

## Cài đặt âm thanh và zoom

Nút bánh răng là **CÀI ĐẶT**, không còn các nút +/− zoom trên màn hình. Mở Cài đặt → **Âm thanh** để hiện thanh trượt âm lượng 0–100%. Áp dụng ngay cho toàn bộ nhạc nền và âm môi trường; 0% tắt tiếng. Tùy chọn được lưu riêng bằng PlayerPrefs, cùng chu kỳ tự lưu hiện có, giữ lại khi quay lại game.

Zoom bằng lăn chuột trên PC và thao tác hai ngón trên mobile, trong giới hạn camera hiện có. Hai ngón tách ra phóng to, chụm lại thu nhỏ; thao tác này không kéo hoặc chọn công trình. Khi Cài đặt mở, thao tác camera và công trình phía sau bị chặn. Canvas WebGL chặn thao tác zoom trang của trình duyệt để dành thao tác hai ngón cho game.

## Ngôn ngữ Việt / Anh

Tiếng Anh là mặc định khi chưa lưu lựa chọn ngôn ngữ. Trong **Cài đặt**, bấm **Language: English** để chuyển sang tiếng Việt; bấm **Ngôn ngữ: Tiếng Việt** để quay lại tiếng Anh. Thay đổi có hiệu lực ngay trên HUD, cửa hàng, sổ tay, thông tin công trình, thông báo và lỗi đặt công trình. Lựa chọn được tự lưu riêng, không thay đổi bản lưu làng hoặc âm lượng. Lựa chọn ngôn ngữ đã lưu vẫn được giữ.

Bảng mapping duy nhất để chỉnh dịch là `Assets/Scripts/Localization/I18n.cs`, gồm key, tiếng Việt và tiếng Anh. `I18n.Source` giữ chuỗi Việt chuẩn cho domain và tên đối tượng; `I18n.Text` / `I18n.Format` lấy bản dịch, còn các helper vẽ giao diện dịch chuỗi chuẩn ngay khi hiển thị. Các câu có số liệu dùng template `{0}`, `{1}`,… để giữ đúng thời gian, giá và phần thưởng khi đổi ngôn ngữ. Không dịch bằng thay thế từng từ.

Màn hình tải WebGL và nhãn truy cập của canvas cũng dùng bảng này. Build callback xuất các key `web.*` thành `i18n-web.json` cạnh `index.html`; đây là file sinh tự động, không chỉnh riêng. Chọn ngôn ngữ trong game cũng cập nhật tiêu đề trang và ngôn ngữ của lần tải sau. Khi triển khai WebGL, giữ file JSON này cùng bản build.

## Nhà ong và điểm làm việc mới

| Công trình | Tên tiếng Anh | Cấp mở | Giá hạt | Công dụng |
|---|---|---:|---:|---|
| Nhà ong lục giác | Honeycomb hive | 10 | 1.300 | Chứa 2 / 3 / 4 ong |
| Hoa tulip | Tulips | 6 | 350 | 1 / 2 / 3 ong làm việc |
| Hoa chuông xanh | Bluebells | 10 | 850 | 1 / 2 / 3 ong làm việc |
| Kho lá | Leaf depot | 3 | 220 | 1 / 2 / 3 kiến làm việc |
| Bãi sỏi | Pebble yard | 8 | 650 | 1 / 2 / 3 kiến làm việc |

Mỗi loại có ba mô hình riêng cho ba cấp. Nhà ong thêm buồng sáp lục giác dưới mái lá; hoa tăng số bông; kho lá thêm tầng chứa; bãi sỏi thêm khay phân loại. Nhà ở bờ làng bên trái, cả bốn điểm làm việc ở vườn bên phải, dùng các chuyến 15 phút / 2 giờ / 6 giờ và cơ chế mưa, thức ăn, thu hoạch, bổ sung hiện có.

Hiệu suất cơ bản của Kho lá/Tulip là 105%, Bãi sỏi/Hoa chuông là 118%; hoa cộng 10 điểm phần trăm theo quy tắc ong hiện có. Các mức này nằm giữa những công trình cũ theo thứ tự mở khóa. Thời gian nâng cấp nhà là 15 phút / 1 giờ, điểm làm việc là 30 phút / 2 giờ. Tab Làm việc dùng lưới ba cột có cuộn; cuộn xuống để thấy đủ 10 điểm làm việc. Enum mới được thêm sau ID 24, giữ bản lưu v8 và ID cũ.

## Mua đất mở rộng

Tab Mở rộng trong Cửa hàng bán hai vùng ngoài hàng ngăn: đất khu nhà phía tây giá 10.000 hạt sồi, đất khu vườn phía đông giá 15.000 hạt sồi. Mỗi vùng mua một lần, mở độc lập; đất chưa mua có nền/cỏ tối và biểu tượng khóa, không cho đặt hoặc chuyển công trình vào. Bấm biểu tượng để mở tab Mở rộng.

Bản lưu v9 bổ sung hai cờ sở hữu và giữ nguyên key lưu, tiền, công trình, XP và hợp đồng đang chạy. Khi chuyển từ v8 hoặc cũ hơn, vùng nào đã có công trình được giữ mở miễn phí; vùng ngoài còn trống bắt đầu khóa. Mua đất lưu ngay và trả màu nền/cỏ bình thường.
