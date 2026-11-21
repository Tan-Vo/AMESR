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
        Console.WriteLine($"Core logic tests: {checks} passed, 0 failed (Timer + SaveData; no Unity runtime).");
    }
}
