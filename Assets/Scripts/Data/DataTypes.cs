using System;
using UnityEngine;

public enum CreatureBehavior { Passive, Schooling, Territorial, Aggressive, Boss }
public enum CreatureSize { Tiny, Small, Medium, Large, Massive }
public enum ItemCategory { Ingredient, Material, Gear, Consumable, QuestItem, Currency }
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
public enum DishQuality { Basic, Fine, Premium, Masterwork }
public enum WeaponType { Harpoon, Blaster, Shotgun, Sniper, NetLauncher, PlasmaBlade, Drill }
public enum UpgradeCategory { OxygenTank, Jetpack, HarpoonPower, WaiterTray, CantinaSeats, KitchenSpeed, SuitArmor, DiveDepth }
public enum EnvironmentHazardType { PoisonCloud, LavaVent, ElectricField, CrushingWall }
public enum DialogueEvent { None, GiveQuest, GiveItem, OpenShop, StartBoss, UnlockArea }
public enum QuestType { MainStory, SideQuest, Daily, BountyHunt }
public enum ObjectiveType { CatchCreature, DefeatBoss, ServeCustomers, ReachDepth, CollectItem, EarnCredits, UpgradeGear }

[Serializable]
public struct LootDrop
{
    [SerializeField] private SO_ItemData item;
    public SO_ItemData Item => item;
    [SerializeField] private int weight;
    public int Weight => weight;
    [SerializeField] private int minAmount;
    public int MinAmount => minAmount;
    [SerializeField] private int maxAmount;
    public int MaxAmount => maxAmount;
}

[Serializable]
public struct RecipeIngredient
{
    [SerializeField] private SO_ItemData item;
    public SO_ItemData Item => item;
    [SerializeField] private int quantity;
    public int Quantity => quantity;
}

[Serializable]
public struct QuestObjective
{
    [SerializeField] private ObjectiveType type;
    public ObjectiveType Type => type;
    [SerializeField] private string targetId;
    public string TargetId => targetId;
    [SerializeField] private int requiredAmount;
    public int RequiredAmount => requiredAmount;
    [SerializeField] private string description;
    public string Description => description;
}
