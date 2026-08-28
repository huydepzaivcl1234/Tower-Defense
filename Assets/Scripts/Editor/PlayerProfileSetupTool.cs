#if UNITY_EDITOR
/// <summary>
/// Legacy compatibility wrapper.
///
/// This class intentionally has NO MenuItem. The authoritative editor entry is
/// PersistentProfileSetupTool at:
/// Tower Defense/Data/Setup Persistent Profile + Reset Data
///
/// Keeping this wrapper preserves any old code that still calls PlayerProfileSetupTool.Setup()
/// while preventing two different setup implementations from competing for the same Unity menu path.
/// </summary>
public static class PlayerProfileSetupTool
{
    public static void Setup()
    {
        PersistentProfileSetupTool.Setup();
    }
}
#endif
