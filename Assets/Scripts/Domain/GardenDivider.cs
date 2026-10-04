using System;

namespace LittleColony
{
    /// <summary>
    /// Defines the fixed flower-pot and rotten-log dividers and their open passages.
    /// </summary>
    public static class GardenDivider
    {
        public const int X = 18;
        public const int VillageX = -18;
        public const int GateZ = 0;
        public const float GateHalfWidth = 1.2f;
        public const float GateApproachDistance = 2.2f;
        public const float WallHalfThickness = .9f;
        public const float RowHalfLength = 12.1f;
        public const int PotsPerSide = 8;
        public const float FirstPotZ = 2.3f;
        public const float PotSpacing = 1.3f;
        public const float WalkingGateHalfWidth = 1.2f;
        public const int LogsPerSide = 3;
        public const float FirstLogZ = 3.7f;
        public const float LogSpacing = 3.3f;
        public const float LogLength = 3.5f;

        /// <summary>
        /// Checks both divider rows while leaving the central stone passages clear.
        /// </summary>
        public static bool BlocksResident(float x, float z)
        {
            return BlocksResidentAt(X, x, z) || BlocksResidentAt(VillageX, x, z);
        }

        /// <summary>
        /// Checks one divider footprint, with extra body clearance along the village stone passage.
        /// </summary>
        /// <param name="dividerX">Horizontal center of the fixed divider.</param>
        /// <param name="x">Resident horizontal world position.</param>
        /// <param name="z">Resident depth coordinate.</param>
        public static bool BlocksResidentAt(float dividerX, float x, float z)
        {
            float clearance = dividerX == VillageX ? WalkingGateHalfWidth : GateHalfWidth;
            return Math.Abs(x - dividerX) < WallHalfThickness
                && Math.Abs(z - GateZ) > clearance
                && Math.Abs(z - GateZ) < RowHalfLength;
        }

        /// <summary>
        /// Determines whether a travel segment crosses the divider outside the gate opening.
        /// </summary>
        /// <remarks>
        /// Endpoints on the divider remain usable for grandfathered buildings in older saves.
        /// Both travel directions use the same intersection rule.
        /// </remarks>
        public static bool NeedsGateDetour(float fromX, float fromZ, float toX, float toZ)
        {
            return NeedsGateDetourAt(X, fromX, fromZ, toX, toZ);
        }

        /// <summary>
        /// Determines whether a segment must pass through the gate of the specified divider.
        /// </summary>
        /// <param name="dividerX">Horizontal center of the divider being crossed.</param>
        /// <param name="fromX">Horizontal coordinate of the segment start.</param>
        /// <param name="fromZ">Depth coordinate of the segment start.</param>
        /// <param name="toX">Horizontal coordinate of the segment destination.</param>
        /// <param name="toZ">Depth coordinate of the segment destination.</param>
        public static bool NeedsGateDetourAt(float dividerX, float fromX, float fromZ, float toX, float toZ)
        {
            float deltaX = toX - fromX;
            if (Math.Abs(deltaX) < .0001f)
            {
                return false;
            }

            float progress = (dividerX - fromX) / deltaX;
            if (progress <= 0 || progress >= 1)
            {
                return false;
            }

            float crossingZ = fromZ + (toZ - fromZ) * progress;
            float clearance = dividerX == VillageX ? WalkingGateHalfWidth : GateHalfWidth;
            return Math.Abs(crossingZ - GateZ) > clearance;
        }
    }
}
