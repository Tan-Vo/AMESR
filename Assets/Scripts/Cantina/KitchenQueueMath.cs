using System.Collections.Generic;

public static class KitchenQueueMath
{
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
