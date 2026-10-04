using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LittleColony.Editor
{
    /// <summary>
    /// Prepares the main Unity scene and configures WebGL builds.
    /// </summary>
    public static class ProjectSetup
    {
        /// <summary>Opens the saved gameplay scene and starts it, preserving unsaved scene edits first.</summary>
        [MenuItem("Little Colony/Play game")]
        public static void PlayGame()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (TryOpenMainScene())
            {
                EditorApplication.isPlaying = true;
            }
        }

        /// <summary>Opens and validates the existing entry scene without rebuilding or replacing its contents.</summary>
        [MenuItem("Little Colony/Open main scene")]
        public static void OpenMainScene()
        {
            TryOpenMainScene();
        }

        /// <summary>Validates and opens the entry scene only when pending edits can safely be left.</summary>
        /// <returns>Whether the gameplay scene is ready to run.</returns>
        private static bool TryOpenMainScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return false;
            }
            const string path = "Assets/Scenes/Main.unity";
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("Main scene is missing. Restore Assets/Scenes/Main.unity from source control.");
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (UnityEngine.Object.FindFirstObjectByType<VillageGame>() == null)
            {
                throw new InvalidOperationException("Main scene must contain an active VillageGame component.");
            }
            Debug.Log("LITTLE_COLONY_MAIN_SCENE_OK: VillageGame is ready; Play creates the camera and garden.");
            return true;
        }

        /// <summary>
        /// Creates and saves the initial scene, then applies project settings.
        /// </summary>
        [MenuItem("Little Colony/Prepare main scene")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Little Colony", typeof(VillageGame));
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true)
            };
            Configure();
            AssetDatabase.SaveAssets();
            Debug.Log("LITTLE_COLONY_PREPARE_OK");
        }

        /// <summary>
        /// Applies the existing player, rendering, and WebGL build settings.
        /// </summary>
        static void Configure()
        {
            PlayerSettings.companyName = "LittleColony";
            PlayerSettings.productName = I18n.Source("project.title");
            PlayerSettings.bundleVersion = "0.9.7";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.template = "PROJECT:LittleColony";
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 2;
            QualitySettings.shadows = ShadowQuality.All;
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Preserves Standard emission variants used by runtime worksite highlight materials in WebGL.
        /// </summary>
        private static void EnsureWorkSiteGlowVariants()
        {
            const string path = "Assets/Resources/WorkSiteGlowVariants.shadervariants";
            var variants = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);
            if (variants == null)
            {
                variants = new ShaderVariantCollection();
                AssetDatabase.CreateAsset(variants, path);
            }

            var shader = Shader.Find("Standard");
            variants.Add(new ShaderVariantCollection.ShaderVariant(shader,
                UnityEngine.Rendering.PassType.ForwardBase, "_EMISSION"));
            EditorUtility.SetDirty(variants);
            AssetDatabase.SaveAssetIfDirty(variants);
        }

        /// <summary>
        /// Builds the saved main scene and throws if Unity reports a failed build.
        /// </summary>
        [MenuItem("Little Colony/Build WebGL")]
        public static void BuildWebGL()
        {
            // Build the saved main scene, without replacing the user's open/unsaved scene.
            if (!File.Exists("Assets/Scenes/Main.unity"))
            {
                throw new Exception("Main scene missing. Use Prepare main scene first.");
            }

            Configure();
            EnsureWorkSiteGlowVariants();
            ConfigureLoginArtwork();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = "Builds/WebGL", target = BuildTarget.WebGL, options = BuildOptions.None });
            Debug.Log("LITTLE_COLONY_BUILD: " + report.summary.result + " / " + report.summary.totalSize + " bytes");
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("WebGL build failed: " + report.summary.result);
            }
        }

        /// <summary>Imports the welcome illustration as a regular color texture supported by WebGL browsers.</summary>
        private static void ConfigureLoginArtwork()
        {
            const string path = "Assets/Resources/UI/LoginGarden.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The welcome illustration is missing.");
            }

            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.ClearPlatformTextureSettings("WebGL");
            importer.SaveAndReimport();
        }
    }
}
