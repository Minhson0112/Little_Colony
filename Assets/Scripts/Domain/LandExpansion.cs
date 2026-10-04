namespace LittleColony
{
    /// <summary>
    /// Identifies the two independently purchased outer plots without changing building save IDs.
    /// </summary>
    public enum LandPlot
    {
        Housing,
        Garden
    }

    /// <summary>
    /// Defines expansion prices and the shared boundaries used by placement and rendering.
    /// </summary>
    public static class LandExpansion
    {
        public const float InnerEdge = GardenDivider.X + GardenDivider.WallHalfThickness;
        public const float OuterEdge = VillageState.BuildHalfWidth + .5f;
        public const float HalfDepth = VillageState.BuildHalfDepth + .5f;

        /// <summary>
        /// Returns the acorn price of a valid plot, or zero for an unknown identifier.
        /// </summary>
        public static int Cost(LandPlot plot)
        {
            return plot == LandPlot.Housing ? 10000 : plot == LandPlot.Garden ? 15000 : 0;
        }

        /// <summary>
        /// Checks whether a point is inside the outer plot beyond its divider.
        /// </summary>
        public static bool Contains(LandPlot plot, float x, float z)
        {
            float distance = plot == LandPlot.Housing ? -x : x;
            return Cost(plot) > 0 && distance >= InnerEdge
                && distance <= OuterEdge && System.Math.Abs(z) <= HalfDepth;
        }
    }
}
