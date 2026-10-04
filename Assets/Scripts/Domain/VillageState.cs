using System;
using System.Collections.Generic;

namespace LittleColony
{
    /// <summary>
    /// Identifies saved building kinds; member order preserves the existing numeric save IDs.
    /// </summary>
    public enum BuildingKind
    {
        AntHome,
        BeeHome,
        Pile,
        Flower,
        Clover,
        Lantern,
        Bench,
        Birdbath,
        FlowerArch,
        MushroomPatch,
        AntBurrow,
        BeeLantern,
        TwigYard,
        SeedMill,
        Lavender,
        Sunflower,
        AntLeafTent,
        BeeFlowerHome,
        SugarCube,
        Cookie,
        Fence,
        AntAcornHome,
        SmallTree,
        TallGrass,
        SmallParasol,
        BeeHoneycombHome,
        Tulip,
        Bluebell,
        LeafDepot,
        PebbleYard,
        WoodenPlanter,
        Birdhouse,
        WindChime,
        LeafFountain,
        FlowerCart
    }

    /// <summary>
    /// Identifies the idle, working, ready-to-collect, and empty worksite states.
    /// </summary>
    public enum JobPhase
    {
        Idle,
        Working,
        Ready,
        Empty
    }

    /// <summary>
    /// Calculates deterministic daylight and rain from the saved world clock and weather seed.
    /// </summary>
    public static class ColonyClimate
    {
        public const double DaySeconds = 240;
        public const double NightSeconds = 60;
        public const double CycleSeconds = DaySeconds + NightSeconds;
        public const double RainSlotSeconds = 30;
        /// <summary>
        /// Wraps a world time into the day and night cycle.
        /// </summary>
        public static double Phase(double time)
        {
            double phase = time % CycleSeconds;
            return phase < 0 ? phase + CycleSeconds : phase;
        }

        /// <summary>
        /// Checks whether the supplied world time falls in the night phase.
        /// </summary>
        public static bool IsNight(double time)
        {
            return Phase(time) >= DaySeconds;
        }

        /// <summary>
        /// Determines whether the current weather slot is rainy using the saved seed.
        /// </summary>
        public static bool IsRaining(double time, int seed)
        {
            long slot = (long)Math.Floor(time / RainSlotSeconds);
            unchecked
            {
                ulong value = (ulong)(slot + (long)seed * 1315423911L);
                value ^= value >> 30;
                value *= 0xBF58476D1CE4E5B9UL;
                value ^= value >> 27;
                value *= 0x94D049BB133111EBUL;
                value ^= value >> 31;
                return value % 20 == 0; // A short shower has a one-in-twenty chance per slot.
            }
        }

        /// <summary>
        /// Checks whether rain allows work at the supplied world time.
        /// </summary>
        public static bool CanWork(double time, int seed)
        {
            return !IsRaining(time, seed);
        }

        /// <summary>
        /// Counts dry work seconds across the requested interval.
        /// </summary>
        public static double WorkSeconds(double start, double duration, int seed)
        {
            double result = 0, elapsed = 0;
            while (elapsed < duration)
            {
                double at = start + elapsed;
                double next = (Math.Floor(at / RainSlotSeconds) + 1) * RainSlotSeconds;
                double step = Math.Min(duration - elapsed, next - at);
                if (CanWork(at, seed))
                {
                    result += step;
                }

                elapsed += step;
            }

            return result;
        }

        /// <summary>
        /// Returns the daylight intensity across dawn, daytime, dusk, and night.
        /// </summary>
        public static float Daylight(double time)
        {
            double phase = Phase(time);
            if (phase < 18)
            {
                return (float)(.22 + .78 * phase / 18);
            }

            if (phase < DaySeconds - 18)
            {
                return 1;
            }

            if (phase < DaySeconds)
            {
                return (float)(.22 + .78 * (DaySeconds - phase) / 18);
            }

            return .22f;
        }
    }

    /// <summary>
    /// Stores the identity, placement, tier, and purchased timers and rewards of one building.
    /// </summary>
    [Serializable]
    public sealed class Building
    {
        public int id;
        public BuildingKind kind;
        public int x;
        public int z;
        public int tier = 1;
        public int rotation;
        public JobPhase phase;
        public double remaining;
        public double duration;
        public int reward;
        public int xpReward;
        public int foodEnergy;
        public int legacyCapacityBonus;
        public double upgradeRemaining;
        public double upgradeDuration;
    }

    /// <summary>
    /// Stores the serializable village and applies economy, placement, jobs, and save migration rules.
    /// </summary>
    [Serializable]
    public sealed class VillageState
    {
        // Shared world extents keep placement, scenery, visitors, and navigation aligned.
        public const int BuildHalfWidth = 30;
        public const int BuildHalfDepth = 10;
        // Fixed backyard furniture is deliberately shared with the world renderer.
        /// <summary>
        /// Describes the footprint of a fixed scenery object used by placement checks.
        /// </summary>
        public readonly struct SceneryBlock
        {
            public readonly int x;
            public readonly int z;
            public readonly float halfX;
            public readonly float halfZ;
            /// <summary>
            /// Creates a fixed scenery footprint from its center and half-extents.
            /// </summary>
            public SceneryBlock(int x, int z, float halfX, float halfZ)
            {
                this.x = x;
                this.z = z;
                this.halfX = halfX;
                this.halfZ = halfZ;
            }
        }

