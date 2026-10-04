using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LittleColony.Editor
{
    // Allows the existing Editor to build instead of launching a second Editor on its project lock.
    // Only the literal commands 'build' and 'inspect' are accepted; no arbitrary execution.
    /// <summary>
    /// Processes explicit build and inspection requests in the active Unity Editor.
    /// </summary>
    [InitializeOnLoad]
    public static class BuildRequestPump
    {
        static double nextCheck;
        /// <summary>
        /// Registers the Editor update callback for queued requests.
        /// </summary>
        static BuildRequestPump()
        {
            EditorApplication.update += Update;
        }

        /// <summary>
        /// Polls for an allowed request when the Editor is idle, then records its result.
        /// </summary>
        static void Update()
        {
            if (Application.isBatchMode
                || EditorApplication.isCompiling
                || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.timeSinceStartup < nextCheck)
            {
                return;
            }

            nextCheck = EditorApplication.timeSinceStartup + 1;
            const string request = "Tools/editor-request.txt";
            if (!File.Exists(request))
            {
                return;
            }

            string command = File.ReadAllText(request).Trim();
            File.Delete(request);
            Directory.CreateDirectory("Logs");
            try
            {
                File.WriteAllText("Logs/editor-request-status.txt", "Running: " + command);
                if (command == "build")
                {
                    ProjectSetup.BuildWebGL();
                }
                else if (command == "inspect")
                {
                    InspectMaterials();
                }
                else
                {
                    throw new Exception("Unknown editor request");
                }

                File.WriteAllText("Logs/editor-request-status.txt", "Succeeded: " + command);
            }
            catch (Exception e)
            {
                File.WriteAllText("Logs/editor-request-status.txt", "Failed: " + e);
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Checks imported model materials and writes their shader and color information.
        /// </summary>
        static void InspectMaterials()
        {
            var report = new StringBuilder();
            foreach (string name in new[]
            {
                "AntHome2",
                "AntHome3",
                "BeeHome2",
                "BeeHome3",
                "GardenPot",
                "WateringCan",
                "Lantern",
                "Bench",
                "Birdbath",
                "FlowerArch",
                "MushroomPatch"
            }

            )
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Models/" + name + ".fbx");
                if (go == null)
                {
                    throw new Exception("Missing model " + name);
                }

                var seen = new System.Collections.Generic.HashSet<string>();
                foreach (var renderer in go.GetComponentsInChildren<Renderer>())
                {
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat == null)
                        {
                            throw new Exception("Missing material: " + name);
                        }

                        if (seen.Add(mat.name))
                        {
                            report.AppendLine(name + " | " + mat.name + " | " + mat.shader.name + " | " + ColorUtility.ToHtmlStringRGBA(mat.color));
                        }
                    }
                }
            }

            File.WriteAllText("Logs/material-inspection.txt", report.ToString());
        }
    }
}
