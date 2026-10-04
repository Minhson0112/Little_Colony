using System.Collections.Generic;
using UnityEngine;

namespace LittleColony
{
    /// <summary>
    /// Associates an interactive world building with its saved identifier.
    /// </summary>
    public sealed class BuildingView : MonoBehaviour
    {
        public int id;
    }

    /// <summary>
    /// Marks a collider hierarchy as an interactive ant lion encounter.
    /// </summary>
    public sealed class AntLionView : MonoBehaviour
    {
    }

    /// <summary>
    /// Marks a collider hierarchy as an interactive ladybug encounter.
    /// </summary>
    public sealed class VisitorView : MonoBehaviour
    {
    }

    /// <summary>
    /// Creates and animates village visuals, resident routes, previews, and interactive visitors.
    /// </summary>
    public sealed class VillageWorld : MonoBehaviour
    {
        const float AntSpeed = 2.1f;
        const float BeeSpeed = 3.2f;
        const float AntScale = .65f;
        const float BeeScale = .75f;
        const float DefaultCameraZoom = 9.7f;
        const float MinCameraZoom = 5.2f;
        const float MaxCameraZoom = 9.7f;
        public Camera ViewCamera { get; private set; }

        private readonly Dictionary<int, GameObject> buildings = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, int> tiers = new Dictionary<int, int>();
        private readonly Dictionary<int, List<Resident>> residents = new Dictionary<int, List<Resident>>();
        private readonly Dictionary<int, Construction> construction = new Dictionary<int, Construction>();
        private readonly Dictionary<int, Light> lanternLights = new Dictionary<int, Light>();
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private readonly Dictionary<string, RenderTexture> thumbnails = new Dictionary<string, RenderTexture>();
        private VillageState currentState;
        private readonly List<Building> antJobs = new List<Building>();
        private readonly List<Building> beeJobs = new List<Building>();
        private Light sun;
        private Mesh rainMesh;
        private GameObject rainObject;
        private readonly List<Vector3> rainVertices = new List<Vector3>();
        private readonly Vector3[] rainSeeds = new Vector3[180];
        public bool IsShelterWeather { get; private set; }

        private GameObject antLion;
        private Transform antLionBody;
        private readonly List<Transform> antLionJaws = new List<Transform>();
        private readonly List<Transform> antLionDust = new List<Transform>();
        private float antLionBorn;
        private float antLionRepelled = -1;
        private Vector3 antLionPosition = new Vector3(-3, .2f, 3);
        public Vector3 AntLionPosition => antLionPosition;
        public bool AntLionLeaving => antLion != null && antLionRepelled >= 0;

        private GameObject visitor;
        private GameObject ghost;
        private string ghostName;
        private Material ghostMaterial;
        private LineRenderer marker;
        private LineRenderer selection;
        private Vector3 cameraTarget = new Vector3(0, 0, 2.1f);
        private readonly Vector3 cameraOffset = new Vector3(0, 18, -19);
        private Vector3 visitorPosition = new Vector3(3, .2f, -3.5f);
        public Vector3 VisitorPosition => visitorPosition;
        public bool VisitorLeaving => visitor != null && visitorRescuedAt >= 0;

        private Transform visitorBody;
        private readonly List<Transform> visitorLegs = new List<Transform>();
        private float visitorRescuedAt = -1;
        private float visitorHeading;
        /// <summary>
        /// Stores the transforms and attachment data of one articulated ant leg.
        /// </summary>
        sealed class Leg
        {
            public Transform upper;
            public Transform lower;
            public Vector3 hip;
            public float side;
            public int row;
        }

        /// <summary>
        /// Stores visual parts, routes, and transient behavior for one rendered resident.
        /// </summary>
        sealed class Resident
        {
            public Transform root;
            public Transform[] parts;
            public Quaternion[] rotations;
            public GameObject cargo;
            public bool bee;
            public Leg[] legs;
            public List<Vector3> route = new List<Vector3>();
            public int routeIndex;
            public float pauseUntil;
            public bool sheltering;
            public bool exiting;
            public float gait;
            [System.NonSerialized]
            public int workJobId;
            [System.NonSerialized]
            public int workStage;
            [System.NonSerialized]
            public int mealId;
        }

        /// <summary>
        /// Stores the visual objects used by a building upgrade animation.
        /// </summary>
        sealed class Construction
        {
            public GameObject root;
            public Transform beetle;
            public Transform hammer;
        }

        /// <summary>
        /// Creates a colored primitive attached to a construction or visitor model.
        /// </summary>
        GameObject CreateSitePart(Transform parent, string name, PrimitiveType type, Vector3 local, Vector3 size, string color)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = local;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = Material(color);
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            return part;
        }

        /// <summary>
        /// Builds the upgrade scaffold and animated beetle for a building.
        /// </summary>
        Construction CreateConstruction(Building b)
        {
            var root = new GameObject(I18n.Source("object.upgrade_beetle"));
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(b.x, .18f, b.z);
            CreateSitePart(root.transform,
                I18n.Source("object.worksite_base"),
                PrimitiveType.Cylinder,
                new Vector3(0, .06f, 0),
                new Vector3(.92f, .08f, .92f),
                "#A77C53");
            for (int side = -1; side <= 1; side += 2)
            {
                CreateSitePart(root.transform,
                    I18n.Source("object.scaffold_post"),
                    PrimitiveType.Cylinder,
                    new Vector3(side * .66f, .68f, -.56f),
                    new Vector3(.065f, .7f, .065f),
                    "#8C6040");
                CreateSitePart(root.transform,
                    I18n.Source("object.scaffold_post"),
                    PrimitiveType.Cylinder,
                    new Vector3(side * .66f, .68f, .56f),
                    new Vector3(.065f, .7f, .065f),
                    "#8C6040");
            }

            CreateSitePart(root.transform,
                I18n.Source("object.worksite_roof"),
                PrimitiveType.Cube,
                new Vector3(0, 1.39f, 0),
                new Vector3(1.5f, .08f, 1.37f),
                "#87A65B");
            var bug = new GameObject(I18n.Source("object.working_beetle"));
            bug.transform.SetParent(root.transform, false);
            bug.transform.localPosition = new Vector3(0, .28f, -.1f);
            CreateSitePart(bug.transform, I18n.Source("object.body"), PrimitiveType.Sphere, new Vector3(0, .12f, 0), new Vector3(.53f, .28f, .6f), "#443F5D");
            CreateSitePart(bug.transform,
                I18n.Source("object.left_wing"),
                PrimitiveType.Sphere,
                new Vector3(-.14f, .23f, 0),
                new Vector3(.28f, .14f, .48f),
                "#C16A4E");
            CreateSitePart(bug.transform, I18n.Source("object.right_wing"), PrimitiveType.Sphere, new Vector3(.14f, .23f, 0), new Vector3(.28f, .14f, .48f), "#D47B4E");
            CreateSitePart(bug.transform, I18n.Source("object.head"), PrimitiveType.Sphere, new Vector3(0, .14f, -.33f), new Vector3(.31f, .27f, .28f), "#594663");
            for (int side = -1; side <= 1; side += 2)
            {
                CreateSitePart(bug.transform,
                    I18n.Source("object.eye"),
                    PrimitiveType.Sphere,
                    new Vector3(side * .105f, .2f, -.46f),
                    new Vector3(.055f, .065f, .045f),
                    "#F5EBC5");
                for (int row = 0; row < 3; row++)
                {
                    CreateSitePart(bug.transform,
                        I18n.Source("object.leg"),
                        PrimitiveType.Cube,
                        new Vector3(side * .32f, .025f, -.16f + row * .2f),
                        new Vector3(.21f, .045f, .045f),
                        "#443F5D");
                }
            }

            var hammer = new GameObject(I18n.Source("object.hammer"));
            hammer.transform.SetParent(bug.transform, false);
            hammer.transform.localPosition = new Vector3(.42f, .36f, -.28f);
            CreateSitePart(hammer.transform,
                I18n.Source("object.hammer_handle"),
                PrimitiveType.Cylinder,
                new Vector3(0, .18f, 0),
                new Vector3(.035f, .22f, .035f),
                "#B88852");
            CreateSitePart(hammer.transform, I18n.Source("object.hammer_head"), PrimitiveType.Cube, new Vector3(0, .4f, 0), new Vector3(.25f, .12f, .12f), "#71838A");
            return new Construction
            {
                root = root,
                beetle = bug.transform,
                hammer = hammer.transform
            };
        }

        /// <summary>
        /// Attaches a glowing lantern core and registers its night light.
        /// </summary>
        void AttachLanternLight(int id, GameObject obj)
        {
            // The imported FBX root has an axis conversion, so keep the light in world space.
            var root = new GameObject(I18n.Source("object.lantern_light"));
            root.transform.SetParent(transform);
            root.transform.position = obj.transform.position + Vector3.up * 1.35f;
            var orb = CreateSitePart(root.transform, I18n.Source("object.lantern_core"), PrimitiveType.Sphere, Vector3.zero, new Vector3(.24f, .24f, .24f), "#FFE7A0");
            var glow = new Material(Shader.Find("Standard"))
            {
                color = ColorOf("#FFE7A0")
            };
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", ColorOf("#FFD077") * 2.2f);
            orb.GetComponent<Renderer>().sharedMaterial = glow;
            var light = orb.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = ColorOf("#FFD18A");
            light.range = 4.2f;
            light.intensity = 0;
            light.shadows = LightShadows.None;
            lanternLights[id] = light;
        }

