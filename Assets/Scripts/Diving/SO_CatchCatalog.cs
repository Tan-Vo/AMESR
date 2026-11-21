using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Data/Catch Catalog")]
public sealed class SO_CatchCatalog : ScriptableObject
{
    [SerializeField] private SO_CreatureData[] creatures;
    [SerializeField] private SO_ItemData[] items;
    /// <summary>Returns the inventory definition for a captured species.</summary>
    public SO_ItemData Find(SO_CreatureData creature)
    {
        if (creatures == null || items == null) return null;
        for (int i = 0; i < Mathf.Min(creatures.Length, items.Length); i++)
            if (creatures[i] == creature) return items[i];
        return null;
    }
}
