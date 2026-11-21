using System;
using System.Collections.Generic;

public static class KitchenQueueMath
{
    /// <summary>Cancels one serving, preferring queued work over cooking and completed dishes.</summary>
    public static bool Cancel<T>(Queue<T> orders, T[] cooking, float[] remaining, Queue<T> ready, T order) where T : class
    {
        if (orders == null) throw new ArgumentNullException(nameof(orders));
        if (cooking == null) throw new ArgumentNullException(nameof(cooking));
        if (remaining == null) throw new ArgumentNullException(nameof(remaining));
        if (ready == null) throw new ArgumentNullException(nameof(ready));
        if (remaining.Length != cooking.Length) throw new ArgumentException("Cooking timers must match slots.", nameof(remaining));
        if (order == null) return false;
        if (Cancel(orders, order)) return true;
        for (int i = 0; i < cooking.Length; i++)
            if (EqualityComparer<T>.Default.Equals(cooking[i], order)) { cooking[i] = null; remaining[i] = 0f; return true; }
        return Cancel(ready, order);
    }
    /// <summary>Discards carried dishes beyond remaining demand and clears all plates after service.</summary>
    public static int PruneTray<T>(T[] tray, int count, Func<T, int> pendingOrders, bool running) where T : class
    {
        if (tray == null) throw new ArgumentNullException(nameof(tray));
        if (count < 0 || count > tray.Length) throw new ArgumentOutOfRangeException(nameof(count));
        if (running && pendingOrders == null) throw new ArgumentNullException(nameof(pendingOrders));
        for (int i = count - 1; i >= 0; i--)
        {
            int earlier = 0;
            for (int j = 0; j < i; j++) if (EqualityComparer<T>.Default.Equals(tray[j], tray[i])) earlier++;
            if (!running || pendingOrders(tray[i]) <= earlier) { tray[i] = tray[--count]; tray[count] = null; }
        }
        return count;
    }
    /// <summary>Removes one matching order without reordering other visits or allocating a replacement queue.</summary>
    public static bool Cancel<T>(Queue<T> queue, T order)
    {
        bool removed = false;
        int count = queue.Count;
        for (int i = 0; i < count; i++)
        {
            T current = queue.Dequeue();
            if (!removed && EqualityComparer<T>.Default.Equals(current, order)) removed = true;
            else queue.Enqueue(current);
        }
        return removed;
    }
}
