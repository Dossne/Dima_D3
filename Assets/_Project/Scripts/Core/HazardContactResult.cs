namespace TapMiner.Core
{
    /// <summary>
    /// Deterministic hazard contact outcomes for T009.
    /// </summary>
    public enum HazardContactResult
    {
        None = 0,
        HazardContactResolved = 1,
        RejectedState = 2,
        RejectedLaneMismatch = 3,
        RejectedNoHazard = 4,
        RejectedInvalidSegment = 5,
        RejectedRunContextMismatch = 6
    }
}
