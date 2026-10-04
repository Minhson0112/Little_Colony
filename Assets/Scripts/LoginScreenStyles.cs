using System;
using UnityEngine;

namespace LittleColony
{
    /// <summary>
    /// Owns the welcome screen's typography and small, procedurally drawn interface textures.
    /// </summary>
    internal sealed class LoginScreenStyles : IDisposable
    {
        private readonly Texture2D roundedTexture;
        private readonly Texture2D circleTexture;
        private readonly Texture2D leafTexture;
        private readonly Texture2D discordTexture;
        private readonly Texture2D veilTexture;

        /// <summary>
        /// Gets the reusable rounded panel style, tinted at draw time.
        /// </summary>
        public GUIStyle Panel { get; }

        /// <summary>
        /// Gets the rounded button style used for keyboard and pointer interactions.
        /// </summary>
        public GUIStyle Button { get; }

        /// <summary>
        /// Gets the large, wrapped garden headline.
        /// </summary>
        public GUIStyle Hero { get; }

        /// <summary>
        /// Gets the main card heading.
        /// </summary>
        public GUIStyle Heading { get; }

        /// <summary>
        /// Gets the card's readable supporting text.
        /// </summary>
        public GUIStyle Body { get; }

        /// <summary>
        /// Gets the larger introduction below the headline.
        /// </summary>
        public GUIStyle Introduction { get; }

        /// <summary>
        /// Gets compact annotations and the guest save explanation.
        /// </summary>
        public GUIStyle Small { get; }

        /// <summary>
        /// Gets spaced-section labels and the brand wordmark.
        /// </summary>
        public GUIStyle Eyebrow { get; }

        /// <summary>
        /// Gets the provider button text.
        /// </summary>
        public GUIStyle Action { get; }

        /// <summary>
        /// Gets a centered style for separators and short status labels.
        /// </summary>
        public GUIStyle Centered { get; }

        /// <summary>
        /// Gets the centered, full-size label for the primary guest action.
        /// </summary>
        public GUIStyle ButtonLabel { get; }

        /// <summary>
        /// Gets a soft mask that keeps the headline readable over the illustration.
        /// </summary>
        public Texture2D Veil => veilTexture;

        /// <summary>
        /// Gets the texture used for fireflies and circular icon backgrounds.
        /// </summary>
        public Texture2D Circle => circleTexture;

        /// <summary>
        /// Gets the original leaf emblem drawn for the game.
        /// </summary>
        public Texture2D Leaf => leafTexture;

        /// <summary>
        /// Gets a procedurally drawn Discord controller mark.
        /// </summary>
        public Texture2D Discord => discordTexture;

        /// <summary>
        /// Creates styles after Unity has initialized the current GUI skin.
        /// </summary>
        public LoginScreenStyles()
        {
            Font font = Resources.Load<Font>("Fonts/BeVietnamPro-Regular");
            roundedTexture = CreateTexture("Welcome rounded panel", RoundedCoverage);
            circleTexture = CreateTexture("Welcome circle", CircleCoverage);
            leafTexture = CreateTexture("Welcome leaf", LeafCoverage);
            discordTexture = CreateTexture("Welcome Discord mark", DiscordCoverage);
            veilTexture = CreateTexture("Welcome headline veil", VeilCoverage);
            Panel = new GUIStyle
            {
                normal = { background = roundedTexture },
                border = new RectOffset(12, 12, 12, 12)
            };
            Button = new GUIStyle(Panel)
            {
                hover = { background = roundedTexture },
                active = { background = roundedTexture },
                focused = { background = roundedTexture }
            };
            Hero = CreateTextStyle(font, 48, true);
            Heading = CreateTextStyle(font, 29, true);
            Body = CreateTextStyle(font, 14);
            Introduction = CreateTextStyle(font, 17);
            Small = CreateTextStyle(font, 11);
            Eyebrow = CreateTextStyle(font, 11, true);
            Action = CreateTextStyle(font, 14, true);
            Action.alignment = TextAnchor.MiddleLeft;
            Centered = CreateTextStyle(font, 11);
            Centered.alignment = TextAnchor.MiddleCenter;
            ButtonLabel = CreateTextStyle(font, 14, true);
            ButtonLabel.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// Creates transparent text styles without relying on platform-specific system fonts.
        /// </summary>
        private static GUIStyle CreateTextStyle(Font font, int size, bool bold = false)
        {
            return new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = size,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                wordWrap = true,
                richText = false,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { textColor = Color.white }
            };
        }

        /// <summary>
        /// Rasterizes an antialiased white mask that can be tinted by the layout.
        /// </summary>
        private static Texture2D CreateTexture(string name, Func<Vector2, float> coverage)
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2((x + .5f) / size, (y + .5f) / size);
                    pixels[y * size + x] = new Color(1, 1, 1, coverage(point));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>
        /// Calculates a rounded-square alpha mask with a one-pixel feathered edge.
        /// </summary>
        private static float RoundedCoverage(Vector2 point)
        {
            Vector2 nearest = new Vector2(Mathf.Clamp(point.x, .12f, .88f), Mathf.Clamp(point.y, .12f, .88f));
            return Mathf.Clamp01((.12f - Vector2.Distance(point, nearest)) * 96 + .5f);
        }

        /// <summary>
        /// Calculates a circular alpha mask for the brand and ambient motes.
        /// </summary>
        private static float CircleCoverage(Vector2 point)
        {
            return Mathf.Clamp01((.49f - Vector2.Distance(point, Vector2.one * .5f)) * 96 + .5f);
        }

        /// <summary>
        /// Draws a tilted leaf with a transparent central vein.
        /// </summary>
        private static float LeafCoverage(Vector2 point)
        {
            float along = (point.x + point.y - 1) * .7071f;
            float across = (point.y - point.x) * .7071f;
            float ellipse = along * along / .18f + across * across / .055f;
            float silhouette = Mathf.Clamp01((1 - ellipse) * 16);
            float vein = Mathf.Abs(across) < .019f && along > -.27f && along < .27f ? .15f : 1;
            return silhouette * vein;
        }

        /// <summary>
        /// Draws a compact controller silhouette and its two eye cutouts.
        /// </summary>
        private static float DiscordCoverage(Vector2 point)
        {
            float x = Mathf.Abs(point.x - .5f);
            float y = point.y;
            bool body = y > .25f && y < .69f && x < .34f - (y - .25f) * .17f;
            bool shoulders = y >= .65f && y < .76f && x > .12f && x < .26f;
            bool eyes = Vector2.Distance(point, new Vector2(.37f, .48f)) < .05f
                || Vector2.Distance(point, new Vector2(.63f, .48f)) < .05f;
            bool bottomCut = y < .31f && x < .16f;
            return (body || shoulders) && !eyes && !bottomCut ? 1 : 0;
        }

        /// <summary>
        /// Softens the upper-left artwork while leaving the village illustration visible below.
        /// </summary>
        private static float VeilCoverage(Vector2 point)
        {
            float vertical = Mathf.SmoothStep(0, 1, Mathf.Clamp01((point.y - .28f) / .40f));
            float horizontal = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((point.x - .35f) / .35f));
            return vertical * horizontal * .91f;
        }

        /// <summary>
        /// Releases only the generated textures; the shared font remains owned by Resources.
        /// </summary>
        public void Dispose()
        {
            UnityEngine.Object.Destroy(roundedTexture);
            UnityEngine.Object.Destroy(circleTexture);
            UnityEngine.Object.Destroy(leafTexture);
            UnityEngine.Object.Destroy(discordTexture);
            UnityEngine.Object.Destroy(veilTexture);
        }
    }
}
