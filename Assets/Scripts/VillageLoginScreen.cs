using UnityEngine;

namespace LittleColony
{
    /// <summary>
    /// Presents a localized garden welcome screen while keeping account sign-in separate from guest play.
    /// </summary>
    public sealed class VillageLoginScreen : MonoBehaviour
    {
        private const float ExitDuration = .3f;
        private VillageGame game;
        private LoginScreenStyles styles;
        private Texture2D background;
        private float openedAt;
        private float dismissedAt = -1;
        private float previousVolume = 1;
        private string unavailableProvider;
        private readonly Color forest = new Color(.16f, .25f, .20f);
        private readonly Color muted = new Color(.40f, .46f, .38f);
        private readonly Color cream = new Color(1, .985f, .95f);

        /// <summary>
        /// Gets whether the welcome screen, including its exit transition, is visible.
        /// </summary>
        public bool IsVisible => dismissedAt < 0 || Time.unscaledTime - dismissedAt < ExitDuration;

        /// <summary>
        /// Attaches the screen to the existing game without changing the village's save data.
        /// </summary>
        /// <param name="villageGame">The game used for guest entry, language, and audio preferences.</param>
        public void Initialize(VillageGame villageGame)
        {
            game = villageGame;
            openedAt = Time.unscaledTime;
            previousVolume = game.MasterVolume > 0 ? game.MasterVolume : 1;
            background = Resources.Load<Texture2D>("UI/LoginGarden");
        }

        /// <summary>
        /// Opens the existing local village through a short fade without creating an authenticated session.
        /// </summary>
        public void ContinueAsGuest()
        {
            if (dismissedAt >= 0 || (game.Cloud != null && game.Cloud.IsTransitioning))
            {
                return;
            }

            dismissedAt = Time.unscaledTime;
        }

        /// <summary>
        /// Explains account sign-in availability without pretending to authenticate a player.
        /// </summary>
        /// <param name="provider">The provider selected by the player.</param>
        private void RequestSignIn(string provider)
        {
            bool started = game.Cloud != null && (provider == "Facebook"
                ? game.Cloud.SignInWithFacebook()
                : provider == "Discord" && game.Cloud.SignInWithDiscord());
            if (started)
            {
                unavailableProvider = null;
                return;
            }

            unavailableProvider = provider;
        }

