using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private T prefab;
    [SerializeField] private int initialSize = 20;
    [SerializeField] private Transform poolParent;
    private readonly Queue<T> available = new();
    private readonly HashSet<T> owned = new();
    private readonly HashSet<T> leased = new();
    private bool sealedPool;
    public int AvailableCount => available.Count;

    private void Awake() { PreWarm(initialSize); }
    private void Start() { sealedPool = true; }

    /// <summary>Allocates capacity during scene initialization only.</summary>
    public void PreWarm(int count)
    {
        if (sealedPool) throw new InvalidOperationException("Pool capacity is fixed after initialization.");
        if (prefab == null) throw new InvalidOperationException("Pool requires a prefab.");
        owned.EnsureCapacity(Math.Max(0, count));
        leased.EnsureCapacity(Math.Max(0, count));
        while (owned.Count < Math.Max(0, count))
        {
            var obj = Instantiate(prefab, poolParent != null ? poolParent : transform);
            obj.gameObject.SetActive(false);
            owned.Add(obj);
            available.Enqueue(obj);
        }
    }
    /// <summary>Returns an active instance, or null when capacity is exhausted.</summary>
    public T Get()
    {
        if (available.Count == 0) return null;
        var obj = available.Dequeue();
        leased.Add(obj);
        obj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        obj.gameObject.SetActive(true);
        return obj;
    }
    /// <summary>Recycles a leased instance; rejects foreign and duplicate returns.</summary>
    public void Return(T obj)
    {
        if (obj == null || !leased.Remove(obj)) return;
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(poolParent != null ? poolParent : transform, false);
        available.Enqueue(obj);
    }
}
