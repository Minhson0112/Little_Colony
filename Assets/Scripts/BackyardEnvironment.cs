using System.Collections.Generic;
using UnityEngine;

namespace LittleColony
{
    // A wide, continuous backyard surrounds the enlarged buildable village.
    /// <summary>
    /// Builds and animates the scenery surrounding the village.
    /// </summary>
    public sealed class BackyardEnvironment : MonoBehaviour
    {
        private const float PerimeterHalfWidth = VillageState.BuildHalfWidth + 4;
        private const float PerimeterHalfDepth = VillageState.BuildHalfDepth + 3.6f;
        private MaterialPropertyBlock expansionTint;
        private VillageWorld world;
        private Material groundMaterial;
        private Material lawnGrassMaterial;
        private readonly List<Transform> floatingLeaves = new List<Transform>();
        private readonly List<Transform> butterflies = new List<Transform>();
        private readonly List<Transform> swayingPlants = new List<Transform>();
        private System.Random rng = new System.Random(21);
        /// <summary>
        /// Returns the stream center at the supplied world Z coordinate.
        /// </summary>
        public static float StreamX(float z)
        {
            return Mathf.Sin(z * .46f) * .28f + Mathf.Sin(z * .91f) * .10f;
        }

        /// <summary>
        /// Samples the seeded scenery random generator within the supplied range.
        /// </summary>
        private float Random(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)rng.NextDouble());
        }

        /// <summary>
        /// Creates a colored primitive and parents it to the backyard environment.
        /// </summary>
        GameObject CreateSceneryPrimitive(string name, PrimitiveType kind, Vector3 p, Vector3 s, string color)
        {
            var o = world.Primitive(name, kind, p, s, color);
            o.transform.SetParent(transform);
            return o;
        }

        /// <summary>
        /// Creates a cylindrical rod spanning the supplied endpoints.
        /// </summary>
        void CreateSceneryRod(string name, Vector3 a, Vector3 b, float r, string color)
        {
            var o = CreateSceneryPrimitive(name, PrimitiveType.Cylinder, (a + b) * .5f, new Vector3(r, (a - b).magnitude * .5f, r), color);
            o.transform.rotation = Quaternion.FromToRotation(Vector3.up, b - a);
        }

        /// <summary>
        /// Builds a terrain strip between two sampled boundary functions.
        /// </summary>
        GameObject CreateTerrainStrip(string name, System.Func<float, float> left, System.Func<float, float> right, float y, Material material)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var tris = new List<int>();
            const int count = 200;
            for (int i = 0; i <= count; i++)
            {
                float z = -40 + i * .4f;
                vertices.Add(new Vector3(left(z), y, z));
                vertices.Add(new Vector3(right(z), y, z));
                uv.Add(new Vector2(0, z));
                uv.Add(new Vector2(1, z));
                if (i < count)
                {
                    int k = i * 2;
                    tris.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 });
                }
            }

            var mesh = new Mesh
            {
                name = name
            };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(transform);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        /// <summary>
        /// Creates the backyard terrain, stream, scenery, and ambient creatures.
        /// </summary>
        public void Create(VillageWorld owner)
        {
            world = owner;
            // Unity native graphics objects must be created on the main thread after AddComponent.
            expansionTint = new MaterialPropertyBlock();
            var grass = new Material(Resources.Load<Shader>("Shaders/GardenGround"));
            groundMaterial = grass;
            grass.SetColor("_Color", VillageWorld.ColorOf("#70944D"));
            grass.SetColor("_Patch", VillageWorld.ColorOf("#AABD70"));
            CreateTerrainStrip("West garden lawn", z => -70, z => StreamX(z) - 1.05f, .16f, grass);
            CreateTerrainStrip("East garden lawn", z => StreamX(z) + 1.05f, z => 70, .16f, grass);
            CreateTerrainStrip("Damp creek bed", z => StreamX(z) - 1.1f, z => StreamX(z) + 1.1f, -.04f, world.Material("#657B59"));
            var water = new Material(Resources.Load<Shader>("Shaders/GardenWater"));
            water.SetColor("_Color", VillageWorld.ColorOf("#368B95"));
            water.SetColor("_Shallow", VillageWorld.ColorOf("#9ED6C0"));
            CreateTerrainStrip("Flowing garden brook", z => StreamX(z) - .98f, z => StreamX(z) + .98f, .085f, water);
            CreateTerrainStrip("West moss bank", z => StreamX(z) - 1.18f, z => StreamX(z) - .97f, .13f, world.Material("#7E9360"));
            CreateTerrainStrip("East moss bank", z => StreamX(z) + .97f, z => StreamX(z) + 1.18f, .13f, world.Material("#7E9360"));
            world.Model("Bridge", new Vector3(0, .17f, 0));
            CreateCreekMushroom();
            for (int i = 0; i < 94; i++)
            {
                float z = -19 + i * .4f;
                if (Mathf.Abs(z) < .9f)
                {
                    continue;
                }

                float side = i % 2 == 0 ? -1 : 1;
                var stone = CreateSceneryPrimitive("Smooth bank stone",
                    PrimitiveType.Sphere,
                    new Vector3(StreamX(z) + side * Random(1.01f, 1.22f), .12f, z),
                    new Vector3(Random(.22f, .44f), Random(.17f, .28f), Random(.26f, .52f)),
                    i % 3 == 0 ? "#B3B3A0" : "#8C9B85");
                stone.transform.rotation = Quaternion.Euler(0, Random(0, 180), 0);
            }

            for (int i = 0; i < 7; i++)
            {
                var leaf = CreateSceneryPrimitive("Leaf carried by current",
                    PrimitiveType.Sphere,
                    Vector3.zero,
                    new Vector3(.13f, .014f, .24f),
                    i % 2 == 0 ? "#D9BD64" : "#BDD093");
                floatingLeaves.Add(leaf.transform);
            }

            CreatePerimeterFence();
            CreateLowerLogBoundary();
            CreateHouseFacade();
            CreateGardenProps();
            CreateGardenDivider();
            CreateVillageDivider();
            CreateGrass();
            for (int i = 0; i < 4; i++)
            {
                var pivot = new GameObject("Garden butterfly").transform;
                pivot.SetParent(transform);
                for (int side = -1; side <= 1; side += 2)
                {
                    var wing = CreateSceneryPrimitive("Butterfly wing",
                        PrimitiveType.Sphere,
                        Vector3.zero,
                        new Vector3(.16f, .022f, .22f),
                        i % 2 == 0 ? "#F4D884" : "#E4B4BD");
                    wing.transform.SetParent(pivot);
                    wing.transform.localPosition = new Vector3(side * .12f, 0, 0);
                }

                butterflies.Add(pivot);
            }
        }

        /// <summary>
        /// Shades the entire enclosed outer areas, including scenery, independently of buildable bounds.
        /// </summary>
        public void SyncExpansions(VillageState state)
        {
            foreach (var material in new[] { groundMaterial, lawnGrassMaterial })
            {
                material.SetVector("_ExpansionLocks", new Vector4(
                    state.housingExpansionOwned ? 0 : 1,
                    state.gardenExpansionOwned ? 0 : 1,
                    GardenDivider.X, PerimeterHalfWidth));
                material.SetFloat("_ExpansionDepth", PerimeterHalfDepth);
            }

            SyncExpansionSceneryTint(state);
        }

        /// <summary>
        /// Tints outer scenery per material without changing shared assets, restoring its colors after purchase.
        /// </summary>
        private void SyncExpansionSceneryTint(VillageState state)
        {
            foreach (var renderer in world.GetComponentsInChildren<Renderer>())
            {
                Vector3 center = renderer.bounds.center;
                if (Mathf.Abs(center.x) < LandExpansion.InnerEdge
                    || Mathf.Abs(center.x) > PerimeterHalfWidth + .5f
                    || Mathf.Abs(center.z) > PerimeterHalfDepth + .5f
                    || renderer.sharedMaterial == groundMaterial
                    || renderer.sharedMaterial == lawnGrassMaterial)
                {
                    continue;
                }

                var plot = center.x < 0 ? LandPlot.Housing : LandPlot.Garden;
                bool locked = !state.IsExpansionOwned(plot);
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || !material.HasProperty("_Color"))
                    {
                        continue;
                    }

                    Color color = material.GetColor("_Color");
                    if (locked)
                    {
                        color.r *= .34f;
                        color.g *= .34f;
                        color.b *= .34f;
                    }

                    expansionTint.Clear();
                    renderer.GetPropertyBlock(expansionTint, i);
                    expansionTint.SetColor("_Color", color);
                    renderer.SetPropertyBlock(expansionTint, i);
                }
            }
        }

        /// <summary>
        /// Adds the decorative mushroom and rock in the stream.
        /// </summary>
        void CreateCreekMushroom()
        {
            float x = StreamX(3.8f), z = 3.8f;
            CreateSceneryPrimitive("Mossy stone in the brook", PrimitiveType.Sphere, new Vector3(x, .18f, z), new Vector3(.53f, .23f, .47f), "#879784");
            CreateSceneryPrimitive("Mushroom stem in the brook",
                PrimitiveType.Cylinder,
                new Vector3(x, .48f, z),
                new Vector3(.11f, .31f, .11f),
                "#F2E3B9");
            CreateSceneryPrimitive("Mushroom cap in the brook", PrimitiveType.Sphere, new Vector3(x, .81f, z), new Vector3(.39f, .14f, .38f), "#CA7359");
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2 / 5;
                CreateSceneryPrimitive("Mushroom cap spot",
                    PrimitiveType.Sphere,
                    new Vector3(x + Mathf.Cos(a) * .23f, .92f, z + Mathf.Sin(a) * .22f),
                    new Vector3(.064f, .027f, .064f),
                    "#FFF2D1");
            }
        }

        /// <summary>
        /// Creates the fixed perimeter fence around the backyard.
        /// </summary>
        void CreatePerimeterFence()
        {
            int picketCount = Mathf.CeilToInt(PerimeterHalfWidth * 2 / .82f);
            for (int i = 0; i <= picketCount; i++)
            {
                float x = Mathf.Lerp(-PerimeterHalfWidth, PerimeterHalfWidth, (float)i / picketCount);
                CreateSceneryPrimitive("Cedar fence picket",
                    PrimitiveType.Cube,
                    new Vector3(x, 1.24f, 13.5f),
                    new Vector3(.72f, 2.25f, .18f),
                    i % 3 == 0 ? "#B8956F" : "#C4A680");
                CreateSceneryPrimitive("Picket cap", PrimitiveType.Sphere, new Vector3(x, 2.37f, 13.5f), new Vector3(.72f, .22f, .19f), "#C4A680");
            }

            for (int j = 0; j < 2; j++)
            {
                CreateSceneryPrimitive("Fence crossbar", PrimitiveType.Cube, new Vector3(0, .75f + j * .9f, 13.35f), new Vector3(PerimeterHalfWidth * 2 + 1, .13f, .14f), "#9F7D57");
            }

            int postCount = Mathf.CeilToInt(PerimeterHalfWidth * 2 / 5.5f);
            for (int i = 0; i <= postCount; i++)
            {
                float x = Mathf.Lerp(-PerimeterHalfWidth, PerimeterHalfWidth, (float)i / postCount);
                CreateSceneryPrimitive("Fence post", PrimitiveType.Cube, new Vector3(x, 1.35f, 13.3f), new Vector3(.25f, 2.7f, .30f), "#9F7D57");
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 33; i++)
                {
                    CreateSceneryPrimitive("Side fence",
                        PrimitiveType.Cube,
                        new Vector3(side * PerimeterHalfWidth, 1.1f, 13 - i * .82f),
                        new Vector3(.18f, 1.9f, .72f),
                        "#B99A75");
                }
            }
        }

        /// <summary>
        /// Encloses the foreground with oversized fallen trunks while leaving the creek open.
        /// </summary>
        private void CreateLowerLogBoundary()
        {
            const float creekClearance = 1.6f;
            const int logsPerBank = 3;
            float length = (PerimeterHalfWidth - creekClearance) / logsPerBank;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < logsPerBank; i++)
                {
                    float x = side * (creekClearance + length * (i + .5f));
                    float diameter = i % 2 == 0 ? 3.2f : 3.5f;
                    var center = new Vector3(x, .16f + diameter * .5f, -PerimeterHalfDepth);
                    CreateBoundaryLog(center, length + .12f, diameter);
                }
            }
        }

        /// <summary>
        /// Builds a horizontal boundary trunk with exposed wood, bark ridges, and moss.
        /// </summary>
        /// <param name="center">World center of the trunk, outside the building plots.</param>
        /// <param name="length">Full length along the horizontal map axis.</param>
        /// <param name="diameter">Full diameter of the bark cylinder.</param>
        private void CreateBoundaryLog(Vector3 center, float length, float diameter)
        {
            var trunk = CreateSceneryPrimitive("Boundary fallen tree trunk", PrimitiveType.Cylinder,
                center, new Vector3(diameter, length * .5f, diameter), "#765139");
            trunk.transform.rotation = Quaternion.Euler(0, 0, 90);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int ring = 0; ring < 3; ring++)
                {
                    float ringDiameter = diameter * (.90f - ring * .24f);
                    var grain = CreateSceneryPrimitive("Boundary trunk end grain", PrimitiveType.Cylinder,
                        center + Vector3.right * side * (length * .5f + .015f + ring * .018f),
                        new Vector3(ringDiameter, .012f, ringDiameter),
                        ring == 1 ? "#A87B50" : "#D0AD78");
                    grain.transform.rotation = Quaternion.Euler(0, 0, 90);
                }
            }

            for (int ridge = 0; ridge < 7; ridge++)
            {
                float angle = ridge * Mathf.PI / 6;
                var offset = new Vector3(0, Mathf.Sin(angle), Mathf.Cos(angle)) * diameter * .47f;
                var bark = CreateSceneryPrimitive("Boundary trunk bark ridge", PrimitiveType.Cube,
                    center + offset, new Vector3(length * .96f, .14f, .20f),
                    ridge % 2 == 0 ? "#573D2C" : "#926949");
                bark.transform.rotation = Quaternion.Euler(angle * Mathf.Rad2Deg, 0, 0);
            }

            CreateSceneryPrimitive("Boundary trunk moss", PrimitiveType.Sphere,
                center + new Vector3(-length * .18f, diameter * .48f, .12f),
                new Vector3(length * .36f, .25f, diameter * .55f), "#739354");
            CreateSceneryRod("Boundary trunk broken branch",
                center + new Vector3(length * .23f, diameter * .38f, .1f),
                center + new Vector3(length * .28f, diameter * .68f, .45f), .30f, "#765139");
        }

        /// <summary>
        /// Builds the large house facade behind the playable garden.
        /// </summary>
        void CreateHouseFacade()
        {
            CreateSceneryPrimitive("Back of the house", PrimitiveType.Cube, new Vector3(-6, 3.1f, 19), new Vector3(12, 6, 5), "#E6D8BB");
            for (int i = 0; i < 11; i++)
            {
                CreateSceneryPrimitive("Weatherboard siding",
                    PrimitiveType.Cube,
                    new Vector3(-6, .55f + i * .51f, 16.45f),
                    new Vector3(12, .025f, .035f),
                    "#CFBFA0");
            }

            CreateSceneryPrimitive("House eaves", PrimitiveType.Cube, new Vector3(-6, 6.2f, 18.7f), new Vector3(13, .28f, 6), "#7C8370");
            const float windowX = -6;
            CreateSceneryPrimitive("Single window frame", PrimitiveType.Cube, new Vector3(windowX, 3, 16.37f), new Vector3(2.5f, 2.2f, .19f), "#FAF0D9");
            CreateSceneryPrimitive("Single window glass", PrimitiveType.Cube, new Vector3(windowX, 3, 16.23f), new Vector3(2.15f, 1.85f, .12f), "#94B8B3");
            CreateSceneryPrimitive("Window mullion", PrimitiveType.Cube, new Vector3(windowX, 3, 16.14f), new Vector3(.075f, 1.85f, .04f), "#FAF0D9");
            CreateSceneryPrimitive("Window crosspiece", PrimitiveType.Cube, new Vector3(windowX, 3, 16.14f), new Vector3(2.15f, .075f, .04f), "#FAF0D9");
            for (int i = 0; i < 5; i++)
            {
                float x = 13 + i * 1.2f;
                CreateSceneryPrimitive("Beyond fence shrub",
                    PrimitiveType.Sphere,
                    new Vector3(x, 1.6f, 17.7f + Mathf.Sin(i)),
                    new Vector3(3.7f, 3.6f, 3.4f),
                    i % 2 == 0 ? "#7A995E" : "#90A773");
            }
        }

        /// <summary>
        /// Populates the backyard with fixed garden props and plants.
        /// </summary>
        void CreateGardenProps()
        {
            CreatePatioFurniture();
            CreateLawnMower();
            CreateFallenLog();
            CreateWorksiteRocks();
            CreateTallGrassClumps();
            world.Model("GardenPot", new Vector3(-PerimeterHalfWidth + 2.2f, .17f, 9.5f), 1.25f);
            world.Model("GardenPot", new Vector3(-17.5f, .17f, 11.6f), .8f);
            var watering = world.Model("WateringCan", new Vector3(PerimeterHalfWidth - 3, .17f, 11.5f), 1.3f);
            watering.transform.rotation = Quaternion.Euler(0, -30, 0);
            world.Model("GardenPot", new Vector3(PerimeterHalfWidth - 2, .17f, -10.7f), 1.45f);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 5;
                CreateSceneryPrimitive("Raised soil bed", PrimitiveType.Cube, new Vector3(x, .19f, 11.6f), new Vector3(4, .16f, 1.4f), "#796046");
                for (int row = 0; row < 2; row++)
                {
                    CreateSceneryPrimitive("Raised bed edging",
                        PrimitiveType.Cube,
                        new Vector3(x, .33f, 10.9f + row * 1.4f),
                        new Vector3(4.2f, .27f, .14f),
                        "#AE8154");
                }

                for (int end = -1; end <= 1; end += 2)
                {
                    CreateSceneryPrimitive("Bed end", PrimitiveType.Cube, new Vector3(x + end * 2.04f, .33f, 11.6f), new Vector3(.14f, .27f, 1.5f), "#AE8154");
                }

                for (int i = 0; i < 6; i++)
                {
                    var o = world.Model(side < 0 ? "Clover" : "Flower", new Vector3(x - 1.55f + i * .61f, .28f, 11.55f), side < 0 ? 1.35f : .65f);
                    swayingPlants.Add(o.transform);
                }
            }

            for (int i = 0; i < 13; i++)
            {
                float x = -9 + i * 1.5f;
                if (Mathf.Abs(x) < 1.7f)
                {
                    continue;
                }

                var o = CreateSceneryPrimitive("Garden stepping stone",
                    PrimitiveType.Cylinder,
                    new Vector3(x, .19f, -.06f + Mathf.Sin(i) * .17f),
                    new Vector3(.59f, .025f, .48f),
                    i % 2 == 0 ? "#C9C3A7" : "#BABB9D");
                o.transform.rotation = Quaternion.Euler(0, Random(0, 50), 0);
            }

            for (int i = 0; i < 28; i++)
            {
                float x = Random(-PerimeterHalfWidth + 3, PerimeterHalfWidth - 3), z = Random(-12, -11.5f);
                if (Mathf.Abs(x) < 1.5f)
                {
                    continue;
                }

                swayingPlants.Add(world.Model(i % 4 == 0 ? "Flower" : "Clover", new Vector3(x, .17f, z), Random(.55f, 1.05f)).transform);
            }

            foreach (var item in new[]
            {
                new Vector3(-PerimeterHalfWidth + 2, .17f, -8),
                new Vector3(PerimeterHalfWidth - 2, .17f, 7),
                new Vector3(-18, .17f, 12)
            }

            )
            {
                world.Model("Lantern", item, .9f);
            }

            world.Model("Bench", new Vector3(-14, .17f, 11), 1.05f);
            world.Model("Birdbath", new Vector3(15, .17f, 11), 1.05f);
            world.Model("FlowerArch", new Vector3(4, .17f, 11), 1.05f);
            for (int i = 0; i < 7; i++)
            {
                float x = -19 + i * 6.2f;
                const float z = -12.4f;
                float scale = Random(.7f, 1.15f);
                // Keep the lower creek opening clear while preserving the seeded scenery sequence.
                if (Mathf.Abs(x - StreamX(z)) < 2f)
                {
                    continue;
                }

                world.Model("MushroomPatch", new Vector3(x, .17f, z), scale);
            }

            // A few towering stems frame the insect-scale world without obscuring the building plots.
            foreach (float x in new[]
            {
                -PerimeterHalfWidth + 1.5f,
                PerimeterHalfWidth - 1.1f
            }

            )
            {
                CreateSceneryRod("Tall garden stem", new Vector3(x, .16f, 2.7f), new Vector3(x, 3.8f, 2.7f), .085f, "#608747");
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * .4f;
                    var leaf = CreateSceneryPrimitive("Broad garden leaf",
                        PrimitiveType.Sphere,
                        new Vector3(x + Mathf.Cos(a) * .42f, 1.3f + i * .44f, 2.7f + Mathf.Sin(a) * .4f),
                        new Vector3(1.2f, .11f, .48f),
                        i % 2 == 0 ? "#789F52" : "#8EAE63");
                    leaf.transform.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 15);
                    swayingPlants.Add(leaf.transform);
                }
            }
        }

        /// <summary>
        /// Separates the two garden plots with flowering pots and a central stone passage.
        /// </summary>
        private void CreateGardenDivider()
        {
            CreateDividerPath(GardenDivider.X);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < GardenDivider.PotsPerSide; i++)
                {
                    float z = GardenDivider.GateZ + side * (GardenDivider.FirstPotZ + i * GardenDivider.PotSpacing);
                    CreateDividerFlowerPot(new Vector3(GardenDivider.X, .17f, z), i);
                }
            }

        }

        /// <summary>
        /// Separates the two housing plots with weathered logs and a central stone passage.
        /// </summary>
        private void CreateVillageDivider()
        {
            CreateDividerPath(GardenDivider.VillageX);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < GardenDivider.LogsPerSide; i++)
                {
                    float z = GardenDivider.GateZ + side * (GardenDivider.FirstLogZ + i * GardenDivider.LogSpacing);
                    CreateRottenDividerLog(new Vector3(GardenDivider.VillageX, .82f, z), i);
                }
            }
        }

        /// <summary>
        /// Lays a short trail of flat stones through the open center of a divider row.
        /// </summary>
        /// <param name="x">Horizontal center shared with the divider placement footprint.</param>
        private void CreateDividerPath(float x)
        {
            for (int i = -2; i <= 2; i++)
            {
                var stone = CreateSceneryPrimitive("Divider passage stepping stone", PrimitiveType.Cylinder,
                    new Vector3(x + i * .85f, .18f, GardenDivider.GateZ),
                    new Vector3(.76f, .025f, .72f), i % 2 == 0 ? "#C9C3A7" : "#BABB9D");
                stone.transform.rotation = Quaternion.Euler(0, i * 17, 0);
            }
        }

        /// <summary>
        /// Builds a low decaying log with hollow ends, cracked bark, moss, and small fungi.
        /// </summary>
        /// <param name="center">World center along the housing divider.</param>
        /// <param name="index">Index used to vary the bark and moss details.</param>
        private void CreateRottenDividerLog(Vector3 center, int index)
        {
            const float diameter = 1.3f;
            float halfLength = GardenDivider.LogLength * .5f;
            var trunk = CreateSceneryPrimitive("Rotten housing divider log", PrimitiveType.Cylinder,
                center, new Vector3(diameter, halfLength, diameter), index % 2 == 0 ? "#70543C" : "#806044");
            trunk.transform.rotation = Quaternion.Euler(90, 0, 0);
            for (int side = -1; side <= 1; side += 2)
            {
                var end = CreateSceneryPrimitive("Weathered log end", PrimitiveType.Cylinder,
                    center + Vector3.forward * side * (halfLength + .015f),
                    new Vector3(1.16f, .02f, 1.16f), "#B39163");
                end.transform.rotation = Quaternion.Euler(90, 0, 0);
                var hollow = CreateSceneryPrimitive("Decayed log hollow", PrimitiveType.Cylinder,
                    center + Vector3.forward * side * (halfLength + .045f),
                    new Vector3(.79f, .012f, .79f), "#49382A");
                hollow.transform.rotation = Quaternion.Euler(90, 0, 0);
                var core = CreateSceneryPrimitive("Dark log interior", PrimitiveType.Cylinder,
                    center + Vector3.forward * side * (halfLength + .061f),
                    new Vector3(.56f, .009f, .56f), "#302A22");
                core.transform.rotation = Quaternion.Euler(90, 0, 0);
            }

            for (int ridge = 0; ridge < 5; ridge++)
            {
                float angle = ridge * Mathf.PI / 4;
                var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * .59f;
                var bark = CreateSceneryPrimitive("Cracked divider bark", PrimitiveType.Cube,
                    center + offset, new Vector3(.10f, .08f, GardenDivider.LogLength * .91f), "#493A2C");
                bark.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            }

            CreateSceneryPrimitive("Moss on decaying divider log", PrimitiveType.Sphere,
                center + new Vector3(index % 2 == 0 ? .12f : -.12f, .58f, -.35f),
                new Vector3(1.02f, .22f, 1.4f), "#789250");
            for (int fungus = 0; fungus < 3; fungus++)
            {
                var position = center + new Vector3(-.57f, .12f + fungus * .12f, -.65f + fungus * .55f);
                CreateSceneryPrimitive("Woodland bracket fungus", PrimitiveType.Sphere,
                    position, new Vector3(.37f, .10f, .42f), fungus % 2 == 0 ? "#D2B48A" : "#A88A60");
            }
        }

        /// <summary>
        /// Adds bright flower heads above an existing clay pot model.
        /// </summary>
        /// <param name="position">Ground position of the pot in the divider row.</param>
        /// <param name="index">Index used to alternate flower colors along the row.</param>
        private void CreateDividerFlowerPot(Vector3 position, int index)
        {
            var pot = world.Model("GardenPot", position);
            pot.transform.SetParent(transform, true);
            string color = index % 3 == 0 ? "#E99DAF" : index % 3 == 1 ? "#C8AFE0" : "#F1D17C";
            for (int flower = 0; flower < 3; flower++)
            {
                float angle = flower * Mathf.PI * 2 / 3;
                var offset = new Vector3(Mathf.Cos(angle) * .26f, 1.7f + flower * .08f, Mathf.Sin(angle) * .26f);
                var center = position + offset;
                CreateSceneryRod("Divider flower stem", position + new Vector3(offset.x, 1.1f, offset.z),
                    center, .045f, "#608747");
                for (int petal = 0; petal < 5; petal++)
                {
                    float petalAngle = petal * Mathf.PI * 2 / 5;
                    CreateSceneryPrimitive("Divider flower petal", PrimitiveType.Sphere,
                        center + new Vector3(Mathf.Cos(petalAngle) * .15f, 0, Mathf.Sin(petalAngle) * .15f),
                        new Vector3(.23f, .09f, .23f), color);
                }

                CreateSceneryPrimitive("Divider flower heart", PrimitiveType.Sphere,
                    center + Vector3.up * .04f, new Vector3(.14f, .10f, .14f), "#F3D474");
            }
        }

        /// <summary>
        /// Creates the oversized patio table, umbrella, and chairs.
        /// </summary>
        void CreatePatioFurniture()
        {
            var c = new Vector3(12, 0, 4);
            CreateSceneryPrimitive("Large patio table top", PrimitiveType.Cylinder, c + Vector3.up * 1.35f, new Vector3(1.5f, .13f, 1.5f), "#B98D5D");
            CreateSceneryPrimitive("Table rim", PrimitiveType.Cylinder, c + Vector3.up * 1.23f, new Vector3(1.39f, .08f, 1.39f), "#765538");
            CreateSceneryPrimitive("Table pedestal", PrimitiveType.Cylinder, c + Vector3.up * .67f, new Vector3(.16f, .6f, .16f), "#6D765E");
            CreateSceneryPrimitive("Table foot", PrimitiveType.Cylinder, c + Vector3.up * .26f, new Vector3(.76f, .08f, .76f), "#6D765E");
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * .5f;
                var outward = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var side = new Vector3(outward.z, 0, -outward.x);
                var seat = c + outward * 2.12f;
                CreateSceneryPrimitive("Patio chair seat", PrimitiveType.Cube, seat + Vector3.up * .82f, new Vector3(.88f, .12f, .88f), "#CC9B61");
                CreateSceneryPrimitive("Patio chair back",
                    PrimitiveType.Cube,
                    seat + outward * .39f + Vector3.up * 1.34f,
                    new Vector3(.92f, .95f, .14f),
                    "#A5794D").transform.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
                for (int j = -1; j <= 1; j += 2)
                {
                    var leg = seat + side * j * .34f;
                    CreateSceneryPrimitive("Chair front leg",
                        PrimitiveType.Cube,
                        leg - outward * .32f + Vector3.up * .42f,
                        new Vector3(.10f, .78f, .11f),
                        "#6D765E");
                    CreateSceneryPrimitive("Chair rear leg",
                        PrimitiveType.Cube,
                        leg + outward * .34f + Vector3.up * .42f,
                        new Vector3(.10f, .78f, .11f),
                        "#6D765E");
                }
            }

        }

        /// <summary>
        /// Builds the decorative lawn mower beside the garden.
        /// </summary>
        void CreateLawnMower()
        {
            var c = new Vector3(11, 0, -6);
            CreateSceneryPrimitive("Mower cutting deck", PrimitiveType.Cylinder, c + Vector3.up * .43f, new Vector3(1.35f, .27f, .85f), "#C9584A");
            CreateSceneryPrimitive("Mower engine", PrimitiveType.Cube, c + Vector3.up * .83f, new Vector3(.83f, .61f, .76f), "#55635C");
            CreateSceneryPrimitive("Mower engine cap", PrimitiveType.Cylinder, c + Vector3.up * 1.19f, new Vector3(.36f, .07f, .36f), "#303A37");
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    CreateSceneryPrimitive("Rubber mower wheel",
                        PrimitiveType.Cylinder,
                        c + new Vector3(sx * 1.03f, .33f, sz * .68f),
                        new Vector3(.31f, .13f, .31f),
                        "#303B3A").transform.rotation = Quaternion.Euler(90, 0, 0);
                }
            }

            CreateSceneryRod("Mower handle left", c + new Vector3(-.55f, .62f, -.6f), c + new Vector3(-.7f, 2.05f, -1.65f), .045f, "#4F6158");
            CreateSceneryRod("Mower handle right", c + new Vector3(.55f, .62f, -.6f), c + new Vector3(.7f, 2.05f, -1.65f), .045f, "#4F6158");
            CreateSceneryRod("Mower grip", c + new Vector3(-.7f, 2.05f, -1.65f), c + new Vector3(.7f, 2.05f, -1.65f), .08f, "#303B3A");
            CreateSceneryPrimitive("Grass catcher", PrimitiveType.Cube, c + new Vector3(0, .55f, 1.07f), new Vector3(1.17f, .61f, 1.04f), "#667B5C");
        }

        /// <summary>
        /// Creates the fixed rock clusters near the playable area.
        /// </summary>
        void CreateWorksiteRocks()
        {
            var rocks = new[]
            {
                new Vector3(33, .32f, -1),
                new Vector3(16, .28f, -9),
                new Vector3(6, .31f, 8)
            };
            for (int i = 0; i < rocks.Length; i++)
            {
                var p = rocks[i];
                var large = CreateSceneryPrimitive("Large worksite boulder",
                    PrimitiveType.Sphere,
                    p,
                    new Vector3(i == 0 ? 2.0f : 1.7f, .85f, 1.5f),
                    i % 2 == 0 ? "#929789" : "#AAA997");
                large.transform.rotation = Quaternion.Euler(0, i * 37, 13);
                for (int j = 0; j < 3; j++)
                {
                    var pebble = CreateSceneryPrimitive("Small worksite rock",
                        PrimitiveType.Sphere,
                        p + new Vector3(-1.1f + j * .72f, -.11f, 1.02f + (j % 2) * .23f),
                        new Vector3(.48f + j * .08f, .34f, .54f),
                        "#B9B5A0");
                    pebble.transform.rotation = Quaternion.Euler(0, j * 28, 9);
                }
            }
        }

        /// <summary>
        /// Builds the fallen log and its surrounding details.
        /// </summary>
        void CreateFallenLog()
        {
            var c = new Vector3(5, .88f, -9);
            var trunk = CreateSceneryPrimitive("Giant fallen garden log", PrimitiveType.Cylinder, c, new Vector3(.88f, 2.7f, .88f), "#8C6548");
            trunk.transform.rotation = Quaternion.Euler(0, 0, 90);
            for (int side = -1; side <= 1; side += 2)
            {
                var end = CreateSceneryPrimitive("Pale log end grain",
                    PrimitiveType.Cylinder,
                    c + new Vector3(side * 2.72f, 0, 0),
                    new Vector3(.78f, .055f, .78f),
                    "#C8A875");
                end.transform.rotation = Quaternion.Euler(0, 0, 90);
                var ring = CreateSceneryPrimitive("Log growth ring",
                    PrimitiveType.Cylinder,
                    c + new Vector3(side * 2.79f, 0, 0),
                    new Vector3(.49f, .013f, .49f),
                    "#A9825D");
                ring.transform.rotation = Quaternion.Euler(0, 0, 90);
            }

            for (int i = 0; i < 6; i++)
            {
                float x = 2.9f + i * .83f;
                var stripe = CreateSceneryPrimitive("Bark ridge",
                    PrimitiveType.Cube,
                    new Vector3(x, 1.61f, -9.1f + Mathf.Sin(i) * .26f),
                    new Vector3(.18f, .075f, 1.1f),
                    i % 2 == 0 ? "#694A37" : "#A67A53");
                stripe.transform.rotation = Quaternion.Euler(0, 8 + i * 19, 0);
            }

            CreateSceneryPrimitive("Moss on fallen log", PrimitiveType.Sphere, new Vector3(4.1f, 1.66f, -8.85f), new Vector3(1.4f, .23f, .82f), "#71925A");
        }

        /// <summary>
        /// Creates the tall grass clumps around the fixed scenery.
        /// </summary>
        void CreateTallGrassClumps()
        {
            var centers = new[]
            {
                new Vector3(1.9f, .16f, -10.3f),
                new Vector3(8.1f, .16f, -10.8f),
                new Vector3(PerimeterHalfWidth - 1.3f, .16f, -4.6f),
                new Vector3(PerimeterHalfWidth - 1.1f, .16f, 4.2f),
                new Vector3(-PerimeterHalfWidth + 1.3f, .16f, 5.9f)
            };
            for (int i = 0; i < centers.Length; i++)
            {
                for (int blade = 0; blade < 9; blade++)
                {
                    float a = blade * 2.399f;
                    float radius = .16f + blade * .065f;
                    var start = centers[i] + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
                    float height = 1.4f + (blade % 4) * .35f;
                    var stem = CreateSceneryPrimitive("Tall swaying grass",
                        PrimitiveType.Cube,
                        start + Vector3.up * height * .5f,
                        new Vector3(.075f, height, .12f),
                        blade % 3 == 0 ? "#89A751" : "#587D45");
                    stem.transform.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, blade % 2 == 0 ? 12 : -11);
                    swayingPlants.Add(stem.transform);
                    if (blade % 3 == 0)
                    {
                        CreateSceneryPrimitive("Grass seed head", PrimitiveType.Sphere, start + Vector3.up * height, new Vector3(.14f, .32f, .15f), "#CCBD78");
                    }
                }
            }
        }

        /// <summary>
        /// Generates the grass mesh and assigns its wind material.
        /// </summary>
        void CreateGrass()
        {
            var vs = new List<Vector3>();
            var ts = new List<int>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            for (int i = 0; i < 4700; i++)
            {
                float x = Random(-PerimeterHalfWidth, PerimeterHalfWidth), z = Random(-15, 13);
                if (Mathf.Abs(x - StreamX(z)) < 1.25f || (Mathf.Abs(x) < 8.8f && Mathf.Abs(z) < 4.7f && rng.NextDouble() < .92))
                {
                    continue;
                }

                for (int j = 0; j < 3; j++)
                {
                    float a = Random(0, 6.28f), h = Random(.16f, .43f), width = Random(.035f, .065f);
                    var p = new Vector3(x, .16f, z);
                    var side = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * width;
                    int k = vs.Count;
                    vs.Add(p - side);
                    vs.Add(p + side);
                    vs.Add(p + Vector3.up * h + new Vector3(.04f, 0, .03f));
                    ts.AddRange(new[] { k, k + 2, k + 1 });
                    uvs.Add(Vector2.zero);
                    uvs.Add(Vector2.zero);
                    uvs.Add(Vector2.up);
                    float c = Random(.8f, 1.25f);
                    for (int n = 0; n < 3; n++)
                    {
                        colors.Add(new Color(c, c, c));
                    }
                }
            }

            var mesh = new Mesh
            {
                name = "Wind grass patches"
            };
            mesh.SetVertices(vs);
            mesh.SetTriangles(ts, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject("Living meadow grass", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            lawnGrassMaterial = new Material(Resources.Load<Shader>("Shaders/GardenGrass"));
            go.GetComponent<Renderer>().sharedMaterial = lawnGrassMaterial;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// Animates floating leaves, butterflies, and swaying plants each frame.
        /// </summary>
        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < floatingLeaves.Count; i++)
            {
                float z = 13 - Mathf.Repeat(t * .45f + i * 3.7f, 30);
                floatingLeaves[i].position = new Vector3(StreamX(z) + Mathf.Sin(i * 7 + z) * .46f, .11f, z);
                floatingLeaves[i].rotation = Quaternion.Euler(0, Mathf.Sin(t * .7f + i) * 35, 0);
            }

            for (int i = 0; i < butterflies.Count; i++)
            {
                var b = butterflies[i];
                b.gameObject.SetActive(!world.IsShelterWeather);
                if (!b.gameObject.activeSelf)
                {
                    continue;
                }

                b.position = new Vector3(Mathf.Sin(t * .20f + i * 2) * 14, 1.8f + Mathf.Sin(t * .7f + i) * .5f, Mathf.Cos(t * .25f + i) * 8);
                b.rotation = Quaternion.Euler(0, t * 13 + i * 45, 0);
                for (int j = 0; j < 2; j++)
                {
                    b.GetChild(j).localRotation = Quaternion.Euler(0, 0, (j == 0 ? -1 : 1) * Mathf.Sin(t * 15) * 45);
                }
            }

            for (int i = 0; i < swayingPlants.Count; i++)
            {
                var p = swayingPlants[i];
                p.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.1f + i) * 2.5f, 0, Mathf.Cos(t * .9f + i) * 3);
            }
        }
    }
}
