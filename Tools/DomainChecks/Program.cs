using System;
using System.Text.Json;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using LittleColony;

/// <summary>
/// Runs the standalone village domain regression checks without Unity.
/// </summary>
static class Program
{
    /// <summary>
    /// Exercises paid plot ownership, placement, visitors, purchases, and save migration.
    /// </summary>
    private static void CheckExpansions()
    {
        var state = ExistingVillage();
        state.acorns = 50000;
        Assert(!state.housingExpansionOwned && !state.gardenExpansionOwned, "New plots start locked");
        Assert(state.PlacementError(BuildingKind.AntHome, -24, 6) == I18n.Source("expansion.locked_ground")
            && state.PlacementError(BuildingKind.Pile, 24, 6) == I18n.Source("expansion.locked_ground"),
            "Locked plots reject home and worksite placement");
        Assert(!state.TryMove(state.buildings[0].id, -24, 6, 0, out _), "Cannot move a home onto locked land");
        for (int x = 19; x <= 30; x++)
        {
            for (int z = -10; z <= 10; z++)
            {
                Assert(state.IsLandLocked(x, z) && state.IsLandLocked(-x, z), "All outer tiles are locked");
                Assert(!state.IsVisitorSpot(x, z) && !state.IsVisitorSpot(-x, z), "Visitors avoid locked plots");
            }
        }

        Assert(!state.IsLandLocked(-17, 6) && !state.IsLandLocked(17, 6), "Original ground remains open");
        Assert(!state.TryBuyExpansion((LandPlot)99, out _) && state.acorns == 50000, "Invalid purchase is atomic");
        state.acorns = LandExpansion.Cost(LandPlot.Housing) - 1;
        Assert(!state.TryBuyExpansion(LandPlot.Housing, out _) && !state.housingExpansionOwned
            && state.acorns == 9999, "Insufficient funds change nothing");
        state.acorns = LandExpansion.Cost(LandPlot.Housing);
        Assert(state.TryBuyExpansion(LandPlot.Housing, out _) && state.acorns == 0
            && state.housingExpansionOwned && !state.gardenExpansionOwned, "Exact funds buy only the selected plot");
        var housingReload = Copy(state);
        Assert(housingReload.Migrate() && housingReload.housingExpansionOwned
            && !housingReload.gardenExpansionOwned && housingReload.acorns == 0,
            "A single purchase persists without granting the other plot");
        Assert(state.buildings.Count == 2 && state.nextId == 3 && state.xp == 0
            && state.energy == 7200, "Buying land preserves buildings, XP, energy, and identifiers");
        state.acorns = 50000;
        Assert(!state.TryBuyExpansion(LandPlot.Housing, out _) && state.acorns == 50000, "Repeated purchase cannot charge twice");
        Assert(state.TryBuild(BuildingKind.AntHome, -24, 6, out _)
            && !state.TryBuild(BuildingKind.Pile, 24, 6, out _), "Only purchased ground becomes buildable");
        int gardenBalance = state.acorns;
        Assert(state.TryBuyExpansion(LandPlot.Garden, out _) && state.acorns == gardenBalance - 15000,
            "Garden purchase charges its own higher price exactly once");
        Assert(state.TryBuild(BuildingKind.Pile, 24, 6, out _),
            "Garden purchase enables worksites");
        var restored = Copy(state);
        Assert(restored.Migrate() && restored.housingExpansionOwned && restored.gardenExpansionOwned
            && restored.acorns == state.acorns, "Both purchases and balance survive reload");
        var freshReload = Copy(ExistingVillage());
        Assert(freshReload.Migrate() && !freshReload.housingExpansionOwned && !freshReload.gardenExpansionOwned,
            "Reload cannot grant unpaid land");
        for (int side = 0; side < 3; side++)
        {
            var legacy = Rich();
            if (side < 2)
            {
                Assert(legacy.TryBuild(side == 0 ? BuildingKind.AntHome : BuildingKind.Pile,
                    side == 0 ? -24 : 24, 6, out _), "Prepare occupied legacy plot");
            }

            legacy.version = 8;
            legacy.housingExpansionOwned = false;
            legacy.gardenExpansionOwned = false;
            Assert(legacy.StartJob(legacy.buildings[1].id, 0, out _), "Prepare a saved work contract");
            var savedContract = Copy(legacy.buildings[1]);
            int balance = legacy.acorns;
            Assert(legacy.Migrate() && legacy.version == 9 && legacy.acorns == balance
                && legacy.housingExpansionOwned == (side == 0)
                && legacy.gardenExpansionOwned == (side == 1), "Migration preserves only occupied legacy plots");
            Assert(legacy.buildings[1].id == savedContract.id
                && legacy.buildings[1].phase == savedContract.phase
                && legacy.buildings[1].remaining == savedContract.remaining
                && legacy.buildings[1].reward == savedContract.reward
                && legacy.buildings[1].xpReward == savedContract.xpReward,
                "Expansion migration preserves running contract identity, timer, and rewards");
            Assert(Copy(legacy).Migrate() && legacy.Migrate(), "Migration is repeatable and survives reload");
        }
    }

    static int checks;
    /// <summary>
    /// Records a successful check or throws with its diagnostic message.
    /// </summary>
    static void Assert(bool value, string message)
    {
        if (!value)
        {
            throw new Exception(message);
        }

        checks++;
    }

    /// <summary>
    /// Creates a funded test village at the supplied experience total.
    /// </summary>
    static VillageState Rich(int xp = 3000)
    {
        var s = ExistingVillage();
        s.acorns = 10000;
        s.xp = xp;
        s.energy = VillageState.MaxEnergy;
        s.housingExpansionOwned = true;
        s.gardenExpansionOwned = true;
        return s;
    }

