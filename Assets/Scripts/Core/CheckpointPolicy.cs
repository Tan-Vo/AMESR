public static class CheckpointPolicy
{
    /// <summary>Allows saves only where loading into the hub cannot discard unfinished gameplay.</summary>
    public static bool CanSave(GameState state) => state == GameState.MorningBrief || state == GameState.DivingPrep ||
        state == GameState.CantinaPrep || state == GameState.NightShop || state == GameState.Sleeping;
}