        public static readonly SceneryBlock[] Scenery =
        {
            new SceneryBlock(12, 4, 3.2f, 3.0f), // patio table and chairs
            new SceneryBlock(11, -6, 2.6f, 2.1f), // mower
            new SceneryBlock(5, -9, 2.8f, 1.0f), // giant fallen log
            new SceneryBlock(33, -1, 1.2f, 1.4f),
            new SceneryBlock(16, -9, 1.2f, 1.0f),
            new SceneryBlock(6, 8, 1.0f, 1.0f),
            new SceneryBlock(GardenDivider.X, GardenDivider.GateZ,
                GardenDivider.WallHalfThickness, GardenDivider.RowHalfLength),
            new SceneryBlock(GardenDivider.X, GardenDivider.GateZ,
                GardenDivider.GateApproachDistance, 1.8f), // reserved garden gate approach
            new SceneryBlock(GardenDivider.VillageX, GardenDivider.GateZ,
                GardenDivider.WallHalfThickness, GardenDivider.RowHalfLength),
            new SceneryBlock(GardenDivider.VillageX, GardenDivider.GateZ,
                GardenDivider.GateApproachDistance, 1.8f) // reserved village gate approach
        };
        public int version = 9;
        public bool housingExpansionOwned;
        public bool gardenExpansionOwned;
        public int legacyUnlockLevel; // Highest level reached under the v7 shop schedule.
        public int acorns = 1000;
        public int xp;
        public double energy = 7200;
        public long savedAt;
        public double worldTime = 10;
        public int weatherSeed = 24681;
        public int nextId = 1;
        public int harvests;
        public int feeds;
        public int visitors;
        public int quest;
        public List<Building> buildings = new List<Building>();
        /// <summary>
        /// Returns the cumulative experience required to reach a level.
        /// </summary>
        public static int XpForLevel(int level)
        {
            long n = Math.Max(0, level - 1);
            return (int)Math.Min(int.MaxValue, 15 * n * (n + 1) + 10 * n);
        }

        /// <summary>
        /// Gets whether the specified outer plot has been purchased or preserved by migration.
        /// </summary>
        public bool IsExpansionOwned(LandPlot plot)
        {
            return plot == LandPlot.Housing ? housingExpansionOwned
                : plot == LandPlot.Garden && gardenExpansionOwned;
        }

        /// <summary>
        /// Checks whether a world point belongs to an unpurchased outer plot.
        /// </summary>
        public bool IsLandLocked(float x, float z)
        {
            return (!housingExpansionOwned && LandExpansion.Contains(LandPlot.Housing, x, z))
                || (!gardenExpansionOwned && LandExpansion.Contains(LandPlot.Garden, x, z));
        }

        /// <summary>
        /// Purchases one plot atomically, rejecting invalid, duplicate, or unaffordable purchases.
        /// </summary>
        public bool TryBuyExpansion(LandPlot plot, out string error)
        {
            error = null;
            int price = LandExpansion.Cost(plot);
            if (price == 0)
            {
                error = I18n.Source("expansion.invalid");
            }
            else if (IsExpansionOwned(plot))
            {
                error = I18n.Source("expansion.already_owned");
            }
            else if (acorns < price)
            {
                error = I18n.Source("error.not_enough_acorns");
            }

            if (error != null)
            {
                return false;
            }

            acorns -= price;
            if (plot == LandPlot.Housing)
            {
                housingExpansionOwned = true;
            }
            else
            {
                gardenExpansionOwned = true;
            }

            return true;
        }

        public int Level
        {
            get
            {
                int level = 1;
                while (level < 999 && xp >= XpForLevel(level + 1))
                {
                    level++;
                }

                return level;
            }
        }

        public int XpIntoLevel => xp - XpForLevel(Level);
        public int XpNeededForNextLevel => XpForLevel(Level + 1) - XpForLevel(Level);
        public bool IsNight => ColonyClimate.IsNight(worldTime);
        public bool IsRaining => ColonyClimate.IsRaining(worldTime, weatherSeed);
        public bool CanWorkNow => ColonyClimate.CanWork(worldTime, weatherSeed);

        public const double MaxEnergy = 43200;
        public const int RefillCost = 7;
        public const int QuestAcorns = 75;
        public const int QuestXp = 25;
        public const int VisitorAcorns = 20;
        public const int VisitorXp = 3;
        public const int AntLionAcorns = 25;
        public const int AntLionXp = 4;
        public Building ActiveFood => buildings.Find(b => IsFood(b.kind));

