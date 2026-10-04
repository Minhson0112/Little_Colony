using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LittleColony
{
    /// <summary>
    /// Identifies the supported interface languages without changing village save data.
    /// </summary>
    public enum GameLanguage
    {
        Vietnamese = 0,
        English = 1
    }

    /// <summary>
    /// Stores the Vietnamese-to-English text catalog and resolves presentation text.
    /// Domain errors and object names retain their canonical Vietnamese values.
    /// </summary>
    public static class I18n
    {
        private static readonly Dictionary<string, Translation> Translations = new Dictionary<string, Translation>
        {
            { "login.brand", new Translation("LÁ NHỎ", "LITTLE COLONY") },
            { "login.hero_title", new Translation("Một thế giới nhỏ.\nMột chốn bình yên.", "A little world.\nA place to grow.") },
            { "login.hero_description", new Translation("Dựng một ngôi làng. Kết bạn với đàn côn trùng.\nGieo những điều tốt lành.", "Build a village. Make tiny friends.\nLet good things grow.") },
            { "login.eyebrow", new Translation("KHU VƯỜN CỦA BẠN ĐANG ĐỢI", "YOUR LITTLE GARDEN AWAITS") },
            { "login.welcome", new Translation("Chào mừng về nhà.", "Welcome home.") },
            { "login.subtitle", new Translation("Chuyến phiêu lưu tí hon tiếp theo bắt đầu ở đây.\nBạn muốn bắt đầu thế nào?", "Your next little adventure starts here.\nHow would you like to begin?") },
            { "login.discord", new Translation("Tiếp tục với Discord", "Continue with Discord") },
            { "login.facebook", new Translation("Tiếp tục với Facebook", "Continue with Facebook") },
            { "login.or", new Translation("HOẶC", "OR") },
            { "login.guest", new Translation("Chơi khách", "Play as a guest") },
            { "login.guest_note", new Translation("Tiến độ chơi khách được lưu trên thiết bị này.", "Guest progress is saved on this device.") },
            { "login.accounts_soon", new Translation("Đăng nhập tài khoản sắp mở.", "Account sign-in is coming soon.") },
            { "login.soon", new Translation("SẮP MỞ", "SOON") },
            { "login.unavailable", new Translation("Đăng nhập {0} chưa sẵn sàng.\nBạn có thể chơi khách ngay.", "{0} sign-in is not available yet.\nYou can play as a guest for now.") },
            { "login.footer", new Translation("Niềm vui từ những điều nhỏ bé.", "A little joy in the everyday.") },
            { "login.tagline", new Translation("Một chuyến phiêu lưu thật thong thả", "A slower kind of adventure") },
            { "login.english_short", new Translation("EN", "EN") },
            { "login.vietnamese_short", new Translation("VI", "VI") },
            { "login.sound_on", new Translation("Âm bật", "Sound on") },
            { "login.sound_off", new Translation("Âm tắt", "Sound off") },
            { "login.cloud_hint", new Translation("Dùng Discord hoặc Facebook để lưu làng cùng tài khoản.", "Use Discord or Facebook to save your garden to your account.") },
            { "cloud.welcome", new Translation("Xin chào {0}!\nTiếp tục với làng đã lưu cùng tài khoản.", "Hello, {0}!\nContinue with your account's saved garden.") },
            { "cloud.enter", new Translation("Mở làng của tôi", "Open my cloud garden") },
            { "cloud.login_failed", new Translation("Đăng nhập chưa hoàn tất. Hãy thử lại bằng nút đăng nhập.", "Sign-in did not finish. Please try the sign-in button again.") },
            { "cloud.loading", new Translation("Đang tải làng của bạn…", "Loading your garden…") },
            { "cloud.saving", new Translation("Đang lưu lên cloud…", "Saving to cloud…") },
            { "cloud.saved", new Translation("Tiến độ đã được lưu trên cloud.", "Progress saved to cloud.") },
            { "cloud.conflict", new Translation("Phiên khác đã lưu. Tiến độ máy này vẫn được giữ.", "Another session saved. Your local progress is kept.") },
            { "cloud.use_cloud", new Translation("Giữ bản dự phòng và tải làng trên cloud", "Keep a backup and load the cloud garden") },
            { "cloud.connection_error", new Translation("Chưa đồng bộ. Game sẽ tự động thử lại.", "Not synced. The game will retry automatically.") },
            { "cloud.invalid", new Translation("Chưa đọc được bản lưu. Làng hiện tại vẫn an toàn.", "This save could not be loaded. Your current garden is safe.") },
            { "cloud.session_expired", new Translation("Phiên đã hết hạn. Hãy đăng nhập lại để tiếp tục đồng bộ.", "Session expired. Sign in again to resume syncing.") },
            { "cloud.sign_in_again", new Translation("Đăng nhập lại", "Sign in again") },
            { "cloud.logout", new Translation("Đăng xuất", "Sign out") },
            { "cloud.logout_pending", new Translation("Hãy đợi tiến độ đồng bộ xong trước khi đăng xuất.", "Wait for progress to sync before signing out.") },
            { "building.bee_honeycomb_home", new Translation("Nhà ong lục giác", "Honeycomb hive") },
            { "building.tulip", new Translation("Hoa tulip", "Tulips") },
            { "building.bluebell", new Translation("Hoa chuông xanh", "Bluebells") },
            { "building.leaf_depot", new Translation("Kho lá", "Leaf depot") },
            { "building.pebble_yard", new Translation("Bãi sỏi", "Pebble yard") },
            { "building.wooden_planter", new Translation("Chậu hoa gỗ", "Wooden planter") },
            { "building.birdhouse", new Translation("Nhà chim", "Birdhouse") },
            { "building.wind_chime", new Translation("Chuông gió", "Wind chime") },
            { "building.leaf_fountain", new Translation("Đài phun lá", "Leaf fountain") },
            { "building.flower_cart", new Translation("Xe hoa", "Flower cart") },
            { "description.wooden_planter", new Translation("Hoa hồng và cúc trong chậu gỗ", "Roses and daisies in a wooden planter") },
            { "description.birdhouse", new Translation("Mái lá xanh và chú chim nhỏ", "A leafy roof and a little bird") },
            { "description.wind_chime", new Translation("Chuông đồng dưới cành lá", "Brass chimes beneath a leafy branch") },
            { "description.leaf_fountain", new Translation("Những tầng lá ôm làn nước xanh", "Leaf bowls cradle cascading blue water") },
            { "description.flower_cart", new Translation("Xe gỗ đầy hoa và mái sọc kem", "A flower-filled cart with a striped canopy") },
            { "description.bee_honeycomb_home", new Translation("Những buồng mật dưới mái lá", "Honeycomb rooms beneath a leafy roof") },
            { "description.tulip", new Translation("Những chén hoa hồng đầy mật", "Pink cups filled with sweet nectar") },
            { "description.bluebell", new Translation("Chùm chuông xanh đón đàn ong", "Blue bells welcoming busy bees") },
            { "description.leaf_depot", new Translation("Nơi kiến gom và xếp lá", "Where ants gather and stack leaves") },
            { "description.pebble_yard", new Translation("Nơi kiến phân loại những viên sỏi", "Where ants sort garden pebbles") },
            { "web.title", new Translation("Lá Nhỏ · Little Colony", "Little Colony") },
            { "web.heading", new Translation("Lá Nhỏ", "Little Colony") },
            { "web.canvas_label", new Translation("Lá Nhỏ, game xây làng côn trùng 3D", "Little Colony, a 3D insect village game") },
            { "web.tagline", new Translation("Một ngôi làng, ngàn điều nhỏ.", "One village, a thousand little wonders.") },
            { "web.welcome", new Translation("Chào những cư dân tí hon của khu vườn.", "Meet the garden's tiny residents.") },
            { "web.loading", new Translation("Đang chuẩn bị khu vườn…", "Preparing the garden…") },
            { "web.failed", new Translation("Chưa mở được khu vườn", "Could not open the garden") },
            { "web.unity_load_failed", new Translation("Không tải được Unity. Hãy mở bản build qua HTTP, không mở trực tiếp file HTML.", "Could not load Unity. Open the build over HTTP rather than opening the HTML file directly.") },
            { "project.title", new Translation("Lá Nhỏ — Little Colony", "Little Colony") },
            { "notice.welcome", new Translation("Chào mừng! Chọn đống hạt ở vườn để giao việc cho kiến.", "Welcome! Select the seed pile in the garden to assign work to your ants.") },
            { "audio.music_load_failed", new Translation("Không tải được nhạc nền của khu vườn.", "Could not load the backyard music.") },
            { "audio.clip_load_failed", new Translation("Không tải được âm thanh: ", "Could not load audio: ") },
            { "save.invalid", new Translation("Bản lưu không hợp lệ. Đã giữ bản sao và tạo làng mới.", "The save is invalid. A backup was kept and a new village was created.") },
            { "save.unreadable", new Translation("Không đọc được bản lưu. Đã tạo làng mới.", "Could not read the save. A new village was created.") },
            { "notice.meal_finished", new Translation("Cả làng đã ăn xong! Thức ăn biến mất, ô đất lại trống.", "Everyone has finished eating! The food is gone and the ground is clear again.") },
            { "notice.upgrade_finished", new Translation("Bọ cánh cứng đã hoàn thành nâng cấp công trình!", "The beetle has finished upgrading the building!") },
            { "notice.drag_building", new Translation("Kéo công trình tới chỗ mới, rồi chọn Hủy, dấu tích xanh hoặc Xoay phía trên.", "Drag the building to its new spot, then choose Cancel, the green check, or Rotate above it.") },
            { "notice.position_selected", new Translation("Đã chọn vị trí. Dùng dấu tích phía trên để xây, hoặc chọn ô đất khác.", "Position selected. Use the check above to build, or choose another spot.") },
            { "notice.unlock_prefix", new Translation("Công trình này mở ở cấp ", "This building unlocks at level ") },
            { "notice.need_acorns", new Translation("Chưa đủ hạt sồi. Thu hoạch hoặc giúp bọ rùa để kiếm thêm.", "Not enough acorns. Harvest or help a ladybug to earn more.") },
            { "error.food_exists", new Translation("Đã có thức ăn trong vườn. Chờ cả làng ăn xong.", "There is already food in the garden. Wait for everyone to finish eating.") },
            { "error.full_energy", new Translation("Cả làng đang no rồi.", "Everyone is already full.") },
            { "notice.place_food", new Translation("Chạm đất bên phải suối, rồi dùng dấu tích phía trên để đặt thức ăn.", "Tap the ground to the right of the stream, then use the check above to place food.") },
            { "notice.place_building", new Translation("Chạm vị trí muốn xây; ba nút phía trên dùng để Hủy, Đặt hoặc Xoay.", "Tap where you want to build; use the three buttons above to Cancel, Place, or Rotate.") },
            { "notice.food_in_use", new Translation("Cư dân đang ăn. Thức ăn sẽ tự biến mất sau bữa.", "Residents are eating. The food will disappear after the meal.") },
            { "notice.wait_for_beetle", new Translation("Hãy chờ bọ cánh cứng hoàn thành nâng cấp trước khi di chuyển.", "Wait for the beetle to finish upgrading before moving this building.") },
            { "notice.preview_rotation", new Translation("Xem trước hướng mới. Chọn dấu tích xanh để lưu.", "Previewing the new orientation. Choose the green check to save.") },
            { "notice.move_building", new Translation("Giữ và kéo để di chuyển. Ba nút phía trên để Hủy, Đặt hoặc Xoay.", "Hold and drag to move. Use the three buttons above to Cancel, Place, or Rotate.") },
            { "notice.move_saved", new Translation("Đã lưu vị trí và hướng mới. Cư dân đã chuyển về nhà!", "The new position and orientation are saved. Residents have moved home!") },
            { "notice.food_placed", new Translation("Đã đặt thức ăn! Kiến và ong sẽ tới ăn rồi trở lại công việc.", "Food is ready! Ants and bees will eat, then return to work.") },
            { "notice.building_ready", new Translation("Công trình mới đã sẵn sàng!", "Your new building is ready!") },
            { "notice.upgrade_started", new Translation("Bọ cánh cứng bắt đầu nâng cấp. Chạm công trình để xem thời gian còn lại.", "The beetle has started upgrading. Tap the building to see the time remaining.") },
            { "notice.move_cancelled", new Translation("Đã hủy thay đổi. Công trình và hạt sồi được giữ nguyên.", "Changes cancelled. Your building and acorns are unchanged.") },
            { "notice.select_building", new Translation("Chọn công trình để chăm sóc ngôi làng.", "Select a building to care for your village.") },
            { "notice.job_started", new Translation("Cư dân đang thu thập. Bạn có thể tiếp tục xây làng.", "Residents are gathering. You can keep building your village.") },
            { "action.harvest_prefix", new Translation("Thu hoạch +", "Harvest +") },
            { "notice.harvest_suffix", new Translation(" hạt sồi! Bổ sung điểm thu thập để làm tiếp.", " acorns! Refill the worksite to keep working.") },
            { "notice.refilled", new Translation("Đã bổ sung. Chọn một công việc mới.", "Refilled. Choose a new job.") },
            { "notice.need_prefix", new Translation("Cần ", "You need ") },
            { "notice.refill_suffix", new Translation(" hạt sồi để bổ sung.", " acorns to refill.") },
            { "notice.quest_prefix", new Translation("Hoàn thành nhiệm vụ! +", "Quest complete! +") },
            { "notice.reward_separator", new Translation(" hạt sồi, +", " acorns, +") },
            { "notice.ant_lion_prefix", new Translation("Đã đuổi kiến sư tử! +", "Antlion chased away! +") },
            { "notice.ladybug_prefix", new Translation("Bọ rùa cảm ơn bạn! +", "The ladybug thanks you! +") },
            { "building.ant_home", new Translation("Nhà nấm", "Mushroom house") },
            { "building.bee_home", new Translation("Tổ ong nhỏ", "Small beehive") },
            { "building.pile", new Translation("Đống hạt", "Seed pile") },
            { "building.flower", new Translation("Hoa cúc", "Daisy") },
            { "building.clover", new Translation("Cỏ ba lá", "Clover") },
            { "building.lantern", new Translation("Đèn vườn", "Garden lantern") },
            { "building.bench", new Translation("Ghế gỗ", "Wooden bench") },
            { "building.birdbath", new Translation("Bồn tắm chim", "Birdbath") },
            { "building.flower_arch", new Translation("Cổng hoa", "Flower gate") },
            { "building.mushroom_patch", new Translation("Nấm nhỏ", "Mushrooms") },
            { "building.ant_burrow", new Translation("Hang kiến", "Ant burrow") },
            { "building.bee_lantern", new Translation("Tổ ong đèn lồng", "Lantern hive") },
            { "building.twig_yard", new Translation("Bãi cành cây", "Twig yard") },
            { "building.seed_mill", new Translation("Máy gom hạt", "Seed mill") },
            { "building.lavender", new Translation("Hoa oải hương", "Lavender") },
            { "building.sunflower", new Translation("Hoa hướng dương", "Sunflower") },
            { "building.ant_leaf_tent", new Translation("Lều lá kiến", "Leaf tent") },
            { "building.bee_flower_home", new Translation("Tổ ong bông hoa", "Flower hive") },
            { "building.sugar_cube", new Translation("Cục đường", "Sugar cube") },
            { "building.cookie", new Translation("Bánh quy", "Cookie") },
            { "building.fence", new Translation("Hàng rào gỗ", "Wooden fence") },
            { "building.ant_acorn_home", new Translation("Nhà quả sồi", "Acorn house") },
            { "building.small_tree", new Translation("Cây nhỏ", "Small tree") },
            { "building.tall_grass", new Translation("Cỏ cao", "Tall grass") },
            { "building.small_parasol", new Translation("Ô nhỏ", "Small parasol") },
            { "description.ant_home", new Translation("Một mái ấm cho kiến", "A cozy home for ants") },
            { "description.bee_home", new Translation("Tổ nhỏ cho một ong", "A little hive for one bee") },
            { "description.pile", new Translation("Kho hạt của đàn kiến", "The ants' seed store") },
            { "description.flower", new Translation("Một chút mật ngọt", "A little sweet nectar") },
            { "description.clover", new Translation("Thêm một góc xanh", "A touch of green") },
            { "description.lantern", new Translation("Ánh đèn ấm bên lối đi", "Warm light beside the path") },
            { "description.bench", new Translation("Chỗ nghỉ giữa khu vườn", "A place to rest in the garden") },
            { "description.birdbath", new Translation("Bồn nước bằng đá", "A stone birdbath") },
            { "description.flower_arch", new Translation("Vòm hoa qua lối nhỏ", "A flower arch over the path") },
            { "description.mushroom_patch", new Translation("Những cây nấm đáng yêu", "A cluster of cute mushrooms") },
            { "description.ant_burrow", new Translation("Hang đất nhiều ngăn", "An underground home with many rooms") },
            { "description.bee_lantern", new Translation("Tổ mật rộng trong đèn hoa", "A roomy hive inside a lantern") },
            { "description.twig_yard", new Translation("Nơi kiến gom cành nhỏ", "Where ants gather small twigs") },
            { "description.seed_mill", new Translation("Nơi kiến phân loại hạt", "Where ants sort seeds") },
            { "description.lavender", new Translation("Bụi hoa thơm cho ong", "Fragrant flowers for bees") },
            { "description.sunflower", new Translation("Đóa hoa lớn cho ong", "A big bloom for bees") },
            { "description.ant_leaf_tent", new Translation("Lều tam giác xếp bằng lá", "A triangular tent made of leaves") },
            { "description.bee_flower_home", new Translation("Tổ mật giữa những cánh hoa", "A hive nestled among petals") },
            { "description.sugar_cube", new Translation("Bữa ăn nhỏ cho cả làng", "A small meal for the village") },
            { "description.cookie", new Translation("Bữa ăn no nê cho cả làng", "A filling meal for the village") },
            { "description.fence", new Translation("Đặt sát để tự nối đoạn rào", "Place side by side to connect") },
            { "description.ant_acorn_home", new Translation("Mái quả sồi ấm áp cho kiến", "A warm acorn roof for ants") },
            { "description.small_tree", new Translation("Một bóng cây xanh nhỏ", "A little leafy shade") },
            { "description.tall_grass", new Translation("Bụi cỏ vươn cao", "A patch of tall grass") },
            { "description.small_parasol", new Translation("Bóng mát dưới ô sọc", "Shade beneath a striped parasol") },
            { "quest.first_harvest", new Translation("Vụ thu hoạch đầu tiên", "The first harvest") },
            { "quest.new_neighbors", new Translation("Đón thêm hàng xóm", "Welcome new neighbors") },
            { "quest.village_meal", new Translation("Bữa ăn của cả làng", "A meal for the village") },
            { "quest.first_bee", new Translation("Tiếng ong đầu mùa", "The first buzz") },
            { "quest.flower_garden", new Translation("Một khu vườn đầy hoa", "A garden full of flowers") },
            { "quest.visitor", new Translation("Người bạn ghé thăm", "A visiting friend") },
            { "quest_hint.first_harvest", new Translation("Mở cửa hàng, xây nhà kiến và đống hạt ở hai bờ suối, rồi giao việc và thu hoạch.", "Build an ant home and a seed pile on opposite banks, then assign work and harvest.") },
            { "quest_hint.new_neighbors", new Translation("Xây thêm một nhà nấm bên trái dòng suối.", "Build another mushroom house to the left of the stream.") },
            { "quest_hint.village_meal", new Translation("Thêm đường hoặc bánh quy vào bữa ăn.", "Add sugar or a cookie to the village meal.") },
            { "quest_hint.first_bee", new Translation("Đạt cấp 5, xây tổ ong để đón bạn mới.", "Reach level 5 and build a hive to welcome a new friend.") },
            { "quest_hint.flower_garden", new Translation("Trồng hoa cúc ở bên phải dòng suối.", "Plant a daisy to the right of the stream.") },
            { "quest_hint.visitor", new Translation("Giúp bọ rùa khi bạn ấy ghé qua.", "Help the ladybug when it visits.") },
            { "status.upgrade_prefix", new Translation("Nâng cấp • ", "Upgrade • ") },
            { "status.harvest_suffix", new Translation("  •  Thu hoạch", "  •  Harvest") },
            { "status.hungry", new Translation("Đang đói", "Hungry") },
            { "status.rain_shelter", new Translation("Trú mưa", "Sheltering") },
            { "status.working_suffix", new Translation("  •  Đang làm", "  •  Working") },
            { "action.refill_prefix", new Translation("Bổ sung • ", "Refill • ") },
            { "unit.acorns_suffix", new Translation(" hạt", " acorns") },
            { "action.assign_job", new Translation("Giao việc", "Assign work") },
            { "action.help_ladybug_prefix", new Translation("Giúp bọ rùa  +", "Help ladybug  +") },
            { "action.repel_ant_lion_prefix", new Translation("Đuổi kiến sư tử +", "Shoo antlion +") },
            { "hud.shop", new Translation("CỬA HÀNG", "SHOP") },
            { "hud.level_prefix", new Translation("CẤP ", "LEVEL ") },
            { "hud.energy", new Translation("NĂNG LƯỢNG", "ENERGY") },
            { "hud.ants", new Translation("KIẾN", "ANTS") },
            { "hud.acorns", new Translation("HẠT SỒI", "ACORNS") },
            { "hud.rain", new Translation("TRỜI MƯA", "RAIN") },
            { "hud.night", new Translation("BAN ĐÊM", "NIGHT") },
            { "hud.day", new Translation("BAN NGÀY", "DAY") },
            { "hud.journal", new Translation("SỔ TAY", "JOURNAL") },
            { "journal.title", new Translation("SỔ TAY KHU VƯỜN", "GARDEN JOURNAL") },
            { "journal.your_garden", new Translation("Góc vườn của bạn", "Your corner of the garden") },
            { "journal.completed_hint", new Translation("Nâng cấp những mái nhà và sắp xếp khu vườn theo ý bạn.", "Upgrade homes and arrange the garden your way.") },
            { "action.claim_prefix", new Translation("Nhận ", "Claim ") },
            { "unit.reward_separator", new Translation(" hạt + ", " acorns + ") },
            { "journal.progress_suffix", new Translation(" / 6 câu chuyện đã hoàn thành", " / 6 stories completed") },
            { "hud.settings", new Translation("CÀI ĐẶT", "SETTINGS") },
            { "settings.title", new Translation("Cài đặt", "Settings") },
            { "settings.audio", new Translation("Âm thanh", "Audio") },
            { "settings.master_volume", new Translation("Âm lượng toàn game", "Master volume") },
            { "shop.title", new Translation("Cửa hàng khu vườn", "Garden shop") },
            { "shop.hint", new Translation("Chọn công trình rồi đặt vào khu đất trống", "Choose a building and place it on clear ground") },
            { "shop.buildings", new Translation("Công trình", "Buildings") },
            { "shop.worksites", new Translation("Làm việc", "Worksites") },
            { "shop.decorations", new Translation("Trang trí", "Decor") },
            { "shop.expansions", new Translation("Mở rộng", "Expansion") },
            { "expansion.housing", new Translation("Đất khu nhà", "Housing plot") },
            { "expansion.garden", new Translation("Đất khu vườn", "Garden plot") },
            { "expansion.housing_hint", new Translation("Mở đất ngoài hàng cây mục để xây thêm nhà kiến và ong.", "Unlock land beyond the logs for more ant and bee homes.") },
            { "expansion.garden_hint", new Translation("Mở đất ngoài hàng chậu hoa để đặt thêm điểm làm việc và trang trí.", "Unlock land beyond the flower pots for more worksites and decor.") },
            { "expansion.shop_hint", new Translation("Mua từng vùng đất để mở rộng ngôi làng", "Buy each outer plot to grow your village") },
            { "expansion.owned", new Translation("Đã mở đất", "Land unlocked") },
            { "expansion.buy", new Translation("Mua đất", "Buy land") },
            { "expansion.locked_ground", new Translation("Đất chưa mở. Mua trong Cửa hàng → Mở rộng.", "Land is locked. Buy it in Shop → Expansion.") },
            { "expansion.invalid", new Translation("Vùng đất không hợp lệ.", "Invalid land plot.") },
            { "expansion.already_owned", new Translation("Vùng đất này đã được mở.", "This plot is already unlocked.") },
            { "expansion.purchase_success", new Translation("Đã mở đất! Bạn có thể xây dựng trên vùng đất mới.", "Land unlocked! You can build on your new plot.") },
            { "expansion.need_more", new Translation("Cần thêm {0} hạt sồi", "Need {0} more acorns") },
            { "shop.food", new Translation("Thức ăn", "Food") },
            { "label.level_prefix", new Translation("Cấp ", "Level ") },
            { "shop.unlock_prefix", new Translation("Mở cấp ", "Unlocks at ") },
            { "unit.ants", new Translation("kiến", "ants") },
            { "shop.workers", new Translation("1 / 2 / 3 lao động", "1 / 2 / 3 workers") },
            { "unit.energy_suffix", new Translation(" năng lượng", " energy") },
            { "shop.fence_hint", new Translation("Tự nối khi đặt sát", "Connects when placed nearby") },
            { "shop.decoration_hint", new Translation("Trang trí khu vườn", "Garden decoration") },
            { "action.locked", new Translation("Chưa mở", "Locked") },
            { "action.select", new Translation("Chọn", "Select") },
            { "action.cancel", new Translation("Hủy", "Cancel") },
            { "inspector.home_heading", new Translation("MÁI ẤM CỦA CƯ DÂN", "A HOME FOR RESIDENTS") },
            { "inspector.garden_heading", new Translation("MỘT GÓC KHU VƯỜN", "A CORNER OF THE GARDEN") },
            { "inspector.capacity_prefix", new Translation("Sức chứa: ", "Capacity: ") },
            { "inspector.home_upgrade_prefix", new Translation("Bọ cánh cứng đang sửa nhà. Còn ", "The beetle is repairing the house. Remaining: ") },
            { "inspector.next_tier_prefix", new Translation("Cấp sau thêm 1 cư dân. Hoàn thành trong ", "Next level adds 1 resident. Ready in ") },
            { "inspector.max_capacity", new Translation("Mái nhà đã đạt sức chứa tối đa.", "This home is at maximum capacity.") },
            { "status.upgrading", new Translation("Đang nâng cấp...", "Upgrading...") },
            { "status.max_level", new Translation("Đã đạt cấp tối đa", "Maximum level reached") },
            { "unit.ant_label", new Translation("Kiến", "Ants") },
            { "inspector.free_prefix", new Translation(" rảnh ", " free ") },
            { "inspector.slots_prefix", new Translation(" • chỗ ", " • slots ") },
            { "inspector.site_upgrade_prefix", new Translation("Bọ cánh cứng đang làm việc • còn ", "The beetle is working • remaining ") },
            { "job.short", new Translation("Chuyến ngắn", "Short trip") },
            { "job.medium", new Translation("Chăm chỉ", "Steady work") },
            { "job.long", new Translation("Chuyến dài", "Long trip") },
            { "inspector.hungry_hint", new Translation("Cư dân đang đói. Thêm thức ăn để tiếp tục.", "Residents are hungry. Add food to continue.") },
            { "inspector.rain_hint", new Translation("Trời mưa: cư dân trú trong tổ, việc sẽ tiếp tục khi tạnh.", "It is raining. Residents will resume work when it clears.") },
            { "inspector.collecting_prefix", new Translation("Đang thu thập • còn ", "Gathering • remaining ") },
            { "inspector.reward_prefix", new Translation("Thành quả: ", "Harvest: ") },
            { "inspector.harvest_ready", new Translation("Thành quả đã sẵn sàng!", "The harvest is ready!") },
            { "inspector.water_hint", new Translation("Một chút nước cho mùa mật tiếp theo.", "A little water for the next nectar season.") },
            { "inspector.refill_hint", new Translation("Bổ sung hạt cho chuyến thu thập mới.", "Refill the seeds for a new gathering trip.") },
            { "action.water", new Translation("Tưới hoa", "Water flowers") },
            { "action.refill", new Translation("Bổ sung", "Refill") },
            { "action.upgrade_site_prefix", new Translation("Nâng điểm • ", "Upgrade • ") },
            { "unit.cost_duration_separator", new Translation(" hạt / ", " acorns / ") },
            { "status.max_workers", new Translation("Đã đủ 3 lao động", "All 3 worker slots unlocked") },
            { "inspector.eating", new Translation("Cả làng đang ăn", "Everyone is eating") },
            { "inspector.remaining_prefix", new Translation("Còn ", "Remaining ") },
            { "inspector.meal_energy_suffix", new Translation(" năng lượng sau bữa ăn.", " energy after the meal.") },
            { "inspector.food_hint", new Translation("Thức ăn sẽ biến mất và giải phóng đất khi ăn xong.", "The food will disappear and clear the ground when the meal ends.") },
            { "inspector.fence_hint", new Translation("Đặt các đoạn sát nhau để tự nối thành hàng, góc hoặc ngã ba.", "Place sections side by side to form lines, corners, or junctions.") },
            { "inspector.decoration_hint", new Translation("Một góc xanh bé xíu làm khu vườn thêm sinh động.", "A tiny green corner brings the garden to life.") },
            { "object.upgrade_beetle", new Translation("Bọ cánh cứng đang nâng cấp", "Upgrading beetle") },
            { "object.worksite_base", new Translation("Nền công trường", "Worksite base") },
            { "object.scaffold_post", new Translation("Cột giàn giáo", "Scaffold post") },
            { "object.worksite_roof", new Translation("Mái lá công trường", "Leaf worksite roof") },
            { "object.working_beetle", new Translation("Bọ cánh cứng làm việc", "Working beetle") },
            { "object.body", new Translation("Thân", "Body") },
            { "object.left_wing", new Translation("Cánh trái", "Left wing") },
            { "object.right_wing", new Translation("Cánh phải", "Right wing") },
            { "object.head", new Translation("Đầu", "Head") },
            { "object.eye", new Translation("Mắt", "Eye") },
            { "object.leg", new Translation("Chân", "Leg") },
            { "object.hammer", new Translation("Búa đang gõ", "Hammer in motion") },
            { "object.hammer_handle", new Translation("Cán búa", "Hammer handle") },
            { "object.hammer_head", new Translation("Đầu búa", "Hammer head") },
            { "object.lantern_light", new Translation("Ánh sáng đèn vườn", "Garden lantern light") },
            { "object.lantern_core", new Translation("Tim đèn phát sáng", "Glowing lantern core") },
            { "object.ant_upper_leg", new Translation("Khớp chân kiến trên", "Ant upper leg joint") },
            { "object.ant_lower_leg", new Translation("Khớp chân kiến dưới", "Ant lower leg joint") },
            { "object.honey_bag", new Translation("Túi mật ong", "Honey bag") },
            { "object.carried_wood", new Translation("Thanh gỗ trên lưng", "Wood carried on back") },
            { "object.bag_strap", new Translation("Dây xách", "Bag strap") },
            { "object.golden_honey_bag", new Translation("Túi mật vàng", "Golden honey bag") },
            { "object.bag_neck", new Translation("Miệng túi buộc", "Tied bag neck") },
            { "object.long_log", new Translation("Khúc gỗ dài", "Long log") },
            { "object.log_cut", new Translation("Mặt cắt gỗ", "Log cut face") },
            { "object.tie", new Translation("Dây buộc", "Tie") },
            { "object.fence_post", new Translation("Cọc rào", "Fence post") },
            { "object.post_cap", new Translation("Đỉnh cọc", "Post cap") },
            { "object.fence_rail", new Translation("Thanh rào", "Fence rail") },
            { "object.ladybug", new Translation("Bọ rùa cần giúp", "Ladybug needing help") },
            { "object.ladybug_body", new Translation("Thân bọ rùa", "Ladybug body") },
            { "object.ladybug_leg", new Translation("Chân bọ rùa", "Ladybug leg") },
            { "object.thigh", new Translation("Đùi", "Thigh") },
            { "object.foot", new Translation("Bàn chân", "Foot") },
            { "object.ant_lion", new Translation("Kiến sư tử trong hố cát", "Antlion in a sand pit") },
            { "object.pit_mouth", new Translation("Miệng hố tối", "Dark pit mouth") },
            { "object.pit_rim", new Translation("Vành đất", "Soil rim") },
            { "object.ant_lion_body", new Translation("Thân kiến sư tử", "Antlion body") },
            { "object.segmented_abdomen", new Translation("Bụng có đốt", "Segmented abdomen") },
            { "object.back_segment", new Translation("Đốt lưng", "Back segment") },
            { "object.jaw", new Translation("Hàm kẹp", "Pincer jaw") },
            { "object.long_jaw", new Translation("Hàm dài", "Long jaw") },
            { "object.curved_jaw_tip", new Translation("Đầu hàm cong", "Curved jaw tip") },
            { "object.digging_leg", new Translation("Chân đào đất", "Digging leg") },
            { "object.dust", new Translation("Bụi đất", "Dust") },
            { "error.invalid_building", new Translation("Loại công trình không hợp lệ.", "Invalid building type.") },
            { "error.unlock_prefix", new Translation("Mở ở cấp ", "Unlocks at level ") },
            { "error.not_enough_acorns", new Translation("Chưa đủ hạt sồi.", "Not enough acorns.") },
            { "error.outside_ground", new Translation("Chọn đất trong làng hoặc vườn, cách dòng suối.", "Choose ground in the village or garden, away from the stream.") },
            { "error.worksite_bank", new Translation("Đặt điểm thu thập ở khu vườn bên phải.", "Place worksites in the garden on the right.") },
            { "error.food_bank", new Translation("Đặt thức ăn ở khu vườn bên phải suối.", "Place food to the right of the stream.") },
            { "error.home_bank", new Translation("Đặt nhà ở khu làng bên trái.", "Place homes in the village on the left.") },
            { "error.fixed_prop", new Translation("Vướng đồ vật cố định trong sân. Chọn vị trí khác.", "A fixed yard object is in the way. Choose another spot.") },
            { "error.fence_occupied", new Translation("Ô này đã có hàng rào.", "This spot already has a fence.") },
            { "error.too_close", new Translation("Vị trí này quá gần công trình khác.", "This spot is too close to another building.") },
            { "error.home_limit", new Translation("Lên cấp để xây thêm nhà loại này.", "Level up to build more homes of this type.") },
            { "error.invalid_rotation", new Translation("Góc xoay không hợp lệ.", "Invalid rotation.") },
            { "error.building_missing", new Translation("Không tìm thấy công trình.", "Building not found.") },
            { "error.food_in_use", new Translation("Thức ăn đang được cả làng dùng.", "Everyone is eating this food.") },
            { "error.upgrade_move", new Translation("Chờ bọ hoàn thành nâng cấp trước khi di chuyển.", "Wait for the beetle to finish upgrading before moving.") },
            { "error.not_upgradeable", new Translation("Công trình này không thể nâng cấp.", "This building cannot be upgraded.") },
            { "error.already_upgrading", new Translation("Công trình đang được nâng cấp.", "This building is already being upgraded.") },
            { "error.max_level", new Translation("Công trình đã đạt cấp tối đa.", "This building has reached its maximum level.") },
            { "error.harvest_before_upgrade", new Translation("Hãy thu hoạch trước khi nâng cấp điểm làm việc.", "Harvest before upgrading the worksite.") },
            { "error.upgrade_cost", new Translation("Chưa đủ hạt sồi để nâng cấp.", "Not enough acorns to upgrade.") },
            { "error.worksite_not_ready", new Translation("Điểm thu thập chưa sẵn sàng.", "The worksite is not ready.") },
            { "error.invalid_job", new Translation("Công việc không hợp lệ.", "Invalid job.") },
            { "error.feed_first", new Translation("Hãy cho đàn ăn trước khi làm việc.", "Feed the residents before assigning work.") },
            { "error.no_workers", new Translation("Cần thêm cư dân rảnh đúng loại. Xây hoặc nâng cấp nhà.", "More idle residents of the right type are needed. Build or upgrade homes.") },
            { "test.unlock_level_five", new Translation("cấp 5", "level 5") },
            { "hud.bees", new Translation("ONG", "BEES") },
            { "unit.bees", new Translation("ong", "bees") },
            { "unit.bee_label", new Translation("Ong", "Bees") },
            { "action.rotate", new Translation("Xoay", "Rotate") },
            { "shop.tier_one_prefix", new Translation("C1: ", "L1: ") },
            { "shop.tier_two_separator", new Translation(" • C2: ", " • L2: ") },
            { "shop.tier_three_separator", new Translation(" • C3: ", " • L3: ") },
            { "settings.language", new Translation("Ngôn ngữ", "Language") },
            { "settings.account", new Translation("Tài khoản", "Account") },
            { "settings.autosave", new Translation("Tiến độ được lưu tự động.", "Progress is saved automatically.") },
            { "settings.guest", new Translation("Làng khách", "Guest village") },
            { "settings.guest_restart", new Translation("Bắt đầu làng khách mới", "Start a new guest village") },
            { "settings.guest_restart_confirm", new Translation("Chơi lại từ làng trống với 1.000 hạt? Tiến độ khách hiện tại sẽ được thay thế và giữ một bản sao cục bộ.", "Start again with an empty village and 1,000 acorns? Your guest progress will be replaced and a local backup kept.") },
            { "settings.guest_restart_start", new Translation("Bắt đầu mới", "Start new") },
            { "settings.guest_started", new Translation("Làng khách mới: 1.000 hạt và đất trống.", "New guest village: 1,000 acorns and empty ground.") },
            { "settings.discord_community", new Translation("Tham gia cộng đồng Discord", "Join our Discord community") },
            { "settings.vietnamese", new Translation("Tiếng Việt", "Vietnamese") },
            { "settings.english", new Translation("Tiếng Anh", "English") },
            { "settings.language_value", new Translation("Ngôn ngữ: {0}", "Language: {0}") },
            { "message.audio_load_failed", new Translation("Không tải được âm thanh: {0}", "Could not load audio: {0}") },
            { "message.building_locked", new Translation("Công trình này mở ở cấp {0}.", "This building unlocks at level {0}.") },
            { "message.harvest", new Translation("Thu hoạch +{0} hạt sồi! Bổ sung điểm thu thập để làm tiếp.", "Harvest +{0} acorns! Refill the worksite to keep working.") },
            { "message.refill_cost", new Translation("Cần {0} hạt sồi để bổ sung.", "You need {0} acorns to refill.") },
            { "message.quest_reward", new Translation("Hoàn thành nhiệm vụ! +{0} hạt sồi, +{1} XP.", "Quest complete! +{0} acorns, +{1} XP.") },
            { "message.ant_lion_reward", new Translation("Đã đuổi kiến sư tử! +{0} hạt sồi, +{1} XP.", "Antlion chased away! +{0} acorns, +{1} XP.") },
            { "message.ladybug_reward", new Translation("Bọ rùa cảm ơn bạn! +{0} hạt sồi, +{1} XP.", "The ladybug thanks you! +{0} acorns, +{1} XP.") },
            { "message.unlock_error", new Translation("Mở ở cấp {0}.", "Unlocks at level {0}.") },
            { "label.harvest", new Translation("+{0}  •  Thu hoạch", "+{0}  •  Harvest") },
            { "label.working", new Translation("{0}  •  Đang làm", "{0}  •  Working") },
            { "label.refill_cost", new Translation("Bổ sung • {0} hạt", "Refill • {0} acorns") },
            { "label.help_ladybug", new Translation("Giúp bọ rùa  +{0}", "Help ladybug  +{0}") },
            { "label.repel_ant_lion", new Translation("Đuổi kiến sư tử +{0}", "Shoo antlion +{0}") },
            { "label.hud_level", new Translation("CẤP {0}", "LEVEL {0}") },
            { "label.claim_reward", new Translation("Nhận {0} hạt + {1} XP", "Claim {0} acorns + {1} XP") },
            { "label.quest_progress", new Translation("{0} / 6 câu chuyện đã hoàn thành", "{0} / 6 stories completed") },
            { "label.home_capacity", new Translation("C1: {0} • C2: {1} • C3: {2} {3}", "L1: {0} • L2: {1} • L3: {2} {3}") },
            { "label.energy_gain", new Translation("+{0} năng lượng", "+{0} energy") },
            { "label.building_tier", new Translation("Cấp {0} / 3   ·   {1}°", "Level {0} / 3   ·   {1}°") },
            { "label.capacity", new Translation("Sức chứa: {0} {1}", "Capacity: {0} {1}") },
            { "label.home_upgrade", new Translation("Bọ cánh cứng đang sửa nhà. Còn {0}.", "The beetle is repairing the house. Remaining: {0}.") },
            { "label.next_tier", new Translation("Cấp sau thêm 1 cư dân. Hoàn thành trong {0}.", "Next level adds 1 resident. Ready in {0}.") },
            { "label.upgrade_cost", new Translation("Nâng cấp • {0} hạt", "Upgrade • {0} acorns") },
            { "label.free_workers", new Translation("{0} rảnh {1}/{2} • chỗ {3}/3", "{0} free {1}/{2} • slots {3}/3") },
            { "label.site_upgrade", new Translation("Bọ cánh cứng đang làm việc • còn {0}", "The beetle is working • remaining {0}") },
            { "label.collecting", new Translation("Đang thu thập • còn {0}", "Gathering • remaining {0}") },
            { "label.harvest_reward", new Translation("Thành quả: {0} hạt + {1} XP", "Harvest: {0} acorns + {1} XP") },
            { "label.harvest_button", new Translation("Thu hoạch +{0} hạt", "Harvest +{0} acorns") },
            { "label.site_upgrade_cost", new Translation("Nâng điểm • {0} hạt / {1}", "Upgrade • {0} acorns / {1}") },
            { "label.meal", new Translation("Còn {0} • +{1} năng lượng sau bữa ăn.", "Remaining {0} • +{1} energy after the meal.") },
            { "label.unlock_level", new Translation("Mở cấp {0}", "Unlocks at {0}") },
            { "label.upgrade_time", new Translation("Nâng cấp • {0}", "Upgrade • {0}") },
            { "label.job_option", new Translation("{0} {1} / +{2}", "{0} {1} / +{2}") },
            { "label.refill_button", new Translation("{0} • {1} hạt", "{0} • {1} acorns") },
            { "label.level", new Translation("Cấp {0}", "Level {0}") },
            { "label.price", new Translation("{0} hạt", "{0} acorns") },
        };

        private static readonly Dictionary<string, Translation> CanonicalTexts = CreateCanonicalIndex();
        private static readonly List<Translation> Templates = CreateTemplateIndex();
        private static readonly Dictionary<string, string> TranslationCache = new Dictionary<string, string>();
        private static GameLanguage language = GameLanguage.English;

        /// <summary>
        /// Gets or changes the interface language and invalidates previously resolved text.
        /// </summary>
        public static GameLanguage Language
        {
            get => language;
            set
            {
                language = value == GameLanguage.Vietnamese ? GameLanguage.Vietnamese : GameLanguage.English;
                TranslationCache.Clear();
            }
        }

        /// <summary>
        /// Gets all stable catalog keys for validation and tooling.
        /// </summary>
        public static IEnumerable<string> Keys => Translations.Keys;

        /// <summary>
        /// Returns canonical Vietnamese text independently of the selected interface language.
        /// </summary>
        /// <remarks>Use this for domain errors, object names, and text retained for later rendering.</remarks>
        public static string Source(string key)
        {
            return Translations[key].Vietnamese;
        }

        /// <summary>
        /// Returns the translation for a stable catalog key in the selected language.
        /// </summary>
        public static string Text(string key)
        {
            return Text(key, Language);
        }

        /// <summary>
        /// Returns a specific translation without changing the current interface language.
        /// </summary>
        public static string Text(string key, GameLanguage selectedLanguage)
        {
            Translation entry = Translations[key];
            return selectedLanguage == GameLanguage.Vietnamese ? entry.Vietnamese : entry.English;
        }

        /// <summary>
        /// Formats a translated message while preserving numeric and duration arguments.
        /// </summary>
        public static string Format(string key, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, Text(key), arguments);
        }

        /// <summary>
        /// Translates canonical text at the presentation boundary, including parameterized messages.
        /// Unknown strings and nonlinguistic values pass through unchanged.
        /// </summary>
        public static string Translate(string text)
        {
            if (Language == GameLanguage.Vietnamese || string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (CanonicalTexts.TryGetValue(text, out Translation literal))
            {
                return literal.English;
            }

            if (TranslationCache.TryGetValue(text, out string cached))
            {
                return cached;
            }

            string translated = text;
            foreach (Translation template in Templates)
            {
                Match match = template.Pattern.Match(text);
                if (!match.Success)
                {
                    continue;
                }

                var arguments = new object[template.ArgumentCount];
                for (int i = 0; i < arguments.Length; i++)
                {
                    string argument = match.Groups["arg" + i].Value;
                    arguments[i] = CanonicalTexts.TryGetValue(argument, out Translation value)
                        ? value.English
                        : argument;
                }

                translated = string.Format(CultureInfo.InvariantCulture, template.English, arguments);
                break;
            }

            // Bound the cache because countdowns and reward amounts produce new strings over time.
            if (TranslationCache.Count >= 2048)
            {
                TranslationCache.Clear();
            }

            TranslationCache[text] = translated;
            return translated;
        }

        /// <summary>
        /// Indexes complete canonical strings for fast literal translation.
        /// </summary>
        private static Dictionary<string, Translation> CreateCanonicalIndex()
        {
            var index = new Dictionary<string, Translation>(StringComparer.Ordinal);
            foreach (Translation entry in Translations.Values)
            {
                if (entry.Pattern == null)
                {
                    index.Add(entry.Vietnamese, entry);
                }
            }

            return index;
        }

        /// <summary>
        /// Collects full message templates for translating existing composed domain and interface text.
        /// </summary>
        private static List<Translation> CreateTemplateIndex()
        {
            var templates = new List<Translation>();
            foreach (Translation entry in Translations.Values)
            {
                if (entry.Pattern != null)
                {
                    templates.Add(entry);
                }
            }

            return templates;
        }

        /// <summary>
        /// Holds a bilingual entry and the exact-match pattern for its optional arguments.
        /// </summary>
        private sealed class Translation
        {
            public readonly string Vietnamese;
            public readonly string English;
            public readonly Regex Pattern;
            public readonly int ArgumentCount;

            /// <summary>
            /// Creates an entry and anchors any numbered placeholders to the complete source message.
            /// </summary>
            public Translation(string vietnamese, string english)
            {
                Vietnamese = vietnamese;
                English = english;
                MatchCollection placeholders = Regex.Matches(vietnamese, @"\{(\d+)\}");
                if (placeholders.Count == 0)
                {
                    return;
                }

                var pattern = new StringBuilder(@"\A");
                int offset = 0;
                foreach (Match placeholder in placeholders)
                {
                    int index = int.Parse(placeholder.Groups[1].Value, CultureInfo.InvariantCulture);
                    ArgumentCount = Math.Max(ArgumentCount, index + 1);
                    pattern.Append(Regex.Escape(vietnamese.Substring(offset, placeholder.Index - offset)));
                    pattern.Append("(?<arg").Append(index).Append(">.*?)");
                    offset = placeholder.Index + placeholder.Length;
                }

                pattern.Append(Regex.Escape(vietnamese.Substring(offset))).Append(@"\z");
                Pattern = new Regex(pattern.ToString());
            }
        }
    }
}
