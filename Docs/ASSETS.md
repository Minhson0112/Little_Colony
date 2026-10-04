# Nguồn asset

Nhạc nền `Assets/Resources/Audio/BackyardMusic.mp3` được người dùng cung cấp từ `D:/Downloads/music.mp3` để phát lặp trong game. Chưa có thông tin tác giả hoặc giấy phép của bản nhạc; cần xác minh quyền sử dụng trước khi phát hành công khai.
Âm đêm `Assets/Resources/Audio/NightAmbience.mp3` và âm mưa `Assets/Resources/Audio/RainAmbience.mp3` cũng do người dùng cung cấp từ `D:/Downloads/night.mp3` và `D:/Downloads/rain.mp3`. Chúng phát theo thời gian và thời tiết, độc lập với nhạc nền và với nhau. Chưa có thông tin tác giả hoặc giấy phép của hai file này.

Toàn bộ 9 mô hình trong `Assets/Resources/Models/` được tạo mới bằng Blender từ primitive qua `Tools/create_assets.py`. Không tải hoặc trích xuất asset, âm thanh, logo, texture hay mã nguồn của Glu.

| File | Nội dung |
|---|---|
| AntHome.fbx | Nhà nấm đỏ, cửa, cửa sổ, ống khói |
| BeeHome.fbx | Tổ ong vàng có mái lá |
| Ant.fbx | Kiến sáu chân, mắt và râu |
| Bee.fbx | Ong có sọc, cánh và râu |
| Flower.fbx | Hoa cúc có luống, thân và lá |
| Pile.fbx | Đống hạt trên khay cành cây |
| Clover.fbx | Cỏ ba lá |
| Bridge.fbx | Cầu ván có tay vịn |
| Ladybug.fbx | Bọ rùa |

File nguồn: `ArtSource/LittleColony.blend`. Bản render minh họa: `ArtSource/art-preview.png` — **đây là cảnh Blender, không phải ảnh chụp gameplay Unity**. Script tái tạo cả FBX, file nguồn và ảnh. Mô hình dùng vật liệu màu phẳng, không có texture bên ngoài. Unity tạo địa hình, nước và đá đơn giản bằng primitive lúc chạy.

