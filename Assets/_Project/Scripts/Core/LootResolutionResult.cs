namespace TapMiner.Core
{
    /// <summary>
    /// Deterministic loot resolution outcomes for T008.
    /// </summary>
    public enum LootResolutionResult
    {
        None = 0,
        LootGranted = 1,
        RejectedNonLootBreakResult = 2,
        RejectedRunContextMismatch = 3,
        RejectedInvalidSegment = 4
    }
}
