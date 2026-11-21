using System;
using System.Collections.Generic;

internal static class ReviewRegressionTests
{
    internal static void Run(Action<bool, string> check)
    {
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

        var orders = new Queue<string>(new[] { "rice", "fish", "rice" });
        var cookingSlots = new[] { "fish", "rice", null }; var timers = new[] { 5f, 7f, 0f };
        var completed = new Queue<string>(new[] { "fish", "rice" });
        check(KitchenQueueMath.Cancel(orders, cookingSlots, timers, completed, "fish") && string.Join(",", orders) == "rice,rice" && cookingSlots[0] == "fish" && timers[0] == 5 && completed.Count == 2, "cancel prefers queued serving and preserves other stages");
        check(KitchenQueueMath.Cancel(orders, cookingSlots, timers, completed, "fish") && cookingSlots[0] == null && timers[0] == 0 && cookingSlots[1] == "rice" && timers[1] == 7 && completed.Count == 2, "cooking cancellation clears only matching slot and timer");
        check(KitchenQueueMath.Cancel(orders, cookingSlots, timers, completed, "fish") && completed.Count == 1 && completed.Peek() == "rice", "ready cancellation preserves other recipe");
        check(!KitchenQueueMath.Cancel(orders, cookingSlots, timers, completed, "missing") && orders.Count == 2 && completed.Count == 1, "unknown staged cancellation leaves meals untouched");
        check(!KitchenQueueMath.Cancel(orders, cookingSlots, timers, completed, null), "null recipe does not cancel empty cooking slot");
        check(!KitchenQueueMath.Cancel(new Queue<string>(), Array.Empty<string>(), Array.Empty<float>(), new Queue<string>(), "fish"), "empty kitchen cancellation safe");

        Func<IEnumerable<string>, string, int> platesOf = (plates, recipe) => { int total = 0; foreach (var plate in plates) if (plate == recipe) total++; return total; };
        for (int queued = 0; queued <= 2; queued++)
        for (int cooking = 0; cooking <= 3; cooking++)
        for (int ready = 0; ready <= 2; ready++)
        for (int carried = 1; carried <= 3; carried++)
        {
            var waiting = new Queue<string>(new[] { "rice" }); var readyPlates = new Queue<string>(new[] { "rice" });
            var cookingPlates = new string[3]; var cookingTimers = new float[3]; var tray = new string[3];
            for (int i = 0; i < queued; i++) waiting.Enqueue("fish");
            for (int i = 0; i < cooking; i++) { cookingPlates[i] = "fish"; cookingTimers[i] = 3; }
            for (int i = 0; i < ready; i++) readyPlates.Enqueue("fish");
            for (int i = 0; i < carried; i++) tray[i] = "fish";
            if (carried < 3) tray[carried] = "rice";
            int fishDemand = queued + cooking + ready + carried - 1, riceDemand = 2 + (carried < 3 ? 1 : 0);
            KitchenQueueMath.Cancel(waiting, cookingPlates, cookingTimers, readyPlates, "fish");
            int count = KitchenQueueMath.PruneTray(tray, Math.Min(carried + 1, 3), recipe => recipe == "fish" ? fishDemand : riceDemand, true);
            check(platesOf(waiting, "fish") + platesOf(cookingPlates, "fish") + platesOf(readyPlates, "fish") + platesOf(tray, "fish") == fishDemand &&
                platesOf(waiting, "rice") + platesOf(cookingPlates, "rice") + platesOf(readyPlates, "rice") + platesOf(tray, "rice") == riceDemand &&
                count == platesOf(tray, "fish") + platesOf(tray, "rice"), "production cancellation and tray cleanup conserve mixed demand: " + queued + "/" + cooking + "/" + ready + "/" + carried);
        }
        var mixedTray = new[] { "rice", "fish", "fish" };
        check(KitchenQueueMath.PruneTray(mixedTray, 3, recipe => recipe == "fish" ? 1 : 0, true) == 1 && mixedTray[0] == "fish" && mixedTray[1] == null && mixedTray[2] == null, "tray swap removal retains demanded recipe and clears vacated slots");
        check(KitchenQueueMath.PruneTray(mixedTray, 1, recipe => 1, true) == 1, "tray cleanup idempotent at exact demand");
        check(KitchenQueueMath.PruneTray(mixedTray, 1, null, false) == 0 && mixedTray[0] == null, "closed service clears tray without consulting demand");
        check(KitchenQueueMath.PruneTray(Array.Empty<string>(), 0, recipe => 0, true) == 0, "empty tray cleanup safe");
        rejected = false;
        try { KitchenQueueMath.PruneTray(mixedTray, -1, recipe => 0, true); } catch (ArgumentOutOfRangeException) { rejected = true; }
        check(rejected, "invalid tray count rejected");
        rejected = false;
        try { KitchenQueueMath.PruneTray(mixedTray, 1, null, true); } catch (ArgumentNullException) { rejected = true; }
        check(rejected, "running tray cleanup requires demand provider");
        rejected = false;
        try { KitchenQueueMath.Cancel(orders, cookingSlots, Array.Empty<float>(), completed, "fish"); } catch (ArgumentException) { rejected = true; }
        check(rejected, "mismatched cooking timers rejected before mutation");

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
