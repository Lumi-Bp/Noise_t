using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class WorldGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int width = 100;
    public int height = 100;
    public float tileSize = 1f;

    [Header("Noise Settings")]
    public float scale = 0.03f;

    [Header("Biome Separation")]
    public float biomeSeparation = 2.5f;

    [Header("Temperature")]
    public int tempSmoothPasses = 8;

    [Header("Humidity")]
    public int humidSmoothPasses = 8;

    [Header("Biome Tile Prefabs")]
    public GameObject polarDesertTile;
    public GameObject tundraTile;
    public GameObject taigaTile;
    public GameObject steppeTile;
    public GameObject tempForestTile;
    public GameObject tempRainForestTile;
    public GameObject hotDesertTile;
    public GameObject savannaTile;
    public GameObject tropicalRainForestTile;
    public GameObject waterTile;

    readonly int[] biomeStackHeights = new int[]
    {
        1, // PolarDesert
        2, // Tundra
        4, // Taiga
        1, // Steppe
        3, // TempForest
        4, // TempRainForest
        1, // HotDesert
        2, // Savanna
        5, // TropicalRainForest
    };

    float[,] landmass;
    float[,] temperature;
    float[,] humidity;

    void Start()
    {
        GenerateNoiseMaps();
        temperature = Smooth(temperature, tempSmoothPasses);
        humidity = Smooth(humidity, humidSmoothPasses);
        SpawnTiles();
    }

    void GenerateNoiseMaps()
    {
        landmass = new float[width, height];
        temperature = new float[width, height];
        humidity = new float[width, height];

        float2 seedL = new float2(Random.Range(-9999f, 9999f), Random.Range(-9999f, 9999f));
        float2 seedT = new float2(Random.Range(-9999f, 9999f), Random.Range(-9999f, 9999f));
        float2 seedH = new float2(Random.Range(-9999f, 9999f), Random.Range(-9999f, 9999f));

        for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                // Один вызов noise.snoise → [-1, 1]
                landmass[x, z] = noise.snoise(new float2((x + seedL.x) * scale,
                                                          (z + seedL.y) * scale));

                humidity[x, z] = noise.snoise(new float2((x + seedH.x) * scale,
                                                          (z + seedH.y) * scale));

                float t = (float)z / (height - 1);

                float temp;
                if (t < 0.35f)
                    temp = Mathf.Lerp(-1f, 0.2f, t / 0.35f);
                else if (t < 0.65f)
                    temp = Mathf.Lerp(-0.1f, 0.2f, (t - 0.35f) / 0.30f);
                else
                    temp = Mathf.Lerp(-0.2f, 1f, (t - 0.65f) / 0.35f);

                float noiseOffset = noise.snoise(new float2((x + seedT.x) * scale,
                                                             (z + seedT.y) * scale)) * 0.3f;

                temperature[x, z] = Mathf.Clamp(temp + noiseOffset, -1f, 1f);
            }
    }

    float[,] Smooth(float[,] map, int passes)
    {
        for (int pass = 0; pass < passes; pass++)
        {
            float[,] smoothed = new float[width, height];

            for (int z = 0; z < height; z++)
                for (int x = 0; x < width; x++)
                {
                    float sum = map[x, z];
                    int count = 1;

                    if (x > 0) { sum += map[x - 1, z]; count++; }
                    if (x < width - 1) { sum += map[x + 1, z]; count++; }
                    if (z > 0) { sum += map[x, z - 1]; count++; }
                    if (z < height - 1) { sum += map[x, z + 1]; count++; }

                    smoothed[x, z] = sum / count;
                }

            map = smoothed;
        }
        return map;
    }

    void SpawnTiles()
    {
        var root = new GameObject("World");

        for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                int biomeIdx = GetBiomeIndex(x, z);
                GameObject prefab = GetPrefab(biomeIdx);
                if (prefab == null) continue;

                int stackHeight = biomeIdx < 0 ? 1 : biomeStackHeights[biomeIdx];

                for (int y = 0; y < stackHeight; y++)
                {
                    Vector3 pos = new Vector3(x * tileSize, y * tileSize, z * tileSize);
                    Instantiate(prefab, pos, Quaternion.identity, root.transform);
                }
            }
    }

    int GetBiomeIndex(int x, int z)
    {
        float l = landmass[x, z];
        float t = temperature[x, z];
        float h = humidity[x, z];

        if (l < 0f) return -1;

        t = Sharpen(t);
        h = Sharpen(h);

        if (t < -0.33f)
        {
            if (h < -0.33f) return 0; // PolarDesert
            if (h < 0.33f) return 1; // Tundra
            return 2;                 // Taiga
        }

        if (t < 0.33f)
        {
            if (h < -0.33f) return 3; // Steppe
            if (h < 0.33f) return 4; // TempForest
            return 5;                 // TempRainForest
        }

        if (h < -0.33f) return 6;     // HotDesert
        if (h < 0.33f) return 7;     // Savanna
        return 8;                     // TropicalRainForest
    }

    float Sharpen(float v)
    {
        return Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), 1f / biomeSeparation);
    }

    GameObject GetPrefab(int biomeIdx) => biomeIdx switch
    {
        -1 => waterTile,
        0 => polarDesertTile,
        1 => tundraTile,
        2 => taigaTile,
        3 => steppeTile,
        4 => tempForestTile,
        5 => tempRainForestTile,
        6 => hotDesertTile,
        7 => savannaTile,
        8 => tropicalRainForestTile,
        _ => null
    };
}