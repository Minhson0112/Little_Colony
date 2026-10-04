using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LittleColony.Editor
{
    /// <summary>
    /// Runs a batch-only Unity Play Mode regression in an empty scene without accessing player saves.
    /// </summary>
    [InitializeOnLoad]
    public static class RuntimeCreationChecks
    {
        private const string PendingKey = "LittleColony.RuntimeCheck.Pending";
        private const string StartedKey = "LittleColony.RuntimeCheck.Started";
        private const string ErrorsKey = "LittleColony.RuntimeCheck.Errors";
        private static VillageWorld world;
        private static VillageState state;
        private static int frames;
        private static bool stopping;

        /// <summary>
        /// Restores check callbacks after the domain reload associated with entering or leaving Play Mode.
        /// </summary>
        static RuntimeCreationChecks()
        {
            EditorApplication.playModeStateChanged += HandlePlayMode;
            EditorApplication.update += Tick;
            Application.logMessageReceived += CaptureError;
        }

        /// <summary>
        /// Schedules an isolated Play Mode check after editor startup and exits batch Unity when checks finish.
        /// </summary>
        public static void Run()
        {
            if (!Application.isBatchMode)
            {
                throw new InvalidOperationException("Run this check with Unity -batchmode, without -quit.");
            }

            EditorApplication.delayCall += BeginCheck;
        }

        /// <summary>Starts collecting runtime errors after the editor's startup callbacks have completed.</summary>
        private static void BeginCheck()
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(StartedKey, false);
            SessionState.SetString(ErrorsKey, "");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        /// <summary>
        /// Collects Unity-native initialization errors and exceptions throughout the test scene lifecycle.
        /// </summary>
        private static void CaptureError(string message, string stack, LogType type)
        {
            if (SessionState.GetBool(PendingKey, false)
                && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
            {
                SessionState.SetString(ErrorsKey,
                    SessionState.GetString(ErrorsKey, "") + message + "\n" + stack + "\n");
            }
        }

        /// <summary>
        /// Creates the real world in Play Mode and reports results after its scene has been torn down.
        /// </summary>
        private static void HandlePlayMode(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                return;
            }

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                SessionState.SetBool(StartedKey, true);
                try
                {
                    var root = new GameObject("Runtime creation check");
                    world = root.AddComponent<VillageWorld>();
                    world.Create();
                    state = VillageState.NewGame(100);
                    world.Sync(state);
                    if (root.GetComponent<BackyardEnvironment>() == null || world.ViewCamera == null
                        || root.GetComponentsInChildren<Renderer>().Length < 100)
                    {
                        throw new Exception("The world is missing its environment, camera, or models.");
                    }

                    VerifyWorkSiteFireflies();
                    VerifyVillageReplacement();
                    VerifyGardenCollectionAssets();
                    VerifyGardenAtmosphere();
                    VerifyCameraFraming();
                }
                catch (Exception error)
                {
                    CaptureError(error.ToString(), "", LogType.Exception);
                    Stop();
                }
            }
            else if (change == PlayModeStateChange.EnteredEditMode
                && SessionState.GetBool(StartedKey, false))
            {
                string errors = SessionState.GetString(ErrorsKey, "");
                bool passed = string.IsNullOrEmpty(errors);
                string report = passed
                    ? "PASS: Unity Play Mode creation, worksite fireflies, account village replacement, five garden decoration assets, garden pollen/night/rain behavior, camera framing after pan/zoom, 60 animation frames, tint restoration, and teardown."
                    : "FAIL:\n" + errors;
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/runtime-creation-result.txt", report);
                SessionState.SetBool(PendingKey, false);
                Debug.Log("LITTLE_COLONY_RUNTIME_CHECK: " + report);
                EditorApplication.Exit(passed ? 0 : 1);
            }
        }

        /// <summary>
        /// Checks all worksite models, live state transitions, motion, relocation, and tier replacement.
        /// </summary>
        private static void VerifyWorkSiteFireflies()
        {
            foreach (BuildingKind kind in Enum.GetValues(typeof(BuildingKind)))
            {
                if (!VillageState.IsWork(kind))
                {
                    continue;
                }

                var building = new Building { id = 10000 + (int)kind, kind = kind, x = 12, z = 6 };
                state.buildings.Add(building);
                world.Sync(state);
                CheckFireflies(building, JobPhase.Idle, true);
                var view = Array.Find(world.GetComponentsInChildren<BuildingView>(), item => item.id == building.id);
                var swarm = view.GetComponentInChildren<ParticleSystem>();
                var before = new ParticleSystem.Particle[6];
                swarm.GetParticles(before);
                world.Animate(state, 3, 0);
                var after = new ParticleSystem.Particle[6];
                swarm.GetParticles(after);
                if (Vector3.Distance(before[0].position, after[0].position) < .1f)
                {
                    throw new Exception("Worksite fireflies do not move: " + kind);
                }

                var material = view.GetComponentInChildren<MeshRenderer>().sharedMaterial;
                float minimum = float.MaxValue;
                float maximum = float.MinValue;
                for (int sample = 0; sample < 8; sample++)
                {
                    world.Animate(state, sample * .4f, 0);
                    float brightness = material.GetColor("_EmissionColor").maxColorComponent;
                    minimum = Mathf.Min(minimum, brightness);
                    maximum = Mathf.Max(maximum, brightness);
                }

                if (maximum - minimum < .02f)
                {
                    throw new Exception("The worksite highlight does not pulse: " + kind);
                }

                CheckFireflies(building, JobPhase.Working, false);
                state.energy = 0;
                CheckFireflies(building, JobPhase.Working, false);
                CheckFireflies(building, JobPhase.Ready, true);
                CheckFireflies(building, JobPhase.Empty, true);
                building.upgradeRemaining = 10;
                CheckFireflies(building, JobPhase.Idle, false);
                building.upgradeRemaining = 0;
                building.tier = 3;
                building.x = 15;
                world.Sync(state);
                CheckFireflies(building, JobPhase.Idle, true);
                view = Array.Find(world.GetComponentsInChildren<BuildingView>(), item => item.id == building.id);
                if (view.transform.position.x != 15)
                {
                    throw new Exception("The worksite indicator did not follow the relocated building.");
                }

                state.buildings.Remove(building);
                world.Sync(state);
            }

            state.energy = 240;
        }

        /// <summary>
        /// Verifies rendered particles after a live job transition without rebuilding the world.
        /// </summary>
        private static void CheckFireflies(Building building, JobPhase phase, bool expectedVisible)
        {
            building.phase = phase;
            world.Animate(state, 0, 0);
            var view = Array.Find(world.GetComponentsInChildren<BuildingView>(), item => item.id == building.id);
            var swarm = view.GetComponentInChildren<ParticleSystem>(true);
            if (swarm == null || swarm.gameObject.activeSelf != expectedVisible
                || swarm.particleCount != (expectedVisible ? 6 : 0))
            {
                throw new Exception("Incorrect firefly visibility: " + building.kind + ", " + phase);
            }

            var material = view.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
            var asset = Resources.Load<GameObject>("Models/" + VillageWorld.ModelName(building.kind, building.tier));
            var original = asset.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
            float glow = (material.GetColor("_EmissionColor") - original.GetColor("_EmissionColor")).maxColorComponent;
            if (material == original || !material.IsKeywordEnabled("_EMISSION")
                || (expectedVisible ? glow < .005f : Mathf.Abs(glow) > .001f))
            {
                throw new Exception("Incorrect or shared worksite highlight: " + building.kind + ", " + phase);
            }

            if (!expectedVisible && material.color != original.color)
            {
                throw new Exception("Worksite colors were not restored: " + building.kind);
            }
        }

        /// <summary>Ensures switching account snapshots rebuilds reused IDs and removes previous lanterns.</summary>
        private static void VerifyVillageReplacement()
        {
            const int reusedId = 29000;
            state.buildings.Add(new Building { id = reusedId, kind = BuildingKind.Lantern, tier = 1, x = 10, z = 8 });
            world.Sync(state);
            BuildingView previous = Array.Find(world.GetComponentsInChildren<BuildingView>(), view => view.id == reusedId);
            state = VillageState.NewGame(100);
            state.buildings.Add(new Building { id = reusedId, kind = BuildingKind.Pile, tier = 1, x = 10, z = 8 });
            world.ReplaceVillage(state);
            BuildingView current = Array.Find(world.GetComponentsInChildren<BuildingView>(), view => view.id == reusedId);
            if (previous == current || previous.gameObject.activeSelf || current == null
                || current.GetComponent<WorkSiteFireflies>() == null)
            {
                throw new Exception("Account switch retained a model from the previous village.");
            }

            foreach (Light light in world.GetComponentsInChildren<Light>())
            {
                if (light.type == LightType.Point)
                {
                    throw new Exception("Account switch retained a lantern from the previous village.");
                }
            }
        }

        /// <summary>Checks imported garden models and their rendered building views without using fallback geometry.</summary>
        private static void VerifyGardenCollectionAssets()
        {
            BuildingKind[] kinds = { BuildingKind.WoodenPlanter, BuildingKind.Birdhouse,
                BuildingKind.WindChime, BuildingKind.LeafFountain, BuildingKind.FlowerCart };
            foreach (BuildingKind kind in kinds)
            {
                var asset = Resources.Load<GameObject>("Models/" + VillageWorld.ModelName(kind, 1));
                if (asset == null || asset.GetComponentsInChildren<MeshRenderer>().Length < 2)
                {
                    throw new Exception("Missing garden decoration meshes: " + kind);
                }

                var building = new Building { id = 30000 + (int)kind, kind = kind, x = 7, z = 5 };
                state.buildings.Add(building);
                world.Sync(state);
                var view = Array.Find(world.GetComponentsInChildren<BuildingView>(), item => item.id == building.id);
                if (view == null || view.GetComponentsInChildren<MeshRenderer>().Length
                    != asset.GetComponentsInChildren<MeshRenderer>().Length)
                {
                    throw new Exception("Garden decoration did not instantiate its imported model: " + kind);
                }

                state.buildings.Remove(building);
                world.Sync(state);
            }
        }

        /// <summary>
        /// Exercises climate and resident animation while Unity runs the environment's Update callback.
        /// </summary>
        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying || stopping || world == null)
            {
                return;
            }

            try
            {
                float time = frames / 60f;
                world.ApplyClimate(state, time);
                world.Animate(state, time, 0);
                frames++;
                if (frames >= 60)
                {
                    state.housingExpansionOwned = true;
                    state.gardenExpansionOwned = true;
                    world.Sync(state);
                    Stop();
                }
            }
            catch (Exception error)
            {
                CaptureError(error.ToString(), "", LogType.Exception);
                Stop();
            }
        }

        /// <summary>Checks that ambient life moves, responds to weather, and adds no gameplay colliders.</summary>
        private static void VerifyGardenAtmosphere()
        {
            var atmosphere = world.GetComponent<GardenAtmosphere>();
            var particles = world.transform.Find("Drifting garden life");
            var flowers = world.transform.Find("Meadow wildflowers");
            if (atmosphere == null || particles == null || flowers == null
                || particles.GetComponent<Collider>() != null || flowers.GetComponent<Collider>() != null)
            {
                throw new Exception("Ambient life must exist without obstructing gameplay placement.");
            }

            Mesh particleMesh = particles.GetComponent<MeshFilter>().sharedMesh;
            atmosphere.Animate(world.ViewCamera, 0, 1, false);
            Vector3 original = particleMesh.vertices[0];
            Color daylightColor = particleMesh.colors[0];
            atmosphere.Animate(world.ViewCamera, 3, .22f, false);
            if (particleMesh.vertexCount != 192 || particleMesh.vertices[0] == original
                || particleMesh.colors[0] == daylightColor || !particles.gameObject.activeSelf)
            {
                throw new Exception("Ambient particles must drift and change their glow at night.");
            }

            atmosphere.Animate(world.ViewCamera, 4, 1, true);
            if (particles.gameObject.activeSelf)
            {
                throw new Exception("Pollen and fireflies must shelter during rain.");
            }

            atmosphere.Animate(world.ViewCamera, 5, 1, false);
            if (!particles.gameObject.activeSelf || flowers.GetComponent<MeshFilter>().sharedMesh.vertexCount == 0)
            {
                throw new Exception("Ambient life must resume when rain ends, with meadow blossoms retained.");
            }
        }

        /// <summary>Checks the actual scenery anchors after extreme pan and zoom gestures at common aspect ratios.</summary>
        private static void VerifyCameraFraming()
        {
            Transform trunk = Array.Find(world.GetComponentsInChildren<Transform>(),
                item => item.name == "Boundary fallen tree trunk");
            Transform upperRail = Array.Find(world.GetComponentsInChildren<Transform>(),
                item => item.name == "Fence crossbar" && item.position.y > 1);
            float originalAspect = world.ViewCamera.aspect;
            foreach (float aspect in new[] { 1280f / 720, 844f / 390, 390f / 844 })
            {
                world.ViewCamera.aspect = aspect;
                foreach (float direction in new[] { -1f, 1f })
                {
                    world.Navigate(100, new Vector2(0, direction * 100));
                    world.Navigate(-100, new Vector2(0, direction * 100));
                    float trunkY = world.ViewCamera.WorldToViewportPoint(trunk.position).y;
                    float fenceY = world.ViewCamera.WorldToViewportPoint(upperRail.position).y;
                    if (Mathf.Abs(trunkY) > .025f || fenceY < .95f || fenceY > 1.01f)
                    {
                        throw new Exception("Farthest zoom must frame half the foreground trunk and the rear fence after panning.");
                    }
                }
            }

            world.ViewCamera.aspect = originalAspect;
            world.ResetCamera();
        }

        /// <summary>
        /// Leaves Play Mode once, allowing lifecycle cleanup errors to be included in the report.
        /// </summary>
        private static void Stop()
        {
            stopping = true;
            EditorApplication.ExitPlaymode();
        }
    }
}
