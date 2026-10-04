using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LittleColony.Editor
{
    /// <summary>
    /// Exports WebGL page translations and versions build assets after a successful build.
    /// </summary>
    public sealed class WebLocalizationBuild : IPostprocessBuildWithReport
    {
        /// <summary>
        /// Gets the order in which this export runs among build callbacks.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Writes the loading page catalog alongside the WebGL index without modifying gameplay saves.
        /// </summary>
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
            {
                return;
            }

            var catalog = new WebCatalog
            {
                entries = I18n.Keys.Where(key => key.StartsWith("web.", StringComparison.Ordinal))
                    .Select(key => new WebEntry
                    {
                        key = key,
                        vi = I18n.Source(key),
                        en = I18n.Text(key, GameLanguage.English)
                    }).ToArray()
            };
            string output = report.summary.outputPath;
            string directory = Path.HasExtension(output) ? Path.GetDirectoryName(output) : output;
            File.WriteAllText(Path.Combine(directory, "i18n-web.json"), JsonUtility.ToJson(catalog, true), new UTF8Encoding(false));
            VersionBuildAssets(directory);
        }

        /// <summary>Changes asset URLs when their content changes so returning browsers load the current game.</summary>
        /// <param name="directory">The completed WebGL output directory containing index.html and Build assets.</param>
        public static void VersionBuildAssets(string directory)
        {
            string indexPath = Path.Combine(directory, "index.html");
            string html = File.ReadAllText(indexPath);
            foreach (string assetPath in Directory.GetFiles(Path.Combine(directory, "Build")))
            {
                using var hasher = SHA256.Create();
                using var stream = File.OpenRead(assetPath);
                string version = BitConverter.ToString(hasher.ComputeHash(stream), 0, 8)
                    .Replace("-", "").ToLowerInvariant();
                string assetUrl = "Build/" + Path.GetFileName(assetPath);
                html = html.Replace(assetUrl, assetUrl + "?v=" + version);
            }
            File.WriteAllText(indexPath, html, new UTF8Encoding(false));
        }

        /// <summary>
        /// Defines the JSON envelope consumed by the WebGL loading page.
        /// </summary>
        [Serializable]
        private sealed class WebCatalog
        {
            public WebEntry[] entries;
        }

        /// <summary>
        /// Defines one exported page translation in both supported languages.
        /// </summary>
        [Serializable]
        private sealed class WebEntry
        {
            public string key;
            public string vi;
            public string en;
        }
    }
}
