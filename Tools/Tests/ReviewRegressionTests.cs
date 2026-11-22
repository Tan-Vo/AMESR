using System;
using System.Collections.Generic;

internal static class ReviewRegressionTests
{
    internal static void Run(Action<bool, string> check)
    {
        var diveScenes = new[] { "SC_DiveZone1_Shallows", "SC_DiveZone2_Twilight", "SC_DiveZone3_Abyssal", "SC_DiveZone4_EventHorizon" };
        var destinationArt = new[] { "shallows", "twilight", "abyssal", "event" };
        for (int i = 0; i < diveScenes.Length; i++)
        {
            var palette = StoryPalette.ForScene(diveScenes[i]).Value;
            check(palette.Biome == i && palette.Backdrop == destinationArt[i], "expedition uses its destination artwork: " + diveScenes[i]);
            check((palette.WaterOpacity > 0f) == (i == 0), "water coloration only applies to the aquatic destination: " + diveScenes[i]);
            check(palette.Ambient >= .8f && palette.WaterOpacity <= .15f, "dive keeps gameplay sprites readable: " + diveScenes[i]);
            if (i > 0)
            {
                var shallower = StoryPalette.ForScene(diveScenes[i - 1]).Value;
                check(palette.Ambient < shallower.Ambient && palette.Vignette > shallower.Vignette && palette.Exposure < shallower.Exposure,
                    "dive atmosphere grows darker with depth: " + diveScenes[i]);
            }
        }
        var mothership = StoryPalette.ForScene("SC_Mothership").Value;
        var cantina = StoryPalette.ForScene("SC_NebulaCantina").Value;
        check(mothership.Accent.r > mothership.Accent.b && cantina.Accent.r > cantina.Accent.b && mothership.Backdrop != cantina.Backdrop,
            "hub and cantina use distinct warm looks");
        check(!StoryPalette.ForScene("SC_BossArena").HasValue, "additive boss arena does not author another camera grade");

        const float backdropWidth = 1376f, backdropHeight = 768f;
        float standard = StoryBackdrop.CoverScale(backdropWidth, backdropHeight, 7f, 16f / 9f);
        check(Math.Abs(backdropWidth * standard - 35f) < .001f, "standard display preserves story backdrop framing");
        float ultrawideAspect = 32f / 9f;
        float ultrawide = StoryBackdrop.CoverScale(backdropWidth, backdropHeight, 7f, ultrawideAspect);
        check(Math.Abs(backdropWidth * ultrawide - 14f * ultrawideAspect) < .001f && ultrawide > standard,
            "super ultrawide display fills the view without side bands");
        float zoomedOut = StoryBackdrop.CoverScale(backdropWidth, backdropHeight, 11f, 9f / 16f);
        check(Math.Abs(backdropHeight * zoomedOut - 22f) < .001f, "zoomed-out portrait display covers the view height");

        var stacks = new List<InventorySlotData> {
            new InventorySlotData { ItemId = "fish", Quantity = 1 }, new InventorySlotData { ItemId = "medkit", Quantity = 2 } };
        var selected = stacks[1]; stacks.RemoveAt(0);
        check(InventoryMath.Resolve(stacks, selected) == 0, "selected stack follows compaction for sell and drag");
        stacks.Remove(selected); stacks.Add(new InventorySlotData { ItemId = "medkit", Quantity = 2 });
        check(InventoryMath.Resolve(stacks, selected) == -1, "removed selection cannot target a replacement stack");
        check(InventoryMath.Resolve(stacks, null) == -1 && InventoryMath.Resolve(null, selected) == -1, "empty inventory selection cancels");

        check(DamageSystem.HealingAmount(60, 100, 25) == 25, "wounded player can consume medkit");
        check(DamageSystem.HealingAmount(90, 100, 25) == 10, "healing capped at missing health");
        check(DamageSystem.HealingAmount(100, 100, 25) == 0 && DamageSystem.HealingAmount(0, 100, 25) == 0, "full or dead player cannot consume medkit");
        check(DamageSystem.HealingAmount(50, 100, 0) == 0 && DamageSystem.HealingAmount(50, 100, -1) == 0, "nonpositive healing rejected");
        check(DamageSystem.HealingAmount(int.MaxValue - 5, int.MaxValue, int.MaxValue) == 5, "healing does not overflow");
        check(DamageSystem.CalculateDamage(10, 1, 4) == 6 && DamageSystem.CalculateDamage(2, 1, 4) == 1, "suit armor reduces raw incoming hits with minimum damage");

        var safe = new HashSet<GameState> { GameState.MorningBrief, GameState.DivingPrep, GameState.CantinaPrep, GameState.NightShop, GameState.Sleeping };
        foreach (GameState state in Enum.GetValues(typeof(GameState)))
            check(CheckpointPolicy.CanSave(state) == safe.Contains(state), "save policy for " + state);

        check(PhaseClockMath.Hour(8, 18, PhaseClockMath.Progress(300, 300)) == 8, "dive starts at 08:00");
        check(PhaseClockMath.Hour(8, 18, PhaseClockMath.Progress(150, 300)) == 13, "dive half-time normalized");
        check(PhaseClockMath.Hour(8, 18, PhaseClockMath.Progress(90, 300)) == 15, "dive warning reachable before expiry");
        check(PhaseClockMath.Hour(8, 18, PhaseClockMath.Progress(0, 300)) == 18, "dive sunset reached at expiry");
        check(PhaseClockMath.Hour(19, 24, PhaseClockMath.Progress(0, 180)) == 24, "service midnight reached at expiry");
        check(PhaseClockMath.Progress(0, 0) == 1 && PhaseClockMath.Progress(-1, 180) == 1 && PhaseClockMath.Progress(400, 300) == 0, "phase clock boundaries clamped");
        bool rejected = false;
        try { PhaseClockMath.Progress(float.NaN, 300); } catch (ArgumentOutOfRangeException) { rejected = true; }
        check(rejected, "invalid phase clock rejected");

        var queue = new Queue<string>(new[] { "rice", "fish", "rice" });
        check(KitchenQueueMath.Cancel(queue, "fish") && queue.Count == 2 && queue.Dequeue() == "rice" && queue.Dequeue() == "rice", "cancel abandoned dish preserves FIFO");
        check(!KitchenQueueMath.Cancel(queue, "fish") && queue.Count == 0, "cancel from empty kitchen safe");
        queue.Enqueue("a"); queue.Enqueue("b");
        check(!KitchenQueueMath.Cancel(queue, "missing") && queue.Dequeue() == "a" && queue.Dequeue() == "b", "unknown cancellation leaves other orders untouched");
        queue.Enqueue("fish"); queue.Enqueue("fish");
        check(KitchenQueueMath.Cancel(queue, "fish") && queue.Count == 1, "cancel only one customer's duplicate recipe");

        var data = new SaveData();
        check(RewardLedger.Store(data, "Maw:legendary", "core", 1), "victory reward recorded independently of cargo");
        check(!RewardLedger.Store(data, "Maw:legendary", "core", 1) && data.PendingRewards.Count == 1, "duplicate pending receipt rejected");
        data.Inventory.Add(new InventorySlotData { ItemId = "fish", Quantity = 1 });
        data.Inventory.Add(new InventorySlotData { ItemId = "ore", Quantity = 1 });
        check(!RewardLedger.TryClaim(data, "Maw:legendary", 1, 2) && data.PendingRewards.Count == 1 && data.Inventory.Count == 2, "full cargo keeps reward pending without partial mutation");
        check(data.IsValid(), "pending rewards valid for JSON save");
        data.Inventory.RemoveAt(0);
        check(RewardLedger.TryClaim(data, "Maw:legendary", 1, 2) && data.Inventory[1].ItemId == "core", "hub claim succeeds after freeing space");
        check(data.PendingRewards.Count == 0 && !RewardLedger.TryClaim(data, "Maw:legendary", 1, 2), "claimed receipt cannot award twice");
        RewardLedger.Store(data, "Maw:rare:0", "gem", 1); RewardLedger.Collected(data, "Maw:rare:0");
        check(!RewardLedger.TryClaim(data, "Maw:rare:0", 10, 20), "physical pickup acknowledgment prevents hub duplication");
        check(!RewardLedger.TryClaim(data, "unknown", 10, 20), "missing reward cannot be claimed");
        rejected = false;
        try { RewardLedger.Store(data, "bad", "ore", -1); } catch (ArgumentOutOfRangeException) { rejected = true; }
        check(rejected, "invalid reward quantity rejected");
        RewardLedger.Store(data, "receipt", "ore", 1);
        data.PendingRewards.Add(new PendingRewardData { RewardId = "receipt", ItemId = "ore", Quantity = 1 });
        check(!data.IsValid(), "duplicate persisted receipts rejected");

        stacks.Clear(); stacks.Add(new InventorySlotData { ItemId = "ore", Quantity = 98 });
        check(!InventoryMath.TryAdd(stacks, "ore", 3, 99, 1) && stacks[0].Quantity == 98, "insufficient reward room does not partially merge");
        check(InventoryMath.TryAdd(stacks, "ore", 3, 99, 2) && stacks[0].Quantity == 99 && stacks[1].Quantity == 2, "reward merges then splits bounded stacks");
        check(!InventoryMath.TryAdd(stacks, "ore", 0, 99, 20) && !InventoryMath.TryAdd(stacks, "ore", 1, 0, 20), "empty or invalid stack request rejected");
        stacks.Clear();
        check(InventoryMath.TryAdd(stacks, "ore", int.MaxValue, int.MaxValue, 1) && stacks[0].Quantity == int.MaxValue, "large reward admission does not overflow");
    }
}