Font giao diện: **Be Vietnam Pro Regular**, từ [Google Fonts](https://github.com/google/fonts/tree/main/ofl/bevietnampro), tác giả Be Vietnam Pro Project Authors. Giấy phép SIL Open Font License 1.1 lưu nguyên văn tại `Assets/Resources/Fonts/OFL.txt`. Font được nhúng để hiển thị đầy đủ dấu tiếng Việt trên WebGL.

Tạo lại bằng PowerShell từ thư mục dự án:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python Tools/create_assets.py
```

Chú ý: chạy lại sẽ ghi đè các asset đã sinh. Nếu đã sửa thủ công file Blender, hãy lưu bản riêng trước.

## Bộ mở rộng khu vườn 0.2

`Tools/create_expansion_assets.py` tạo 6 mô hình bổ sung, lưu nguồn riêng ở `ArtSource/BackyardAssets.blend`, không ghi đè file Blender gốc:

- `AntHome2`, `AntHome3`: nhà nấm có ban công, thêm cửa sổ, phòng bên và cờ.
- `BeeHome2`, `BeeHome3`: tổ cao hơn, thêm cửa và hiên đậu, hoa trên mái.
- `GardenPot`: chậu đất nung có lá xanh.
- `WateringCan`: bình tưới men xanh ngọc.

Màu được gán cả ở `diffuse_color` và `Principled BSDF / Base Color` để FBX sang Unity có vật liệu đúng. Đã kiểm tra vật liệu `Standard` của cả 6 mô hình trong Editor, gồm màu đỏ đất, kem, vàng mật, xanh lá và xanh ngọc.

Địa hình vườn liên tục, nhà phía sau, hàng rào, luống rau, đá và cỏ được dựng bằng mesh/primitive trong `BackyardEnvironment`. Suối có shader gợn nước chạy theo thời gian và lá trôi theo dòng. Cỏ dùng shader gió; bướm và cây có chuyển động riêng. Không dùng ảnh hoặc mô hình bên thứ ba cho phần mở rộng này.

## Đồ trang trí khu vườn 0.3

`Tools/create_decor_assets.py` tạo 5 mô hình gốc có vật liệu màu phẳng: `Lantern` (đèn), `Bench` (ghế), `Birdbath` (bồn nước), `FlowerArch` (cổng hoa), `MushroomPatch` (nấm). Nguồn Blender ở `ArtSource/GardenDecor.blend`; FBX ở `Assets/Resources/Models/`. Màu được ghi vào cả `diffuse_color` và Principled BSDF Base Color để Unity nhập đúng. Không dùng asset bên thứ ba cho nhóm này.

## Đạo cụ sân vườn 0.4

Bộ bàn ghế, dù che, máy cắt cỏ, thân cây đổ khổng lồ, bụi cỏ cao, đá lớn và đá nhỏ được dựng trực tiếp từ Unity primitive trong `Assets/Scripts/BackyardEnvironment.cs`; thanh gỗ kiến mang và túi mật ong trong `Assets/Scripts/VillageWorld.cs`. Tất cả dùng màu vật liệu tự thiết kế, không tải asset bên ngoài. Ảnh Bug Village gốc chỉ được xem để tham khảo cách sắp xếp giao diện, không được đưa vào build.

Các icon giao diện 0.4.1 là texture 64×64 được vẽ bằng mã trong `Assets/Scripts/VillageUI.cs` khi chạy. Không sử dụng ảnh chụp, sprite hoặc biểu tượng trích từ Bug Village gốc.

## Bộ nhà và điểm làm việc 0.6

`Tools/create_colony_assets.py` tạo mới 22 FBX màu: ba cấp cho `AntBurrow` và `BeeLantern`; cấp 2/3 cho `Pile` và `Flower`; ba cấp cho `TwigYard`, `SeedMill`, `Lavender` và `Sunflower`. Nguồn Blender là `ArtSource/ColonyExpansion.blend`. Tất cả hình khối, màu và vật liệu được tự thiết kế; không có asset bên thứ ba. Nấm trên đá giữa suối được dựng bằng primitive trong `BackyardEnvironment.cs`.

## Lều lá và tổ hoa 0.7

`Tools/create_flower_leaf_homes.py` tạo sáu FBX màu cho ba cấp của `AntLeafTent` và `BeeFlowerHome`. Nguồn Blender ở `ArtSource/LeafAndFlowerHomes.blend`. Mái lều xếp lá thành tam giác, tổ ong là bầu mật dưới cánh hoa. Bọ cánh cứng, giàn giáo và tim đèn phát sáng được dựng bằng Unity primitive trong `VillageWorld.cs`. Tất cả đều tự thiết kế, không dùng asset bên thứ ba.

## Thức ăn 0.8

`Tools/create_food_assets.py` tạo hai FBX màu `SugarCube` và `Cookie`: cục đường vát góc có tinh thể sáng; bánh quy có lớp vỏ, vụn bánh và hạt sô-cô-la trên đĩa lá. Nguồn Blender ở `ArtSource/VillageFood.blend`. Hai mô hình và vật liệu đều tự tạo, không dùng asset bên thứ ba.

## Nhà quả sồi và trang trí mới — 03/10/2026

`Tools/create_acorn_garden.py` tạo sáu FBX: `AntAcornHome`, `AntAcornHome2`, `AntAcornHome3`, `SmallTree`, `TallGrass`, `SmallParasol`. Nguồn Blender: `ArtSource/AcornGarden.blend`; xuất tại `Assets/Resources/Models`. Nhà quả sồi có cửa tròn, mái vảy, lá trên cuống; cấp 2/3 thêm phòng phụ và cờ lá. Cây tán tròn, cỏ lá cong có bông hạt và ô sọc kem–xanh đều tự thiết kế, không dùng tài sản bên thứ ba.

`Tools/render_acorn_preview.py` tạo `Docs/acorn-garden-art-preview.png`: đây là ảnh dựng Blender, không phải screenshot gameplay. `Docs/shop-acorn-garden-webgl.png` là ảnh cửa hàng thực tế trong Unity WebGL.

## Hoạt ảnh cứu bọ rùa

Giữ mô hình Ladybug tự tạo hiện có; `VillageWorld` bổ sung sáu chân bằng primitive, trục xoay thân và hoạt ảnh quẫy/lật/chào. Không thêm asset bên thứ ba.

## Kiến sư tử

Mô hình và hoạt ảnh tự dựng bằng Unity primitive trong `VillageWorld.cs`: hố đất, vành cát, bụng có đốt, đầu, mắt, sáu chân, hai hàm cong và hạt bụi. Không có asset bên thứ ba; không dùng mô hình trích xuất từ Bug Village gốc.

## Nhà ong lục giác và bốn điểm làm việc — 03/10/2026

`Tools/create_honeycomb_worksites.py` tự dựng và xuất 15 FBX: ba cấp của `BeeHoneycombHome`, `Tulip`, `Bluebell`, `LeafDepot`, `PebbleYard`. Nguồn tại `ArtSource/HoneycombWorksites.blend`, FBX tại `Assets/Resources/Models`. Vật liệu màu phẳng, hình học, ô sáp lục giác, cánh tulip, chuông hoa, giá lá và xe sỏi đều tự thiết kế; không dùng asset bên thứ ba.

Thêm `-- --render-preview` khi chạy script bằng Blender để dựng `Docs/honeycomb-worksites-art-preview.png`. Ảnh này là **preview Blender**, không phải gameplay; năm cột theo thứ tự nhà ong, tulip, hoa chuông, kho lá, bãi sỏi và ba hàng theo cấp 1–3.

## Hàng chậu hoa và cổng chia vườn — 03/10/2026

Tái sử dụng `GardenPot.fbx` từ `ArtSource/BackyardAssets.blend` và `FlowerArch.fbx` từ bộ trang trí tự dựng. Các cuống, cánh và nhụy hoa màu hồng/tím/vàng, cùng đá lát qua cổng, dựng bằng Unity primitive trong `BackyardEnvironment.cs`. Không thêm tài sản bên thứ ba.

## Khúc cây mục chia khu nhà — 03/10/2026

Sáu khúc gỗ có đầu rỗng tối, vành gỗ sáng, vỏ nứt, rêu và nấm gỗ tự dựng bằng Unity primitive trong `BackyardEnvironment.cs`. Cả hai hàng ngăn dùng năm phiến đá thấp ở khoảng mở giữa, không còn cổng vòm tại hai lối này. Không thêm asset bên thứ ba.

## Biểu tượng đất khóa — 03/10/2026

Biểu tượng ổ khóa màu vàng được tự vẽ bằng mã trong `VillageUI.cs`. Nền và cỏ của đất chưa mua được làm tối bằng shader hiện có. Không thêm asset bên thứ ba.

## Minh họa màn hình chào — 04/10/2026

`Assets/Resources/UI/LoginGarden.png` là tranh làng nấm/quả sồi được tạo mới bằng công cụ ImageGen tích hợp trong Codex. Không dùng hình trích từ Bug Village hoặc tham chiếu ảnh bên thứ ba. Đây là **minh họa màn hình chào**, không phải screenshot gameplay. Prompt đầy đủ và cách tích hợp ở `Docs/LOGIN_SCREEN.md`. Font Be Vietnam Pro và giấy phép OFL hiện có được tái sử dụng. Các mask panel, lá, đom đóm và dấu Discord được vẽ bằng mã trong `LoginScreenStyles.cs`; chữ Facebook được dựng bằng font hiện có. Tên và dấu nhận diện Discord/Facebook thuộc các thương hiệu tương ứng; không ngụ ý sự tài trợ.

## Bộ trang trí vườn — 04/10/2026

Thêm năm mô hình tự thiết kế bằng `Tools/create_garden_collection.py`: `WoodenPlanter`, `Birdhouse`, `WindChime`, `LeafFountain`, `FlowerCart`. Nguồn Blender tại `ArtSource/GardenCollection.blend`; FBX tại `Assets/Resources/Models`. Chậu gỗ có hoa hồng/trắng/tím, nhà chim mái lá, chuông đồng treo cành lá, đài phun hai tầng lá và xe hoa mái sọc kem–xanh. Không dùng tài sản bên thứ ba. Các phần được gộp theo vật liệu để giới hạn số mesh của mỗi mô hình.

`Docs/garden-collection-art-preview.png` là ảnh dựng Blender, không phải screenshot gameplay. Có thể tái tạo bằng Blender với `--background --python Tools/create_garden_collection.py -- --render-preview`.

`Docs/garden-collection-shop-webgl.jpg` và `Docs/garden-collection-locked-webgl.jpg` là ảnh cửa hàng Unity WebGL thực tế, kiểm tra lần lượt tại cấp 9 và cấp 1 trên tài khoản giả lập cục bộ. `Docs/garden-collection-placed-webgl.jpg` ghi lại xe hoa sau khi mua, tự động lưu và tải lại. Bản kiểm thử dùng snapshot trong bộ nhớ, không sửa dữ liệu tài khoản thật.