    /// <summary>
    /// Round-trips a value through JSON with the existing field serialization options.
    /// </summary>
    static T Copy<T>(T value)
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true, IgnoreReadOnlyProperties = true }),
            new JsonSerializerOptions { IncludeFields = true });
    }

    /// <summary>Creates a historical two-building fixture for existing economy and save regression scenarios.</summary>
    /// <returns>A village representing a previously saved starter layout.</returns>
    private static VillageState ExistingVillage()
    {
        var state = VillageState.NewGame(100);
        state.buildings.Add(new Building { id = state.nextId++, kind = BuildingKind.AntHome, x = -4, z = 0 });
        state.buildings.Add(new Building { id = state.nextId++, kind = BuildingKind.Pile, x = 4, z = 0 });
        return state;
    }

    /// <summary>Checks empty first entry, persistence, and progression after buying the first home and worksite.</summary>
    private static void CheckNewVillage()
    {
        var state = VillageState.NewGame(100);
        Assert(state.acorns == 1000 && state.buildings.Count == 0 && state.nextId == 1,
            "New village has 1,000 acorns and no granted buildings");
        Assert(state.Level == 1 && state.xp == 0 && state.Capacity(false) == 0 && state.Capacity(true) == 0,
            "An empty new village has no residents or progression");
        Assert(state.IsValid() && Copy(state).Migrate() && Copy(state).buildings.Count == 0,
            "An empty village survives save reload without adding starter buildings");
        Assert(!state.QuestReady, "Empty village does not auto-complete the first tutorial objective");
        Assert(state.TryBuild(BuildingKind.AntHome, -4, 0, out _)
            && state.TryBuild(BuildingKind.Pile, 4, 0, out _),
            "Starting funds allow the player to buy a home and a worksite");
        Assert(state.acorns == 1000 - VillageState.Cost(BuildingKind.AntHome) - VillageState.Cost(BuildingKind.Pile)
            && state.buildings[0].id == 1 && state.buildings[1].id == 2,
            "First construction charges its price and uses fresh stable identifiers");
        state.weatherSeed = 419;
        state.worldTime = 10;
        Assert(state.StartJob(state.buildings[1].id, 0, out _), "Player-built home enables the first harvest job");
        state.Advance(1100);
        Assert(state.Collect(state.buildings[1].id) && state.ClaimQuest(),
            "An empty start can complete the first tutorial through paid construction");
        var restored = Copy(state);
        Assert(restored.Migrate() && restored.acorns == state.acorns && restored.buildings.Count == 2
            && restored.quest == 1 && restored.xp == state.xp,
            "Returning guest keeps purchases, currency and tutorial progress");
    }

    /// <summary>
    /// Runs the domain regression scenarios and prints the number of successful checks.
    /// </summary>
    static void Main()
    {
        CheckNewVillage();
        var s = ExistingVillage();
        int pile = s.buildings[1].id;
        Assert(s.IsValid() && s.version == 9, "New game and save version");
        Assert(s.energy >= VillageState.JobSeconds[0], "Starter energy supports a short task");
        Assert(VillageState.JobSeconds[0] == 900
            && VillageState.JobSeconds[1] == 7200
            && VillageState.JobSeconds[2] == 21600,
            "Work lengths: 15 minutes, 2 hours and 6 hours");
        Assert(VillageState.XpForLevel(1) == 0
            && VillageState.XpForLevel(2) == 40
            && VillageState.XpForLevel(3) == 110
            && VillageState.XpForLevel(4) == 210,
            "Increasing XP thresholds");
        s.xp = 39;
        Assert(s.Level == 1 && s.XpIntoLevel == 39 && s.XpNeededForNextLevel == 40, "Level-one progress");
        s.xp = 40;
        Assert(s.Level == 2 && s.XpIntoLevel == 0 && s.XpNeededForNextLevel == 70, "Level-two progress");
        s.xp = 110;
        Assert(s.Level == 3 && s.XpNeededForNextLevel == 100, "Level-three progress");
        Assert(VillageState.UnlockLevel(BuildingKind.AntHome) == 1
            && VillageState.UnlockLevel(BuildingKind.TwigYard) == 5
            && VillageState.UnlockLevel(BuildingKind.BeeHome) == 5
            && VillageState.UnlockLevel(BuildingKind.SeedMill) == 10
            && VillageState.UnlockLevel(BuildingKind.BeeFlowerHome) == 11,
            "Unlocks are staggered");
        s.xp = 0;
        Assert(!s.TryBuild(BuildingKind.TwigYard, 6, 5, out var locked) && locked.Contains(I18n.Source("test.unlock_level_five")), "Early worksite is locked");
        Assert(!s.TryBuild(BuildingKind.BeeHome, -8, 3, out _), "Bee home stays locked until level five");
        Assert(!s.TryBuild(BuildingKind.Pile, -8, 3, out _)
            && !s.TryBuild(BuildingKind.AntHome, 6, 3, out _),
            "Work/home banks stay separate");
        Assert(!s.TryBuild(BuildingKind.Pile, 12, 4, out _), "Fixed patio blocks building");
        Assert(s.StartJob(pile, 0, out _), "Starter task starts");
        Assert(s.buildings[1].duration == 900
            && s.buildings[1].reward == 46
            && s.buildings[1].xpReward == 18,
            "Short task stores current payout");
        Assert(!s.StartJob(pile, 0, out _), "Cannot assign twice");
        s.energy = 5;
        s.Advance(10);
        Assert(s.energy == 0 && s.buildings[1].remaining == 895, "Hunger pauses remaining work");
        Assert(s.TryBuild(BuildingKind.SugarCube, 7, 5, out _)
            && s.ActiveFood.foodEnergy == 7200
            && s.acorns == 1000 - VillageState.Cost(BuildingKind.SugarCube),
            "Sugar has new cost and energy");
        Assert(!s.TryBuild(BuildingKind.SugarCube, 9, 5, out _), "Only one meal at a time");
        double before = s.buildings[1].remaining;
        s.Advance(10);
        Assert(s.ActiveFood.remaining == 18 && s.buildings[1].remaining == before, "Eating pauses work");
        Assert(Copy(s).Migrate(), "Meal survives save round trip");
        Assert(s.Advance(18) && s.ActiveFood == null && s.energy == 7200, "Finished meal frees ground and feeds village");
        Assert(s.TryBuild(BuildingKind.Pile, 7, 5, out _), "Meal tile becomes buildable again");
        s.weatherSeed = 419;
        s.worldTime = 10;
        s.Advance(1100);
        Assert(s.buildings[1].phase == JobPhase.Ready, "Short task completes after feeding");
        int cash = s.acorns, xp = s.xp;
        Assert(s.Collect(pile) && s.acorns == cash + 46 && s.xp == xp + 18, "Harvest credits saved reward and XP once");
        Assert(!s.Collect(pile) && s.Refill(pile) && s.buildings[1].phase == JobPhase.Idle, "Harvest once, pay to refill");
        Assert(s.ClaimQuest()
            && s.acorns == cash + 46 - VillageState.RefillCost + VillageState.QuestAcorns,
            "Quest reward funds early progression");
        var expanded = Rich();
        Assert(expanded.TryBuild(BuildingKind.AntHome, -30, 0, out _)
            && expanded.TryBuild(BuildingKind.Pile, 30, 0, out _),
            "Homes and worksites build at both expanded horizontal edges");
        Assert(expanded.TryBuild(BuildingKind.Clover, -24, -10, out _)
            && expanded.TryBuild(BuildingKind.Clover, 24, 10, out _),
            "Expanded ground retains the full original depth");
        Assert(!expanded.TryBuild(BuildingKind.AntHome, -31, 5, out _)
            && !expanded.TryBuild(BuildingKind.Pile, 31, 5, out _)
            && !expanded.TryBuild(BuildingKind.Clover, 24, 11, out _)
            && !expanded.TryBuild(BuildingKind.Clover, -24, -11, out _),
            "Placement rejects all positions beyond the expanded boundary");
        Assert(!expanded.TryBuild(BuildingKind.AntHome, 24, 0, out _)
            && !expanded.TryBuild(BuildingKind.Pile, -24, 0, out _)
            && !expanded.TryBuild(BuildingKind.Clover, 0, 5, out _),
            "Expansion preserves home, worksite, and stream restrictions");
        Assert(expanded.TryMove(expanded.buildings[0].id, -26, 5, 1, out _),
            "Existing buildings can move and rotate into the extension");
        var reloadedExpansion = Copy(expanded);
        Assert(reloadedExpansion.Migrate() && reloadedExpansion.IsValid()
            && reloadedExpansion.version == 9
            && reloadedExpansion.buildings[0].x == -26
            && reloadedExpansion.buildings.Exists(b => b.x == 30),
            "Expanded placements survive save reload without changing save format");
        var oldMapSave = Copy(ExistingVillage());
        Assert(oldMapSave.Migrate() && oldMapSave.buildings[0].x == -4
            && oldMapSave.buildings[1].x == 4 && oldMapSave.acorns == 1000,
            "Original village coordinates and resources remain unchanged");
        Assert(expanded.IsVisitorSpot(-29, -5) && expanded.IsVisitorSpot(29, 5)
            && !expanded.IsVisitorSpot(-30, -5) && !expanded.IsVisitorSpot(30, 5),
            "Visitors use both new banks and keep clearance from the perimeter");
        var rich = Rich();
        Assert(rich.TryBuild(BuildingKind.AntBurrow, -8, 3, out _), "Upgraded ant home is buildable");
        int burrow = rich.buildings[rich.buildings.Count - 1].id;
        Assert(rich.Upgrade(burrow, out _), "First upgrade begins");
        var home = rich.buildings.Find(b => b.id == burrow);
        Assert(home.tier == 1 && home.upgradeRemaining == 900, "Upgrade has real fifteen-minute delay");
        Assert(!rich.Upgrade(burrow, out _) && !rich.TryMove(burrow, -9, 3, 0, out _), "In-progress upgrade blocks repeats and moves");
        rich.Advance(899);
        Assert(home.tier == 1 && home.upgradeRemaining == 1, "Upgrade retains partial progress");
        Assert(Copy(rich).IsValid(), "Pending upgrade survives serialization");
        rich.Advance(1);
        Assert(home.tier == 2 && home.upgradeRemaining == 0, "Upgrade completes once");
        Assert(VillageState.UpgradeTime(home) == 3600 && rich.Upgrade(burrow, out _), "Higher tier upgrade lasts longer");
        rich.Advance(3600);
        Assert(home.tier == 3 && rich.Capacity(false) == 6, "Third tier adds a resident");
        Assert(!rich.Upgrade(burrow, out _), "Maximum tier cannot be upgraded");
        Assert(rich.TryBuild(BuildingKind.TwigYard, 6, 5, out _), "Specialized worksite builds");
        int yard = rich.buildings[rich.buildings.Count - 1].id;
        Assert(VillageState.UpgradeTime(rich.buildings.Find(b => b.id == yard)) == 1800, "Worksite upgrade lasts thirty minutes");
        Assert(rich.Upgrade(yard, out _), "Worksite upgrade begins");
        rich.Advance(1800);
        Assert(rich.Upgrade(yard, out _), "Second worksite upgrade begins");
        Assert(rich.buildings.Find(b => b.id == yard).upgradeRemaining == 7200, "Third worksite tier requires two hours");
        rich.Advance(7200);
        Assert(rich.StartJob(yard, 1, out _) && rich.Busy(false) == 3, "Three workers share an upgraded site");
        Assert(rich.buildings.Find(b => b.id == yard).reward == VillageState.JobReward(BuildingKind.TwigYard, 3, 1),
            "Specialized site payout matches offer");
        Assert(VillageState.JobReward(BuildingKind.SeedMill, 1, 0) > VillageState.JobReward(BuildingKind.TwigYard, 1, 0)
            && VillageState.JobReward(BuildingKind.TwigYard, 1, 0) > VillageState.JobReward(BuildingKind.Pile, 1, 0),
            "Later worksites pay more");
        Assert(VillageState.JobReward(BuildingKind.Flower, 1, 0) > VillageState.JobReward(BuildingKind.Pile, 1, 0),
            "Bee forage has a reward premium");
        Assert(VillageState.JobXp(BuildingKind.Pile, 3, 0) < VillageState.JobXp(BuildingKind.Pile, 1, 0) * 3,
            "Shared work does not triple XP");
        var recovery = ExistingVillage();
        recovery.acorns = 0;
        recovery.HelpVisitor();
        Assert(recovery.acorns >= VillageState.Cost(BuildingKind.SugarCube), "Visitor can rescue a village with zero funds");
        var old = ExistingVillage();
        old.version = 6;
        old.energy = 50;
        old.acorns = 123;
        old.buildings[1].phase = JobPhase.Working;
        old.buildings[1].duration = 120;
        old.buildings[1].remaining = 70;
        old.buildings[1].reward = 145;
        old.buildings.Add(new Building { id = old.nextId++, kind = BuildingKind.Cookie, x = 7, z = 5, duration = 40, remaining = 20 });
        Assert(old.Migrate()
            && old.version == 9
            && old.energy == 50
            && old.acorns == 123
            && old.buildings[1].remaining == 70
            && old.buildings[1].reward == 145,
            "V6 migration keeps live work and money");
        Assert(old.ActiveFood.foodEnergy == 300 && old.IsValid(), "Purchased old meal keeps original energy reward");
        old.Advance(20);
        Assert(old.ActiveFood == null && old.energy == 350, "Old meal completes with historical reward");
        var oldLevel = ExistingVillage();
        oldLevel.version = 6;
        oldLevel.xp = 126;
        Assert(oldLevel.Migrate()
            && oldLevel.Level == 4
            && oldLevel.XpIntoLevel > 0,
            "Migration keeps a veteran village at its reached level");
        var oldUpgrade = ExistingVillage();
        oldUpgrade.version = 6;
        oldUpgrade.buildings[0].upgradeRemaining = 20;
        oldUpgrade.buildings[0].upgradeDuration = 25;
        Assert(oldUpgrade.Migrate()
            && oldUpgrade.buildings[0].upgradeRemaining == 20
            && oldUpgrade.IsValid(),
            "Old pending upgrade timer remains valid");
        var v1 = ExistingVillage();
        v1.version = 1;
        v1.buildings[0].tier = 0;
        Assert(v1.Migrate() && v1.version == 9 && v1.buildings[0].tier == 1, "Original save migrates through all versions");
        var fence = ExistingVillage();
        int fenceMoney = fence.acorns;
        Assert(fence.TryBuild(BuildingKind.Fence, -8, 5, out _)
            && fence.acorns == fenceMoney - VillageState.Cost(BuildingKind.Fence),
            "Fence is an affordable early decoration");
        Assert(fence.TryBuild(BuildingKind.Fence, -7, 5, out _)
            && fence.TryBuild(BuildingKind.Fence, -7, 6, out _),
            "Fence pieces can form a line and corner on adjacent tiles");
        Assert(!fence.TryBuild(BuildingKind.Fence, -8, 5, out _), "Two fence posts cannot occupy the same tile");
        Assert(!fence.TryBuild(BuildingKind.Clover, -6, 5, out _), "Ordinary decoration cannot overlap a fence");
        Assert(fence.TryMove(fence.buildings[fence.buildings.Count - 1].id, -8, 6, 1, out _)
            && fence.IsValid(),
            "Fence can move and rotate beside another fence");
        Assert(Copy(fence).Migrate(), "Existing save format accepts added fence kind");
        Assert(rich.IsValid() && Copy(rich).IsValid(), "Long tasks and upgrades survive save validation");
        // Migration preserves purchases, timers, XP and shop access, and is idempotent.
        var veteran = ExistingVillage();
        veteran.version = 7;
        veteran.xp = 110;
        veteran.acorns = 123;
        veteran.energy = 350;
        veteran.buildings[1].phase = JobPhase.Working;
        veteran.buildings[1].duration = 2700;
        veteran.buildings[1].remaining = 2000;
        veteran.buildings[1].reward = 330;
        veteran.buildings[1].xpReward = 100;
        veteran.buildings.Add(new Building { id = veteran.nextId++, kind = BuildingKind.Cookie, x = 7, z = 5, duration = 40, remaining = 20, foodEnergy = 1800 });
        Assert(veteran.Migrate()
            && veteran.Level == 3
            && veteran.acorns == 123
            && veteran.energy == 350,
            "V7 preserves level, currency and energy");
        Assert(veteran.IsUnlocked(BuildingKind.BeeHome)
            && !veteran.IsUnlocked(BuildingKind.SeedMill),
            "Old unlocked items stay available without unlocking future items");
        Assert(veteran.buildings[1].remaining == 2000
            && veteran.buildings[1].reward == 330
            && veteran.buildings[1].xpReward == 100
            && veteran.ActiveFood.foodEnergy == 1800,
            "V7 contracts retain all purchased values");
        Assert(Copy(veteran).Migrate() && veteran.Migrate() && veteran.legacyUnlockLevel == 3, "Migration survives reload and repeats");
        var fresh = ExistingVillage();
        fresh.xp = 110;
        Assert(!fresh.IsUnlocked(BuildingKind.BeeHome)
            && fresh.IsUnlocked(BuildingKind.Cookie),
            "New villages unlock bees at five and can prepare long trips immediately");
        fresh.xp = VillageState.XpForLevel(5);
        Assert(fresh.IsUnlocked(BuildingKind.BeeHome)
            && fresh.IsUnlocked(BuildingKind.TwigYard),
            "Level five opens bees and medium worksite");
        for (int option = 0; option < 3; option++)
        {
            var online = ExistingVillage();
            online.energy = 0;
            Assert(online.TryBuild(BuildingKind.Cookie, 7, 5, out _), "Long-trip food is affordable at level one");
            online.Advance(40);
            Assert(online.StartJob(online.buildings[1].id, option, out _), "Fed long task starts");
            var offline = Copy(online);
            double elapsed = VillageState.JobSeconds[option] * 1.2;
            offline.Advance(elapsed);
            for (int tick = 0; tick < (int)elapsed; tick++)
            {
                online.Advance(1);
            }

            Assert(offline.buildings[1].phase == JobPhase.Ready
                && online.buildings[1].phase == JobPhase.Ready
                && Math.Abs(online.energy - offline.energy) < .001,
                "Online/offline agree including rain for each task length");
            Assert(Copy(offline).Migrate() && offline.Collect(offline.buildings[1].id), "Long job survives save and collects");
        }

        double previousRate = double.MaxValue;
        for (int option = 0; option < 3; option++)
        {
            double net = VillageState.JobReward(BuildingKind.Pile, 1, option) - VillageState.RefillCost - VillageState.Cost(BuildingKind.Cookie) * VillageState.JobSeconds[option] / VillageState.FoodEnergy(BuildingKind.Cookie);
            double rate = net / VillageState.JobSeconds[option];
            Assert(net > 0 && rate < previousRate, "Net hourly income rewards frequent visits after refill and food costs");
            previousRate = rate;
        }

        var cap = ExistingVillage();
        cap.energy = VillageState.MaxEnergy - 1;
        Assert(cap.TryBuild(BuildingKind.Cookie, 7, 5, out _), "Meal near cap starts");
        cap.Advance(40);
        Assert(cap.energy == VillageState.MaxEnergy && cap.IsValid(), "Food respects twelve-hour energy cap");
        var garden = Rich(VillageState.XpForLevel(9));
        Assert((int)BuildingKind.Fence == 20 && (int)BuildingKind.AntAcornHome == 21, "New types append without changing saved IDs");
        Assert(garden.TryBuild(BuildingKind.AntAcornHome, -9, 5, out _) && garden.Capacity(false) == 4, "Acorn home houses two ants");
        var acorn = garden.buildings[garden.buildings.Count - 1];
        Assert(!garden.TryBuild(BuildingKind.AntAcornHome, 7, 5, out _), "Acorn home stays on village bank");
        Assert(garden.Upgrade(acorn.id, out _), "Acorn home second tier starts");
        garden.Advance(900);
        Assert(acorn.tier == 2 && garden.Capacity(false) == 5, "Second acorn tier adds one ant");
        Assert(garden.Upgrade(acorn.id, out _), "Acorn home third tier starts");
        garden.Advance(3600);
        Assert(acorn.tier == 3 && garden.Capacity(false) == 6, "Third acorn tier adds one ant");
        Assert(garden.TryMove(acorn.id, -12, 5, 1, out _) && Copy(garden).Migrate(), "Acorn home moves, rotates and saves");
        foreach (var kind in new[]
        {
            BuildingKind.SmallTree,
            BuildingKind.TallGrass,
            BuildingKind.SmallParasol
        }

        )
        {
            var decor = Rich();
            Assert(decor.TryBuild(kind, -8, 5, out _) && decor.TryBuild(kind, 7, 5, out _), "New decor can occupy either bank");
            var placed = decor.buildings[decor.buildings.Count - 1];
            Assert(!decor.TryBuild(kind, 7, 5, out _) && !decor.Upgrade(placed.id, out _), "Decor blocks overlap and cannot upgrade");
            Assert(decor.TryMove(placed.id, 9, 8, 2, out _) && Copy(decor).Migrate(), "New decor moves and survives save");
        }

        var oldShop = ExistingVillage();
        oldShop.version = 7;
        oldShop.xp = 110;
        Assert(oldShop.Migrate() && !oldShop.IsUnlocked(BuildingKind.AntAcornHome), "Legacy shop access does not bypass new home unlock");
        var visitorVillage = ExistingVillage();
        visitorVillage.acorns = 0;
        Assert(visitorVillage.IsVisitorSpot(-8, 5)
            && visitorVillage.IsVisitorSpot(7, 5),
            "Visitor can appear on clear ground on either bank without money");
        Assert(!visitorVillage.IsVisitorSpot(0, 0)
            && !visitorVillage.IsVisitorSpot(2, 3)
            && !visitorVillage.IsVisitorSpot(VillageState.BuildHalfWidth, 0),
            "Visitor avoids stream and map edge");
        Assert(!visitorVillage.IsVisitorSpot(-4, 0)
            && !visitorVillage.IsVisitorSpot(5, 1),
            "Visitor avoids buildings and their immediate surroundings");
        Assert(!visitorVillage.IsVisitorSpot(12, 4) && !visitorVillage.IsVisitorSpot(11, -6), "Visitor avoids fixed patio and mower");
        visitorVillage.buildings.Add(new Building { id = visitorVillage.nextId++, kind = BuildingKind.TallGrass, x = 7, z = 5 });
        Assert(!visitorVillage.IsVisitorSpot(7, 5), "Visitor avoids new decoration too");
        var lionReward = ExistingVillage();
        lionReward.quest = 5;
        int lionCash = lionReward.acorns, lionXp = lionReward.xp;
        lionReward.RewardAntLion();
        Assert(lionReward.acorns == lionCash + 25 && lionReward.xp == lionXp + 4, "Repelling ant lion rewards currency and XP");
        Assert(lionReward.visitors == 0 && !lionReward.QuestReady, "Ant lion does not complete ladybug quest");
        Assert(Copy(lionReward).Migrate(), "Ant lion reward persists in existing save format");
        for (int turn = 0; turn < 4; turn++)
        {
            var gates = Rich();
            int gx = -10, gz = 5;
            Assert(gates.TryBuild(BuildingKind.FlowerArch, gx, gz, out _, turn), "Gate builds at each rotation");
            int gateId = gates.buildings[gates.buildings.Count - 1].id;
            int dx = turn % 2 == 0 ? 1 : 0, dz = turn % 2 == 0 ? 0 : 1;
            Assert(gates.TryBuild(BuildingKind.Fence, gx + dx, gz + dz, out _)
                && gates.TryBuild(BuildingKind.Fence, gx - dx, gz - dz, out _),
                "Fences touch both gate posts");
            Assert(!gates.TryBuild(BuildingKind.Fence, gx + dz, gz + dx, out _)
                && !gates.TryBuild(BuildingKind.Fence, gx, gz, out _),
                "Gate entrance and center stay clear");
            Assert(!gates.TryMove(gateId, gx, gz, (turn + 1) % 4, out _), "Rotating gate into neighboring fences is rejected");
            Assert(Copy(gates).Migrate() && gates.TryMove(gateId, -14, 5, turn, out _), "Connected gate saves and can move away");
            var fenceFirst = Rich();
            Assert(fenceFirst.TryBuild(BuildingKind.Fence, gx + dx, gz + dz, out _)
                && fenceFirst.TryBuild(BuildingKind.FlowerArch, gx, gz, out _, turn),
                "Gate can be placed beside existing fence too");
        }

        for (int turn = 0; turn < 4; turn++)
        {
            var gate = new Building
            {
                kind = BuildingKind.FlowerArch,
                x = -10,
                z = 5,
                rotation = turn
            };
            var fenceA = new Building
            {
                kind = BuildingKind.Fence,
                x = -10 + (turn % 2 == 0 ? 1 : 0),
                z = 5 + (turn % 2 == 0 ? 0 : 1)
            };
            var fenceB = new Building
            {
                kind = BuildingKind.Fence,
                x = -10 - (turn % 2 == 0 ? 1 : 0),
                z = 5 - (turn % 2 == 0 ? 0 : 1)
            };
            for (int step = -10; step <= 10; step++)
            {
                float x = -10 + (turn % 2 == 0 ? 0 : step * .1f), z = 5 + (turn % 2 == 0 ? step * .1f : 0);
                Assert(!VillageState.BlocksResident(gate, x, z)
                    && !VillageState.BlocksResident(fenceA, x, z)
                    && !VillageState.BlocksResident(fenceB, x, z),
                    "Gate corridor stays clear with both connected fences");
            }

            Assert(VillageState.BlocksResident(gate, -10 + (turn % 2 == 0 ? .71f : 0), 5 + (turn % 2 == 0 ? 0 : .71f)),
                "Gate post remains blocked at every orientation");
            Assert(VillageState.BlocksResident(fenceA, fenceA.x, fenceA.z), "Fence remains an obstacle");
        }

        CheckGardenDivider();
        CheckVillageDivider();
        CheckExpansions();
        CheckHoneycombExpansion();
        CheckGardenCollection();
        CheckLocalization();
        Console.WriteLine($"PASS: {checks} domain and localization checks");
    }

    /// <summary>
    /// Verifies that the fixed flower row protects both plots and keeps the stone passage usable.
    /// </summary>
    private static void CheckGardenDivider()
    {
        var village = Rich();
        Assert(village.TryBuild(BuildingKind.Clover, 10, 9, out _)
            && village.TryBuild(BuildingKind.Clover, 24, 9, out _)
            && village.TryBuild(BuildingKind.Pile, 24, -4, out _),
            "Both garden plots retain usable building ground");
        for (int z = -VillageState.BuildHalfDepth; z <= VillageState.BuildHalfDepth; z++)
        {
            Assert(village.PlacementError(BuildingKind.Clover, GardenDivider.X, z)
                == I18n.Source("error.fixed_prop"),
                "The pot row and gate corridor reject new building overlap");
            Assert(!village.IsVisitorSpot(GardenDivider.X, z),
                "Visitors do not appear on the flower row or in the passage");
        }

        for (int side = -1; side <= 1; side += 2)
        {
            for (int pot = 0; pot < GardenDivider.PotsPerSide; pot++)
            {
                float z = side * (GardenDivider.FirstPotZ + pot * GardenDivider.PotSpacing);
                Assert(GardenDivider.BlocksResident(GardenDivider.X, z),
                    "Every fixed pot footprint blocks resident travel");
            }

            Assert(!GardenDivider.BlocksResident(GardenDivider.X, side * 1.1f),
                "Removing the arch posts leaves a wider stone passage");
            Assert(GardenDivider.NeedsGateDetour(GardenDivider.X - side * 5, 8,
                GardenDivider.X + side * 5, 6),
                "Travel through the flower row uses the gate in either direction");
            Assert(GardenDivider.NeedsGateDetour(GardenDivider.X - side * 5, -8,
                GardenDivider.X + side * 5, -6),
                "Southern crossings also use the central gate");
            Assert(!GardenDivider.NeedsGateDetour(GardenDivider.X - side * 5, 0,
                GardenDivider.X + side * 5, 0),
                "A straight passage through the gate does not insert another detour");
        }

        for (int step = -30; step <= 30; step++)
        {
            float x = GardenDivider.X + step * .1f;
            Assert(!GardenDivider.BlocksResident(x, 0)
                && !GardenDivider.BlocksResident(x, .7f)
                && !GardenDivider.BlocksResident(x, -.7f),
                "The gate provides a continuous corridor wide enough for both species");
        }

        Assert(!GardenDivider.NeedsGateDetour(10, 6, 14, 6)
            && !GardenDivider.NeedsGateDetour(22, 6, 26, 6)
            && !GardenDivider.NeedsGateDetour(24, -8, 24, 8),
            "Trips within one plot and parallel travel stay unchanged");
        Assert(!GardenDivider.NeedsGateDetour(10, -6, 26, 6),
            "A diagonal crossing through the opening stays direct");
        var legacy = Rich();
        legacy.buildings.Add(new Building
        {
            id = legacy.nextId++, kind = BuildingKind.Clover, x = GardenDivider.X, z = 6
        });
        int oldId = legacy.buildings[legacy.buildings.Count - 1].id;
        Assert(Copy(legacy).Migrate() && legacy.TryMove(oldId, GardenDivider.X, 6, 1, out _),
            "Existing buildings on the new divider remain saved and can rotate in place");
        Assert(!legacy.TryMove(oldId, GardenDivider.X + 1, 7, 0, out _)
            && legacy.TryMove(oldId, 24, 6, 0, out _),
            "An old building can move clear of the row without entering a newly blocked tile");
        Assert(!GardenDivider.NeedsGateDetour(10, 6, GardenDivider.X, 6),
            "A grandfathered endpoint on the divider cannot create an endless detour");
        Assert(Copy(village).Migrate() && Copy(village).IsValid(),
            "Building in both plots retains the existing save format");
    }

    /// <summary>
    /// Checks the housing divider, protected stone approaches, and walking-grid connectivity.
    /// </summary>
    private static void CheckVillageDivider()
    {
        var village = Rich();
        Assert(village.TryBuild(BuildingKind.AntHome, -24, 6, out _)
            && village.TryBuild(BuildingKind.BeeHome, -10, 6, out _),
            "Both housing plots accept homes for their resident species");
        for (int z = -VillageState.BuildHalfDepth; z <= VillageState.BuildHalfDepth; z++)
        {
            Assert(village.PlacementError(BuildingKind.AntHome, GardenDivider.VillageX, z)
                == I18n.Source("error.fixed_prop")
                && village.PlacementError(BuildingKind.BeeHome, GardenDivider.VillageX, z)
                == I18n.Source("error.fixed_prop"),
                "The rotten-log row and arch corridor reject home overlap");
            Assert(!village.IsVisitorSpot(GardenDivider.VillageX, z),
                "Visitors keep clear of the housing divider");
        }

        for (int side = -1; side <= 1; side += 2)
        {
            Assert(village.PlacementError(BuildingKind.AntHome,
                GardenDivider.VillageX + side * 3, 0) == I18n.Source("error.fixed_prop"),
                "Gate approaches cannot be filled by a new home");
            for (int log = 0; log < GardenDivider.LogsPerSide; log++)
            {
                float z = side * (GardenDivider.FirstLogZ + log * GardenDivider.LogSpacing);
                Assert(GardenDivider.BlocksResident(GardenDivider.VillageX, z),
                    "Rotten logs block travel on both sides of the arch");
            }

            Assert(GardenDivider.NeedsGateDetourAt(GardenDivider.VillageX,
                GardenDivider.VillageX - side * 6, 8, GardenDivider.VillageX + side * 6, 5),
                "Housing trips use the arch in both directions");
            Assert(!GardenDivider.NeedsGateDetourAt(GardenDivider.VillageX,
                GardenDivider.VillageX - side * 6, 0, GardenDivider.VillageX + side * 6, 0),
                "Travel through the housing gate remains direct");
        }

        for (int step = -30; step <= 30; step++)
        {
            Assert(!GardenDivider.BlocksResident(GardenDivider.VillageX + step * .1f, 0)
                && !GardenDivider.BlocksResident(GardenDivider.VillageX + step * .1f, 1f)
                && !GardenDivider.BlocksResident(GardenDivider.VillageX + step * .1f, -1f),
                "The half-unit walking grid has continuous clear lanes across the stone passage");
        }

        var frontier = new System.Collections.Generic.Queue<(int x, int z)>();
        var visited = new System.Collections.Generic.HashSet<(int x, int z)>();
        frontier.Enqueue((-50, 16));
        visited.Add((-50, 16));
        bool reached = false;
        while (frontier.Count > 0)
        {
            var point = frontier.Dequeue();
            if (point == (-12, -16))
            {
                reached = true;
                break;
            }

            foreach (var next in new[]
            {
                (x: point.x - 1, z: point.z), (x: point.x + 1, z: point.z),
                (x: point.x, z: point.z - 1), (x: point.x, z: point.z + 1)
            })
            {
                if (next.x < -60 || next.x > -4 || Math.Abs(next.z) > 20
                    || visited.Contains(next)
                    || GardenDivider.BlocksResident(next.x * .5f, next.z * .5f))
                {
                    continue;
                }

                visited.Add(next);
                frontier.Enqueue(next);
            }
        }

        Assert(reached, "The walking grid connects opposite housing plots via the arch");
        Assert(GardenDivider.NeedsGateDetourAt(GardenDivider.VillageX, -25, 6, 25, 6)
            && GardenDivider.NeedsGateDetourAt(GardenDivider.X, -25, 6, 25, 6),
            "A long trip can cross both dividers without omitting either gate");
        Assert(!GardenDivider.NeedsGateDetourAt(GardenDivider.VillageX, -20.2f, 0, -15.8f, 0)
            && !GardenDivider.NeedsGateDetourAt(GardenDivider.X, 15.8f, 0, 20.2f, 0),
            "Inserted gate crossings cannot create repeated detours");
        var legacy = Rich();
        legacy.buildings.Add(new Building
        {
            id = legacy.nextId++, kind = BuildingKind.AntHome, x = GardenDivider.VillageX, z = 6
        });
        int id = legacy.buildings[legacy.buildings.Count - 1].id;
        int money = legacy.acorns;
        Assert(Copy(legacy).Migrate() && legacy.TryMove(id, GardenDivider.VillageX, 6, 1, out _)
            && legacy.acorns == money,
            "An old home on the row remains saved and can rotate without charging acorns");
        Assert(!legacy.TryMove(id, GardenDivider.VillageX + 1, 7, 0, out _)
            && legacy.TryMove(id, -24, 6, 0, out _) && Copy(legacy).IsValid(),
            "An old home can move to clear housing ground while the new row stays protected");
        Assert(Copy(village).Migrate() && village.version == 9,
            "New housing plots use the original save format");
    }

    /// <summary>
    /// Verifies decoration unlocks, atomic purchases, placement, movement, and save compatibility.
    /// </summary>
    static void CheckGardenCollection()
    {
        Assert((int)BuildingKind.PebbleYard == 29 && (int)BuildingKind.WoodenPlanter == 30
            && (int)BuildingKind.FlowerCart == 34, "Garden collection preserves existing building IDs");
        foreach (BuildingKind kind in new[]
        {
            BuildingKind.WoodenPlanter, BuildingKind.Birdhouse, BuildingKind.WindChime,
            BuildingKind.LeafFountain, BuildingKind.FlowerCart
        })
        {
            int level = VillageState.UnlockLevel(kind);
            var locked = Rich(VillageState.XpForLevel(level - 1));
            locked.legacyUnlockLevel = 999;
            int lockedCash = locked.acorns;
            int lockedBuildings = locked.buildings.Count;
            Assert(!locked.IsUnlocked(kind) && !locked.TryBuild(kind, -9, 5, out _)
                && locked.acorns == lockedCash && locked.buildings.Count == lockedBuildings,
                "New decoration cannot bypass unlock or spend while locked: " + kind);
            var poor = Rich(VillageState.XpForLevel(level));
            poor.acorns = VillageState.Cost(kind) - 1;
            Assert(!poor.TryBuild(kind, -9, 5, out _) && poor.acorns == VillageState.Cost(kind) - 1,
                "Unaffordable decoration cannot spend currency: " + kind);
            var village = Rich(VillageState.XpForLevel(level));
            int money = village.acorns;
            int ants = village.Capacity(false);
            int bees = village.Capacity(true);
            Assert(village.IsUnlocked(kind) && village.TryBuild(kind, -9, 5, out _)
                && village.acorns == money - VillageState.Cost(kind),
                "Decoration unlocks and charges once: " + kind);
            Building decoration = village.buildings[village.buildings.Count - 1];
            Assert(!VillageState.IsHome(kind) && !VillageState.IsWork(kind) && !VillageState.IsFood(kind)
                && village.Capacity(false) == ants && village.Capacity(true) == bees
                && !village.StartJob(decoration.id, 0, out _) && !village.Upgrade(decoration.id, out _),
                "Decoration does not add workers, jobs, or upgrades: " + kind);
            money = village.acorns;
            Assert(!village.TryBuild(kind, -9, 5, out _) && !village.TryMove(decoration.id, 0, 5, 1, out _)
                && village.acorns == money, "Decoration rejects overlaps and stream placement: " + kind);
            Assert(village.TryMove(decoration.id, 7, 5, 3, out _) && village.acorns == money,
                "Decoration moves freely to the other bank and rotates: " + kind);
            VillageState saved = Copy(village);
            Building restored = saved.buildings.Find(building => building.id == decoration.id);
            Assert(saved.Migrate() && saved.version == 9 && restored.kind == kind
                && restored.x == 7 && restored.z == 5 && restored.rotation == 3
                && saved.acorns == money, "Decoration survives version-nine save round-trip: " + kind);
        }
    }

    /// <summary>
    /// Exercises new building unlocks, worker species, upgrade tiers, job rewards, and saved contracts.
    /// </summary>
    static void CheckHoneycombExpansion()
    {
        Assert((int)BuildingKind.SmallParasol == 24 && (int)BuildingKind.BeeHoneycombHome == 25
            && (int)BuildingKind.PebbleYard == 29, "Expansion appends stable building IDs");
        foreach (BuildingKind kind in new[]
        {
            BuildingKind.BeeHoneycombHome, BuildingKind.Tulip, BuildingKind.Bluebell,
            BuildingKind.LeafDepot, BuildingKind.PebbleYard
        })
        {
            int level = VillageState.UnlockLevel(kind);
            var locked = Rich(VillageState.XpForLevel(level - 1));
            locked.legacyUnlockLevel = 999;
            Assert(!locked.IsUnlocked(kind), "Legacy access does not bypass new unlock: " + kind);
            locked.xp = VillageState.XpForLevel(level);
            Assert(locked.IsUnlocked(kind) && VillageState.Cost(kind) > 0, "New type unlocks at its scheduled level: " + kind);
        }

        var homes = Rich(VillageState.XpForLevel(12));
        Assert(!homes.TryBuild(BuildingKind.BeeHoneycombHome, 7, 5, out _), "Honeycomb home belongs on the village bank");
        Assert(homes.TryBuild(BuildingKind.BeeHoneycombHome, -9, 5, out _) && homes.Capacity(true) == 2,
            "Honeycomb home adds two bees");
        var hive = homes.buildings[homes.buildings.Count - 1];
        for (int tier = 2; tier <= 3; tier++)
        {
            double duration = VillageState.UpgradeTime(hive);
            Assert(homes.Upgrade(hive.id, out _) && Copy(homes).Migrate(), "Honeycomb upgrade survives saving");
            homes.Advance(duration);
            Assert(hive.tier == tier && homes.Capacity(true) == tier + 1, "Honeycomb tier increases bee capacity");
        }
        Assert(!homes.Upgrade(hive.id, out _) && homes.TryMove(hive.id, -12, 5, 2, out _)
            && Copy(homes).Migrate(), "Maximum honeycomb home moves, rotates, and saves");

        foreach (BuildingKind kind in new[] { BuildingKind.Tulip, BuildingKind.Bluebell, BuildingKind.LeafDepot, BuildingKind.PebbleYard })
        {
            bool bee = kind == BuildingKind.Tulip || kind == BuildingKind.Bluebell;
            var village = Rich(VillageState.XpForLevel(12));
            Assert(VillageState.IsWork(kind) && VillageState.IsBee(kind) == bee, "Worksite belongs to the correct species: " + kind);
            Assert(village.TryBuild(bee ? BuildingKind.BeeHoneycombHome : BuildingKind.AntHome, -9, 5, out _),
                "Prepare enough residents for all worksite tiers");
            village.buildings[village.buildings.Count - 1].tier = 3;
            Assert(!village.TryBuild(kind, -12, 5, out _) && village.TryBuild(kind, 7, 5, out _),
                "Worksite placement uses the garden bank: " + kind);
            var worksite = village.buildings[village.buildings.Count - 1];
            Assert(!village.TryBuild(kind, 7, 5, out _), "New worksite rejects overlap");
            for (int tier = 1; tier <= 3; tier++)
            {
                if (tier > 1)
                {
                    double duration = VillageState.UpgradeTime(worksite);
                    Assert(village.Upgrade(worksite.id, out _), "Start worksite tier upgrade");
                    Assert(!village.StartJob(worksite.id, 0, out _), "Upgrading worksite cannot accept jobs");
                    village.Advance(duration);
                }
                Assert(worksite.tier == tier, "Worksite reaches requested tier");
                for (int option = 0; option < 3; option++)
                {
                    village.energy = VillageState.MaxEnergy;
                    Assert(village.StartJob(worksite.id, option, out _) && village.Busy(bee) == tier
                        && village.Busy(!bee) == 0, "New worksite allocates only the correct workers");
                    var saved = Copy(village);
                    var contract = saved.buildings.Find(building => building.id == worksite.id);
                    Assert(saved.Migrate() && contract.kind == kind && contract.tier == tier
                        && contract.remaining == VillageState.JobSeconds[option] && contract.reward == worksite.reward,
                        "New worksite preserves a running contract through save/load");
                    village.Advance(VillageState.MaxEnergy);
                    Assert(worksite.phase == JobPhase.Ready && village.Busy(bee) == 0, "New job finishes offline and releases workers");
                    int money = village.acorns;
                    Assert(village.Collect(worksite.id) && village.acorns == money + contract.reward
                        && !village.Collect(worksite.id), "New worksite pays its stored reward exactly once");
                    Assert(village.Refill(worksite.id), "New worksite can refill for another job");
                }
            }
            Assert(!village.Upgrade(worksite.id, out _) && village.TryMove(worksite.id, 9, 8, 1, out _)
                && Copy(village).Migrate(), "New worksite supports moving, rotation, and save validation");
        }

        foreach (int option in new[] { 0, 1, 2 })
        {
            Assert(VillageState.JobReward(BuildingKind.Pile, 1, option)
                < VillageState.JobReward(BuildingKind.LeafDepot, 1, option)
                && VillageState.JobReward(BuildingKind.LeafDepot, 1, option)
                < VillageState.JobReward(BuildingKind.TwigYard, 1, option)
                && VillageState.JobReward(BuildingKind.TwigYard, 1, option)
                < VillageState.JobReward(BuildingKind.PebbleYard, 1, option)
                && VillageState.JobReward(BuildingKind.PebbleYard, 1, option)
                < VillageState.JobReward(BuildingKind.SeedMill, 1, option), "Ant worksite rewards follow unlock progression");
            Assert(VillageState.JobReward(BuildingKind.Flower, 1, option)
                < VillageState.JobReward(BuildingKind.Tulip, 1, option)
                && VillageState.JobReward(BuildingKind.Tulip, 1, option)
                < VillageState.JobReward(BuildingKind.Lavender, 1, option)
                && VillageState.JobReward(BuildingKind.Lavender, 1, option)
                < VillageState.JobReward(BuildingKind.Bluebell, 1, option)
                && VillageState.JobReward(BuildingKind.Bluebell, 1, option)
                < VillageState.JobReward(BuildingKind.Sunflower, 1, option), "Bee worksite rewards follow unlock progression");
        }
    }

    /// <summary>
    /// Verifies bilingual coverage, composed messages, language switching, and save independence.
    /// </summary>
    static void CheckLocalization()
    {
        Assert(I18n.Language == GameLanguage.English, "New sessions default to English");
        foreach (string key in I18n.Keys)
        {
            I18n.Language = GameLanguage.Vietnamese;
            string source = I18n.Source(key);
            Assert(!string.IsNullOrWhiteSpace(source), "Vietnamese catalog entry: " + key);
            I18n.Language = GameLanguage.English;
            string english = I18n.Text(key);
            Assert(!string.IsNullOrWhiteSpace(english), "English catalog entry: " + key);
            string[] sourceSlots = Regex.Matches(source, @"\{\d+\}").Select(match => match.Value).OrderBy(value => value).ToArray();
            string[] englishSlots = Regex.Matches(english, @"\{\d+\}").Select(match => match.Value).OrderBy(value => value).ToArray();
            Assert(sourceSlots.SequenceEqual(englishSlots), "Both languages preserve placeholders: " + key);
            if (sourceSlots.Length == 0)
            {
                Assert(I18n.Translate(source) == english, "Canonical literal translates: " + key);
            }
            else
            {
                object[] arguments = { "12", "7", "4", "3" };
                string composed = string.Format(CultureInfo.InvariantCulture, source, arguments);
                Assert(I18n.Translate(composed) == I18n.Format(key, arguments), "Composed template translates: " + key);
            }
        }

        Assert(I18n.Translate(I18n.Source("label.free_workers").Replace("{0}", I18n.Source("unit.bee_label"))
            .Replace("{1}", "2").Replace("{2}", "4").Replace("{3}", "1")) == "Bees free 2/4 • slots 1/3",
            "Dynamic species labels translate inside a message");
        Assert(I18n.Translate("0:15:00") == "0:15:00" && I18n.Translate(null) == null,
            "Durations and null messages pass through unchanged");
        var village = ExistingVillage();
        string saved = JsonSerializer.Serialize(village, new JsonSerializerOptions { IncludeFields = true, IgnoreReadOnlyProperties = true });
        string error = village.PlacementError(BuildingKind.TwigYard, 6, 5);
        Assert(error == I18n.Source("error.unlock_prefix") + "5.", "Domain errors remain canonical in English mode");
        Assert(I18n.Translate(error) == "Unlocks at level 5.", "Domain unlock error translates with its level");
        I18n.Language = GameLanguage.Vietnamese;
        Assert(I18n.Translate(error) == error && I18n.Text("settings.title") == I18n.Source("settings.title"),
            "Switching back restores Vietnamese text");
        Assert(saved == JsonSerializer.Serialize(village, new JsonSerializerOptions { IncludeFields = true, IgnoreReadOnlyProperties = true }),
            "Language changes do not alter village serialization");
        I18n.Language = (GameLanguage)99;
        Assert(I18n.Language == GameLanguage.English, "Unknown saved language falls back to English");
    }
}
