using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Data/BiomeData")]
public class SO_BiomeData : ScriptableObject
{
    [SerializeField] private string biomeName;
    public string BiomeName => biomeName;
    [SerializeField] private int depthMin;
    public int DepthMin => depthMin;
    [SerializeField] private int depthMax = 500;
    public int DepthMax => depthMax;
    [SerializeField] private Color waterTintColor = Color.blue;
    public Color WaterTintColor => waterTintColor;
    [SerializeField] private float waterTintIntensity = 0.3f;
    public float WaterTintIntensity => waterTintIntensity;
    [SerializeField] private float ambientLightIntensity = 0.7f;
    public float AmbientLightIntensity => ambientLightIntensity;
    [SerializeField] private SO_CreatureData[] spawnableCreatures;
    public SO_CreatureData[] SpawnableCreatures => spawnableCreatures;
    [SerializeField] private float[] spawnWeights;
    public float[] SpawnWeights => spawnWeights;
    [SerializeField] private SO_CreatureData miniBoss;
    public SO_CreatureData MiniBoss => miniBoss;
    [SerializeField] private SO_CreatureData mainBoss;
    public SO_CreatureData MainBoss => mainBoss;
    [SerializeField] private Sprite[] backgroundTiles;
    public Sprite[] BackgroundTiles => backgroundTiles;
    [SerializeField] private Sprite[] environmentProps;
    public Sprite[] EnvironmentProps => environmentProps;
    [SerializeField] private float hazardFrequency;
    public float HazardFrequency => hazardFrequency;
    [SerializeField] private EnvironmentHazardType[] hazardTypes;
    public EnvironmentHazardType[] HazardTypes => hazardTypes;
}