        /// <summary>
        /// Checks whether a building kind represents a meal.
        /// </summary>
        public static bool IsFood(BuildingKind kind)
        {
            return kind == BuildingKind.SugarCube || kind == BuildingKind.Cookie;
        }

        /// <summary>
        /// Returns the existing meal duration for a food kind.
        /// </summary>
        public static double FoodDuration(BuildingKind kind)
        {
            return kind == BuildingKind.Cookie ? 40 : 28;
        }

        /// <summary>
        /// Returns the energy credited when a newly purchased meal finishes.
        /// </summary>
        public static int FoodEnergy(BuildingKind kind)
        {
            return kind == BuildingKind.Cookie ? 28800 : 7200;
        }

        /// <summary>
        /// Counts buildings of the specified kind.
        /// </summary>
        public int Count(BuildingKind kind)
        {
            return buildings.FindAll(b => b.kind == kind).Count;
        }

        /// <summary>
        /// Sums the housing capacity for ants or bees.
        /// </summary>
        public int Capacity(bool bee)
        {
            int result = 0;
            foreach (var b in buildings)
            {
                if (IsHome(b.kind) && IsBee(b.kind) == bee)
                {
                    result += HomeCapacity(b);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns home capacity, including the saved legacy bonus when a building is supplied.
        /// </summary>
        public static int HomeCapacity(Building b)
        {
            return b.tier + (b.kind == BuildingKind.BeeHome ? 0 : 1) + b.legacyCapacityBonus;
        }

        /// <summary>
        /// Returns home capacity, including the saved legacy bonus when a building is supplied.
        /// </summary>
        public static int HomeCapacity(BuildingKind kind, int tier)
        {
            return tier + (kind == BuildingKind.BeeHome ? 0 : 1);
        }

        /// <summary>
        /// Returns the upgrade duration for the current building tier.
        /// </summary>
        public static double UpgradeTime(Building b)
        {
            return (IsHome(b.kind) ? 900 : 1800) * (b.tier == 1 ? 1 : 4);
        }

        /// <summary>
        /// Checks whether a building kind houses residents.
        /// </summary>
        public static bool IsHome(BuildingKind kind)
        {
            return kind == BuildingKind.AntHome
                || kind == BuildingKind.BeeHome
                || kind == BuildingKind.AntBurrow
                || kind == BuildingKind.BeeLantern
                || kind == BuildingKind.AntLeafTent
                || kind == BuildingKind.BeeFlowerHome
                || kind == BuildingKind.AntAcornHome
                || kind == BuildingKind.BeeHoneycombHome;
        }

        /// <summary>
        /// Returns the upgrade price for a building kind and tier.
        /// </summary>
        public static int UpgradeCost(Building b)
        {
            int basis = IsHome(b.kind) ? (IsBee(b.kind) ? 110 : 85) : IsWork(b.kind) ? (IsBee(b.kind) ? 85 : 70) : 0;
            if (b.kind == BuildingKind.AntBurrow
                || b.kind == BuildingKind.BeeLantern
                || b.kind == BuildingKind.TwigYard
                || b.kind == BuildingKind.Lavender
                || b.kind == BuildingKind.LeafDepot
                || b.kind == BuildingKind.Tulip)
            {
                basis += 25;
            }

            if (b.kind == BuildingKind.AntAcornHome || b.kind == BuildingKind.BeeHoneycombHome)
            {
                basis += 75;
            }

            if (b.kind == BuildingKind.AntLeafTent
                || b.kind == BuildingKind.BeeFlowerHome
                || b.kind == BuildingKind.SeedMill
                || b.kind == BuildingKind.Sunflower
                || b.kind == BuildingKind.PebbleYard
                || b.kind == BuildingKind.Bluebell)
            {
                basis += 50;
            }

            return (b.tier == 1 ? basis : basis * 2 + 20) * 3;
        }

        /// <summary>
        /// Counts workers currently assigned to jobs of the requested species.
        /// </summary>
        public int Busy(bool bee)
        {
            int result = 0;
            foreach (var b in buildings)
            {
                if (IsWork(b.kind) && IsBee(b.kind) == bee && b.phase == JobPhase.Working)
                {
                    result += b.tier;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the current purchase price for a building kind.
        /// </summary>
        public static int Cost(BuildingKind kind)
        {
            return kind switch
            {
                BuildingKind.AntHome => 300,
                BuildingKind.BeeHome => 600,
                BuildingKind.Pile => 100,
                BuildingKind.Flower => 200,
                BuildingKind.Clover => 18,
                BuildingKind.Lantern => 40,
                BuildingKind.Bench => 55,
                BuildingKind.Birdbath => 75,
                BuildingKind.FlowerArch => 110,
                BuildingKind.MushroomPatch => 28,
                BuildingKind.AntBurrow => 500,
                BuildingKind.BeeLantern => 1000,
                BuildingKind.TwigYard => 350,
                BuildingKind.SeedMill => 900,
                BuildingKind.Lavender => 650,
                BuildingKind.Sunflower => 1200,
                BuildingKind.AntLeafTent => 850,
                BuildingKind.BeeFlowerHome => 1500,
                BuildingKind.SugarCube => 18,
                BuildingKind.Cookie => 90,
                BuildingKind.Fence => 14,
                BuildingKind.AntAcornHome => 1200,
                BuildingKind.SmallTree => 85,
                BuildingKind.TallGrass => 24,
                BuildingKind.SmallParasol => 65,
                BuildingKind.BeeHoneycombHome => 1300,
                BuildingKind.Tulip => 350,
                BuildingKind.Bluebell => 850,
                BuildingKind.LeafDepot => 220,
                BuildingKind.PebbleYard => 650,
                BuildingKind.WoodenPlanter => 95,
                BuildingKind.Birdhouse => 140,
                BuildingKind.WindChime => 190,
                BuildingKind.LeafFountain => 320,
                BuildingKind.FlowerCart => 450,
                _ => 0
            };
        }

        /// <summary>
        /// Checks whether a building kind provides a worksite.
        /// </summary>
        public static bool IsWork(BuildingKind kind)
        {
            return kind == BuildingKind.Pile
                || kind == BuildingKind.Flower
                || kind == BuildingKind.TwigYard
                || kind == BuildingKind.SeedMill
                || kind == BuildingKind.Lavender
                || kind == BuildingKind.Sunflower
                || kind == BuildingKind.Tulip
                || kind == BuildingKind.Bluebell
                || kind == BuildingKind.LeafDepot
                || kind == BuildingKind.PebbleYard;
        }

        /// <summary>
        /// Checks whether a building kind belongs to bee housing or forage.
        /// </summary>
        public static bool IsBee(BuildingKind kind)
        {
            return kind == BuildingKind.BeeHome
                || kind == BuildingKind.Flower
                || kind == BuildingKind.BeeLantern
                || kind == BuildingKind.Lavender
                || kind == BuildingKind.Sunflower
                || kind == BuildingKind.BeeFlowerHome
                || kind == BuildingKind.BeeHoneycombHome
                || kind == BuildingKind.Tulip
                || kind == BuildingKind.Bluebell;
        }

        /// <summary>
        /// Returns the previous shop schedule used to preserve legacy purchase access.
        /// </summary>
        private static int PreviousUnlockLevel(BuildingKind kind)
        {
            return kind switch
            {
                BuildingKind.AntBurrow or BuildingKind.TwigYard or BuildingKind.Lantern or BuildingKind.Bench => 2,
                BuildingKind.BeeHome or BuildingKind.Flower or BuildingKind.AntLeafTent or BuildingKind.Birdbath or BuildingKind.Cookie => 3,
                BuildingKind.BeeLantern or BuildingKind.SeedMill or BuildingKind.Lavender or BuildingKind.FlowerArch => 4,
                BuildingKind.BeeFlowerHome or BuildingKind.Sunflower => 5,
                _ => 1
            };
        }

        /// <summary>
        /// Checks current level access and eligible legacy unlocks.
        /// </summary>
        public bool IsUnlocked(BuildingKind kind)
        {
            return Level >= UnlockLevel(kind)
                || ((int)kind <= (int)BuildingKind.Fence
                    && legacyUnlockLevel > 0
                    && legacyUnlockLevel >= PreviousUnlockLevel(kind));
        }

        /// <summary>
        /// Returns the current shop unlock level for a building kind.
        /// </summary>
        public static int UnlockLevel(BuildingKind kind)
        {
            return kind switch
            {
                BuildingKind.Lantern or BuildingKind.Bench or BuildingKind.SmallParasol => 2,
                BuildingKind.SmallTree => 3,
                BuildingKind.AntAcornHome => 9,
                BuildingKind.AntBurrow or BuildingKind.Birdbath => 3,
                BuildingKind.FlowerArch => 4,
                BuildingKind.BeeHome or BuildingKind.Flower or BuildingKind.TwigYard => 5,
                BuildingKind.AntLeafTent => 7,
                BuildingKind.BeeLantern => 8,
                BuildingKind.Lavender => 9,
                BuildingKind.SeedMill => 10,
                BuildingKind.BeeFlowerHome => 11,
                BuildingKind.Sunflower => 12,
                BuildingKind.BeeHoneycombHome or BuildingKind.Bluebell => 10,
                BuildingKind.Tulip => 6,
                BuildingKind.LeafDepot => 3,
                BuildingKind.PebbleYard => 8,
                BuildingKind.WoodenPlanter => 3,
                BuildingKind.Birdhouse => 4,
                BuildingKind.WindChime => 5,
                BuildingKind.LeafFountain => 7,
                BuildingKind.FlowerCart => 9,
                _ => 1
            };
        }

        public static readonly double[] JobSeconds =
        {
            900,
            7200,
            21600
        };
        /// <summary>
        /// Calculates the acorn reward for a new job, including worksite and worker bonuses.
        /// </summary>
        public static int JobReward(BuildingKind kind, int tier, int option)
        {
            int baseReward = new[]
            {
                46,
                300,
                780
            }[option];
            int percent = kind switch
            {
                BuildingKind.LeafDepot or BuildingKind.Tulip => 105,
                BuildingKind.TwigYard or BuildingKind.Lavender => 110,
                BuildingKind.PebbleYard or BuildingKind.Bluebell => 118,
                BuildingKind.SeedMill or BuildingKind.Sunflower => 125,
                _ => 100
            };
            if (IsBee(kind))
            {
                percent += 10;
            }

            return baseReward * percent * tier / 100;
        }

        /// <summary>
        /// Calculates the experience reward for a new job and its worker count.
        /// </summary>
        public static int JobXp(BuildingKind kind, int tier, int option)
        {
            return new[]
            {
                18,
                100,
                240
            }[option] * (tier + 1) / 2 + (kind == BuildingKind.SeedMill || kind == BuildingKind.Sunflower ? 5 : 0);
        }

        /// <summary>
        /// Creates a village with the existing starter resources, home, and worksite.
        /// </summary>
        public static VillageState NewGame(long now)
        {
            var state = new VillageState
            {
                savedAt = now,
                weatherSeed = (int)(now % int.MaxValue)
            };
            state.Add(BuildingKind.AntHome, -4, 0);
            state.Add(BuildingKind.Pile, 4, 0);
            return state;
        }

        /// <summary>
        /// Appends a building with the next stable save identifier.
        /// </summary>
        private void Add(BuildingKind kind, int x, int z)
        {
            buildings.Add(new Building { id = nextId++, kind = kind, x = x, z = z });
        }

        // Visitors need a clear patch on either bank, independent of money or unlocks.
        /// <summary>
        /// Checks whether visitors can occupy a patch clear of the stream, buildings, and scenery.
        /// </summary>
        public bool IsVisitorSpot(int x, int z)
        {
            if (Math.Abs(x) < 3
                || Math.Abs(x) > BuildHalfWidth - 1
                || Math.Abs(z) > BuildHalfDepth - 1)
            {
                return false;
            }

            if (IsLandLocked(x, z))
            {
                return false;
            }

            foreach (var obstacle in Scenery)
            {
                if (Math.Abs(x - obstacle.x) < obstacle.halfX + 1f && Math.Abs(z - obstacle.z) < obstacle.halfZ + 1f)
                {
                    return false;
                }
            }

            foreach (var b in buildings)
            {
                if (Math.Abs(x - b.x) < 2 && Math.Abs(z - b.z) < 2)
                {
                    return false;
                }
            }

            return true;
        }

        // Navigation clearance is separate from placement spacing. Leave a corridor through the arch.
        /// <summary>
        /// Checks a building navigation footprint while leaving the gate corridor open.
        /// </summary>
        public static bool BlocksResident(Building b, float x, float z)
        {
            float dx = x - b.x, dz = z - b.z;
            if (b.kind == BuildingKind.FlowerArch)
            {
                float across = b.rotation % 2 == 0 ? dx : dz, along = b.rotation % 2 == 0 ? dz : dx;
                return Math.Abs(Math.Abs(across) - .71f) < .29f && Math.Abs(along) < .34f;
            }

            if (b.kind == BuildingKind.Fence)
            {
                return Math.Abs(dx) < .68f && Math.Abs(dz) < .68f;
            }

            return Math.Abs(dx) < 1.12f && Math.Abs(dz) < 1.12f;
        }

        /// <summary>
        /// Checks whether an adjacent fence touches a gate post for the gate rotation.
        /// </summary>
        public static bool FenceMeetsGate(int fenceX, int fenceZ, int gateX, int gateZ, int gateRotation)
        {
            int dx = Math.Abs(fenceX - gateX), dz = Math.Abs(fenceZ - gateZ);
            return gateRotation % 2 == 0 ? dx == 1 && dz == 0 : dx == 0 && dz == 1;
        }

        /// <summary>
        /// Returns the first placement validation error, or null for a legal position.
        /// </summary>
        public string PlacementError(BuildingKind kind, int x, int z, int ignoreId = 0, int rotation = 0)
        {
            if (!Enum.IsDefined(typeof(BuildingKind), kind))
            {
                return I18n.Source("error.invalid_building");
            }

            if (ignoreId == 0 && !IsUnlocked(kind))
            {
                return I18n.Source("error.unlock_prefix") + UnlockLevel(kind) + ".";
            }

            if (ignoreId == 0 && acorns < Cost(kind))
            {
                return I18n.Source("error.not_enough_acorns");
            }

            if (IsFood(kind) && ignoreId == 0 && ActiveFood != null)
            {
                return I18n.Source("error.food_exists");
            }

            if (IsFood(kind) && ignoreId == 0 && energy >= MaxEnergy)
            {
                return I18n.Source("error.full_energy");
            }

            if (Math.Abs(z) > BuildHalfDepth || Math.Abs(x) > BuildHalfWidth || Math.Abs(x) < 2)
            {
                return I18n.Source("error.outside_ground");
            }

            if (IsWork(kind) && x < 0)
            {
                return I18n.Source("error.worksite_bank");
            }

            if (IsFood(kind) && x < 0)
            {
                return I18n.Source("error.food_bank");
            }

            if (IsHome(kind) && x > 0)
            {
                return I18n.Source("error.home_bank");
            }

            // Existing saves are grandfathered if a building already occupies its own square.
            bool unchanged = ignoreId != 0 && buildings.Exists(b => b.id == ignoreId && b.x == x && b.z == z);
            if (!unchanged)
            {
                if (IsLandLocked(x, z))
                {
                    return I18n.Source("expansion.locked_ground");
                }

                foreach (var obstacle in Scenery)
                {
                    if (Math.Abs(x - obstacle.x) < obstacle.halfX + 0.85f && Math.Abs(z - obstacle.z) < obstacle.halfZ + 0.85f)
                    {
                        return I18n.Source("error.fixed_prop");
                    }
                }
            }

            foreach (var b in buildings)
            {
                if (b.id == ignoreId)
                {
                    continue;
                }

                int dx = Math.Abs(b.x - x), dz = Math.Abs(b.z - z);
                if (kind == BuildingKind.Fence && b.kind == BuildingKind.Fence)
                {
                    if (dx == 0 && dz == 0)
                    {
                        return I18n.Source("error.fence_occupied");
                    }

                    continue; // Adjacent pieces may touch and form corners or junctions.
                }

                if (kind == BuildingKind.Fence && b.kind == BuildingKind.FlowerArch && FenceMeetsGate(x, z, b.x, b.z, b.rotation))
                {
                    continue;
                }

                if (kind == BuildingKind.FlowerArch && b.kind == BuildingKind.Fence && FenceMeetsGate(b.x, b.z, x, z, rotation))
                {
                    continue;
                }

                if (dx < 2 && dz < 2)
                {
                    return I18n.Source("error.too_close");
                }
            }

            if (ignoreId == 0 && IsHome(kind) && buildings.FindAll(b => IsHome(b.kind) && IsBee(b.kind) == IsBee(kind)).Count >= Level + 1)
            {
                return I18n.Source("error.home_limit");
            }

            return null;
        }

        /// <summary>
        /// Validates and purchases a building or meal at the requested position and rotation.
        /// </summary>
        public bool TryBuild(BuildingKind kind, int x, int z, out string error, int rotation = 0)
        {
            if (rotation < 0 || rotation > 3)
            {
                error = I18n.Source("error.invalid_rotation");
                return false;
            }

            error = PlacementError(kind, x, z, 0, rotation);
            if (error != null)
            {
                return false;
            }

            acorns -= Cost(kind);
            Add(kind, x, z);
            buildings[buildings.Count - 1].rotation = rotation;
            if (IsFood(kind))
            {
                var food = buildings[buildings.Count - 1];
                food.duration = food.remaining = FoodDuration(kind);
                food.foodEnergy = FoodEnergy(kind);
            }
            else
            {
                xp += 5;
            }

            return true;
        }

        /// <summary>
        /// Moves and rotates an existing building after validating its destination.
        /// </summary>
        public bool TryMove(int id, int x, int z, int rotation, out string error)
        {
            var b = buildings.Find(item => item.id == id);
            if (b == null)
            {
                error = I18n.Source("error.building_missing");
                return false;
            }

            if (IsFood(b.kind))
            {
                error = I18n.Source("error.food_in_use");
                return false;
            }

            if (b.upgradeRemaining > 0)
            {
                error = I18n.Source("error.upgrade_move");
                return false;
            }

            if (rotation < 0 || rotation > 3)
            {
                error = I18n.Source("error.invalid_rotation");
                return false;
            }

            error = PlacementError(b.kind, x, z, id, rotation);
            if (error != null)
            {
                return false;
            }

            b.x = x;
            b.z = z;
            b.rotation = rotation;
            return true;
        }

        /// <summary>
        /// Starts a paid upgrade after checking tier, worksite state, and available currency.
        /// </summary>
        public bool Upgrade(int id, out string error)
        {
            error = null;
            var b = buildings.Find(item => item.id == id);
            if (b == null || (!IsHome(b.kind) && !IsWork(b.kind)))
            {
                error = I18n.Source("error.not_upgradeable");
            }
            else if (b.upgradeRemaining > 0)
            {
                error = I18n.Source("error.already_upgrading");
            }
            else if (b.tier >= 3)
            {
                error = I18n.Source("error.max_level");
            }
            else if (IsWork(b.kind) && (b.phase == JobPhase.Working || b.phase == JobPhase.Ready))
            {
                error = I18n.Source("error.harvest_before_upgrade");
            }
            else if (acorns < UpgradeCost(b))
            {
                error = I18n.Source("error.upgrade_cost");
            }

            if (error != null)
            {
                return false;
            }

            acorns -= UpgradeCost(b);
            b.upgradeDuration = b.upgradeRemaining = UpgradeTime(b);
            return true;
        }

        // Keep the original save key, migrate additive fields, preserve currency/jobs/IDs.
        /// <summary>
        /// Applies the existing save migrations in order and validates the resulting village.
        /// </summary>
        public bool Migrate()
        {
            if (version == 1 && buildings != null)
            {
                foreach (var b in buildings)
                {
                    if (b != null)
                    {
                        b.tier = 1;
                        b.rotation = 0;
                    }
                }

                version = 2;
            }

            if (version == 2)
            {
                worldTime = 10;
                weatherSeed = (int)(savedAt % int.MaxValue);
                version = 3;
            }

            if (version == 3)
            {
                version = 4; // Existing homes gain one resident; worksite tiers remain at one.
            }

            if (version == 4)
            {
                // The earlier small hive held one extra bee. Preserve that resident in old villages.
                foreach (var b in buildings)
                {
                    if (b != null && b.kind == BuildingKind.BeeHome)
                    {
                        b.legacyCapacityBonus = 1;
                    }
                }

                version = 5;
            }

            if (version == 5)
            {
                version = 6; // Food is additive; existing jobs and currency remain unchanged.
            }

            if (version == 6)
            {
                // Keep the player's reached level and fractional progress when the XP curve grows.
                int oldLevel = 1 + xp / 40, oldProgress = xp % 40;
                xp = XpForLevel(oldLevel) + (XpForLevel(oldLevel + 1) - XpForLevel(oldLevel)) * oldProgress / 40;
                foreach (var b in buildings)
                {
                    if (b != null && IsFood(b.kind) && b.foodEnergy == 0)
                    {
                        b.foodEnergy = b.kind == BuildingKind.Cookie ? 300 : 90;
                    }
                }

                version = 7; // Existing job, upgrade and meal timers remain as saved.
            }

            if (version == 7)
            {
                legacyUnlockLevel = Level;
                version = 8;
            }

            if (version == 8)
            {
                // Preserve plots already used by older saves, without charging or relocating buildings.
                if (buildings != null)
                {
                    housingExpansionOwned = buildings.Exists(b => b != null
                        && LandExpansion.Contains(LandPlot.Housing, b.x, b.z));
                    gardenExpansionOwned = buildings.Exists(b => b != null
                        && LandExpansion.Contains(LandPlot.Garden, b.x, b.z));
                }

                version = 9;
            }

            return IsValid();
        }

        /// <summary>
        /// Assigns workers and snapshots the selected job duration and rewards.
        /// </summary>
        public bool StartJob(int id, int option, out string error)
        {
            error = null;
            var b = buildings.Find(item => item.id == id);
            if (b == null || !IsWork(b.kind) || b.phase != JobPhase.Idle || b.upgradeRemaining > 0)
            {
                error = I18n.Source("error.worksite_not_ready");
            }
            else if (option < 0 || option > 2)
            {
                error = I18n.Source("error.invalid_job");
            }
            else if (energy <= 0)
            {
                error = I18n.Source("error.feed_first");
            }
            else if (Capacity(IsBee(b.kind)) - Busy(IsBee(b.kind)) < b.tier)
            {
                error = I18n.Source("error.no_workers");
            }

            if (error != null)
            {
                return false;
            }

            b.duration = b.remaining = JobSeconds[option];
            b.reward = JobReward(b.kind, b.tier, option);
            b.xpReward = JobXp(b.kind, b.tier, option);
            b.phase = JobPhase.Working;
            return true;
        }

        // Eating pauses work and energy drain. Afterwards, jobs resume from their saved timers.
        // Clock rollback never adds time; offline time is bounded as before.
        /// <summary>
        /// Advances world time, meals, energy, jobs, and upgrades using the existing offline limit.
        /// </summary>
        public bool Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0)
            {
                return false;
            }

            seconds = Math.Min(seconds, 86400);
            bool completed = false;
            double left = seconds;
            while (left > 0)
            {
                var food = ActiveFood;
                double step = food == null ? left : Math.Min(left, food.remaining);
                if (food != null)
                {
                    food.remaining = Math.Max(0, food.remaining - step);
                }
                else
                {
                    double active = Math.Min(step, energy);
                    double work = ColonyClimate.WorkSeconds(worldTime, active, weatherSeed);
                    energy = Math.Max(0, energy - step);
                    foreach (var job in buildings)
                    {
                        if (job.phase != JobPhase.Working || !IsWork(job.kind))
                        {
                            continue;
                        }

                        job.remaining = Math.Max(0, job.remaining - work);
                        if (job.remaining <= 0)
                        {
                            job.phase = JobPhase.Ready;
                        }
                    }
                }

                worldTime += step;
                left -= step;
                if (food != null && food.remaining <= 0)
                {
                    energy = Math.Min(MaxEnergy, energy + food.foodEnergy);
                    buildings.Remove(food);
                    feeds++;
                    completed = true;
                }
            }

            foreach (var b in buildings)
            {
                if (b.upgradeRemaining > 0)
                {
                    b.upgradeRemaining = Math.Max(0, b.upgradeRemaining - seconds);
                    if (b.upgradeRemaining == 0)
                    {
                        b.tier++;
                        b.upgradeDuration = 0;
                        xp += 10;
                        completed = true;
                    }
                }
            }

            return completed;
        }

        /// <summary>
        /// Credits a completed job once and marks its worksite empty.
        /// </summary>
        public bool Collect(int id)
        {
            var b = buildings.Find(item => item.id == id);
            if (b == null || b.phase != JobPhase.Ready)
            {
                return false;
            }

            acorns += b.reward;
            xp += b.xpReward;
            b.phase = JobPhase.Empty;
            harvests++;
            return true;
        }

        /// <summary>
        /// Pays the refill cost and makes an empty worksite available for another job.
        /// </summary>
        public bool Refill(int id)
        {
            var b = buildings.Find(item => item.id == id);
            if (b == null || b.phase != JobPhase.Empty || acorns < RefillCost)
            {
                return false;
            }

            acorns -= RefillCost;
            b.phase = JobPhase.Idle;
            return true;
        }

        public bool QuestReady => quest == 0
            ? harvests >= 1
            : quest == 1
                ? buildings.FindAll(b => IsHome(b.kind)
            && !IsBee(b.kind)).Count >= 2
                : quest == 2
                    ? feeds >= 1
                    : quest == 3
                        ? buildings.FindAll(b => IsHome(b.kind)
            && IsBee(b.kind)).Count >= 1
                        : quest == 4 ? buildings.FindAll(b => IsWork(b.kind)
            && IsBee(b.kind)).Count >= 1 : quest == 5 ? visitors >= 1 : false;

        /// <summary>
        /// Credits the current completed tutorial reward and advances its index.
        /// </summary>
        public bool ClaimQuest()
        {
            if (!QuestReady)
            {
                return false;
            }

            acorns += QuestAcorns;
            xp += QuestXp;
            quest++;
            return true;
        }

        /// <summary>
        /// Credits the ant lion reward without advancing the ladybug tutorial objective.
        /// </summary>
        public void RewardAntLion()
        {
            acorns += AntLionAcorns;
            xp += AntLionXp;
        }

        /// <summary>
        /// Credits the ladybug reward and increments the completed visitor count.
        /// </summary>
        public void HelpVisitor()
        {
            acorns += VisitorAcorns;
            xp += VisitorXp;
            visitors++;
        }

        /// <summary>
        /// Validates saved values, identifiers, building types, tiers, and timer limits.
        /// </summary>
        public bool IsValid()
        {
            if (version != 9
                || legacyUnlockLevel < 0
                || legacyUnlockLevel > 999
                || buildings == null
                || buildings.Count > 240
                || acorns < 0
                || xp < 0
                || quest < 0
                || quest > 6
                || double.IsNaN(energy)
                || energy < 0
                || energy > MaxEnergy
                || double.IsNaN(worldTime)
                || double.IsInfinity(worldTime)
                || worldTime < 0
                || worldTime > 1e12
                || buildings.FindAll(b => b != null
                && IsFood(b.kind)).Count > 1)
            {
                return false;
            }

            var ids = new HashSet<int>();
            foreach (var b in buildings)
            {
                if (b == null
                    || !ids.Add(b.id)
                    || b.id <= 0
                    || b.id >= nextId
                    || b.tier < 1
                    || b.tier > 3
                    || b.rotation < 0
                    || b.rotation > 3
                    || (!IsHome(b.kind)
                        && !IsWork(b.kind)
                        && b.tier != 1)
                    || !Enum.IsDefined(typeof(BuildingKind), b.kind)
                    || !Enum.IsDefined(typeof(JobPhase), b.phase)
                    || double.IsNaN(b.remaining)
                    || double.IsInfinity(b.remaining)
                    || b.remaining < 0
                    || b.remaining > 21600
                    || (IsFood(b.kind)
                        && (b.phase != JobPhase.Idle
                            || b.remaining <= 0
                            || b.duration != FoodDuration(b.kind)
                            || b.foodEnergy <= 0
                            || b.foodEnergy > 28800))
                    || b.legacyCapacityBonus < 0
                    || b.legacyCapacityBonus > 1
                    || (b.legacyCapacityBonus > 0
                        && b.kind != BuildingKind.BeeHome)
                    || double.IsNaN(b.upgradeRemaining)
                    || double.IsInfinity(b.upgradeRemaining)
                    || b.upgradeRemaining < 0
                    || b.upgradeRemaining > 7200
                    || double.IsNaN(b.upgradeDuration)
                    || double.IsInfinity(b.upgradeDuration)
                    || b.upgradeDuration < 0
                    || b.upgradeDuration > 7200
                    || (b.upgradeRemaining > 0
                        && (b.upgradeDuration < b.upgradeRemaining
                            || b.tier >= 3))
                    || Math.Abs(b.x) > BuildHalfWidth
                    || Math.Abs(b.x) < 2
                    || Math.Abs(b.z) > BuildHalfDepth)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
