using System;
using System.Collections.Generic;
using System.Reflection;

internal static class CoreTests
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
    }
    private static void Set(object obj, string field, object value) =>
        obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);

    private static void Main()
    {
        var timer = new Timer();
        int completed = 0;
        timer.Start(2, () => completed++);
        timer.Tick(1);
        Check(timer.Remaining == 1 && completed == 0, "timer countdown");
        timer.Tick(-5);
        Check(timer.Remaining == 1, "negative delta ignored");
        timer.Tick(10);
        timer.Tick(10);
        Check(timer.IsFinished && completed == 1, "completion once");
        timer.Start(0, null);
        timer.Tick(0);
        Check(timer.IsFinished, "zero duration null callback");
        timer.Start(3, () => completed++);
        timer.Stop();
        timer.Tick(5);
        Check(completed == 1, "stopped timer does not complete");
        timer.Reset();
        Check(timer.Remaining == 3, "reset restores duration");
        timer.Start(-10, () => timer.Start(2, () => completed++));
        timer.Tick(0);
        Check(timer.Remaining == 2, "callback can restart timer");

        var data = new SaveData();
        Check(data.IsValid() && data.CurrentDay == 1 && data.Credits == 500, "default save");
        data.UpgradeLevels.Add("oxygen", 3);
        data.OnBeforeSerialize();
        Set(data, "upgradeLevels", new Dictionary<string, int>());
        data.OnAfterDeserialize();
        Check(data.UpgradeLevels["oxygen"] == 3, "upgrade serialization callbacks");
        data.Inventory.Add(new InventorySlotData { ItemId = "meat", Quantity = 2 });
        Check(data.IsValid(), "populated save");
        data.Inventory.Add(null);
        Check(!data.IsValid(), "null inventory entry rejected");
        data.Inventory.RemoveAt(1);
        data.Inventory[0].Quantity = 0;
        Check(!data.IsValid(), "invalid quantity rejected");
        data.Inventory.Clear();
        data.TotalPlayTime = float.NaN;
        Check(!data.IsValid(), "nonfinite play time rejected");
        data.TotalPlayTime = 0;
        data.CurrentDay = 0;
        Check(!data.IsValid(), "invalid day rejected");
        data.CurrentDay = 1;
        data.UpgradeLevels["oxygen"] = -1;
        Check(!data.IsValid(), "negative upgrade rejected");
        data.UpgradeLevels["oxygen"] = 3;
        Set(data, "upgradeValues", new List<int>());
        Check(!data.IsValid(), "mismatched upgrade lists rejected");
        Set(data, "inventory", null);
        Check(!data.IsValid(), "null collection rejected");
        Check(Math.Abs(OxygenMath.DrainRate(0, false) - 0.45f) < 0.0001f, "surface oxygen rate");
        Check(Math.Abs(OxygenMath.DrainRate(2500, false) - 1.2f) < 0.0001f, "maximum reference depth rate");
        Check(Math.Abs(OxygenMath.DrainRate(2500, true) - 3.84f) < 0.0001f, "boost multiplier");
        Check(OxygenMath.DrainRate(-5, false) == OxygenMath.DrainRate(0, false), "negative depth clamped");
        Check(OxygenMath.DrainRate(5000, false) > OxygenMath.DrainRate(2500, false), "deep pressure continues scaling");
        bool rejected = false;
        try { OxygenMath.DrainRate(float.NaN, false); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "nonfinite depth rejected");
        Check(WeightedSelection.SelectIndex(new int[] { 70, 25, 5 }, 0) == 0, "first weighted interval");
        Check(WeightedSelection.SelectIndex(new int[] { 70, 25, 5 }, 0.7) == 1, "weighted boundary is half-open");
        Check(WeightedSelection.SelectIndex(new int[] { 70, 25, 5 }, 0.999999) == 2, "last weighted interval");
        Check(WeightedSelection.SelectIndex(Array.Empty<int>(), 0.5) == -1, "empty weights");
        Check(WeightedSelection.SelectIndex(new int[] { 0, -10 }, 0.5) == -1, "zero and negative weights");
        Check(WeightedSelection.SelectIndex(new int[] { 0, -10, 2 }, 0) == 2, "invalid weights skipped");
        Check(WeightedSelection.SelectIndex(new int[] { int.MaxValue, int.MaxValue }, 0.75) == 1, "weight sum does not overflow");
        Check(WeightedSelection.SelectIndex(new float[] { float.NaN, float.PositiveInfinity, 0, 4 }, 0.5) == 3, "nonfinite float weights skipped");
        Check(WeightedSelection.SelectIndex(new float[] { 0.25f, 0.75f }, 0.25) == 1, "fractional weight boundary");
        rejected = false;
        try { WeightedSelection.SelectIndex(new int[] { 1 }, 1); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "invalid unit sample rejected");
        rejected = false;
        try { WeightedSelection.SelectIndex(new int[] { 1 }, double.NaN); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "NaN unit sample rejected");
        int[] frequencies = new int[3];
        for (int i = 0; i < 1000; i++) frequencies[WeightedSelection.SelectIndex(new int[] { 70, 25, 5 }, (i + 0.5) / 1000)]++;
        Check(frequencies[0] == 700 && frequencies[1] == 250 && frequencies[2] == 50, "70/25/5 distribution");
        Console.WriteLine($"Core logic tests: {checks} passed, 0 failed (Timer + SaveData + OxygenMath + WeightedSelection; no Unity runtime).");
    }
}