        /// <summary>
        /// Draws a responsive welcome layout and restores shared GUI state after rendering.
        /// </summary>
        private void OnGUI()
        {
            if (game == null || !IsVisible)
            {
                return;
            }

            if (styles == null)
            {
                styles = new LoginScreenStyles();
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            bool previousEnabled = GUI.enabled;
            int previousDepth = GUI.depth;
            try
            {
                GUI.depth = -1000;
                GUI.matrix = Matrix4x4.identity;
                float opacity = dismissedAt < 0 ? 1 : 1 - (Time.unscaledTime - dismissedAt) / ExitDuration;
                GUI.color = new Color(1, 1, 1, opacity);
                DrawBackground();
                bool portrait = Screen.width < Screen.height;
                float scale = portrait
                    ? Mathf.Min(Screen.width / 480f, Screen.height / 850f)
                    : Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
                scale = Mathf.Max(scale, .1f);
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
                float width = Screen.width / scale;
                float height = Screen.height / scale;
                GUI.enabled = previousEnabled && dismissedAt < 0 && (game.Cloud == null || !game.Cloud.IsTransitioning);
                DrawAmbientMotes(width, height);
                DrawBrand(portrait ? 28 : 64, portrait ? 27 : 42);
                DrawPreferences(width, portrait);

                if (portrait)
                {
                    DrawPortraitLayout(width, height);
                }
                else
                {
                    DrawLandscapeLayout(width, height);
                }
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
                GUI.enabled = previousEnabled;
                GUI.depth = previousDepth;
            }
        }

        /// <summary>
        /// Covers the viewport with storybook artwork, with a solid fallback during asset loading.
        /// </summary>
        private void DrawBackground()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(.88f, .91f, .81f));
            if (background != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), background, ScaleMode.ScaleAndCrop);
            }

            TintTexture(new Rect(0, 0, Screen.width, Screen.height), styles.Veil, cream);
        }

        /// <summary>
        /// Draws the game's original leaf emblem and localized wordmark.
        /// </summary>
        private void DrawBrand(float x, float y)
        {
            TintTexture(new Rect(x, y, 36, 36), styles.Circle, forest);
            TintTexture(new Rect(x + 7, y + 7, 22, 22), styles.Leaf, cream);
            Label(new Rect(x + 49, y + 10, 220, 22), I18n.Text("login.brand"), styles.Eyebrow, forest);
        }

        /// <summary>
        /// Keeps language and sound preferences accessible before entering the village.
        /// </summary>
        private void DrawPreferences(float width, bool portrait)
        {
            float x = width - (portrait ? 205 : 233);
            float y = portrait ? 28 : 44;
            Rect language = new Rect(x, y, 104, 34);
            Panel(language, new Color(1, .99f, .95f, .85f));
            if (TextButton(new Rect(x + 3, y + 3, 47, 28), I18n.Text("login.english_short"),
                I18n.Language == GameLanguage.English ? forest : Color.clear,
                I18n.Language == GameLanguage.English ? cream : muted, "welcome-english"))
            {
                game.SetLanguage(GameLanguage.English);
            }

            if (TextButton(new Rect(x + 54, y + 3, 47, 28), I18n.Text("login.vietnamese_short"),
                I18n.Language == GameLanguage.Vietnamese ? forest : Color.clear,
                I18n.Language == GameLanguage.Vietnamese ? cream : muted, "welcome-vietnamese"))
            {
                game.SetLanguage(GameLanguage.Vietnamese);
            }

            string soundKey = game.MasterVolume > 0 ? "login.sound_on" : "login.sound_off";
            Rect sound = new Rect(x + 112, y, 70, 34);
            string soundText = I18n.Text(soundKey);
            if (TextButton(sound, soundText, new Color(1, .99f, .95f, .85f), forest, "welcome-sound"))
            {
                if (game.MasterVolume > 0)
                {
                    previousVolume = game.MasterVolume;
                    game.SetMasterVolume(0);
                }
                else
                {
                    game.SetMasterVolume(previousVolume);
                }
            }
        }

        /// <summary>
        /// Places the introduction beside the card on desktop and landscape screens.
        /// </summary>
        private void DrawLandscapeLayout(float width, float height)
        {
            Label(new Rect(68, 139, 530, 146), I18n.Text("login.hero_title"), styles.Hero, forest);
            Label(new Rect(72, 297, 410, 65), I18n.Text("login.hero_description"), styles.Introduction, muted);
            DrawTagline(new Rect(70, height - 85, 330, 37));
            float rise = Mathf.SmoothStep(16, 0, Mathf.Clamp01((Time.unscaledTime - openedAt) / .65f));
            Rect card = new Rect(width - 500, (height - 542) / 2 + rise, 416, 542);
            DrawCard(card);
            Label(new Rect(card.x, card.yMax + 22, card.width, 28),
                I18n.Text("login.footer"), styles.Centered, muted);
        }

        /// <summary>
        /// Stacks a compact headline and full-size touch targets on portrait screens.
        /// </summary>
        private void DrawPortraitLayout(float width, float height)
        {
            GUIStyle compactHero = styles.Heading;
            Label(new Rect(34, 103, width - 68, 96), I18n.Text("login.hero_title"), compactHero, forest);
            Rect card = new Rect((width - 416) / 2, Mathf.Max(220, (height - 542) / 2 + 65), 416, 542);
            DrawCard(card);
            Label(new Rect(card.x, card.yMax + 19, card.width, 24),
                I18n.Text("login.footer"), styles.Centered, forest);
        }

        /// <summary>
        /// Draws provider choices, their availability, and the working local guest entry.
        /// </summary>
        private void DrawCard(Rect card)
        {
            Panel(new Rect(card.x - 8, card.y + 14, card.width + 16, card.height + 8), new Color(.16f, .25f, .18f, .06f));
            Panel(new Rect(card.x - 3, card.y + 7, card.width + 6, card.height + 4), new Color(.16f, .25f, .18f, .09f));
            Panel(card, cream);
            float x = card.x + 36;
            float width = card.width - 72;
            TintTexture(new Rect(x, card.y + 34, 23, 23), styles.Leaf, new Color(.43f, .56f, .35f));
            Label(new Rect(x + 33, card.y + 41, width - 33, 22),
                I18n.Text("login.eyebrow"), styles.Eyebrow, muted);
            Label(new Rect(x, card.y + 85, width, 49), I18n.Text("login.welcome"), styles.Heading, forest);
            string introduction = string.IsNullOrEmpty(unavailableProvider)
                ? I18n.Text("login.subtitle")
                : I18n.Format("login.unavailable", unavailableProvider);
            if (game.Cloud != null && (game.Cloud.IsTransitioning || game.Cloud.HasLoadError))
            {
                introduction = game.Cloud.Status;
            }
            else if (game.Cloud != null && game.Cloud.IsAuthenticated)
            {
                introduction = I18n.Format("cloud.welcome", game.Cloud.DisplayName);
            }
            Label(new Rect(x, card.y + 143, width, 53), introduction, styles.Body,
                string.IsNullOrEmpty(unavailableProvider) ? muted : new Color(.58f, .36f, .17f));

            if (ProviderButton(new Rect(x, card.y + 208, width, 54), "Discord", "login.discord",
                new Color(.34f, .38f, .86f)))
            {
                RequestSignIn("Discord");
            }

            if (ProviderButton(new Rect(x, card.y + 275, width, 54), "Facebook", "login.facebook",
                new Color(.16f, .38f, .75f)))
            {
                RequestSignIn("Facebook");
            }

            Fill(new Rect(x, card.y + 355, width / 2 - 26, 1), new Color(.83f, .85f, .79f));
            Fill(new Rect(x + width / 2 + 26, card.y + 355, width / 2 - 26, 1), new Color(.83f, .85f, .79f));
            Label(new Rect(x, card.y + 341, width, 27), I18n.Text("login.or"), styles.Centered, muted);
            if (TextButton(new Rect(x, card.y + 382, width, 54), I18n.Text("login.guest"),
                forest, cream, "welcome-guest"))
            {
                ContinueAsGuest();
            }

            Label(new Rect(x, card.y + 453, width, 41), I18n.Text("login.guest_note"), styles.Centered, muted);
            Label(new Rect(x, card.y + 505, width, 20), I18n.Text("login.cloud_hint"), styles.Centered, muted);
        }

        /// <summary>
        /// Renders a provider button with a recognizable mark and a small availability label.
        /// </summary>
        private bool ProviderButton(Rect bounds, string provider, string textKey, Color color)
        {
            bool clicked = TextButton(bounds, "", color, Color.white, "welcome-" + provider);
            if (provider == "Discord")
            {
                TintTexture(new Rect(bounds.x + 17, bounds.y + 11, 32, 32), styles.Discord, Color.white);
            }
            else
            {
                Label(new Rect(bounds.x + 25, bounds.y + 7, 23, 37), "f", styles.Heading, Color.white);
            }

            bool available = game.Cloud != null && (game.Cloud.IsAuthenticated
                || (provider == "Facebook" && game.Cloud.FacebookEnabled)
                || (provider == "Discord" && game.Cloud.DiscordEnabled));
            string label = available && game.Cloud.IsAuthenticated ? I18n.Text("cloud.enter") : I18n.Text(textKey);
            Label(new Rect(bounds.x + 61, bounds.y + 2, bounds.width - 109, bounds.height - 4),
                label, styles.Action, Color.white);
            if (!available)
            {
                Label(new Rect(bounds.xMax - 52, bounds.y + 17, 44, 21),
                    I18n.Text("login.soon"), styles.Centered, new Color(1, 1, 1, .74f));
            }
            return clicked;
        }

        /// <summary>
        /// Draws an inset footer ribbon over the illustrated garden.
        /// </summary>
        private void DrawTagline(Rect bounds)
        {
            Panel(bounds, new Color(1, .99f, .94f, .89f));
            TintTexture(new Rect(bounds.x + 15, bounds.y + 10, 17, 17), styles.Leaf, forest);
            Label(new Rect(bounds.x + 42, bounds.y + 1, bounds.width - 50, bounds.height - 2),
                I18n.Text("login.tagline"), styles.Action, forest);
        }

        /// <summary>
        /// Adds a few slow fireflies without changing the garden simulation or random seed.
        /// </summary>
        private void DrawAmbientMotes(float width, float height)
        {
            float time = Time.unscaledTime;
            for (int index = 0; index < 12; index++)
            {
                float x = width * (.04f + (index * .073f) % .57f) + Mathf.Sin(time * .23f + index) * 9;
                float y = height * (.47f + (index * .117f) % .42f) + Mathf.Cos(time * .29f + index * 2) * 13;
                float glow = .3f + Mathf.Sin(time * .9f + index) * .18f;
                TintTexture(new Rect(x - 6, y - 6, 17, 17), styles.Circle, new Color(1, .88f, .53f, glow * .2f));
                TintTexture(new Rect(x, y, 4, 4), styles.Circle, new Color(1, .97f, .79f, glow));
            }
        }

        /// <summary>
        /// Draws an accessible button with hover, pressed, and keyboard focus feedback.
        /// </summary>
        private bool TextButton(Rect bounds, string text, Color backgroundColor, Color textColor, string controlName)
        {
            bool hovered = bounds.Contains(Event.current.mousePosition) && GUI.enabled;
            bool focused = GUI.GetNameOfFocusedControl() == controlName;
            if (focused)
            {
                Panel(new Rect(bounds.x - 3, bounds.y - 3, bounds.width + 6, bounds.height + 6), new Color(.80f, .65f, .33f));
            }

            Color previousColor = GUI.color;
            Color buttonColor = hovered ? Color.Lerp(backgroundColor, Color.white, .09f) : backgroundColor;
            GUI.color = new Color(buttonColor.r, buttonColor.g, buttonColor.b, previousColor.a * buttonColor.a);
            GUI.SetNextControlName(controlName);
            bool clicked = GUI.Button(bounds, GUIContent.none, styles.Button);
            GUI.color = previousColor;
            Label(bounds, text, bounds.height > 40 ? styles.ButtonLabel : styles.Centered, textColor);
            return clicked;
        }

        /// <summary>
        /// Tints a reusable rounded panel without leaking GUI color to other components.
        /// </summary>
        private void Panel(Rect bounds, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * previousColor.a);
            GUI.Box(bounds, GUIContent.none, styles.Panel);
            GUI.color = previousColor;
        }

        /// <summary>
        /// Draws a colored rectangular fill while preserving the current fade opacity.
        /// </summary>
        private void Fill(Rect bounds, Color color)
        {
            TintTexture(bounds, Texture2D.whiteTexture, color);
        }

        /// <summary>
        /// Tints a texture and restores the enclosing draw state's color.
        /// </summary>
        private void TintTexture(Rect bounds, Texture2D texture, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * previousColor.a);
            GUI.DrawTexture(bounds, texture);
            GUI.color = previousColor;
        }

        /// <summary>
        /// Draws already-localized text in a selected palette color.
        /// </summary>
        private void Label(Rect bounds, string text, GUIStyle style, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * previousColor.a);
            GUI.Label(bounds, text, style);
            GUI.color = previousColor;
        }

        /// <summary>
        /// Releases generated UI textures while leaving the shared illustration owned by Resources.
        /// </summary>
        private void OnDestroy()
        {
            styles?.Dispose();
        }
    }
}
