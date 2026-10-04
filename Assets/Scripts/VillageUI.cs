using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace LittleColony
{
    /// <summary>
    /// Draws the localized gameplay interface and routes interface actions to the game.
    /// </summary>
    public sealed class VillageUI : MonoBehaviour
    {
        public VillageGame Game;
        private Font font;
        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle small;
        private GUIStyle tiny;
        private GUIStyle number;
        private GUIStyle button;
        private GUIStyle center;
        private GUIStyle paper;
        private GUIStyle forest;
        private GUIStyle honey;
        private GUIStyle sage;
        private GUIStyle shadow;
        private GUIStyle hudFrame;
        private GUIStyle hudDark;
        private Texture2D paperTexture;
        private Texture2D forestTexture;
        private Texture2D honeyTexture;
        private Texture2D sageTexture;
        private Texture2D shadowTexture;
        private Texture2D checkTexture;
        private Texture2D frameTexture;
        private Texture2D darkTexture;
        private Texture2D goldBadge;
        private Texture2D greenBadge;
        private Texture2D antIcon;
        private Texture2D beeIcon;
        private Texture2D acornIcon;
        private Texture2D foodIcon;
        private Texture2D cookieIcon;
        private Texture2D shopIcon;
        private Texture2D lockIcon;
        private string expansionMessage;
        private Texture2D bookIcon;
        private Texture2D gearIcon;
        private Texture2D discordIcon;
        private const string CommunityInviteUrl = "https://discord.gg/chill-station";
        private Texture2D sunIcon;
        private Texture2D moonIcon;
        private Texture2D rainIcon;
        private bool journal;
        private bool shopOpen;
        private bool settingsOpen;
        private string notificationTitle;
        private string notificationMessage;
        private int notificationClosedFrame = -1;

        /// <summary>Blocks gameplay through the frame that dismisses a notification.</summary>
        public bool IsNotificationOpen => notificationMessage != null || notificationClosedFrame == Time.frameCount;

        /// <summary>Displays a modal explanation without discarding the current shop or selection.</summary>
        public void ShowNotification(string titleText, string message)
        {
            notificationTitle = titleText;
            notificationMessage = message;
            shopDrag.Cancel();
        }

        /// <summary>Dismisses a notification while preventing the closing input from reaching the world.</summary>
        public void CloseNotification()
        {
            notificationMessage = null;
            notificationClosedFrame = Time.frameCount;
        }
        private bool audioSettingsOpen;
        private bool guestResetPending;
        private GUIStyle volumeSliderTrack;
        private GUIStyle volumeSliderThumb;

        /// <summary>
        /// Gets whether the modal settings panel is consuming gameplay input.
        /// </summary>
        public bool IsSettingsOpen => settingsOpen;

        /// <summary>
        /// Gets settings bounds with room for expanded audio and active account actions.
        /// </summary>
        private Rect SettingsPanel
        {
            get
            {
                float height = (audioSettingsOpen ? 285 : 190) + 60;
                if (Game.Cloud != null && Game.Cloud.IsActive)
                {
                    height += 144;
                    if (Game.Cloud.HasConflict || Game.Cloud.NeedsSignIn)
                    {
                        height += 48;
                    }
                }
                else
                {
                    height += guestResetPending ? 154 : 110;
                }
                return new Rect((W - 400) / 2, (H - height) / 2, 400, height);
            }
        }
        private int shopPage;
        private readonly Vector2[] shopScroll = new Vector2[5];
        private readonly DragScrollView shopDrag = new DragScrollView();
        private int dragShopPage = -1;
        private Color ink = VillageWorld.ColorOf("#304639");
        private Color muted = VillageWorld.ColorOf("#78816C");
        private float Scale => Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        private float W => Screen.width / Scale;
        private float H => Screen.height / Scale;
#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>Reads the canvas pixel density used to keep touch targets sized in browser pixels.</summary>
        [DllImport("__Internal")]
        private static extern float LittleColonyCanvasPixelRatio();
#endif
        /// <summary>Gets rendering pixels per browser pixel for physical touch target sizing.</summary>
        private float ScreenPixelRatio
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return Mathf.Max(1, LittleColonyCanvasPixelRatio());
#else
                return 1;
#endif
            }
        }
        private float InspectorScale => Mathf.Max(Scale, Mathf.Min(ScreenPixelRatio, Screen.width / 310f, Screen.height / 460f));
        private float InspectorCloseSize => Mathf.Max(48, 48 * ScreenPixelRatio / InspectorScale);
        private float InspectorHeaderInset => Mathf.Max(20, InspectorCloseSize - 36);
        private Rect InspectorLayout => new Rect(Screen.width / InspectorScale - 298,
            Scale < ScreenPixelRatio ? Mathf.Max(8, (Screen.height / InspectorScale - 405 - InspectorHeaderInset) / 2) : 200,
            284, 405 + InspectorHeaderInset);
        private Rect Inspector => new Rect(InspectorLayout.position * (InspectorScale / Scale),
            InspectorLayout.size * (InspectorScale / Scale));
        private Rect Journal => new Rect(W - 284, 201, 260, journal ? 200 : 0);
        private Rect ShopPanel => new Rect((W - 925) / 2,
            H - (shopPage <= 2 ? 505 : shopPage == 4 ? 416 : 357),
            925, shopPage <= 2 ? 479 : shopPage == 4 ? 390 : 331);

        public static readonly string[] Names =
        {
            I18n.Source("building.ant_home"),
            I18n.Source("building.bee_home"),
            I18n.Source("building.pile"),
            I18n.Source("building.flower"),
            I18n.Source("building.clover"),
            I18n.Source("building.lantern"),
            I18n.Source("building.bench"),
            I18n.Source("building.birdbath"),
            I18n.Source("building.flower_arch"),
            I18n.Source("building.mushroom_patch"),
            I18n.Source("building.ant_burrow"),
            I18n.Source("building.bee_lantern"),
            I18n.Source("building.twig_yard"),
            I18n.Source("building.seed_mill"),
            I18n.Source("building.lavender"),
            I18n.Source("building.sunflower"),
            I18n.Source("building.ant_leaf_tent"),
            I18n.Source("building.bee_flower_home"),
            I18n.Source("building.sugar_cube"),
            I18n.Source("building.cookie"),
            I18n.Source("building.fence"),
            I18n.Source("building.ant_acorn_home"),
            I18n.Source("building.small_tree"),
            I18n.Source("building.tall_grass"),
            I18n.Source("building.small_parasol"),
            I18n.Source("building.bee_honeycomb_home"),
            I18n.Source("building.tulip"),
            I18n.Source("building.bluebell"),
            I18n.Source("building.leaf_depot"),
            I18n.Source("building.pebble_yard"),
            I18n.Source("building.wooden_planter"),
            I18n.Source("building.birdhouse"),
            I18n.Source("building.wind_chime"),
            I18n.Source("building.leaf_fountain"),
            I18n.Source("building.flower_cart")
        };
        private static readonly BuildingKind[][] ShopGroups =
        {
            new[]
            {
                BuildingKind.AntHome,
                BuildingKind.AntBurrow,
                BuildingKind.AntLeafTent,
                BuildingKind.BeeHome,
                BuildingKind.BeeLantern,
                BuildingKind.BeeFlowerHome,
                BuildingKind.AntAcornHome,
                BuildingKind.BeeHoneycombHome
            },
            new[]
            {
                BuildingKind.Pile,
                BuildingKind.LeafDepot,
                BuildingKind.TwigYard,
                BuildingKind.PebbleYard,
                BuildingKind.SeedMill,
                BuildingKind.Flower,
                BuildingKind.Tulip,
                BuildingKind.Lavender,
                BuildingKind.Bluebell,
                BuildingKind.Sunflower
            },
            new[]
            {
                BuildingKind.Clover,
                BuildingKind.Fence,
                BuildingKind.Lantern,
                BuildingKind.Bench,
                BuildingKind.Birdbath,
                BuildingKind.FlowerArch,
                BuildingKind.MushroomPatch,
                BuildingKind.SmallTree,
                BuildingKind.TallGrass,
                BuildingKind.SmallParasol,
                BuildingKind.WoodenPlanter,
                BuildingKind.Birdhouse,
                BuildingKind.WindChime,
                BuildingKind.LeafFountain,
                BuildingKind.FlowerCart
            },
            new[]
            {
                BuildingKind.SugarCube,
                BuildingKind.Cookie
            }
        };
        private static readonly string[] Descriptions =
        {
            I18n.Source("description.ant_home"),
            I18n.Source("description.bee_home"),
            I18n.Source("description.pile"),
            I18n.Source("description.flower"),
            I18n.Source("description.clover"),
            I18n.Source("description.lantern"),
            I18n.Source("description.bench"),
            I18n.Source("description.birdbath"),
            I18n.Source("description.flower_arch"),
            I18n.Source("description.mushroom_patch"),
            I18n.Source("description.ant_burrow"),
            I18n.Source("description.bee_lantern"),
            I18n.Source("description.twig_yard"),
            I18n.Source("description.seed_mill"),
            I18n.Source("description.lavender"),
            I18n.Source("description.sunflower"),
            I18n.Source("description.ant_leaf_tent"),
            I18n.Source("description.bee_flower_home"),
            I18n.Source("description.sugar_cube"),
            I18n.Source("description.cookie"),
            I18n.Source("description.fence"),
            I18n.Source("description.ant_acorn_home"),
            I18n.Source("description.small_tree"),
            I18n.Source("description.tall_grass"),
            I18n.Source("description.small_parasol"),
            I18n.Source("description.bee_honeycomb_home"),
            I18n.Source("description.tulip"),
            I18n.Source("description.bluebell"),
            I18n.Source("description.leaf_depot"),
            I18n.Source("description.pebble_yard"),
            I18n.Source("description.wooden_planter"),
            I18n.Source("description.birdhouse"),
            I18n.Source("description.wind_chime"),
            I18n.Source("description.leaf_fountain"),
            I18n.Source("description.flower_cart")
        };
        private static readonly string[] Quests =
        {
            I18n.Source("quest.first_harvest"),
            I18n.Source("quest.new_neighbors"),
            I18n.Source("quest.village_meal"),
            I18n.Source("quest.first_bee"),
            I18n.Source("quest.flower_garden"),
            I18n.Source("quest.visitor")
        };
        private static readonly string[] QuestHints =
        {
            I18n.Source("quest_hint.first_harvest"),
            I18n.Source("quest_hint.new_neighbors"),
            I18n.Source("quest_hint.village_meal"),
            I18n.Source("quest_hint.first_bee"),
            I18n.Source("quest_hint.flower_garden"),
            I18n.Source("quest_hint.visitor")
        };
        /// <summary>
        /// Creates a rounded, tinted texture for interface backgrounds.
        /// </summary>
        Texture2D CreateRoundedTexture(string hex)
        {
            var t = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color color = VillageWorld.ColorOf(hex);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dx = Mathf.Max(0, 9 - x, x - 22), dy = Mathf.Max(0, 9 - y, y - 22);
                    var c = color;
                    c.a *= Mathf.Clamp01(9.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    t.SetPixel(x, y, c);
                }
            }

            t.Apply();
            return t;
        }

        /// <summary>
        /// Creates a sliced background style from the supplied texture.
        /// </summary>
        GUIStyle CreateBackgroundStyle(Texture2D t)
        {
            return new GUIStyle
            {
                normal =
                {
                    background = t
                },
                border = new RectOffset(10, 10, 10, 10)
            };
        }

        /// <summary>
        /// Paints a filled disk into an interface texture.
        /// </summary>
        static void PaintDisk(Texture2D t, float cx, float cy, float radius, Color color)
        {
            for (int y = 0; y < t.height; y++)
            {
                for (int x = 0; x < t.width; x++)
                {
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                    {
                        t.SetPixel(x, y, color);
                    }
                }
            }
        }

        /// <summary>
        /// Paints a thick line segment into an interface texture.
        /// </summary>
        static void PaintStroke(Texture2D t, Vector2 a, Vector2 b, float width, Color color)
        {
            for (int y = 0; y < t.height; y++)
            {
                for (int x = 0; x < t.width; x++)
                {
                    if (SegmentDistance(new Vector2(x, y), a, b) <= width)
                    {
                        t.SetPixel(x, y, color);
                    }
                }
            }
        }

        /// <summary>
        /// Fills a rectangular region of an interface texture.
        /// </summary>
        static void FillTextureRectangle(Texture2D t, int x0, int y0, int x1, int y1, Color color)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    t.SetPixel(x, y, color);
                }
            }
        }

        /// <summary>
        /// Creates the decorative circular badge texture.
        /// </summary>
        Texture2D CreateBadgeTexture(string fill)
        {
            var t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var clear = Color.clear;
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    t.SetPixel(x, y, clear);
                }
            }

            PaintDisk(t, 32, 32, 30, VillageWorld.ColorOf("#513F2D"));
            PaintDisk(t, 32, 34, 27, VillageWorld.ColorOf("#E8CC8D"));
            PaintDisk(t, 32, 32, 24, VillageWorld.ColorOf(fill));
            t.Apply();
            t.filterMode = FilterMode.Bilinear;
            return t;
        }

        /// <summary>
        /// Draws the requested original gameplay icon into a texture.
        /// </summary>
        Texture2D CreateIconTexture(string kind)
        {
            var t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    t.SetPixel(x, y, Color.clear);
                }
            }

            var outline = VillageWorld.ColorOf("#493A2C");
            var red = VillageWorld.ColorOf("#C95345");
            var yellow = VillageWorld.ColorOf("#F3C54F");
            if (kind == "ant" || kind == "bee")
            {
                if (kind == "bee")
                {
                    PaintDisk(t, 27, 44, 10, VillageWorld.ColorOf("#DCEFE8"));
                    PaintDisk(t, 42, 44, 10, VillageWorld.ColorOf("#DCEFE8"));
                }

                for (int i = 0; i < 3; i++)
                {
                    PaintStroke(t, new Vector2(24 + i * 5, 25), new Vector2(14 + i * 5, 12), 2.1f, outline);
                    PaintStroke(t, new Vector2(32 + i * 4, 25), new Vector2(45 + i * 4, 12), 2.1f, outline);
                }

                PaintDisk(t, 39, 28, 17, outline);
                PaintDisk(t, 39, 28, 14, kind == "ant" ? red : yellow);
                if (kind == "bee")
                {
                    for (int x = 32; x <= 44; x += 7)
                    {
                        FillTextureRectangle(t, x, 16, x + 3, 40, outline);
                    }
                }

                PaintDisk(t, 22, 31, 12, outline);
                PaintDisk(t, 22, 31, 10, kind == "ant" ? red : yellow);
                PaintDisk(t, 17, 35, 2, Color.white);
                PaintStroke(t, new Vector2(19, 42), new Vector2(13, 53), 1.4f, outline);
                PaintStroke(t, new Vector2(26, 42), new Vector2(31, 52), 1.4f, outline);
            }
            else if (kind == "acorn" || kind == "shop")
            {
                PaintDisk(t, 32, 28, 19, outline);
                PaintDisk(t, 32, 28, 16, VillageWorld.ColorOf("#A86F3F"));
                PaintDisk(t, 32, 41, 18, outline);
                PaintDisk(t, 32, 43, 15, VillageWorld.ColorOf("#D4A75D"));
                PaintStroke(t, new Vector2(32, 48), new Vector2(36, 58), 2.4f, outline);
                if (kind == "shop")
                {
                    PaintDisk(t, 46, 52, 10, VillageWorld.ColorOf("#638B4B"));
                    PaintStroke(t, new Vector2(39, 47), new Vector2(53, 57), 1.2f, VillageWorld.ColorOf("#365C3F"));
                }
            }
            else if (kind == "lock")
            {
                PaintDisk(t, 32, 43, 16, outline);
                PaintDisk(t, 32, 43, 11, VillageWorld.ColorOf("#F8E9BE"));
                FillTextureRectangle(t, 12, 8, 52, 39, outline);
                FillTextureRectangle(t, 16, 12, 48, 35, yellow);
                PaintDisk(t, 32, 26, 4, outline);
                FillTextureRectangle(t, 30, 17, 34, 26, outline);
            }
            else if (kind == "food")
            {
                FillTextureRectangle(t, 16, 16, 46, 43, VillageWorld.ColorOf("#B5D8DC"));
                FillTextureRectangle(t, 18, 19, 44, 41, Color.white);
                PaintStroke(t, new Vector2(18, 41), new Vector2(31, 50), 2, VillageWorld.ColorOf("#83A9AF"));
                PaintStroke(t, new Vector2(44, 41), new Vector2(31, 50), 2, VillageWorld.ColorOf("#83A9AF"));
            }
            else if (kind == "cookie")
            {
                PaintDisk(t, 32, 31, 23, outline);
                PaintDisk(t, 32, 31, 20, VillageWorld.ColorOf("#D5A36C"));
                foreach (var p in new[]
                {
                    new Vector2(23, 37),
                    new Vector2(38, 41),
                    new Vector2(43, 26),
                    new Vector2(27, 20),
                    new Vector2(18, 28)
                }

                )
                {
                    PaintDisk(t, p.x, p.y, 3, VillageWorld.ColorOf("#795236"));
                }
            }
            else if (kind == "book")
            {
                FillTextureRectangle(t, 12, 12, 51, 51, outline);
                FillTextureRectangle(t, 16, 16, 29, 47, VillageWorld.ColorOf("#F8E9BE"));
                FillTextureRectangle(t, 34, 16, 47, 47, VillageWorld.ColorOf("#F8E9BE"));
                PaintStroke(t, new Vector2(32, 12), new Vector2(32, 52), 1.5f, VillageWorld.ColorOf("#A26F47"));
                PaintDisk(t, 31, 31, 5, VillageWorld.ColorOf("#D58C5D"));
            }
            else if (kind == "gear")
            {
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    PaintStroke(t,
                        new Vector2(32 + Mathf.Cos(a) * 15, 32 + Mathf.Sin(a) * 15),
                        new Vector2(32 + Mathf.Cos(a) * 25, 32 + Mathf.Sin(a) * 25),
                        4,
                        outline);
                }

                PaintDisk(t, 32, 32, 18, outline);
                PaintDisk(t, 32, 32, 12, VillageWorld.ColorOf("#D9D4BE"));
                PaintDisk(t, 32, 32, 6, outline);
            }
            else if (kind == "sun")
            {
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    PaintStroke(t,
                        new Vector2(32 + Mathf.Cos(a) * 19, 32 + Mathf.Sin(a) * 19),
                        new Vector2(32 + Mathf.Cos(a) * 27, 32 + Mathf.Sin(a) * 27),
                        2.5f,
                        yellow);
                }

                PaintDisk(t, 32, 32, 15, outline);
                PaintDisk(t, 32, 32, 12, yellow);
            }
            else if (kind == "moon")
            {
                PaintDisk(t, 30, 32, 22, VillageWorld.ColorOf("#F0DA9B"));
                PaintDisk(t, 41, 39, 20, Color.clear);
                PaintDisk(t, 20, 18, 2, Color.white);
                PaintDisk(t, 49, 18, 2, Color.white);
            }
            else if (kind == "rain")
            {
                PaintDisk(t, 24, 39, 13, VillageWorld.ColorOf("#C9DCE6"));
                PaintDisk(t, 39, 39, 15, VillageWorld.ColorOf("#C9DCE6"));
                FillTextureRectangle(t, 14, 29, 50, 40, VillageWorld.ColorOf("#C9DCE6"));
                for (int i = 0; i < 4; i++)
                {
                    PaintStroke(t, new Vector2(18 + i * 9, 24), new Vector2(14 + i * 9, 10), 2.2f, VillageWorld.ColorOf("#72BBD6"));
                }
            }

            t.Apply();
            t.filterMode = FilterMode.Bilinear;
            return t;
        }

        /// <summary>
        /// Returns the shortest distance from a point to a line segment.
        /// </summary>
        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            return Vector2.Distance(p, a + ab * Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude));
        }

        /// <summary>
        /// Creates a text style with the requested size and emphasis.
        /// </summary>
        GUIStyle CreateTextStyle(int size, bool bold = false)
        {
            return new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = size,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                normal =
                {
                    textColor = ink
                },
                padding = new RectOffset(0, 0, 0, 0),
                wordWrap = true
            };
        }

        /// <summary>
        /// Creates the interface textures, icons, font, and styles on first use.
        /// </summary>
        void InitializeStyles()
        {
            if (title != null)
            {
                return;
            }

            font = Resources.Load<Font>("Fonts/BeVietnamPro-Regular");
            paperTexture = CreateRoundedTexture("#FFFBED");
            forestTexture = CreateRoundedTexture("#395C48");
            honeyTexture = CreateRoundedTexture("#EEC86F");
            sageTexture = CreateRoundedTexture("#E5EBD7");
            shadowTexture = CreateRoundedTexture("#233B2928");
            frameTexture = CreateRoundedTexture("#C99B5A");
            darkTexture = CreateRoundedTexture("#313833");
            goldBadge = CreateBadgeTexture("#D3A45B");
            greenBadge = CreateBadgeTexture("#6B9361");
            antIcon = CreateIconTexture("ant");
            beeIcon = CreateIconTexture("bee");
            acornIcon = CreateIconTexture("acorn");
            foodIcon = CreateIconTexture("food");
            cookieIcon = CreateIconTexture("cookie");
            shopIcon = CreateIconTexture("shop");
            lockIcon = CreateIconTexture("lock");
            bookIcon = CreateIconTexture("book");
            gearIcon = CreateIconTexture("gear");
            discordIcon = LoginScreenStyles.CreateDiscordIcon();
            sunIcon = CreateIconTexture("sun");
            moonIcon = CreateIconTexture("moon");
            rainIcon = CreateIconTexture("rain");
            checkTexture = new Texture2D(20, 20, TextureFormat.RGBA32, false);
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 20; x++)
                {
                    var p = new Vector2(x, y);
                    bool line = SegmentDistance(p, new Vector2(2, 10), new Vector2(7, 5)) < 1.6f
                        || SegmentDistance(p, new Vector2(7, 5), new Vector2(18, 17)) < 1.6f;
                    checkTexture.SetPixel(x, y, line ? Color.white : Color.clear);
                }
            }

            checkTexture.Apply();
            checkTexture.filterMode = FilterMode.Bilinear;
            paper = CreateBackgroundStyle(paperTexture);
            forest = CreateBackgroundStyle(forestTexture);
            honey = CreateBackgroundStyle(honeyTexture);
            sage = CreateBackgroundStyle(sageTexture);
            shadow = CreateBackgroundStyle(shadowTexture);
            hudFrame = CreateBackgroundStyle(frameTexture);
            hudDark = CreateBackgroundStyle(darkTexture);
            volumeSliderTrack = new GUIStyle(GUI.skin.horizontalSlider)
            {
                normal = { background = darkTexture },
                fixedHeight = 16,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            volumeSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                normal = { background = honeyTexture },
                hover = { background = honeyTexture },
                active = { background = honeyTexture },
                fixedWidth = 28,
                fixedHeight = 28,
                overflow = new RectOffset(0, 0, 6, 6)
            };
            title = CreateTextStyle(30, true);
            heading = CreateTextStyle(19, true);
            body = CreateTextStyle(13);
            small = CreateTextStyle(11);
            tiny = CreateTextStyle(10, true);
            number = CreateTextStyle(24, true);
            button = CreateTextStyle(13, true);
            button.alignment = TextAnchor.MiddleCenter;
            center = CreateTextStyle(12, true);
            center.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// Draws a panel background and its optional shadow.
        /// </summary>
        void DrawBox(Rect rect, GUIStyle style = null, bool drop = true)
        {
            if (drop)
            {
                GUI.Box(new Rect(rect.x, rect.y + 3, rect.width, rect.height), GUIContent.none, shadow);
            }

            GUI.Box(rect, GUIContent.none, style ?? paper);
        }

        /// <summary>
        /// Draws text with the supplied style and optional color.
        /// </summary>
        void DrawLabel(Rect rect, string text, GUIStyle style, Color? color = null)
        {
            Color prev = style.normal.textColor;
            style.normal.textColor = color ?? ink;
            GUI.Label(rect, I18n.Translate(text), style);
            style.normal.textColor = prev;
        }

        /// <summary>
        /// Draws a styled button while restoring the previous GUI enabled and color state.
        /// </summary>
        bool DrawButton(Rect r, string text, bool enabled = true, bool accent = false, bool soft = false)
        {
            Color prev = GUI.color;
            GUI.color = enabled ? Color.white : new Color(1, 1, 1, .46f);
            DrawBox(r, accent ? honey : soft ? sage : forest, false);
            bool old = GUI.enabled;
            GUI.enabled = old && enabled;
            button.normal.textColor = accent || soft ? ink : Color.white;
            bool clicked = GUI.Button(r, I18n.Translate(text), button);
            GUI.enabled = old;
            GUI.color = prev;
            return clicked && !shopDrag.SuppressClicks;
        }

        /// <summary>
        /// Draws a clamped progress bar in the supplied color.
        /// </summary>
        void DrawProgressBar(Rect r, float amount, string color)
        {
            GUI.color = VillageWorld.ColorOf("#DDE4D0");
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = VillageWorld.ColorOf(color);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(amount), r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        /// <summary>
        /// Draws a status row with its icon, values, and optional progress.
        /// </summary>
        void DrawHudBar(Rect r, Texture icon, string label, string value, float progress = -1, string fill = "#75C9C8")
        {
            DrawBox(new Rect(r.x + 14, r.y, r.width - 14, r.height), hudFrame, false);
            DrawBox(new Rect(r.x + 17, r.y + 3, r.width - 20, r.height - 6), hudDark, false);
            GUI.DrawTexture(new Rect(r.x - 2, r.y - 5, r.height + 10, r.height + 10), icon, ScaleMode.ScaleToFit, true);
            if (r.width < 150)
            {
                DrawLabel(new Rect(r.x + r.height + 5, r.y + 5, 49, 18), label, tiny, Color.white);
                DrawLabel(new Rect(r.xMax - 27, r.y + 3, 23, 22), value, small, Color.white);
            }
            else
            {
                DrawLabel(new Rect(r.x + r.height + 9, r.y + 5, r.width - r.height - 62, 18), label, tiny, Color.white);
                DrawLabel(new Rect(r.xMax - 57, r.y + 4, 51, 22), value, small, Color.white);
            }

            if (progress >= 0)
            {
                DrawBox(new Rect(r.x + r.height + 8, r.y + r.height - 11, r.width - r.height - 19, 6), hudDark, false);
                GUI.color = VillageWorld.ColorOf(fill);
                GUI.DrawTexture(new Rect(r.x + r.height + 10, r.y + r.height - 9, (r.width - r.height - 23) * Mathf.Clamp01(progress), 3),
                    Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        /// <summary>
        /// Draws a circular icon button with a caption.
        /// </summary>
        bool DrawIconButton(Rect r, Texture2D icon, string caption, bool enabled = true, bool green = false)
        {
            GUI.color = enabled ? Color.white : new Color(1, 1, 1, .42f);
            float disk = Mathf.Min(r.width, r.height - 18);
            GUI.DrawTexture(new Rect(r.x + (r.width - disk) / 2, r.y, disk, disk), green ? greenBadge : goldBadge, ScaleMode.ScaleToFit, true);
            GUI.DrawTexture(new Rect(r.x + (r.width - disk) / 2 + disk * .18f, r.y + disk * .16f, disk * .64f, disk * .64f),
                icon,
                ScaleMode.ScaleToFit,
                true);
            var plate = new Rect(r.x + 2, r.y + r.height - 22, r.width - 4, 22);
            DrawBox(plate, hudDark, false);
            string localizedCaption = I18n.Translate(caption);
            int previousFontSize = center.fontSize;
            bool previousWordWrap = center.wordWrap;
            center.wordWrap = false;
            float textWidth = center.CalcSize(new GUIContent(localizedCaption)).x;
            if (textWidth > plate.width - 6)
            {
                center.fontSize = Mathf.Max(9, Mathf.FloorToInt(previousFontSize * (plate.width - 6) / textWidth));
            }

            DrawLabel(new Rect(plate.x + 3, plate.y + 2, plate.width - 6, 18), localizedCaption, center, Color.white);
            center.fontSize = previousFontSize;
            center.wordWrap = previousWordWrap;
            bool old = GUI.enabled;
            GUI.enabled = old && enabled;
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            GUI.enabled = old;
            GUI.color = Color.white;
            return clicked;
        }

        /// <summary>
        /// Formats a duration as minutes and seconds, adding hours for longer durations.
        /// </summary>
        static string FormatDuration(double value)
        {
            int seconds = Mathf.CeilToInt((float)value);
            return seconds >= 3600
                ? seconds / 3600 + ":" + ((seconds / 60) % 60).ToString("00") + ":" + (seconds % 60).ToString("00")
                : seconds / 60 + ":" + (seconds % 60).ToString("00");
        }

        /// <summary>
        /// Formats the village energy remaining for the status display.
        /// </summary>
        string FormatEnergy()
        {
            return FormatDuration(Game.State.energy);
        }

        /// <summary>
        /// Projects a world position to the interface rectangle used by floating labels.
        /// </summary>
        Rect GetFloatingLabelRect(Vector3 p, float width = 144)
        {
            var s = Game.World.ViewCamera.WorldToScreenPoint(p);
            return new Rect(s.x / Scale - width / 2, (Screen.height - s.y) / Scale - 12, width, 25);
        }

        /// <summary>
        /// Positions the placement controls above the current preview within screen bounds.
        /// </summary>
        Rect GetMoveControlsRect()
        {
            var s = Game.World.ViewCamera.WorldToScreenPoint(Game.DraftPosition + Vector3.up * 2.1f);
            float x = Mathf.Clamp(s.x / Scale - 105, 14, W - 224), y = Mathf.Clamp((Screen.height - s.y) / Scale - 76, 98, H - 246);
            return new Rect(x, y, 210, 42);
        }

        /// <summary>
        /// Checks whether a floating label is clear of the current interface panels.
        /// </summary>
        bool IsFloatingLabelVisible(Rect r)
        {
            return !shopOpen && !settingsOpen && r.y > 85 && r.yMax < H - 112 && !r.Overlaps(Journal) && !(Game.Selected != null && r.Overlaps(Inspector));
        }

        /// <summary>
        /// Checks whether interface elements should consume input at a screen position.
        /// </summary>
        public bool BlocksPointer(Vector2 screen)
        {
            if (Game.IsWelcomeScreenOpen)
            {
                return true;
            }

            Vector2 p = new Vector2(screen.x / Scale, (Screen.height - screen.y) / Scale);
            if (shopOpen || settingsOpen || IsNotificationOpen)
            {
                return true;
            }

            if (Game.Placing.HasValue && Game.DraftLocked && Game.DraftVisible && GetMoveControlsRect().Contains(p))
            {
                return true;
            }

            if (new Rect(14, 6, 270, 135).Contains(p)
                || new Rect(W - 244, 6, 235, 194).Contains(p)
                || new Rect(17, H - 111, 110, 102).Contains(p)
                || Journal.Contains(p)
                || (!Game.Placing.HasValue
                    && Game.Selected != null
                    && !journal
                    && Inspector.Contains(p))
                || new Rect(18, H - 208, 100, 78).Contains(p))
            {
                return true;
            }

            if (Game.Placing.HasValue)
            {
                return false;
            }

            foreach (LandPlot plot in System.Enum.GetValues(typeof(LandPlot)))
            {
                if (!Game.State.IsExpansionOwned(plot) && GetPlotBadgeRect(plot).Contains(p))
                {
                    return true;
                }
            }

            var visitor = GetFloatingLabelRect(Game.World.VisitorPosition + Vector3.up * 1.1f);
            var lion = GetFloatingLabelRect(Game.World.AntLionPosition + Vector3.up * 1.35f, 190);
            return (Game.VisitorActive
                    && IsFloatingLabelVisible(visitor)
                    && visitor.Contains(p))
                || (Game.AntLionActive
                    && IsFloatingLabelVisible(lion)
                    && lion.Contains(p));
        }

        /// <summary>
        /// Draws the scaled gameplay interface after guest entry and handles its immediate-mode input.
        /// </summary>
        void OnGUI()
        {
            if (Game == null || Game.State == null || Game.IsWelcomeScreenOpen
                || (Game.Cloud != null && Game.Cloud.IsTransitioning))
            {
                return;
            }

            if (Game.Placing.HasValue)
            {
                shopOpen = false;
                journal = false;
            }

            if (shopOpen)
            {
                journal = false;
            }

            InitializeStyles();
            GUI.matrix = Matrix4x4.Scale(Vector3.one * Scale);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !settingsOpen && !IsNotificationOpen;
            DrawHeader();
            DrawJournalPanel();
            if (!Game.Placing.HasValue)
            {
                DrawLockedPlots();
                var visitorRect = GetFloatingLabelRect(Game.World.VisitorPosition + Vector3.up * 1.1f);
                if (Game.VisitorActive
                    && IsFloatingLabelVisible(visitorRect)
                    && DrawButton(visitorRect, I18n.Source("action.help_ladybug_prefix") + VillageState.VisitorAcorns, true, true))
                {
                    Game.HelpVisitor();
                }

                var lionRect = GetFloatingLabelRect(Game.World.AntLionPosition + Vector3.up * 1.35f, 190);
                if (Game.AntLionActive
                    && IsFloatingLabelVisible(lionRect)
                    && DrawButton(lionRect, I18n.Source("action.repel_ant_lion_prefix") + VillageState.AntLionAcorns, true, true))
                {
                    Game.RepelAntLion();
                }

                if (Game.Selected != null && !shopOpen && !journal && !settingsOpen)
                {
                    GUI.matrix = Matrix4x4.Scale(Vector3.one * InspectorScale);
                    DrawSelectionPanel();
                    GUI.matrix = Matrix4x4.Scale(Vector3.one * Scale);
                }
            }

            DrawSettingsButton();
            GUI.enabled = previousEnabled && !settingsOpen && !IsNotificationOpen;
            if (Game.Placing.HasValue)
            {
                if (Game.DraftLocked && Game.DraftVisible)
                {
                    DrawMoveControls();
                }
            }
            else
            {
                if (!shopOpen && DrawIconButton(new Rect(19, H - 111, 99, 96), shopIcon, I18n.Source("hud.shop"), !settingsOpen, true))
                {
                    shopOpen = true;
                }

                if (shopOpen)
                {
                    DrawShop();
                }
            }

            GUI.enabled = previousEnabled && !IsNotificationOpen;
            DrawSettingsPanel();
            GUI.enabled = previousEnabled;
            GUI.matrix = Matrix4x4.identity;
            DrawNotification();
        }

        /// <summary>Draws a readable, touch-sized modal above the game on desktop and mobile.</summary>
        private void DrawNotification()
        {
            if (notificationMessage == null)
            {
                return;
            }

            float scale = Mathf.Min(Mathf.Max(Scale, ScreenPixelRatio), Screen.width / 340f, Screen.height / 300f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            DrawBox(new Rect(0, 0, width, height), shadow, false);
            var panel = new Rect((width - 324) / 2, (height - 280) / 2, 324, 280);
            DrawBox(panel, hudFrame);
            DrawBox(new Rect(panel.x + 5, panel.y + 5, panel.width - 10, panel.height - 10), paper, false);
            DrawLabel(new Rect(panel.x + 20, panel.y + 20, 284, 36), notificationTitle, heading, ink);
            DrawLabel(new Rect(panel.x + 20, panel.y + 70, 284, 128), notificationMessage, body, ink);
            if (DrawButton(new Rect(panel.x + 20, panel.y + 212, 284, 48), I18n.Source("popup.ok"), true, true))
            {
                CloseNotification();
            }
            GUI.matrix = Matrix4x4.identity;
        }

        /// <summary>Updates drag scrolling once per frame and releases capture when the list changes.</summary>
        private void Update()
        {
            if (!shopOpen || settingsOpen || IsNotificationOpen || shopPage == 4 || Game == null
                || Game.IsWelcomeScreenOpen || Game.Placing.HasValue || dragShopPage != shopPage)
            {
                shopDrag.Cancel();
                dragShopPage = shopPage;
                return;
            }

            shopScroll[shopPage] = shopDrag.UpdateInput(shopScroll[shopPage], Scale);
        }

        /// <summary>
        /// Draws level, energy, population, currency, and weather status.
        /// </summary>
        void DrawHeader()
        {
            var s = Game.State;
            DrawHudBar(new Rect(23, 15, 238, 38),
                antIcon,
                I18n.Source("hud.level_prefix") + s.Level,
                s.XpIntoLevel + "/" + s.XpNeededForNextLevel,
                (float)s.XpIntoLevel / s.XpNeededForNextLevel);
            DrawHudBar(new Rect(23, 58, 238, 35),
                foodIcon,
                I18n.Source("hud.energy"),
                FormatEnergy(),
                (float)(s.energy / VillageState.MaxEnergy),
                "#EFC45F");
            DrawHudBar(new Rect(23, 100, 116, 30), antIcon, I18n.Source("hud.ants"), s.Capacity(false).ToString());
            DrawHudBar(new Rect(148, 100, 116, 30), beeIcon, I18n.Source("hud.bees"), s.Capacity(true).ToString());
            DrawHudBar(new Rect(W - 226, 18, 204, 43), acornIcon, I18n.Source("hud.acorns"), s.acorns.ToString());
            double phase = ColonyClimate.Phase(s.worldTime);
            int left = Mathf.CeilToInt((float)(s.IsRaining
                ? ColonyClimate.RainSlotSeconds - s.worldTime % ColonyClimate.RainSlotSeconds
                : (s.IsNight ? ColonyClimate.CycleSeconds : ColonyClimate.DaySeconds) - phase));
            DrawHudBar(new Rect(W - 226, 67, 204, 37),
                s.IsRaining ? rainIcon : s.IsNight ? moonIcon : sunIcon,
                s.IsRaining ? I18n.Source("hud.rain") : s.IsNight ? I18n.Source("hud.night") : I18n.Source("hud.day"),
                left / 60 + ":" + (left % 60).ToString("00"));
        }

        /// <summary>
        /// Draws the current tutorial objective and its reward action.
        /// </summary>
        void DrawJournalPanel()
        {
            if (DrawIconButton(new Rect(W - 91, 115, 73, 78), bookIcon, I18n.Source("hud.journal"), !settingsOpen, true))
            {
                journal = !journal;
            }

            if (!journal)
            {
                return;
            }

            var s = Game.State;
            var r = Journal;
            DrawBox(r, hudFrame);
            DrawBox(new Rect(r.x + 5, r.y + 5, r.width - 10, r.height - 10), paper, false);
            DrawLabel(new Rect(r.x + 16, r.y + 14, 220, 20), I18n.Source("journal.title"), tiny, muted);
            DrawLabel(new Rect(r.x + 16, r.y + 44, 211, 44), s.quest < 6 ? Quests[s.quest] : I18n.Source("journal.your_garden"), heading);
            DrawLabel(new Rect(r.x + 16, r.y + 96, 210, 45),
                s.quest < 6 ? QuestHints[s.quest] : I18n.Source("journal.completed_hint"),
                body,
                muted);
            if (s.QuestReady)
            {
                if (DrawButton(new Rect(r.x + 16, r.y + 157, 210, 29),
                    I18n.Source("action.claim_prefix") + VillageState.QuestAcorns + I18n.Source("unit.reward_separator") + VillageState.QuestXp + " XP",
                    true,
                    true))
                {
                    Game.Quest();
                }
            }
            else
            {
                DrawProgressBar(new Rect(r.x + 16, r.y + 162, 210, 4), s.quest / 6f, "#8EAB70");
                DrawLabel(new Rect(r.x + 16, r.y + 174, 210, 18), s.quest + I18n.Source("journal.progress_suffix"), small, muted);
            }
        }

        /// <summary>
        /// Draws the settings button and closes other panels when opening settings.
        /// </summary>
        private void DrawSettingsButton()
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = !IsNotificationOpen;
            bool clicked = DrawIconButton(new Rect(18, H - 208, 100, 78), gearIcon, I18n.Source("hud.settings"));
            GUI.enabled = previousEnabled;
            if (clicked)
            {
                settingsOpen = !settingsOpen;
                guestResetPending = false;
                if (settingsOpen)
                {
                    shopOpen = false;
                    journal = false;
                }
            }
        }

        /// <summary>
        /// Closes settings when Escape is pressed, consuming the action before gameplay cancellation.
        /// </summary>
        /// <returns>Whether an open settings panel was closed.</returns>
        public bool CloseSettings()
        {
            if (!settingsOpen)
            {
                return false;
            }

            settingsOpen = false;
            guestResetPending = false;
            return true;
        }

        /// <summary>
        /// Draws language, expandable audio, and account settings inside the modal panel.
        /// </summary>
        private void DrawSettingsPanel()
        {
            if (!settingsOpen)
            {
                return;
            }

            DrawBox(new Rect(0, 0, W, H), shadow, false);
            var panel = SettingsPanel;
            DrawBox(panel, hudFrame);
            DrawBox(new Rect(panel.x + 5, panel.y + 5, panel.width - 10, panel.height - 10), paper, false);
            DrawLabel(new Rect(panel.x + 20, panel.y + 17, 260, 30), I18n.Source("settings.title"), heading);
            if (DrawButton(new Rect(panel.xMax - 54, panel.y + 12, 38, 34), "×", true, false, true))
            {
                CloseSettings();
            }

            if (DrawButton(new Rect(panel.x + 20, panel.y + 64, panel.width - 40, 42), I18n.Source("settings.audio"), true, audioSettingsOpen))
            {
                audioSettingsOpen = !audioSettingsOpen;
            }

            string languageName = I18n.Text(I18n.Language == GameLanguage.Vietnamese
                ? "settings.vietnamese"
                : "settings.english");
            if (DrawButton(new Rect(panel.x + 20, panel.y + 116, panel.width - 40, 42),
                I18n.Format("settings.language_value", languageName), true, false, true))
            {
                Game.SetLanguage(I18n.Language == GameLanguage.Vietnamese
                    ? GameLanguage.English
                    : GameLanguage.Vietnamese);
            }

            if (!audioSettingsOpen)
            {
                DrawAccountSettings(panel, panel.y + 177);
                DrawCommunityLink(panel);
                return;
            }

            DrawLabel(new Rect(panel.x + 20, panel.y + 177, 230, 24), I18n.Source("settings.master_volume"), body);
            DrawLabel(new Rect(panel.xMax - 73, panel.y + 177, 53, 24), Mathf.RoundToInt(Game.MasterVolume * 100) + "%", body);
            float volume = GUI.HorizontalSlider(new Rect(panel.x + 20, panel.y + 214, panel.width - 40, 34),
                Game.MasterVolume, 0f, 1f, volumeSliderTrack, volumeSliderThumb);
            if (!Mathf.Approximately(volume, Game.MasterVolume))
            {
                Game.SetMasterVolume(volume);
            }
            DrawAccountSettings(panel, panel.y + 272);
            DrawCommunityLink(panel);
        }

        /// <summary>Shows automatic synchronization status and account actions only inside settings.</summary>
        private void DrawAccountSettings(Rect panel, float y)
        {
            VillageCloudSave cloud = Game.Cloud;
            if (cloud == null || !cloud.IsActive)
            {
                DrawGuestSettings(panel, y);
                return;
            }

            DrawLabel(new Rect(panel.x + 20, y, panel.width - 40, 24), I18n.Text("settings.account"), heading);
            DrawLabel(new Rect(panel.x + 20, y + 29, panel.width - 40, 24), I18n.Text("settings.autosave"), small);
            DrawLabel(new Rect(panel.x + 20, y + 56, panel.width - 40, 44), cloud.Status, small);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && cloud.CanManageAccount;
            float actionY = y + 96;
            if (cloud.HasConflict || cloud.NeedsSignIn)
            {
                string action = cloud.NeedsSignIn ? I18n.Text("cloud.sign_in_again") : I18n.Text("cloud.use_cloud");
                if (DrawButton(new Rect(panel.x + 20, actionY, panel.width - 40, 42), action, true, false, true))
                {
                    if (cloud.NeedsSignIn)
                    {
                        cloud.RequestSignInAgain();
                    }
                    else
                    {
                        cloud.RequestCloudRecovery();
                    }
                }
                actionY += 48;
            }

            if (DrawButton(new Rect(panel.x + 20, actionY, panel.width - 40, 42), I18n.Text("cloud.logout"), true, false, true))
            {
                cloud.RequestLogout();
            }
            GUI.enabled = previousEnabled;
        }

        /// <summary>Shows local autosave and an explicit, cancelable restart for guest progress.</summary>
        /// <param name="panel">The settings panel containing the controls.</param>
        /// <param name="y">The vertical start of the guest section.</param>
        private void DrawGuestSettings(Rect panel, float y)
        {
            DrawLabel(new Rect(panel.x + 20, y, panel.width - 40, 24), I18n.Text("settings.guest"), heading);
            if (!guestResetPending)
            {
                DrawLabel(new Rect(panel.x + 20, y + 29, panel.width - 40, 24), I18n.Text("settings.autosave"), small);
                if (DrawButton(new Rect(panel.x + 20, y + 60, panel.width - 40, 42),
                    I18n.Text("settings.guest_restart"), Game.CanStartNewGuestVillage, false, true))
                {
                    guestResetPending = true;
                }
                return;
            }

            DrawLabel(new Rect(panel.x + 20, y + 29, panel.width - 40, 58),
                I18n.Text("settings.guest_restart_confirm"), small);
            if (DrawButton(new Rect(panel.x + 20, y + 96, 174, 42), I18n.Text("action.cancel"), true, false, true))
            {
                guestResetPending = false;
            }
            if (DrawButton(new Rect(panel.x + 206, y + 96, 174, 42),
                I18n.Text("settings.guest_restart_start"), Game.CanStartNewGuestVillage, false, true))
            {
                if (Game.StartNewGuestVillage())
                {
                    guestResetPending = false;
                    CloseSettings();
                }
            }
        }

        /// <summary>Opens the community invitation in the browser when its Discord settings control is pressed.</summary>
        /// <param name="panel">The settings panel whose footer contains the community control.</param>
        private void DrawCommunityLink(Rect panel)
        {
            var bounds = new Rect(panel.x + 20, panel.yMax - 60, panel.width - 40, 42);
            if (DrawButton(bounds, "", true, false, true))
            {
                Application.OpenURL(CommunityInviteUrl);
            }

            Color previousColor = GUI.color;
            GUI.color = VillageWorld.ColorOf("#5865F2");
            GUI.DrawTexture(new Rect(bounds.x + 12, bounds.y + 5, 32, 32), discordIcon, ScaleMode.ScaleToFit, true);
            GUI.color = previousColor;
            DrawLabel(new Rect(bounds.x + 55, bounds.y + 7, bounds.width - 65, 28), I18n.Text("settings.discord_community"), body);
        }

        /// <summary>
        /// Draws the shop tabs and cards for buildings, decor, food, and permanent land expansions.
        /// </summary>
        void DrawShop()
        {
            var panel = ShopPanel;
            float x = panel.x, y = panel.y, width = panel.width;
            DrawBox(panel, hudFrame);
            DrawBox(new Rect(x + 6, y + 6, width - 12, panel.height - 12), paper, false);
            DrawBox(new Rect(x + 11, y + 10, width - 22, 56), hudDark, false);
            GUI.DrawTexture(new Rect(x + 17, y + 11, 46, 46), shopIcon, ScaleMode.ScaleToFit, true);
            DrawLabel(new Rect(x + 70, y + 16, 250, 28), I18n.Source("shop.title"), heading, Color.white);
            DrawLabel(new Rect(x + 70, y + 41, 250, 18), I18n.Source(shopPage == 4 ? "expansion.shop_hint" : "shop.hint"), small, VillageWorld.ColorOf("#E6D6B7"));
            if (DrawButton(new Rect(x + 340, y + 19, 95, 38), I18n.Source("shop.buildings"), true, shopPage == 0, shopPage != 0))
            {
                shopPage = 0;
            }

            if (DrawButton(new Rect(x + 441, y + 19, 95, 38), I18n.Source("shop.worksites"), true, shopPage == 1, shopPage != 1))
            {
                shopPage = 1;
            }

            if (DrawButton(new Rect(x + 542, y + 19, 95, 38), I18n.Source("shop.decorations"), true, shopPage == 2, shopPage != 2))
            {
                shopPage = 2;
            }

            if (DrawButton(new Rect(x + 643, y + 19, 95, 38), I18n.Source("shop.food"), true, shopPage == 3, shopPage != 3))
            {
                shopPage = 3;
            }

            if (DrawButton(new Rect(x + 744, y + 19, 95, 38), I18n.Source("shop.expansions"), true, shopPage == 4, shopPage != 4))
            {
                shopPage = 4;
                expansionMessage = null;
            }

            if (DrawButton(new Rect(x + 849, y + 19, 53, 38), "×", true, false, true))
            {
                shopOpen = false;
            }

            if (shopPage == 4)
            {
                DrawExpansionCards(panel);
                return;
            }

            var items = ShopGroups[shopPage];
            bool grid = shopPage <= 2;
            int columns = shopPage == 2 ? 4 : 3;
            float contentWidth = width - 42;
            float stride = grid ? contentWidth / columns : 224;
            float cardWidth = stride - 8, start = grid || items.Length >= 6 ? 4 : (contentWidth - items.Length * stride) / 2;
            float contentHeight = grid ? Mathf.CeilToInt(items.Length / (float)columns) * 190 + 4 : 234;
            Rect viewport = new Rect(x + 10, y + 72, width - 20, panel.height - 80);
            shopDrag.Configure(viewport, new Vector2(contentWidth, contentHeight));
            shopScroll[shopPage] = GUI.BeginScrollView(viewport,
                shopScroll[shopPage],
                new Rect(0, 0, contentWidth, contentHeight));
            for (int i = 0; i < items.Length; i++)
            {
                var kind = items[i];
                float cx = start + (grid ? i % columns : i) * stride, cy = 4 + (grid ? i / columns * 190 : 0);
                bool locked = !Game.State.IsUnlocked(kind);
                var card = new Rect(cx, cy, cardWidth, grid ? 182 : 226);
                DrawBox(card, hudFrame, false);
                DrawBox(new Rect(card.x + 4, card.y + 4, card.width - 8, card.height - 8),
                    !locked
                    && card.Contains(Event.current.mousePosition) ? honey : paper,
                    false);
                GUI.color = locked ? new Color(1, 1, 1, .38f) : Color.white;
                GUI.DrawTexture(new Rect(cx + 14, cy + 2, grid ? 93 : cardWidth - 35, grid ? 110 : 110),
                    Game.World.Thumbnail(kind),
                    ScaleMode.ScaleToFit,
                    true);
                GUI.color = Color.white;
                DrawLabel(new Rect(cx + (grid ? 108 : 12), cy + (grid ? 16 : 114), cardWidth - (grid ? 116 : 30), grid ? 30 : 24),
                    Names[(int)kind],
                    grid ? body : small);
                GUI.DrawTexture(new Rect(cx + (grid ? 108 : 11), cy + (grid ? 47 : 138), 20, 20), acornIcon, ScaleMode.ScaleToFit, true);
                DrawLabel(new Rect(cx + (grid ? 133 : 35), cy + (grid ? 48 : 139), cardWidth - (grid ? 143 : 54), 20),
                    locked ? (shopPage == 2 ? I18n.Source("label.level_prefix") : I18n.Source("shop.unlock_prefix")) + VillageState.UnlockLevel(kind) : VillageState.Cost(kind) + I18n.Source("unit.acorns_suffix"),
                    tiny,
                    muted);
                string capacity = VillageState.IsHome(kind)
                    ? I18n.Source("shop.tier_one_prefix") + VillageState.HomeCapacity(kind, 1) + I18n.Source("shop.tier_two_separator") + VillageState.HomeCapacity(kind, 2) + I18n.Source("shop.tier_three_separator") + VillageState.HomeCapacity(kind, 3) + " " + (VillageState.IsBee(kind) ? I18n.Source("unit.bees") : I18n.Source("unit.ants"))
                    : VillageState.IsWork(kind)
                        ? I18n.Source("shop.workers")
                        : VillageState.IsFood(kind)
                            ? "+" + FormatDuration(VillageState.FoodEnergy(kind)) + I18n.Source("unit.energy_suffix")
                            : kind == BuildingKind.Fence ? I18n.Source("shop.fence_hint") : I18n.Source("shop.decoration_hint");
                DrawLabel(new Rect(cx + (grid ? 108 : 12), cy + (grid ? 77 : 166), cardWidth - (grid ? 116 : 24), grid ? 34 : 20),
                    capacity,
                    small,
                    ink);
                if (DrawButton(new Rect(cx + 11, cy + (grid ? 139 : 194), cardWidth - 30, grid ? 32 : 25), locked ? I18n.Source("action.locked") : I18n.Source("action.select"), !locked, true))
                {
                    Game.Build(kind);
                    shopOpen = !Game.Placing.HasValue;
                }
            }

            GUI.EndScrollView();
        }

        /// <summary>
        /// Gets a camera-projected lock badge centered on an outer plot.
        /// </summary>
        private Rect GetPlotBadgeRect(LandPlot plot)
        {
            float x = (LandExpansion.InnerEdge + LandExpansion.OuterEdge) * .5f;
            var rect = GetFloatingLabelRect(new Vector3(plot == LandPlot.Housing ? -x : x, .3f, 2), 190);
            rect.y -= 46;
            rect.height = 122;
            return rect;
        }

        /// <summary>
        /// Draws the locked plot markers and opens the expansion shop when a marker is clicked.
        /// </summary>
        private void DrawLockedPlots()
        {
            if (shopOpen || settingsOpen || journal)
            {
                return;
            }

            foreach (LandPlot plot in System.Enum.GetValues(typeof(LandPlot)))
            {
                var rect = GetPlotBadgeRect(plot);
                if (Game.State.IsExpansionOwned(plot) || !IsFloatingLabelVisible(rect))
                {
                    continue;
                }

                DrawBox(rect, hudDark);
                GUI.DrawTexture(new Rect(rect.center.x - 27, rect.y + 6, 54, 54), lockIcon);
                DrawLabel(new Rect(rect.x + 10, rect.y + 62, 170, 22),
                    I18n.Source(plot == LandPlot.Housing ? "expansion.housing" : "expansion.garden"), center, Color.white);
                if (DrawButton(new Rect(rect.x + 10, rect.y + 89, 170, 25),
                    LandExpansion.Cost(plot).ToString("N0") + I18n.Source("unit.acorns_suffix"), true, true))
                {
                    shopPage = 4;
                    shopOpen = true;
                    expansionMessage = null;
                }
            }
        }

        /// <summary>
        /// Draws the two independent land purchases, affordability, and ownership feedback.
        /// </summary>
        private void DrawExpansionCards(Rect panel)
        {
            for (int i = 0; i < 2; i++)
            {
                var plot = (LandPlot)i;
                bool owned = Game.State.IsExpansionOwned(plot);
                int price = LandExpansion.Cost(plot);
                bool affordable = Game.State.acorns >= price;
                var card = new Rect(panel.x + 26 + i * 440, panel.y + 80, 430, 270);
                DrawBox(card, hudFrame, false);
                DrawBox(new Rect(card.x + 4, card.y + 4, card.width - 8, card.height - 8), paper, false);
                var preview = new Rect(card.x + 12, card.y + 12, card.width - 24, 92);
                DrawBox(preview, owned ? sage : hudDark, false);
                GUI.DrawTexture(new Rect(preview.x + 24, preview.y + 15, 62, 62), owned ? checkTexture : lockIcon,
                    ScaleMode.ScaleToFit, true);
                DrawLabel(new Rect(preview.x + 108, preview.y + 20, 280, 26),
                    I18n.Source(i == 0 ? "expansion.housing" : "expansion.garden"), heading,
                    owned ? ink : Color.white);
                DrawLabel(new Rect(preview.x + 108, preview.y + 52, 280, 25),
                    owned ? I18n.Source("expansion.owned") : I18n.Source("action.locked"), body,
                    owned ? ink : VillageWorld.ColorOf("#E6D6B7"));
                GUI.DrawTexture(new Rect(card.x + 18, card.y + 116, 24, 24), acornIcon);
                DrawLabel(new Rect(card.x + 50, card.y + 114, 350, 28),
                    price.ToString("N0") + I18n.Source("unit.acorns_suffix"), heading);
                DrawLabel(new Rect(card.x + 18, card.y + 150, card.width - 36, 44),
                    I18n.Source(i == 0 ? "expansion.housing_hint" : "expansion.garden_hint"), small);
                if (!owned && !affordable)
                {
                    DrawLabel(new Rect(card.x + 18, card.y + 199, card.width - 36, 20),
                        I18n.Format("expansion.need_more", (price - Game.State.acorns).ToString("N0")), small, muted);
                }

                if (DrawButton(new Rect(card.x + 18, card.y + 229, card.width - 36, 30),
                    I18n.Source(owned ? "expansion.owned" : "expansion.buy"), !owned && affordable, true))
                {
                    bool purchased = Game.BuyExpansion(plot, out string error);
                    expansionMessage = purchased ? I18n.Source("expansion.purchase_success") : error;
                }
            }

            if (!string.IsNullOrEmpty(expansionMessage))
            {
                DrawLabel(new Rect(panel.x + 26, panel.y + 356, panel.width - 52, 24), expansionMessage, small);
            }
        }

        /// <summary>
        /// Draws placement controls and sizes translated error messages to fit their complete text.
        /// </summary>
        void DrawMoveControls()
        {
            var r = GetMoveControlsRect();
            DrawBox(r, hudFrame);
            DrawBox(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), paper, false);
            if (DrawButton(new Rect(r.x + 5, r.y + 4, 61, 34), I18n.Source("action.cancel"), true, false, true))
            {
                Game.Cancel();
            }

            bool valid = !settingsOpen && Game.DraftVisible && Game.DraftLocked && Game.DraftError == null;
            if (DrawButton(new Rect(r.x + 72, r.y + 4, 64, 34), "", valid))
            {
                Game.ConfirmDraft();
            }

            GUI.color = valid ? Color.white : new Color(1, 1, 1, .5f);
            GUI.DrawTexture(new Rect(r.x + 94, r.y + 11, 20, 20), checkTexture);
            GUI.color = Color.white;
            if (DrawButton(new Rect(r.x + 142, r.y + 4, 63, 34), I18n.Source("action.rotate"), true, false, true))
            {
                Game.RotateDraft();
            }

            if (Game.Placing.HasValue && Game.DraftError != null)
            {
                string localizedError = I18n.Translate(Game.DraftError);
                float errorHeight = Mathf.Max(35, small.CalcHeight(new GUIContent(localizedError), 322) + 16);
                var error = new Rect(Mathf.Clamp(r.x - 65, 12, W - 340), r.y + 46, 340, errorHeight);
                DrawBox(error, paper);
                DrawLabel(new Rect(error.x + 9, error.y + 8, error.width - 18, error.height - 16), localizedError, small, VillageWorld.ColorOf("#AD624F"));
            }
        }

        /// <summary>
        /// Draws details and available actions for the selected building.
        /// </summary>
        void DrawSelectionPanel()
        {
            var b = Game.Selected;
            var r = InspectorLayout;
            DrawBox(r, hudFrame);
            DrawBox(new Rect(r.x + 5, r.y + 5, r.width - 10, r.height - 10), paper, false);
            float closeSize = InspectorCloseSize;
            DrawLabel(new Rect(r.x + 17, r.y + 22, r.width - closeSize - 38, 18),
                VillageState.IsHome(b.kind) ? I18n.Source("inspector.home_heading") : I18n.Source("inspector.garden_heading"),
                tiny,
                muted);
            if (DrawInspectorClose(new Rect(r.xMax - closeSize - 9, r.y + 7, closeSize, closeSize)))
            {
                Game.Cancel();
                return;
            }

            // Reserve a taller header while retaining the existing body and footer positions.
            r.y += InspectorHeaderInset;
            r.height -= InspectorHeaderInset;

            DrawBox(new Rect(r.x + 14, r.y + 43, 256, 93), sage, false);
            GUI.DrawTexture(new Rect(r.x + 13, r.y + 35, 110, 106), Game.World.Thumbnail(b.kind, b.tier), ScaleMode.ScaleToFit, true);
            DrawLabel(new Rect(r.x + 127, r.y + 45, 131, 50), Names[(int)b.kind], heading);
            DrawLabel(new Rect(r.x + 127, r.y + 96, 132, 28),
                (VillageState.IsHome(b.kind)
                    || VillageState.IsWork(b.kind))
                ? I18n.Source("label.level_prefix") + b.tier + " / 3   ·   " + b.rotation * 90 + "°"
                : Descriptions[(int)b.kind],
                small,
                muted);
            float x = r.x + 17, y = r.y + 151;
            if (VillageState.IsHome(b.kind))
            {
                DrawLabel(new Rect(x, y, 246, 25),
                    I18n.Source("inspector.capacity_prefix") + VillageState.HomeCapacity(b) + " " + (VillageState.IsBee(b.kind) ? I18n.Source("unit.bees") : I18n.Source("unit.ants")),
                    heading);
                DrawLabel(new Rect(x, y + 36, 247, 48),
                    b.upgradeRemaining > 0
                    ? I18n.Source("inspector.home_upgrade_prefix") + FormatDuration(b.upgradeRemaining) + "."
                    : b.tier < 3
                        ? I18n.Source("inspector.next_tier_prefix") + FormatDuration(VillageState.UpgradeTime(b)) + "."
                        : I18n.Source("inspector.max_capacity"),
                    body,
                    muted);
                if (b.upgradeRemaining > 0)
                {
                    DrawProgressBar(new Rect(x, y + 86, 247, 6), 1 - (float)(b.upgradeRemaining / b.upgradeDuration), "#E2AB57");
                }

                if (DrawButton(new Rect(x, y + 97, 250, 40),
                    b.upgradeRemaining > 0
                    ? I18n.Source("status.upgrading")
                    : b.tier < 3 ? I18n.Source("status.upgrade_prefix") + VillageState.UpgradeCost(b) + I18n.Source("unit.acorns_suffix") : I18n.Source("status.max_level"),
                    b.tier < 3
                    && b.upgradeRemaining <= 0,
                    true))
                {
                    Game.Upgrade();
                }
            }
            else if (VillageState.IsWork(b.kind))
            {
                bool bee = VillageState.IsBee(b.kind);
                DrawLabel(new Rect(x, y, 247, 22),
                    (bee ? I18n.Source("unit.bee_label") : I18n.Source("unit.ant_label")) + I18n.Source("inspector.free_prefix") + (Game.State.Capacity(bee) - Game.State.Busy(bee)) + "/" + Game.State.Capacity(bee) + I18n.Source("inspector.slots_prefix") + b.tier + "/3",
                    body,
                    muted);
                y += 31;
                if (b.upgradeRemaining > 0)
                {
                    DrawLabel(new Rect(x, y, 250, 44), I18n.Source("inspector.site_upgrade_prefix") + FormatDuration(b.upgradeRemaining), body);
                    DrawProgressBar(new Rect(x, y + 54, 250, 6), 1 - (float)(b.upgradeRemaining / b.upgradeDuration), "#E2AB57");
                }
                else if (b.phase == JobPhase.Idle)
                {
                    string[] names =
                    {
                        I18n.Source("job.short"),
                        I18n.Source("job.medium"),
                        I18n.Source("job.long")
                    };
                    for (int i = 0; i < 3; i++)
                    {
                        if (DrawButton(new Rect(x, y + i * 36, 250, 30),
                            I18n.Format("label.job_option", I18n.Translate(names[i]),
                                FormatDuration(VillageState.JobSeconds[i]), VillageState.JobReward(b.kind, b.tier, i))))
                        {
                            Game.StartJob(i);
                        }
                    }
                }
                else if (b.phase == JobPhase.Working)
                {
                    DrawLabel(new Rect(x, y, 250, 42),
                        Game.State.energy <= 0
                        ? I18n.Source("inspector.hungry_hint")
                        : Game.State.IsRaining
                            ? I18n.Source("inspector.rain_hint")
                            : I18n.Source("inspector.collecting_prefix") + FormatDuration(b.remaining),
                        body);
                    DrawProgressBar(new Rect(x, y + 53, 250, 6), 1 - (float)(b.remaining / b.duration), "#8EAB70");
                    DrawLabel(new Rect(x, y + 72, 250, 25), I18n.Source("inspector.reward_prefix") + b.reward + I18n.Source("unit.reward_separator") + b.xpReward + " XP", body, muted);
                }
                else if (b.phase == JobPhase.Ready)
                {
                    DrawLabel(new Rect(x, y, 250, 35), I18n.Source("inspector.harvest_ready"), body, muted);
                    if (DrawButton(new Rect(x, y + 43, 250, 40), I18n.Source("action.harvest_prefix") + b.reward + I18n.Source("unit.acorns_suffix"), true, true))
                    {
                        Game.Collect(b.id);
                    }
                }
                else
                {
                    DrawLabel(new Rect(x, y, 250, 44),
                        bee ? I18n.Source("inspector.water_hint") : I18n.Source("inspector.refill_hint"),
                        body,
                        muted);
                    if (DrawButton(new Rect(x, y + 55, 250, 38), (bee ? I18n.Source("action.water") : I18n.Source("action.refill")) + " • " + VillageState.RefillCost + I18n.Source("unit.acorns_suffix"), true, true))
                    {
                        Game.Refill();
                    }
                }

                if (DrawButton(new Rect(x, r.yMax - 116, 250, 30),
                    b.upgradeRemaining > 0
                    ? I18n.Source("status.upgrading")
                    : b.tier < 3
                        ? I18n.Source("action.upgrade_site_prefix") + VillageState.UpgradeCost(b) + I18n.Source("unit.cost_duration_separator") + FormatDuration(VillageState.UpgradeTime(b))
                        : I18n.Source("status.max_workers"),
                    b.tier < 3
                    && b.upgradeRemaining <= 0
                    && b.phase != JobPhase.Working
                    && b.phase != JobPhase.Ready,
                    true))
                {
                    Game.Upgrade();
                }
            }
            else if (VillageState.IsFood(b.kind))
            {
                DrawLabel(new Rect(x, y, 246, 28), I18n.Source("inspector.eating"), heading);
                DrawLabel(new Rect(x, y + 36, 246, 52),
                    I18n.Source("inspector.remaining_prefix") + FormatDuration(b.remaining) + " • +" + FormatDuration(b.foodEnergy) + I18n.Source("inspector.meal_energy_suffix"),
                    body,
                    muted);
                DrawProgressBar(new Rect(x, y + 94, 246, 6), 1 - (float)(b.remaining / b.duration), "#EFC45F");
                DrawLabel(new Rect(x, y + 117, 246, 49), I18n.Source("inspector.food_hint"), body, muted);
            }
            else
            {
                DrawLabel(new Rect(x, y, 246, 85),
                    b.kind == BuildingKind.Fence
                    ? I18n.Source("inspector.fence_hint")
                    : I18n.Source("inspector.decoration_hint"),
                    body,
                    muted);
            }
        }

        /// <summary>Draws a readable close glyph inside the inspector's physical touch target.</summary>
        private bool DrawInspectorClose(Rect bounds)
        {
            int previousFontSize = button.fontSize;
            button.fontSize = Mathf.Max(previousFontSize, Mathf.RoundToInt(28 * ScreenPixelRatio / InspectorScale));
            bool clicked = DrawButton(bounds, "×", true, false, true);
            button.fontSize = previousFontSize;
            return clicked;
        }

        /// <summary>
        /// Releases the generated interface textures.
        /// </summary>
        void OnDestroy()
        {
            foreach (var texture in new[]
            {
                paperTexture,
                forestTexture,
                honeyTexture,
                sageTexture,
                shadowTexture,
                checkTexture,
                frameTexture,
                darkTexture,
                goldBadge,
                greenBadge,
                antIcon,
                beeIcon,
                acornIcon,
                foodIcon,
                cookieIcon,
                shopIcon,
                bookIcon,
                gearIcon,
                discordIcon,
                sunIcon,
                moonIcon,
                rainIcon
            }

            )
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }
    }
}
