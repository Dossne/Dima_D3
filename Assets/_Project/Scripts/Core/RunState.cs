namespace TapMiner.Core
{
    /// <summary>
    /// Locked run states from T001. No additional runtime states are permitted.
    /// </summary>
    public enum RunState
    {
        RunReady = 0,
        RunActive = 1,
        RunDeathResolved = 2,
        RunRestarting = 3
    }
}
