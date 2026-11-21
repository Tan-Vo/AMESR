using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Data/CreatureData")]
public class SO_CreatureData : ScriptableObject
{
    [SerializeField] private string creatureName;
    public string CreatureName => creatureName;
    [SerializeField] private Sprite icon;
    public Sprite Icon => icon;
    [SerializeField] private Sprite[] animationFrames;
    public Sprite[] AnimationFrames => animationFrames;
    [SerializeField] private int maxHP = 20;
    public int MaxHP => maxHP;
    [SerializeField] private float moveSpeed = 2f;
    public float MoveSpeed => moveSpeed;
    [SerializeField] private float detectionRadius = 5f;
    public float DetectionRadius => detectionRadius;
    [SerializeField] private CreatureBehavior behaviorType;
    public CreatureBehavior BehaviorType => behaviorType;
    [SerializeField] private CreatureSize sizeCategory;
    public CreatureSize SizeCategory => sizeCategory;
    [SerializeField] private float mass = 1f;
    public float Mass => mass;
    [SerializeField] private int depthZoneMin;
    public int DepthZoneMin => depthZoneMin;
    [SerializeField] private int depthZoneMax = 500;
    public int DepthZoneMax => depthZoneMax;
    [SerializeField] private LootDrop[] lootTable;
    public LootDrop[] LootTable => lootTable;
    [SerializeField] private int catchDifficulty = 1;
    public int CatchDifficulty => catchDifficulty;
    [SerializeField] private int sellPrice = 20;
    public int SellPrice => sellPrice;
    [SerializeField] private SO_DishRecipe[] usableInRecipes;
    public SO_DishRecipe[] UsableInRecipes => usableInRecipes;
}
