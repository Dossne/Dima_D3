namespace TapMiner.Core
{
    /// <summary>
    /// Deterministic break resolution outcomes for T007.
    /// </summary>
    public enum BreakResolutionResult
    {
        None = 0,
        BreakSucceeded = 1,
        RejectedState = 2,
        RejectedLaneMismatch = 3,
        RejectedNoTarget = 4,
        RejectedAlreadyBroken = 5,
        RejectedInvalidSegment = 6
    }
}