        /// <summary>
        /// Creates a colored cylindrical segment for an articulated ant leg.
        /// </summary>
        Transform CreateLegSegment(Transform parent, string name, string color)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.GetComponent<Renderer>().sharedMaterial = Material(color);
            var collider = piece.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            return piece.transform;
        }

        /// <summary>
        /// Positions and scales a leg segment between two local endpoints.
        /// </summary>
        static void SetLeg(Transform part, Vector3 a, Vector3 b, float radius)
        {
            part.localPosition = (a + b) * .5f;
            part.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            part.localScale = new Vector3(radius, (b - a).magnitude * .5f, radius);
        }

        /// <summary>
        /// Creates the six articulated legs used by an ant resident.
        /// </summary>
        Leg[] CreateLegs(Transform ant)
        {
            var legs = new Leg[6];
            int k = 0;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 3; row++)
                {
                    var leg = new Leg
                    {
                        side = side,
                        row = row,
                        hip = new Vector3(side * .13f, .32f, .19f - row * .2f)
                    };
                    leg.upper = CreateLegSegment(ant, I18n.Source("object.ant_upper_leg"), "#74391F");
                    leg.lower = CreateLegSegment(ant, I18n.Source("object.ant_lower_leg"), "#4C382E");
                    legs[k++] = leg;
                }
            }

            return legs;
        }

        /// <summary>
        /// Creates a colored primitive belonging to a resident cargo model.
        /// </summary>
        GameObject CreateCargoPart(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, string color)
        {
            var piece = GameObject.CreatePrimitive(type);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            piece.GetComponent<Renderer>().sharedMaterial = Material(color);
            var collider = piece.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            return piece;
        }

        /// <summary>
        /// Builds the carried log or honey bag and initially hides it.
        /// </summary>
        GameObject CreateCargo(Transform carrier, bool bee)
        {
            var cargo = new GameObject(bee ? I18n.Source("object.honey_bag") : I18n.Source("object.carried_wood"));
            cargo.transform.SetParent(carrier, false);
            cargo.transform.localPosition = bee ? new Vector3(0, -.18f, .48f) : new Vector3(0, .51f, .13f);
            if (bee)
            {
                CreateCargoPart(cargo.transform,
                    I18n.Source("object.bag_strap"),
                    PrimitiveType.Cylinder,
                    new Vector3(0, .17f, 0),
                    new Vector3(.035f, .20f, .035f),
                    "#8B653E");
                CreateCargoPart(cargo.transform,
                    I18n.Source("object.golden_honey_bag"),
                    PrimitiveType.Sphere,
                    new Vector3(0, -.07f, 0),
                    new Vector3(.27f, .29f, .22f),
                    "#E9AD49");
                CreateCargoPart(cargo.transform,
                    I18n.Source("object.bag_neck"),
                    PrimitiveType.Sphere,
                    new Vector3(0, .10f, 0),
                    new Vector3(.15f, .09f, .15f),
                    "#F6D98B");
            }
            else
            {
                var log = CreateCargoPart(cargo.transform, I18n.Source("object.long_log"), PrimitiveType.Cylinder, Vector3.zero, new Vector3(.19f, .56f, .19f), "#A87447");
                log.transform.localRotation = Quaternion.Euler(0, 0, 90);
                for (int side = -1; side <= 1; side += 2)
                {
                    var end = CreateCargoPart(cargo.transform,
                        I18n.Source("object.log_cut"),
                        PrimitiveType.Cylinder,
                        new Vector3(side * .55f, 0, 0),
                        new Vector3(.20f, .025f, .20f),
                        "#D4AF75");
                    end.transform.localRotation = Quaternion.Euler(0, 0, 90);
                }

                CreateCargoPart(cargo.transform, I18n.Source("object.tie"), PrimitiveType.Cube, new Vector3(0, .18f, 0), new Vector3(.12f, .06f, .41f), "#E4C492");
            }

            cargo.SetActive(false);
            return cargo;
        }

        /// <summary>
        /// Parses a hexadecimal color used by the village palette.
        /// </summary>
        public static Color ColorOf(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        /// <summary>
        /// Returns a cached low-gloss material for the supplied color.
        /// </summary>
        public Material Material(string hex)
        {
            if (materials.TryGetValue(hex, out var result))
            {
                return result;
            }

            result = new Material(Shader.Find("Standard"))
            {
                color = ColorOf(hex)
            };
            result.SetFloat("_Glossiness", .08f);
            materials[hex] = result;
            return result;
        }

        /// <summary>
        /// Creates a colored world primitive without a gameplay collider.
        /// </summary>
        public GameObject Primitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, string color)
        {
            var o = GameObject.CreatePrimitive(type);
            o.name = name;
            o.transform.SetParent(transform);
            o.transform.position = pos;
            o.transform.localScale = scale;
            o.GetComponent<Renderer>().sharedMaterial = Material(color);
            var c = o.GetComponent<Collider>();
            if (c != null)
            {
                c.enabled = false;
                Destroy(c);
            }

            return o;
        }

        /// <summary>
        /// Returns the resource name for a building kind and visual tier.
        /// </summary>
        public static string ModelName(BuildingKind kind, int tier = 1)
        {
            return kind.ToString() + ((VillageState.IsHome(kind) || VillageState.IsWork(kind)) && tier > 1 ? tier.ToString() : "");
        }

        /// <summary>
        /// Creates a fence post with four independently visible rail arms.
        /// </summary>
        GameObject CreateFenceModel(Vector3 pos, float scale)
        {
            var root = new GameObject("Fence");
            root.transform.SetParent(transform);
            root.transform.position = pos;
            root.transform.localScale = Vector3.one * scale;
            CreateSitePart(root.transform, I18n.Source("object.fence_post"), PrimitiveType.Cube, new Vector3(0, .5f, 0), new Vector3(.16f, 1f, .16f), "#A97649");
            CreateSitePart(root.transform, I18n.Source("object.post_cap"), PrimitiveType.Cube, new Vector3(0, 1.04f, 0), new Vector3(.22f, .10f, .22f), "#D6B77C");
            for (int side = 0; side < 4; side++)
            {
                var arm = new GameObject("FenceArm" + side);
                arm.transform.SetParent(root.transform, false);
                float dx = side == 0 ? 1 : side == 1 ? -1 : 0, dz = side == 2 ? 1 : side == 3 ? -1 : 0;
                for (int rail = 0; rail < 2; rail++)
                {
                    CreateSitePart(arm.transform,
                        I18n.Source("object.fence_rail"),
                        PrimitiveType.Cube,
                        new Vector3(dx * .25f, rail == 0 ? .39f : .76f, dz * .25f),
                        dx != 0 ? new Vector3(.52f, .10f, .11f) : new Vector3(.11f, .10f, .52f),
                        rail == 0 ? "#9A6D45" : "#B98A59");
                }

                arm.SetActive(side < 2);
            }

            return root;
        }

        /// <summary>
        /// Updates rail directions and lengths for neighboring fences and gate posts.
        /// </summary>
        void SetFenceConnections(GameObject root, int x, int z, VillageState state, int ignoreId = 0)
        {
            if (state == null)
            {
                return;
            }

            var connected = new bool[4];
            var gate = new bool[4];
            int count = 0;
            foreach (var b in state.buildings)
            {
                if (b.id == ignoreId || (b.kind != BuildingKind.Fence && b.kind != BuildingKind.FlowerArch))
                {
                    continue;
                }

                if (b.kind == BuildingKind.FlowerArch && !VillageState.FenceMeetsGate(x, z, b.x, b.z, b.rotation))
                {
                    continue;
                }

                int dx = b.x - x, dz = b.z - z;
                if (System.Math.Abs(dx) + System.Math.Abs(dz) != 1)
                {
                    continue;
                }

                Vector3 local = Quaternion.Inverse(root.transform.rotation) * new Vector3(dx, 0, dz);
                int side = Mathf.Abs(local.x) > .5f ? (local.x > 0 ? 0 : 1) : (local.z > 0 ? 2 : 3);
                if (!connected[side])
                {
                    connected[side] = true;
                    count++;
                }

                gate[side] = b.kind == BuildingKind.FlowerArch;
            }

            if (count == 0)
            {
                connected[0] = connected[1] = true;
            }
            else if (count == 1)
            {
                for (int side = 0; side < 4; side++)
                {
                    if (connected[side])
                    {
                        connected[side ^ 1] = true;
                        break;
                    }
                }
            }

            for (int side = 0; side < 4; side++)
            {
                var arm = root.transform.Find("FenceArm" + side);
                arm.gameObject.SetActive(connected[side]);
                // Gate posts stand 0.71 units from its center: stop rails at the post, leaving the opening clear.
                float length = gate[side] ? .30f : .52f, offset = gate[side] ? .15f : .25f;
                float dx = side == 0 ? 1 : side == 1 ? -1 : 0, dz = side == 2 ? 1 : side == 3 ? -1 : 0;
                foreach (Transform rail in arm)
                {
                    rail.localPosition = new Vector3(dx * offset, rail.localPosition.y, dz * offset);
                    rail.localScale = dx != 0 ? new Vector3(length, .10f, .11f) : new Vector3(.11f, .10f, length);
                }
            }
        }

        /// <summary>
        /// Instantiates a model resource or its primitive fallback, with special handling for fences.
        /// </summary>
        public GameObject Model(string name, Vector3 pos, float scale = 1)
        {
            if (name == "Fence")
            {
                return CreateFenceModel(pos, scale);
            }

            var asset = Resources.Load<GameObject>("Models/" + name);
            var o = asset != null
                ? Instantiate(asset, pos, Quaternion.identity, transform)
                : Primitive(name, PrimitiveType.Cylinder, pos, new Vector3(.7f, .35f, .7f), "#C88754");
            o.name = name;
            o.transform.localScale *= scale;
            foreach (var c in o.GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
                Destroy(c);
            }

            return o;
        }

        /// <summary>
        /// Creates an initially hidden world-space outline renderer.
        /// </summary>
        LineRenderer CreateOutline(string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var line = go.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = .045f;
            line.loop = true;
            line.positionCount = 4;
            line.useWorldSpace = true;
            line.enabled = false;
            line.numCornerVertices = 3;
            return line;
        }

        /// <summary>
        /// Positions a square outline around a supplied world position.
        /// </summary>
        void SetOutline(LineRenderer line, Vector3 p, Color color, float half = .98f)
        {
            line.enabled = true;
            line.startColor = line.endColor = color;
            p.y = .22f;
            line.SetPositions(new[] { p + new Vector3(-half, 0, -half), p + new Vector3(half, 0, -half), p + new Vector3(half, 0, half), p + new Vector3(-half, 0, half) });
        }

        /// <summary>
        /// Initializes the village camera, lighting, terrain, outlines, and weather visuals.
        /// </summary>
        public void Create()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.71f, .79f, .79f);
            RenderSettings.ambientEquatorColor = new Color(.54f, .58f, .44f);
            RenderSettings.ambientGroundColor = new Color(.34f, .39f, .28f);
            RenderSettings.fog = false;
            var cam = new GameObject("Backyard camera", typeof(Camera), typeof(AudioListener));
            ViewCamera = cam.GetComponent<Camera>();
            ViewCamera.orthographic = true;
            ViewCamera.orthographicSize = DefaultCameraZoom;
            ViewCamera.clearFlags = CameraClearFlags.SolidColor;
            ViewCamera.backgroundColor = ColorOf("#D2E0C7");
            ViewCamera.nearClipPlane = .1f;
            ViewCamera.farClipPlane = 150;
            ViewCamera.cullingMask = ~(1 << 31);
            ViewCamera.transform.position = cameraTarget + cameraOffset;
            ViewCamera.transform.LookAt(cameraTarget);
            sun = new GameObject("Garden daylight", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            sun.color = new Color(1, .94f, .81f);
            sun.intensity = .95f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .58f;
            sun.shadowBias = .05f;
            QualitySettings.shadowDistance = 65;
            CreateRain();
            gameObject.AddComponent<BackyardEnvironment>().Create(this);
            marker = CreateOutline("Placement footprint", ColorOf("#E6D985"));
            selection = CreateOutline("Selected building", ColorOf("#FAE6A4"));
            ghostMaterial = new Material(Resources.Load<Shader>("Shaders/GardenGhost"));
        }

        /// <summary>
        /// Creates the reusable dynamic mesh and material for rain streaks.
        /// </summary>
        void CreateRain()
        {
            var rng = new System.Random(7301);
            for (int i = 0; i < rainSeeds.Length; i++)
            {
                rainSeeds[i] = new Vector3((float)rng.NextDouble() - .5f, (float)rng.NextDouble(), (float)rng.NextDouble() - .5f);
            }

            rainObject = new GameObject("Moving rain streaks", typeof(MeshFilter), typeof(MeshRenderer));
            rainObject.transform.SetParent(transform);
            rainMesh = new Mesh
            {
                name = "Rain shower mesh"
            };
            rainMesh.MarkDynamic();
            var triangles = new int[rainSeeds.Length * 6];
            for (int i = 0; i < rainSeeds.Length; i++)
            {
                int v = i * 4, t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            for (int i = 0; i < rainSeeds.Length * 4; i++)
            {
                rainVertices.Add(Vector3.zero);
            }

            rainMesh.SetVertices(rainVertices);
            rainMesh.SetTriangles(triangles, 0);
            rainObject.GetComponent<MeshFilter>().sharedMesh = rainMesh;
            var material = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(.73f, .89f, .95f, .55f)
            };
            var renderer = rainObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rainObject.SetActive(false);
        }

        /// <summary>
        /// Applies saved weather and daylight to lighting, lanterns, and rain visibility.
        /// </summary>
        public void ApplyClimate(VillageState state, float time)
        {
            float day = ColonyClimate.Daylight(state.worldTime);
            float rain = state.IsRaining ? 1 : 0;
            IsShelterWeather = state.IsNight || state.IsRaining;
            sun.intensity = Mathf.Lerp(.23f, .98f, day) * (rain > 0 ? .63f : 1f);
            sun.color = Color.Lerp(ColorOf("#9DB4CF"), ColorOf("#FFE9BC"), day);
            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(18, 58, day), -32 + Mathf.Sin((float)state.worldTime * .04f) * 20, 0);
            RenderSettings.ambientSkyColor = Color.Lerp(ColorOf("#405974"), ColorOf("#B9C9C9"), day) * (rain > 0 ? .78f : 1f);
            RenderSettings.ambientEquatorColor = Color.Lerp(ColorOf("#4B5D60"), ColorOf("#8A9570"), day) * (rain > 0 ? .80f : 1f);
            RenderSettings.ambientGroundColor = Color.Lerp(ColorOf("#34444A"), ColorOf("#586448"), day);
            ViewCamera.backgroundColor = Color.Lerp(ColorOf("#526877"), ColorOf("#D2E0C7"), day);
            rainObject.SetActive(state.IsRaining);
            float glow = Mathf.Clamp01((.58f - day) * 3.2f);
            foreach (var light in lanternLights.Values)
            {
                if (light != null)
                {
                    light.enabled = glow > .02f;
                    light.intensity = 2.8f * glow;
                }
            }

            if (state.IsRaining)
            {
                AnimateRain(time);
            }
        }

        /// <summary>
        /// Moves the rain streak mesh around the camera target.
        /// </summary>
        void AnimateRain(float time)
        {
            for (int i = 0; i < rainSeeds.Length; i++)
            {
                var seed = rainSeeds[i];
                float x = cameraTarget.x + seed.x * 40, z = cameraTarget.z + seed.z * 33;
                float y = .7f + Mathf.Repeat(seed.y * 7 - time * 10, 6.8f);
                int v = i * 4;
                rainVertices[v] = new Vector3(x, y, z);
                rainVertices[v + 1] = new Vector3(x + .035f, y, z);
                rainVertices[v + 2] = new Vector3(x - .12f, y - .72f, z - .11f);
                rainVertices[v + 3] = new Vector3(x - .085f, y - .72f, z - .11f);
            }

            rainMesh.SetVertices(rainVertices);
            rainMesh.RecalculateBounds();
        }

        /// <summary>
        /// Synchronizes building models, worksite indicators, upgrades, residents, and fence connections with saved state.
        /// </summary>
        public void Sync(VillageState state)
        {
            currentState = state;
            GetComponent<BackyardEnvironment>().SyncExpansions(state);
            var removed = new List<int>();
            foreach (var id in buildings.Keys)
            {
                if (!state.buildings.Exists(b => b.id == id))
                {
                    removed.Add(id);
                }
            }

            foreach (var id in removed)
            {
                Destroy(buildings[id]);
                buildings.Remove(id);
                tiers.Remove(id);
                if (construction.TryGetValue(id, out var site))
                {
                    Destroy(site.root);
                    construction.Remove(id);
                }

                if (lanternLights.TryGetValue(id, out var light))
                {
                    if (light != null)
                    {
                        Destroy(light.transform.parent.gameObject);
                    }

                    lanternLights.Remove(id);
                }
            }

            foreach (var b in state.buildings)
            {
                if (!buildings.TryGetValue(b.id, out var obj) || tiers[b.id] != b.tier)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                        Destroy(obj);
                    }

                    obj = Model(ModelName(b.kind, b.tier), new Vector3(b.x, .17f, b.z));
                    var collider = obj.AddComponent<BoxCollider>();
                    collider.center = new Vector3(0, b.kind == BuildingKind.Fence ? .52f : .75f, 0);
                    collider.size = b.kind == BuildingKind.Fence ? new Vector3(.86f, 1.12f, .86f) : new Vector3(1.65f, 1.8f, 1.65f);
                    obj.AddComponent<BuildingView>().id = b.id;
                    buildings[b.id] = obj;
                    tiers[b.id] = b.tier;
                    if (VillageState.IsWork(b.kind))
                    {
                        obj.AddComponent<WorkSiteFireflies>().Initialize();
                    }

                    if (b.kind == BuildingKind.Lantern)
                    {
                        AttachLanternLight(b.id, obj);
                    }
                }

                obj.transform.position = new Vector3(b.x, .17f, b.z);
                obj.transform.rotation = Quaternion.Euler(0, b.rotation * 90 + (VillageState.IsHome(b.kind) ? 180 : 0), 0);
                if (obj.TryGetComponent<WorkSiteFireflies>(out var fireflies))
                {
                    fireflies.Animate(b, Time.time);
                }

                if (b.kind == BuildingKind.Lantern && lanternLights.TryGetValue(b.id, out var lanternLight) && lanternLight != null)
                {
                    lanternLight.transform.parent.position = obj.transform.position + Vector3.up * 1.35f;
                }

                bool upgrading = b.upgradeRemaining > 0;
                foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = !upgrading;
                }

                if (upgrading)
                {
                    if (!construction.TryGetValue(b.id, out var site))
                    {
                        site = CreateConstruction(b);
                        construction[b.id] = site;
                    }

                    site.root.transform.position = new Vector3(b.x, .18f, b.z);
                    site.root.transform.rotation = Quaternion.Euler(0, b.rotation * 90, 0);
                }
                else if (construction.TryGetValue(b.id, out var oldSite))
                {
                    Destroy(oldSite.root);
                    construction.Remove(b.id);
                }

                if (!VillageState.IsHome(b.kind))
                {
                    continue;
                }

                if (!residents.TryGetValue(b.id, out var list))
                {
                    list = new List<Resident>();
                    residents[b.id] = list;
                }

                while (list.Count < VillageState.HomeCapacity(b))
                {
                    bool bee = VillageState.IsBee(b.kind);
                    var bug = Model(bee ? "Bee" : "Ant", new Vector3(b.x, .22f, b.z - 1), bee ? BeeScale : AntScale);
                    var animated = new List<Transform>();
                    foreach (var child in bug.GetComponentsInChildren<Transform>())
                    {
                        if (bee)
                        {
                            if (child.name.Contains("Wing"))
                            {
                                animated.Add(child);
                            }
                        }
                        else if (child.name.Contains("leg"))
                        {
                            var renderer = child.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.enabled = false;
                            }
                        }
                    }

                    var rotations = new Quaternion[animated.Count];
                    for (int i = 0; i < rotations.Length; i++)
                    {
                        rotations[i] = animated[i].localRotation;
                    }

                    var cargo = CreateCargo(bug.transform, bee);
                    list.Add(new Resident { root = bug.transform, parts = animated.ToArray(), rotations = rotations, cargo = cargo, bee = bee, legs = bee ? null : CreateLegs(bug.transform) });
                }
            }

            foreach (var b in state.buildings)
            {
                if (b.kind == BuildingKind.Fence && buildings.TryGetValue(b.id, out var fence))
                {
                    SetFenceConnections(fence, b.x, b.z, state, b.id);
                }
            }
        }

        /// <summary>Rebuilds account-specific models and residents when switching between independently saved villages.</summary>
        public void ReplaceVillage(VillageState state)
        {
            foreach (GameObject building in buildings.Values)
            {
                building.SetActive(false);
                Destroy(building);
            }

            foreach (Construction site in construction.Values)
            {
                site.root.SetActive(false);
                Destroy(site.root);
            }

            foreach (List<Resident> household in residents.Values)
            {
                foreach (Resident resident in household)
                {
                    resident.root.gameObject.SetActive(false);
                    Destroy(resident.root.gameObject);
                }
            }

            buildings.Clear();
            tiers.Clear();
            construction.Clear();
            residents.Clear();
            foreach (Light lantern in lanternLights.Values)
            {
                if (lantern != null)
                {
                    lantern.transform.parent.gameObject.SetActive(false);
                    Destroy(lantern.transform.parent.gameObject);
                }
            }

            lanternLights.Clear();
            antJobs.Clear();
            beeJobs.Clear();
            Sync(state);
        }

        /// <summary>
        /// Checks walking-grid limits, the village divider, and building footprints.
        /// </summary>
        bool IsNavigationBlocked(float x, float z, VillageState state)
        {
            if (x < -VillageState.BuildHalfWidth || x > -2.0f
                || z < -VillageState.BuildHalfDepth || z > VillageState.BuildHalfDepth)
            {
                return true;
            }

            if (state.IsLandLocked(x, z)
                || GardenDivider.BlocksResidentAt(GardenDivider.VillageX, x, z))
            {
                return true;
            }

            foreach (var b in state.buildings)
            {
                if (VillageState.BlocksResident(b, x, z))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Samples a straight walking segment for blocked positions.
        /// </summary>
        bool IsWalkSegmentClear(Vector3 a, Vector3 b, VillageState state)
        {
            int steps = Mathf.CeilToInt(Vector3.Distance(a, b) / .22f);
            for (int i = 1; i < steps; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)steps);
                if (IsNavigationBlocked(p.x, p.z, state))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Finds a route on the half-unit walking grid and simplifies clear segments.
        /// </summary>
        List<Vector3> FindWalkRoute(Vector3 start, Vector3 goal, VillageState state, float height)
        {
            const int nx = (VillageState.BuildHalfWidth - 2) * 2 + 1;
            const int nz = VillageState.BuildHalfDepth * 4 + 1;
            const int count = nx * nz;
            /* <summary>
 * Maps a world position to a clamped walking-grid cell index.
 * </summary> */
            int WorldToCell(Vector3 p)
            {
                int ix = Mathf.Clamp(Mathf.RoundToInt((p.x + VillageState.BuildHalfWidth) / .5f), 0, nx - 1);
                int iz = Mathf.Clamp(Mathf.RoundToInt((p.z + VillageState.BuildHalfDepth) / .5f), 0, nz - 1);
                return iz * nx + ix;
            }

            /* <summary>
 * Maps a walking-grid cell index back to its world position.
 * </summary> */
            Vector3 CellToWorld(int cell) => new Vector3(
                -VillageState.BuildHalfWidth + (cell % nx) * .5f,
                height,
                -VillageState.BuildHalfDepth + (cell / nx) * .5f);
            int first = WorldToCell(start), last = WorldToCell(goal);
            var parent = new int[count];
            for (int i = 0; i < count; i++)
            {
                parent[i] = -1;
            }

            var queue = new Queue<int>();
            queue.Enqueue(first);
            parent[first] = first;
            while (queue.Count > 0 && parent[last] < 0)
            {
                int current = queue.Dequeue(), ix = current % nx, iz = current / nx;
                foreach (var next in new[]
                {
                    ix > 0 ? current - 1 : -1,
                    ix < nx - 1 ? current + 1 : -1,
                    iz > 0 ? current - nx : -1,
                    iz < nz - 1 ? current + nx : -1
                }

                )
                {
                    if (next < 0 || parent[next] >= 0)
                    {
                        continue;
                    }

                    var p = CellToWorld(next);
                    if (IsNavigationBlocked(p.x, p.z, state) && next != last)
                    {
                        continue;
                    }

                    parent[next] = current;
                    queue.Enqueue(next);
                }
            }

            var result = new List<Vector3>();
            if (parent[last] < 0)
            {
                return result;
            }

            var cells = new List<Vector3>();
            for (int node = last; node != first; node = parent[node])
            {
                cells.Add(CellToWorld(node));
            }

            cells.Reverse();
            Vector3 anchor = start;
            int cursor = 0;
            while (cursor < cells.Count)
            {
                int far = cursor;
                for (int j = cursor + 1; j < cells.Count; j++)
                {
                    if (IsWalkSegmentClear(anchor, cells[j], state))
                    {
                        far = j;
                    }
                }

                result.Add(cells[far]);
                anchor = cells[far];
                cursor = far + 1;
            }

            if (IsWalkSegmentClear(anchor, goal, state))
            {
                result.Add(goal);
            }

            return result;
        }

        /// <summary>
        /// Chooses a reachable random exploration destination near a resident home.
        /// </summary>
        void PlanWanderRoute(Resident resident, Building home, VillageState state, float time, float height)
        {
            resident.route.Clear();
            resident.routeIndex = 0;
            for (int attempt = 0; attempt < 16; attempt++)
            {
                float x = Random.Range(Mathf.Max(-VillageState.BuildHalfWidth + .8f, home.x - 7f), Mathf.Min(-2.5f, home.x + 7f));
                float z = Random.Range(Mathf.Max(-VillageState.BuildHalfDepth + 1, home.z - 6f), Mathf.Min(VillageState.BuildHalfDepth - 1, home.z + 6f));
                if (IsNavigationBlocked(x, z, state))
                {
                    continue;
                }

                var target = new Vector3(x, height, z);
                if (Vector3.Distance(resident.root.position, target) < 1.3f)
                {
                    continue;
                }

                var route = FindWalkRoute(resident.root.position, target, state, height);
                if (route.Count == 0)
                {
                    continue;
                }

                resident.route = route;
                return;
            }

            resident.pauseUntil = time + 2;
        }

        /// <summary>
        /// Moves a resident toward its next waypoint through the divider passages and, for ants, the bridge.
        /// </summary>
        bool FollowRoute(Resident resident, float speed)
        {
            if (resident.routeIndex >= resident.route.Count)
            {
                return true;
            }

            // An ant may receive a new route while eating ends, including mid-trip.
            // Never let a newly chosen waypoint carry it across the creek off the bridge.
            if (!resident.bee)
            {
                var from = resident.root.position;
                var next = resident.route[resident.routeIndex];
                bool crossesCreek = (from.x <= 0 && next.x > .6f) || (from.x >= 0 && next.x < -.6f);
                bool alreadyOnBridge = Mathf.Abs(from.z) < .85f && Mathf.Abs(next.z) < .85f;
                if (crossesCreek && !alreadyOnBridge)
                {
                    float y = from.y;
                    resident.route.Insert(resident.routeIndex, new Vector3(from.x <= 0 ? -1.35f : 1.35f, y, .34f));
                    resident.route.Insert(resident.routeIndex + 1, new Vector3(from.x <= 0 ? 1.35f : -1.35f, y, .34f));
                }
            }

            InsertDividerGateCrossing(resident);
            var target = resident.route[resident.routeIndex];
            MoveResident(resident, target, speed);
            if (Vector3.Distance(resident.root.position, target) < .11f)
            {
                resident.routeIndex++;
            }

            return resident.routeIndex >= resident.route.Count;
        }

        /// <summary>
        /// Routes active trips through the nearest stone passage instead of through either divider row.
        /// </summary>
        /// <remarks>
        /// This also covers replanned meal, work, return, and shelter trips. Bees descend to
        /// their usual flight height along the stone passage.
        /// </remarks>
        private void InsertDividerGateCrossing(Resident resident)
        {
            var from = resident.root.position;
            // Curved bee flights may have sampled intermediate points inside a divider row.
            // Keep the final destination for older saves, but bypass those intermediate samples.
            while (resident.routeIndex < resident.route.Count - 1)
            {
                var waypoint = resident.route[resident.routeIndex];
                if (!GardenDivider.BlocksResident(waypoint.x, waypoint.z))
                {
                    break;
                }

                resident.route.RemoveAt(resident.routeIndex);
            }

            var next = resident.route[resident.routeIndex];
            bool crossesGarden = GardenDivider.NeedsGateDetourAt(GardenDivider.X, from.x, from.z, next.x, next.z);
            bool crossesVillage = GardenDivider.NeedsGateDetourAt(GardenDivider.VillageX, from.x, from.z, next.x, next.z);
            if (!crossesGarden && !crossesVillage)
            {
                return;
            }

            float dividerX = crossesVillage && (!crossesGarden
                || Mathf.Abs(from.x - GardenDivider.VillageX) < Mathf.Abs(from.x - GardenDivider.X))
                ? GardenDivider.VillageX : GardenDivider.X;
            float side = from.x < dividerX ? -1 : 1;
            float height = resident.bee ? .72f : from.y;
            resident.route.Insert(resident.routeIndex, new Vector3(
                dividerX + side * GardenDivider.GateApproachDistance, height, GardenDivider.GateZ));
            resident.route.Insert(resident.routeIndex + 1, new Vector3(
                dividerX - side * GardenDivider.GateApproachDistance, height, GardenDivider.GateZ));
        }

        /// <summary>
        /// Moves and turns a resident at its fixed speed while advancing its leg gait.
        /// </summary>
        void MoveResident(Resident resident, Vector3 destination, float speed)
        {
            var before = resident.root.position;
            resident.root.position = Vector3.MoveTowards(before, destination, speed * Mathf.Min(Time.deltaTime, .05f));
            var direction = resident.root.position - before;
            direction.y = 0;
            if (direction.sqrMagnitude > .00001f)
            {
                resident.root.rotation = Quaternion.Slerp(resident.root.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 11);
            }

            if (!resident.bee && resident.legs != null)
            {
                resident.gait += Time.deltaTime * (direction.sqrMagnitude > .00001f ? 15 : 2);
            }
        }

        /// <summary>
        /// Updates the alternating ant leg joints for walking or idle movement.
        /// </summary>
        void AnimateLegs(Resident resident, bool walking)
        {
            if (resident.legs == null)
            {
                return;
            }

            float reach = walking ? 1 : .16f;
            foreach (var leg in resident.legs)
            {
                float phase = resident.gait + (leg.row + (leg.side > 0 ? 1 : 0)) % 2 * Mathf.PI;
                float stride = Mathf.Sin(phase) * .14f * reach;
                float lift = Mathf.Max(0, Mathf.Cos(phase)) * .15f * reach;
                var knee = leg.hip + new Vector3(leg.side * .22f, -.08f, stride * .45f);
                var foot = leg.hip + new Vector3(leg.side * .34f, -.28f + lift, stride);
                SetLeg(leg.upper, leg.hip, knee, .032f);
                SetLeg(leg.lower, knee, foot, .027f);
            }
        }

        /// <summary>
        /// Plans a resident route back inside its home during shelter weather.
        /// </summary>
        void BeginShelter(Resident resident, VillageState state, Vector3 outside, Vector3 entrance, Vector3 center, float height)
        {
            resident.route.Clear();
            resident.routeIndex = 0;
            resident.workJobId = 0;
            resident.workStage = 0;
            if (!resident.root.gameObject.activeSelf || Vector3.Distance(resident.root.position, center) < .18f)
            {
                resident.root.position = center;
                resident.sheltering = true;
                resident.exiting = false;
                return;
            }

            var start = resident.root.position;
            if (resident.bee)
            {
                resident.route.Add(Vector3.Lerp(start, outside, .5f) + Vector3.up * 1.3f);
                resident.route.Add(outside);
                resident.route.Add(entrance);
                resident.route.Add(center);
                resident.sheltering = true;
                resident.exiting = false;
                return;
            }

            if (start.x > 0.5f)
            {
                var right = new Vector3(1.35f, height, .34f);
                var left = new Vector3(-1.35f, height, .34f);
                resident.route.Add(right);
                resident.route.Add(left);
                start = left;
            }

            resident.route.AddRange(FindWalkRoute(start, outside, state, height));
            resident.route.Add(outside);
            resident.route.Add(entrance);
            resident.route.Add(center);
            resident.sheltering = true;
            resident.exiting = false;
        }

        /// <summary>
        /// Samples a curved bee flight between two positions.
        /// </summary>
        static Vector3 BeeFlight(Vector3 from, Vector3 to, float progress)
        {
            float t = Mathf.Clamp01(progress);
            return Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 1.35f);
        }

        /// <summary>
        /// Adds the sampled bee flight waypoints to a resident route.
        /// </summary>
        void AddBeeFlight(Resident resident, Vector3 from, Vector3 to)
        {
            for (int i = 1; i <= 8; i++)
            {
                resident.route.Add(BeeFlight(from, to, i / 8f));
            }
        }

        /// <summary>
        /// Plans travel from the resident current position to a worksite.
        /// </summary>
        void BeginWorkTrip(Resident resident, VillageState state, Vector3 outside, Vector3 work)
        {
            resident.route.Clear();
            resident.routeIndex = 0;
            if (resident.bee)
            {
                AddBeeFlight(resident, resident.root.position, work);
            }
            else if (resident.root.position.x > .5f)
            {
                resident.route.Add(work);
            }
            else
            {
                resident.route.AddRange(FindWalkRoute(resident.root.position, outside, state, outside.y));
                resident.route.Add(outside);
                resident.route.Add(new Vector3(-1.35f, outside.y, 0));
                resident.route.Add(new Vector3(1.35f, outside.y, 0));
                resident.route.Add(work);
            }

            resident.workStage = 0;
        }

        /// <summary>
        /// Plans the cargo return route through the home entrance.
        /// </summary>
        void BeginReturnTrip(Resident resident, Vector3 outside, Vector3 entrance, Vector3 center)
        {
            resident.route.Clear();
            resident.routeIndex = 0;
            if (resident.bee)
            {
                AddBeeFlight(resident, resident.root.position, outside);
            }
            else if (resident.root.position.x > .5f)
            {
                resident.route.Add(new Vector3(1.35f, outside.y, 0));
                resident.route.Add(new Vector3(-1.35f, outside.y, 0));
                resident.route.Add(outside);
            }
            else
            {
                resident.route.Add(outside);
            }

            resident.route.Add(entrance);
            resident.route.Add(center);
            resident.workStage = 2;
        }

        /// <summary>
        /// Flaps the bee wing parts around their original rotations.
        /// </summary>
        void AnimateWings(Resident resident, float time, float phase)
        {
            for (int j = 0; j < resident.parts.Length; j++)
            {
                resident.parts[j].localRotation = resident.rotations[j] * Quaternion.Euler(0, 0, Mathf.Sin(time * 39 + j * 2.1f + phase) * 34);
            }
        }

        /// <summary>
        /// Replaces a resident route with travel to the active meal.
        /// </summary>
        void BeginMealTrip(Resident resident, Building food, VillageState state, Vector3 outside, Vector3 target)
        {
            resident.mealId = food.id;
            resident.workJobId = 0;
            resident.workStage = 0;
            resident.sheltering = false;
            resident.exiting = false;
            resident.root.gameObject.SetActive(true);
            resident.route.Clear();
            resident.routeIndex = 0;
            var start = resident.root.position;
            if (resident.bee)
            {
                AddBeeFlight(resident, start, target);
            }
            else if (start.x < -.5f)
            {
                resident.route.AddRange(FindWalkRoute(start, outside, state, outside.y));
                resident.route.Add(outside);
                resident.route.Add(new Vector3(-1.35f, outside.y, 0));
                resident.route.Add(new Vector3(1.35f, outside.y, 0));
                resident.route.Add(target);
            }
            else
            {
                resident.route.Add(target);
            }
        }

        /// <summary>
        /// Updates construction, worksite fireflies, resident behavior, cargo, selection, and visitor animations.
        /// </summary>
        public void Animate(VillageState state, float time, int selectedId)
        {
            foreach (var site in construction.Values)
            {
                if (site.root == null)
                {
                    continue;
                }

                site.beetle.localPosition = new Vector3(0, .28f + Mathf.Abs(Mathf.Sin(time * 7)) * .09f, -.1f);
                site.beetle.localRotation = Quaternion.Euler(0, Mathf.Sin(time * 4) * 8, 0);
                site.hammer.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 10) * 48);
            }

            antJobs.Clear();
            beeJobs.Clear();
            var food = state.ActiveFood;
            foreach (var b in state.buildings)
            {
                if (b.phase == JobPhase.Working)
                {
                    for (int slot = 0; slot < b.tier; slot++)
                    {
                        (VillageState.IsBee(b.kind) ? beeJobs : antJobs).Add(b);
                    }
                }
            }

            int ai = 0, bi = 0;
            foreach (var home in state.buildings)
            {
                if (!residents.TryGetValue(home.id, out var bugs))
                {
                    continue;
                }

                for (int n = 0; n < bugs.Count; n++)
                {
                    var resident = bugs[n];
                    bool bee = resident.bee;
                    int index = bee ? bi++ : ai++;
                    var jobs = bee ? beeJobs : antJobs;
                    Vector3 door = Quaternion.Euler(0, home.rotation * 90, 0) * Vector3.back;
                    float baseY = bee ? .72f : .23f;
                    Vector3 center = new Vector3(home.x, baseY, home.z);
                    Vector3 entrance = center + door * .78f, outside = center + door * 1.33f;
                    float phase = home.id * .7f + n * 2.1f;
                    bool inside = false;
                    if (food != null)
                    {
                        if (!resident.root.gameObject.activeSelf)
                        {
                            resident.root.position = center;
                        }

                        var mealPoint = new Vector3(food.x + (n % 3 - 1) * .42f, baseY, food.z - .72f + (n / 3) * .36f);
                        if (resident.mealId != food.id)
                        {
                            BeginMealTrip(resident, food, state, outside, mealPoint);
                        }

                        bool eating = FollowRoute(resident, bee ? BeeSpeed : AntSpeed);
                        resident.cargo.SetActive(false);
                        if (eating)
                        {
                            resident.root.position = new Vector3(resident.root.position.x, baseY + Mathf.Sin(time * 9 + phase) * .035f, resident.root.position.z);
                        }

                        if (bee)
                        {
                            AnimateWings(resident, time, phase);
                        }

                        AnimateLegs(resident, !eating);
                        continue;
                    }

                    if (resident.mealId != 0)
                    {
                        resident.mealId = 0;
                        resident.route.Clear();
                        resident.routeIndex = 0;
                        resident.workJobId = 0;
                        resident.workStage = 0;
                        resident.pauseUntil = 0;
                        if (resident.root.position.x > .5f)
                        {
                            if (bee)
                            {
                                AddBeeFlight(resident, resident.root.position, outside);
                            }
                            else
                            {
                                var bridgeRight = new Vector3(1.35f, baseY, .34f);
                                var bridgeLeft = new Vector3(-1.35f, baseY, .34f);
                                resident.route.Add(bridgeRight);
                                resident.route.Add(bridgeLeft);
                                resident.route.AddRange(FindWalkRoute(bridgeLeft, outside, state, baseY));
                                resident.route.Add(outside);
                            }
                        }
                    }

                    bool assigned = index < jobs.Count && state.energy > 0;
                    bool shelter = state.IsRaining || (state.IsNight && !assigned);
                    if (shelter)
                    {
                        if (!resident.sheltering)
                        {
                            BeginShelter(resident, state, outside, entrance, center, baseY);
                        }

                        bool arrived = FollowRoute(resident, bee ? BeeSpeed : AntSpeed);
                        inside = arrived || Vector3.Distance(resident.root.position, center) < .18f;
                        resident.root.gameObject.SetActive(!inside);
                        resident.cargo.SetActive(false);
                        if (bee && !inside)
                        {
                            AnimateWings(resident, time, phase);
                        }

                        AnimateLegs(resident, !inside);
                        continue;
                    }

                    if (resident.sheltering)
                    {
                        bool wasInside = !resident.root.gameObject.activeSelf || Vector3.Distance(resident.root.position, center) < .18f;
                        resident.sheltering = false;
                        resident.exiting = wasInside;
                        resident.route.Clear();
                        resident.routeIndex = 0;
                        if (wasInside)
                        {
                            resident.root.position = center;
                        }

                        resident.root.gameObject.SetActive(true);
                    }

                    if (resident.exiting)
                    {
                        MoveResident(resident, outside, bee ? BeeSpeed : AntSpeed);
                        resident.cargo.SetActive(false);
                        if (bee)
                        {
                            AnimateWings(resident, time, phase);
                        }

                        AnimateLegs(resident, true);
                        if (Vector3.Distance(resident.root.position, outside) < .1f)
                        {
                            resident.exiting = false;
                        }

                        continue;
                    }

                    if (assigned)
                    {
                        var job = jobs[index];
                        if (resident.workJobId != job.id)
                        {
                            resident.workJobId = job.id;
                            resident.workStage = 0;
                            resident.route.Clear();
                            resident.routeIndex = 0;
                        }
                    }
                    else if (resident.workJobId != 0 && resident.workStage < 2)
                    {
                        BeginReturnTrip(resident, outside, entrance, center);
                    }

                    if (resident.workJobId != 0)
                    {
                        float speed = bee ? BeeSpeed : AntSpeed;
                        if (resident.workStage == 0)
                        {
                            if (resident.route.Count == 0)
                            {
                                var job = jobs[index];
                                int coworker = 0;
                                for (int j = 0; j < index; j++)
                                {
                                    if (jobs[j].id == job.id)
                                    {
                                        coworker++;
                                    }
                                }

                                float workOffset = (coworker - (job.tier - 1) * .5f) * .42f;
                                BeginWorkTrip(resident, state, outside, new Vector3(job.x + workOffset, baseY, job.z - .65f));
                            }

                            if (FollowRoute(resident, speed))
                            {
                                resident.workStage = 1;
                                resident.pauseUntil = time + 1.2f;
                                resident.route.Clear();
                                resident.routeIndex = 0;
                            }
                        }
                        else if (resident.workStage == 1)
                        {
                            if (time >= resident.pauseUntil)
                            {
                                BeginReturnTrip(resident, outside, entrance, center);
                            }
                        }
                        else if (resident.workStage == 2)
                        {
                            if (FollowRoute(resident, speed))
                            {
                                resident.workStage = 3;
                                resident.pauseUntil = time + .8f;
                            }
                        }
                        else if (resident.workStage == 3)
                        {
                            if (time >= resident.pauseUntil)
                            {
                                resident.workStage = 4;
                            }
                        }
                        else
                        {
                            MoveResident(resident, outside, speed);
                            if (Vector3.Distance(resident.root.position, outside) < .1f)
                            {
                                resident.workStage = 0;
                                resident.route.Clear();
                                resident.routeIndex = 0;
                                if (!assigned)
                                {
                                    resident.workJobId = 0;
                                }
                            }
                        }

                        inside = resident.workStage == 3;
                        resident.root.gameObject.SetActive(!inside);
                        resident.cargo.SetActive(resident.workStage == 2 && !inside);
                        resident.cargo.transform.localRotation = Quaternion.Euler(bee ? Mathf.Sin(time * 12 + phase) * 13 : 0,
                            0,
                            bee ? Mathf.Sin(time * 12 + phase) * 9 : Mathf.Sin(time * 18 + phase) * 3);
                        if (bee && !inside)
                        {
                            AnimateWings(resident, time, phase);
                        }

                        AnimateLegs(resident, resident.workStage == 0 || resident.workStage == 2 || resident.workStage == 4);
                        continue;
                    }

                    resident.root.gameObject.SetActive(true);
                    resident.cargo.SetActive(false);
                    if (resident.routeIndex >= resident.route.Count && time >= resident.pauseUntil)
                    {
                        PlanWanderRoute(resident, home, state, time, baseY);
                    }

                    if (resident.routeIndex < resident.route.Count)
                    {
                        if (FollowRoute(resident, bee ? BeeSpeed : AntSpeed))
                        {
                            resident.pauseUntil = time + Random.Range(.7f, 2.3f);
                        }
                    }

                    if (bee)
                    {
                        AnimateWings(resident, time, phase);
                    }

                    AnimateLegs(resident, resident.routeIndex < resident.route.Count);
                }
            }

            selection.enabled = false;
            foreach (var b in state.buildings)
            {
                if (b.id == selectedId)
                {
                    SetOutline(selection, new Vector3(b.x, .2f, b.z), ColorOf("#FAE6A4"));
                }

                if (buildings.TryGetValue(b.id, out var obj))
                {
                    if (obj.TryGetComponent<WorkSiteFireflies>(out var fireflies))
                    {
                        fireflies.Animate(b, time);
                    }

                    obj.transform.localScale = Vector3.one * (VillageState.IsFood(b.kind)
                        ? .72f + .28f * (float)(b.remaining / b.duration)
                        : b.phase == JobPhase.Ready ? 1 + Mathf.Sin(time * 3) * .018f : 1);
                }
            }

            AnimateVisitor(time);
            AnimateAntLion(time);
        }

        /// <summary>
        /// Displays and tints the building preview with its rotation and placement outline.
        /// </summary>
        public void ShowDraft(bool show, BuildingKind kind, int tier, Vector3 position, int rotation, bool valid, int ignoreId = 0)
        {
            marker.enabled = show;
            if (!show)
            {
                if (ghost != null)
                {
                    ghost.SetActive(false);
                }

                return;
            }

            string name = ModelName(kind, tier);
            if (ghost == null || ghostName != name)
            {
                if (ghost != null)
                {
                    ghost.SetActive(false);
                    Destroy(ghost);
                }

                ghost = Model(name, position);
                ghostName = name;
                foreach (var r in ghost.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = ghostMaterial;
                    }

                    r.sharedMaterials = mats;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            ghost.SetActive(true);
            position.y = .17f;
            ghost.transform.position = position;
            ghost.transform.rotation = Quaternion.Euler(0, rotation * 90 + (VillageState.IsHome(kind) ? 180 : 0), 0);
            if (kind == BuildingKind.Fence)
            {
                SetFenceConnections(ghost, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z), currentState, ignoreId);
            }

            Color tint = ColorOf(valid ? "#9FE4BD" : "#ED8877");
            tint.a = .5f;
            ghostMaterial.color = tint;
            SetOutline(marker, position, ColorOf(valid ? "#DBF6AE" : "#F99E88"));
        }

        /// <summary>
        /// Renders and caches a building preview using fixed lighting, restoring scene lighting afterward.
        /// </summary>
        public Texture Thumbnail(BuildingKind kind, int tier = 1)
        {
            string name = ModelName(kind, tier);
            if (thumbnails.TryGetValue(name, out var texture))
            {
                return texture;
            }

            var root = Model(name, new Vector3(100, 0, 0));
            if (VillageState.IsHome(kind))
            {
                root.transform.rotation = Quaternion.Euler(0, 180, 0);
            }

            foreach (var child in root.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = 31;
            }

            var bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }

            var cam = new GameObject("Icon camera", typeof(Camera)).GetComponent<Camera>();
            cam.enabled = false;
            cam.cullingMask = 1 << 31;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x * .9f, bounds.extents.z) * 1.4f;
            cam.transform.position = bounds.center + new Vector3(2, 2.2f, -4);
            cam.transform.LookAt(bounds.center);
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 20;
            texture = new RenderTexture(192, 160, 16, RenderTextureFormat.ARGB32);
            texture.antiAliasing = 2;
            var oldSunIntensity = sun.intensity;
            var oldSunColor = sun.color;
            var oldSunRotation = sun.transform.rotation;
            var oldSky = RenderSettings.ambientSkyColor;
            var oldEquator = RenderSettings.ambientEquatorColor;
            var oldGround = RenderSettings.ambientGroundColor;
            try
            {
                sun.intensity = .98f;
                sun.color = ColorOf("#FFE9BC");
                sun.transform.rotation = Quaternion.Euler(48, -32, 0);
                RenderSettings.ambientSkyColor = ColorOf("#B9C9C9");
                RenderSettings.ambientEquatorColor = ColorOf("#8A9570");
                RenderSettings.ambientGroundColor = ColorOf("#586448");
                cam.targetTexture = texture;
                cam.Render();
                cam.targetTexture = null;
            }
            finally
            {
                sun.intensity = oldSunIntensity;
                sun.color = oldSunColor;
                sun.transform.rotation = oldSunRotation;
                RenderSettings.ambientSkyColor = oldSky;
                RenderSettings.ambientEquatorColor = oldEquator;
                RenderSettings.ambientGroundColor = oldGround;
            }

            root.SetActive(false);
            Destroy(root);
            Destroy(cam.gameObject);
            thumbnails[name] = texture;
            return texture;
        }

        /// <summary>
        /// Spawns a ladybug on a random clear patch, preferring the visible area.
        /// </summary>
        public bool TryShowVisitor(VillageState state)
        {
            if (visitor != null)
            {
                return false;
            }

            var visible = new List<Vector3>();
            var available = new List<Vector3>();
            for (int x = -VillageState.BuildHalfWidth + 1; x < VillageState.BuildHalfWidth; x++)
            {
                for (int z = -VillageState.BuildHalfDepth + 1; z < VillageState.BuildHalfDepth; z++)
                {
                    if (!state.IsVisitorSpot(x, z))
                    {
                        continue;
                    }

                    var point = new Vector3(x, .2f, z);
                    if (Vector3.Distance(point, visitorPosition) < 2.5f
                        || (antLion != null
                            && Vector3.Distance(point, antLionPosition) < 3))
                    {
                        continue;
                    }

                    available.Add(point);
                    var screen = ViewCamera.WorldToViewportPoint(point);
                    if (screen.z > 0 && screen.x > .22f && screen.x < .74f && screen.y > .25f && screen.y < .75f)
                    {
                        visible.Add(point);
                    }
                }
            }

            var candidates = visible.Count > 0 ? visible : available;
            if (candidates.Count == 0)
            {
                return false;
            }

            visitorPosition = candidates[Random.Range(0, candidates.Count)];
            visitor = new GameObject(I18n.Source("object.ladybug"));
            visitor.transform.SetParent(transform);
            visitor.transform.position = visitorPosition;
            visitor.AddComponent<VisitorView>();
            var collider = visitor.AddComponent<SphereCollider>();
            collider.center = Vector3.up * .48f;
            collider.radius = .65f;
            visitorBody = new GameObject(I18n.Source("object.ladybug_body")).transform;
            visitorBody.SetParent(visitor.transform, false);
            var shell = Model("Ladybug", Vector3.zero, 1.35f);
            shell.transform.SetParent(visitorBody, false);
            shell.transform.localPosition = Vector3.down * .38f;
            visitorLegs.Clear();
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 3; row++)
                {
                    var leg = new GameObject(I18n.Source("object.ladybug_leg")).transform;
                    leg.SetParent(visitorBody, false);
                    leg.localPosition = new Vector3(side * .26f, -.17f, (row - 1) * .24f);
                    CreateSitePart(leg, I18n.Source("object.thigh"), PrimitiveType.Capsule, new Vector3(side * .13f, -.04f, 0), new Vector3(.065f, .16f, .065f), "#403329").transform.localRotation = Quaternion.Euler(0, 0, side * 65);
                    CreateSitePart(leg, I18n.Source("object.foot"), PrimitiveType.Capsule, new Vector3(side * .24f, -.16f, 0), new Vector3(.055f, .13f, .055f), "#403329");
                    visitorLegs.Add(leg);
                }
            }

            visitorHeading = Random.Range(0f, 360f);
            visitorRescuedAt = -1;
            AnimateVisitor(Time.time);
            return true;
        }

        /// <summary>
        /// Starts the ladybug recovery animation and disables further collider interactions.
        /// </summary>
        public void RescueVisitor()
        {
            if (visitor == null || visitorRescuedAt >= 0)
            {
                return;
            }

            visitorRescuedAt = Time.time;
            visitor.GetComponent<Collider>().enabled = false;
        }

        /// <summary>
        /// Animates the overturned ladybug legs, recovery roll, greeting, and disappearance.
        /// </summary>
        void AnimateVisitor(float time)
        {
            if (visitor == null)
            {
                return;
            }

            float elapsed = visitorRescuedAt < 0 ? 0 : time - visitorRescuedAt;
            if (visitorRescuedAt >= 0 && elapsed >= 2.4f)
            {
                Destroy(visitor);
                visitor = null;
                visitorBody = null;
                visitorLegs.Clear();
                return;
            }

            float roll = 180 + Mathf.Sin(time * 5) * 12;
            float height = .49f + Mathf.Sin(time * 7) * .025f;
            if (visitorRescuedAt >= 0)
            {
                float t = Mathf.Clamp01(elapsed / .85f);
                roll = Mathf.Lerp(180, 0, Mathf.SmoothStep(0, 1, t));
                height = .49f + Mathf.Sin(t * Mathf.PI) * .23f;
                float goodbye = Mathf.Clamp01((elapsed - 1.7f) / .7f);
                visitorBody.localScale = Vector3.one * (1 - goodbye);
                height += elapsed > .85f ? Mathf.Abs(Mathf.Sin((elapsed - .85f) * 7)) * .07f : 0;
            }

            visitorBody.localPosition = Vector3.up * height;
            visitorBody.localRotation = Quaternion.Euler(0, visitorHeading, roll);
            for (int i = 0; i < visitorLegs.Count; i++)
            {
                float speed = visitorRescuedAt < 0 ? 14 : 8;
                visitorLegs[i].localRotation = Quaternion.Euler(Mathf.Sin(time * speed + i * 1.7f) * 25, Mathf.Cos(time * speed + i) * 18, Mathf.Sin(time * speed + i) * 12);
            }
        }

        /// <summary>
        /// Builds an ant lion encounter on clear ground away from the active ladybug.
        /// </summary>
        public bool TryShowAntLion(VillageState state)
        {
            if (antLion != null)
            {
                return false;
            }

            var available = new List<Vector3>();
            var visible = new List<Vector3>();
            for (int x = -VillageState.BuildHalfWidth + 1; x < VillageState.BuildHalfWidth; x++)
            {
                for (int z = -VillageState.BuildHalfDepth + 1; z < VillageState.BuildHalfDepth; z++)
                {
                    var point = new Vector3(x, .2f, z);
                    if (!state.IsVisitorSpot(x, z)
                        || Vector3.Distance(point, antLionPosition) < 2.5f
                        || (visitor != null
                            && Vector3.Distance(point, visitorPosition) < 3))
                    {
                        continue;
                    }

                    available.Add(point);
                    var screen = ViewCamera.WorldToViewportPoint(point);
                    if (screen.z > 0 && screen.x > .22f && screen.x < .74f && screen.y > .25f && screen.y < .75f)
                    {
                        visible.Add(point);
                    }
                }
            }

            var candidates = visible.Count > 0 ? visible : available;
            if (candidates.Count == 0)
            {
                return false;
            }

            antLionPosition = candidates[Random.Range(0, candidates.Count)];
            antLion = new GameObject(I18n.Source("object.ant_lion"));
            antLion.transform.SetParent(transform);
            antLion.transform.position = antLionPosition;
            antLion.AddComponent<AntLionView>();
            var collider = antLion.AddComponent<SphereCollider>();
            collider.center = Vector3.up * .35f;
            collider.radius = .85f;
            CreateSitePart(antLion.transform, I18n.Source("object.pit_mouth"), PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.25f, .025f, 1.25f), "#503726");
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2 / 12;
                CreateSitePart(antLion.transform,
                    I18n.Source("object.pit_rim"),
                    PrimitiveType.Sphere,
                    new Vector3(Mathf.Cos(angle) * .64f, .035f, Mathf.Sin(angle) * .64f),
                    new Vector3(.30f, .14f, .28f),
                    i % 2 == 0 ? "#C4A16B" : "#A47C4E");
            }

            antLionBody = new GameObject(I18n.Source("object.ant_lion_body")).transform;
            antLionBody.SetParent(antLion.transform, false);
            antLionBody.localRotation = Quaternion.Euler(0, Random.Range(-35f, 35f), 0);
            CreateSitePart(antLionBody, I18n.Source("object.segmented_abdomen"), PrimitiveType.Sphere, new Vector3(0, .22f, .20f), new Vector3(.72f, .48f, .75f), "#A87946");
            for (int i = 0; i < 3; i++)
            {
                CreateSitePart(antLionBody,
                    I18n.Source("object.back_segment"),
                    PrimitiveType.Sphere,
                    new Vector3(0, .38f, .12f + i * .15f),
                    new Vector3(.64f - i * .08f, .14f, .14f),
                    "#C29960");
            }

            CreateSitePart(antLionBody, I18n.Source("object.head"), PrimitiveType.Sphere, new Vector3(0, .40f, -.20f), new Vector3(.60f, .37f, .46f), "#805538");
            antLionJaws.Clear();
            antLionDust.Clear();
            for (int side = -1; side <= 1; side += 2)
            {
                CreateSitePart(antLionBody,
                    I18n.Source("object.eye"),
                    PrimitiveType.Sphere,
                    new Vector3(side * .19f, .53f, -.34f),
                    new Vector3(.09f, .09f, .07f),
                    "#211F1B");
                var jaw = new GameObject(I18n.Source("object.jaw")).transform;
                jaw.SetParent(antLionBody, false);
                jaw.localPosition = new Vector3(side * .24f, .38f, -.35f);
                CreateSitePart(jaw, I18n.Source("object.long_jaw"), PrimitiveType.Capsule, new Vector3(side * .08f, 0, -.22f), new Vector3(.085f, .24f, .085f), "#E5C488").transform.localRotation = Quaternion.Euler(90, 0, side * 15);
                CreateSitePart(jaw, I18n.Source("object.curved_jaw_tip"), PrimitiveType.Capsule, new Vector3(0, 0, -.43f), new Vector3(.07f, .13f, .07f), "#E5C488").transform.localRotation = Quaternion.Euler(90, side * 55, 0);
                antLionJaws.Add(jaw);
                for (int row = 0; row < 3; row++)
                {
                    CreateSitePart(antLionBody,
                        I18n.Source("object.digging_leg"),
                        PrimitiveType.Capsule,
                        new Vector3(side * .36f, .15f, -.05f + row * .17f),
                        new Vector3(.08f, .22f, .08f),
                        "#69462E").transform.localRotation = Quaternion.Euler(0, 0, side * 65);
                }
            }

            for (int i = 0; i < 9; i++)
            {
                antLionDust.Add(CreateSitePart(antLion.transform, I18n.Source("object.dust"), PrimitiveType.Sphere, Vector3.zero, Vector3.one * .12f, "#C5A778").transform);
            }

            antLionBorn = Time.time;
            antLionRepelled = -1;
            AnimateAntLion(Time.time);
            return true;
        }

        /// <summary>
        /// Starts the ant lion retreat animation and disables further collider interactions.
        /// </summary>
        public void RepelAntLion()
        {
            if (antLion == null || antLionRepelled >= 0)
            {
                return;
            }

            antLionRepelled = Time.time;
            antLion.GetComponent<Collider>().enabled = false;
        }

        /// <summary>
        /// Animates the ant lion emergence, jaws, retreat, and soil particles.
        /// </summary>
        void AnimateAntLion(float time)
        {
            if (antLion == null)
            {
                return;
            }

            float age = time - antLionBorn, exit = antLionRepelled < 0 ? -1 : time - antLionRepelled;
            if (exit >= 1.6f)
            {
                Destroy(antLion);
                antLion = null;
                antLionBody = null;
                antLionJaws.Clear();
                antLionDust.Clear();
                return;
            }

            float rise = Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / .8f));
            float retreat = exit < 0 ? 0 : Mathf.SmoothStep(0, 1, Mathf.Clamp01(exit / .85f));
            antLionBody.localPosition = Vector3.up * (-.55f * (1 - rise) - retreat * .8f + Mathf.Sin(time * 3) * .035f);
            antLionBody.localScale = Vector3.one * rise * (1 - retreat);
            for (int i = 0; i < antLionJaws.Count; i++)
            {
                antLionJaws[i].localRotation = Quaternion.Euler(0, (i == 0 ? -1 : 1) * (10 + Mathf.Sin(time * 6) * 18), 0);
            }

            float puff = exit >= 0 ? Mathf.Clamp01(exit / 1.3f) : Mathf.Clamp01(age / .8f);
            for (int i = 0; i < antLionDust.Count; i++)
            {
                float a = i * Mathf.PI * 2 / antLionDust.Count;
                antLionDust[i].localPosition = new Vector3(Mathf.Cos(a) * (.35f + puff * .55f), .08f + Mathf.Sin(puff * Mathf.PI) * .4f, Mathf.Sin(a) * (.35f + puff * .55f));
                antLionDust[i].localScale = Vector3.one * (.16f * (1 - puff));
            }

            if (exit > .85f)
            {
                antLion.transform.localScale = Vector3.one * (1 - Mathf.Clamp01((exit - .85f) / .75f));
            }
        }

        /// <summary>
        /// Projects a screen position onto the village ground plane.
        /// </summary>
        public bool Ground(Vector2 screen, out Vector3 point)
        {
            var ray = ViewCamera.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, new Vector3(0, .17f, 0));
            if (plane.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = default;
            return false;
        }

        /// <summary>
        /// Applies camera zoom and pan within the existing limits.
        /// </summary>
        public void Navigate(float zoom, Vector2 pan)
        {
            ViewCamera.orthographicSize = Mathf.Clamp(ViewCamera.orthographicSize - zoom, MinCameraZoom, MaxCameraZoom);
            cameraTarget += new Vector3(pan.x, 0, pan.y);
            cameraTarget.x = Mathf.Clamp(cameraTarget.x, -VillageState.BuildHalfWidth + 3, VillageState.BuildHalfWidth - 3);
            cameraTarget.z = Mathf.Clamp(cameraTarget.z, -VillageState.BuildHalfDepth + 2, VillageState.BuildHalfDepth);
            ViewCamera.transform.position = cameraTarget + cameraOffset;
        }

        /// <summary>
        /// Restores the default camera target and zoom.
        /// </summary>
        public void ResetCamera()
        {
            cameraTarget = new Vector3(0, 0, 2.1f);
            ViewCamera.orthographicSize = DefaultCameraZoom;
            Navigate(0, Vector2.zero);
        }

        /// <summary>
        /// Releases cached thumbnail render textures.
        /// </summary>
        void OnDestroy()
        {
            foreach (var t in thumbnails.Values)
            {
                t.Release();
                Destroy(t);
            }
        }
    }
}
