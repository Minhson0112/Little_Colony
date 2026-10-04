using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LittleColony.Editor
{
    /// <summary>
    /// Exports WebGL page translations from the shared C# catalog after a successful build.
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
