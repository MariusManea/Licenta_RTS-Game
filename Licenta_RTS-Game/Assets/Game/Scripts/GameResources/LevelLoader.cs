﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RTS;
using Pathfinding;
using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Policies;

public class LevelLoader : MonoSingleton<LevelLoader>
{
    private const int CLUMP_SIZE = 6500;
    private const int SMOOTH_COUNT = 3;

    private int clumpNumber;
    private float chaosLevel;
    private List<Vector3> territories;

    private List<Vector3> playersPositions;

    private List<Vector3>[] resourcesLand;

    public Material BordersSourceMaterial;
    private List<Vector3>[] borders;

    public int loadingPercent;
    public int loadingMax;

    public struct MapPoint
    {
        public int owner;
        public int id;
        public bool flatland;
    }


    private static int nextObjectId = 0;
    private bool initialised = false;

    public int playersNumber;
    public Color[] teamColors;
    public GameSize mapSize;
    public string seed = "";

    private MapPoint[][] map;
    private float[,] heightMap;
    private Terrain terrain;
    public TerrainLayer[] terrainLayers;

    void Awake()
    {
        if (this != Instance) return;
        initialised = true;

        if (initialised)
        {
            SelectPlayerMenu menu = GameObject.FindObjectOfType(typeof(SelectPlayerMenu)) as SelectPlayerMenu;
            if (!menu)
            {
                StartCoroutine(InitNewWorld(true));
                //we have started from inside a map, rather than the main menu
                //this happens if we launch Unity from inside a map file for testing
                Player[] players = GameObject.FindObjectsOfType(typeof(Player)) as Player[];
                foreach (Player player in players)
                {
                    if (player.isHuman)
                    {
                        PlayerManager.SelectPlayer(player.userName, 0);
                    }
                }
                SetObjectIds();
            }
        }
    }

    private void Update()
    {
        //Debug.Log(loadingPercent + "/" + loadingMax);
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += LevelLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= LevelLoaded;
    }

    void LevelLoaded(Scene scene, LoadSceneMode mode)
    {
        if (initialised)
        {
            loadingPercent = 0;
            if (ResourceManager.LevelName != null && ResourceManager.LevelName != "")
            {
                LoadManager.LoadGame(ResourceManager.LevelName);
            }
            else
            {
                if (scene.name == "GameScene")
                {
                    Terrain terrain = (Terrain)GameObject.FindObjectOfType(typeof(Terrain));
                    if (terrain)
                    {
                        terrain.terrainData.size = new Vector3((float)mapSize, 20, (float)mapSize);
                    }
                    loadingMax = 1 + (6 + 2 * ((int)mapSize / 50)) + (1 + 2 * CLUMP_SIZE / 500) + (1 + (int)(0.55 * CLUMP_SIZE / 400)) + (1 + 3 * ((mapSize == GameSize.Small || mapSize == GameSize.Medium) ? 10 : 20)) +
                        SMOOTH_COUNT * (1 + ((mapSize == GameSize.Small || mapSize == GameSize.Medium) ? 10 : 20)) +
                        ResourceManager.GetGameResources.Length + 2 * ((int)(0.75 / (CLUMP_SIZE / Mathf.Pow((int)mapSize, 2))) + 1) + 3;
                    GetComponent<LoadingScreen>().enabled = true;
                    StartCoroutine(InitNewWorld(false));
                }

                SetObjectIds();
            }
            Time.timeScale = 1.0f;
            ResourceManager.MenuOpen = false;
           
            if (scene.name != "MainMenu")
            {
                NavGraph[] graphs = AstarPath.active.graphs;
                foreach (NavGraph graph in graphs)
                {
                    if (graph.GetType() == typeof(GridGraph))
                    {
                        GridGraph gridGraph = graph as GridGraph;
                        gridGraph.center = new Vector3((int)mapSize / 2, 0, (int)mapSize / 2);
                        gridGraph.cutCorners = false;
                        gridGraph.SetDimensions((int)mapSize, (int)mapSize, 1);
                        gridGraph.maxSlope = 30;
                    }
                }
                
                AstarPath.active.Scan();
            }

        }
    }

    private void SetObjectIds()
    {
        WorldObjects[] worldObjects = GameObject.FindObjectsOfType(typeof(WorldObjects)) as WorldObjects[];
        foreach (WorldObjects worldObject in worldObjects)
        {
            worldObject.ObjectId = nextObjectId++;
            if (nextObjectId >= int.MaxValue) nextObjectId = 0;
        }
    }

    public int GetNewObjectId()
    {
        nextObjectId++;
        if (nextObjectId >= int.MaxValue) nextObjectId = 0;
        return nextObjectId;
    }

    public MapPoint[][] GetMap()
    {
        return map;
    }

    public void LoadBorders(List<Vector3>[] loadedBorders)
    {
        borders = loadedBorders;
        ((GameManager)FindObjectOfType(typeof(GameManager))).InitOwnership(borders.Length, playersNumber);
        terrain = (Terrain)GameObject.FindObjectOfType(typeof(Terrain));
        for (int i = 0; i < borders.Length; ++i)
        {
            Color color = new Color(0.5f, 0.5f, 0.5f);
            if (i < playersNumber)
            {
                color = teamColors[i];
            }
            StartCoroutine(DrawBorder(borders[i], terrain, color, BordersSourceMaterial, "Territory" + i));
        }
    }

    private IEnumerator InitNewWorld(bool autoTest)
    {
        // new level init
        // obtain seed for generation
        if (seed == "")
        {
            Random.InitState(System.DateTime.Now.Second);
            seed = Random.Range(0, int.MaxValue).ToString();
            PRG.SetSeed(uint.Parse(seed));
        } 
        else
        {
            uint numberSeed = 0;
            foreach (char c in seed)
            {
                if (c >= '0' && c <= '9') {
                    numberSeed = (numberSeed * 10 + (uint)(c - '0')) % int.MaxValue;
                }
                else
                {
                    uint unicode = c;
                    numberSeed = (numberSeed * 10 + unicode) % int.MaxValue;
                }
            }
            PRG.SetSeed(numberSeed);
        }
        loadingPercent++;  
        yield return null;

        // Level Generation

        // Step 1
        Debug.Log("Placing players");
        yield return StartCoroutine(PlacePlayers());
        // Step 2
        Debug.Log("Growing land");
        yield return StartCoroutine(GrowPlayerLand());
        // Step 3
        Debug.Log("Adding Flatlands");
        yield return StartCoroutine(AddFlatLands());
        // Step 4
        Debug.Log("Generating Heightmap");
        yield return StartCoroutine(GenerateHeightMap());
        // Step 5
        Debug.Log("Merging Maps");
        yield return StartCoroutine(CombineMaps());
        // Step 6
        Debug.Log("Fixing Anomalies");
        yield return StartCoroutine(FixAnomalies());
        // Step 7
        Debug.Log("Distributing Resources");
        yield return StartCoroutine(DistributeResources());
        // Step 8 (Maybe?)
        // TODO: PlaceTrees();
        // Step 9
        Debug.Log("Drawing Map");
        yield return StartCoroutine(TextureMap());
        // Step 10
        Debug.Log("Drawing Borders");
        yield return StartCoroutine(DrawBorders());
        // Step 11
        Debug.Log("Initializing Players");
        yield return StartCoroutine(InitialUnits(autoTest));

        GetComponent<LoadingScreen>().enabled = false;
    }


    private IEnumerator PlacePlayers()
    {
        Debug.Log("Getting players positions");
        yield return StartCoroutine(GetPlayersPositions());
        Debug.Log("Initializing map");
        map = new MapPoint[(int)mapSize][];
        for (int i = 0; i < (int)mapSize; ++i)
        {
            map[i] = new MapPoint[(int)mapSize];
            for (int j = 0; j < (int)mapSize; ++j)
            {
                map[i][j].owner = -1;
                map[i][j].flatland = false;
            }
            if ((i + 1) % 50 == 0) { loadingPercent++;   yield return null; }
        }
        yield return null;

        Debug.Log("Setting map");
        for (int i = 0; i < playersNumber; ++i)
        {
            map[(int)playersPositions[i].x][(int)playersPositions[i].z].owner = i;
            map[(int)playersPositions[i].x][(int)playersPositions[i].z].id = i;
        }
        loadingPercent++;  
        yield return null;

        territories = new List<Vector3>(playersPositions);
        Debug.Log("Getting colonies positions");
        for (int dummyPlayer = playersNumber; dummyPlayer < 0.75 / (CLUMP_SIZE / Mathf.Pow((int)mapSize, 2)); ++dummyPlayer)
        {
            Vector3 colonizablePosition = GetColonyPosition(territories);
            map[(int)colonizablePosition.x][(int)colonizablePosition.z].owner = dummyPlayer;
            map[(int)colonizablePosition.x][(int)colonizablePosition.z].id = dummyPlayer;
            territories.Add(colonizablePosition);
        }
        loadingPercent++;  
    }

    private IEnumerator GetPlayersPositions()
    {
        playersPositions = new List<Vector3>();


        float outerDiskRadius = ((float)mapSize - 16) / 2;
        float innerDiskRadius = ((float)mapSize * 0.5f + PRG.GetNextRandom() % ((float)mapSize * 0.33f)) / 2;
        List<float> angles = new List<float>();
        Debug.Log("Getting positions angles");
        yield return StartCoroutine(GetAngles(angles));
        Debug.Log("Generating positions");
        for (int i = 0; i < playersNumber; ++i)
        {
            float radius = innerDiskRadius + PRG.GetNextRandom() % (outerDiskRadius - innerDiskRadius);
            float X = (float)mapSize / 2 + radius * Mathf.Cos(Mathf.Deg2Rad * angles[i]);
            float Z = (float)mapSize / 2 + radius * Mathf.Sin(Mathf.Deg2Rad * angles[i]);

            playersPositions.Add(new Vector3(X, 0, Z));
        }
        loadingPercent++;  
    }

    private IEnumerator GetAngles(List<float> angles)
    {
        // Generate angles around the center of the map
        float optimalDistance = 359.0f / playersNumber;
        Debug.Log("Generating angles");
        for (int i = 0; i < playersNumber; ++i)
        {
            angles.Add(i * optimalDistance + PRG.GetNextRandom() % optimalDistance);
        }
        loadingPercent++;  
        yield return null;
        // Optimize angles
        Debug.Log("Optimizing angles");
        bool optimized;
        do
        {
            optimized = true;
            for (int i = 1; i < playersNumber; ++i)
            {
                if ((angles[i] - angles[i - 1]) / optimalDistance < 0.65f)
                {
                    if (angles[i - 1] > 0)
                    {
                        angles[i - 1]--;
                        optimized = false;
                    }
                }
            }
            if ((360 - angles[playersNumber - 1] + angles[0]) / optimalDistance < 0.65f)
            {
                angles[playersNumber - 1]--;
                optimized = false;
                yield return null;
            }
        } while (!optimized);
        loadingPercent++;  
        yield return null;
        // Randomize angles positions
        for (int i = 0; i < angles.Count; ++i)
        {
            float temp = angles[i];
            int randomIndex = i + (int)(PRG.GetNextRandom() % (angles.Count - i));
            angles[i] = angles[randomIndex];
            angles[randomIndex] = temp;
        }
        loadingPercent++;  
    }

    private Vector3 GetColonyPosition(List<Vector3> territories)
    {
        Vector3 position;
        bool far;
        do
        {
            far = true;
            position = new Vector3(PRG.GetNextRandom() % ((int)mapSize), 0, PRG.GetNextRandom() % ((int)mapSize));
            foreach(Vector3 known in territories)
            {
                if (Vector3.Distance(known, position) < (float)mapSize / 20)
                {
                    far = false;
                    break;
                }
            }
        } while (!far);

        return position;
    }

    private IEnumerator GrowPlayerLand()
    {
        clumpNumber = territories.Count;
        chaosLevel = (PRG.GetNextRandom() % 1000) / 1000.0f;
        List<Vector3>[] validEdges = new List<Vector3>[clumpNumber];
        for (int i = 0; i < clumpNumber; ++i)
        {
            validEdges[i] = new List<Vector3>
            {
                territories[i]
            };
        }
        loadingPercent++;  
        yield return null;
        Debug.Log("Generating Players Land");
        for (int size = 0; size < CLUMP_SIZE; ++size)
        {
            for (int id = 0; id < playersNumber; ++id)
            {
                Vector3 nextGrowth = GetNextGrowth(validEdges[id], chaosLevel);
                if (nextGrowth != -Vector3.one)
                {
                    map[(int)nextGrowth.x][(int)nextGrowth.z].owner = id;
                    validEdges[id].Add(nextGrowth);
                }
            }
            if ((size + 1) % 500 == 0) { loadingPercent++;   yield return null; }
        }

        Debug.Log("Generating Colonies Land");
        for (int size = 0; size < CLUMP_SIZE; ++size)
        {
            for (int id = playersNumber; id < clumpNumber; ++id)
            {
                Vector3 nextGrowth = GetNextGrowth(validEdges[id], chaosLevel);
                if (nextGrowth != -Vector3.one)
                {
                    map[(int)nextGrowth.x][(int)nextGrowth.z].owner = id;
                    validEdges[id].Add(nextGrowth);
                }
            }
            if ((size + 1) % 500 == 0) { loadingPercent++;   yield return null; }
        }
    }

    private Vector3 GetNextGrowth(List<Vector3> validEdges, float chaosLevel)
    {
        while (validEdges.Count > 0)
        {
            int index = (int)(chaosLevel * PRG.GetNextRandom()) % validEdges.Count;
            List<Vector3> neighbours = GetFreeNeighbours(validEdges[index]);
            if (neighbours.Count == 0)
            {
                validEdges.RemoveAt(index);
            }
            else
            {
                return neighbours[(int)(chaosLevel * PRG.GetNextRandom()) % neighbours.Count];
            }
        }
        return -Vector3.one;
    }

    private List<Vector3> GetFreeNeighbours(Vector3 position)
    {
        float[] dx = { 0, 1, 0, -1 };
        float[] dz = { 1, 0, -1, 0 };
        List<Vector3> freeNeighbours = new List<Vector3>();
        for (int i = 0; i < 4; ++i)
        {
            float x = position.x + dx[i];
            float z = position.z + dz[i];
            if (PositionInBounds(x, z) && map[(int)x][(int)z].owner == -1)
            {
                freeNeighbours.Add(new Vector3(x, 0, z));
            }
        }
        return freeNeighbours;
    }
    private bool PositionInBounds(float X, float Z)
    {
        int size = (int)mapSize;
        return X >= 0 && X < size && Z >= 0 && Z < size;
    }

    private IEnumerator AddFlatLands()
    {
        List<Vector3>[] validEdges = new List<Vector3>[clumpNumber];
        resourcesLand = new List<Vector3>[clumpNumber];
        for (int i = 0; i < clumpNumber; ++i)
        {
            resourcesLand[i] = new List<Vector3>();
            validEdges[i] = new List<Vector3>
            {
                territories[i]
            };
        }
        loadingPercent++;  
        yield return null;
        Debug.Log("Generating Flatlands");
        for (int size = 0; size < 0.55 * CLUMP_SIZE; ++size)
        {
            for (int id = 0; id < clumpNumber; ++id)
            {
                Vector3 nextGrowth = GetNextFlatTile(validEdges[id], chaosLevel, id);
                if (nextGrowth != -Vector3.one)
                {
                    map[(int)nextGrowth.x][(int)nextGrowth.z].flatland = true;
                    resourcesLand[id].Add(nextGrowth);
                    validEdges[id].Add(nextGrowth);
                }
            }
            if ((size + 1) % 400 == 0) { loadingPercent++;   yield return null; }
        }
    }

    private Vector3 GetNextFlatTile(List<Vector3> validEdges, float chaosLevel, int owner)
    {
        while (validEdges.Count > 0)
        {
            int index = (int)(chaosLevel * PRG.GetNextRandom()) % validEdges.Count;
            List<Vector3> neighbours = GetOwnedUnflatNeighbours(validEdges[index], owner);
            if (neighbours.Count == 0)
            {
                validEdges.RemoveAt(index);
            }
            else
            {
                return neighbours[(int)(chaosLevel * PRG.GetNextRandom()) % neighbours.Count];
            }
        }
        return -Vector3.one;
    }

    private List<Vector3> GetOwnedUnflatNeighbours(Vector3 position, int owner)
    {
        float[] dx = { 0, 1, 0, -1 };
        float[] dz = { 1, 0, -1, 0 };
        List<Vector3> unflatNeighbours = new List<Vector3>();
        for (int i = 0; i < 4; ++i)
        {
            float x = position.x + dx[i];
            float z = position.z + dz[i];
            if (PositionInBounds(x, z) && map[(int)x][(int)z].owner == owner && !map[(int)x][(int)z].flatland)
            {
                unflatNeighbours.Add(new Vector3(x, 0, z));
            }
        }
        return unflatNeighbours;
    }

    private IEnumerator GenerateHeightMap()
    {
        int resolution;
        switch (mapSize)
        {
            case GameSize.Small: resolution = 513; break;
            case GameSize.Medium: resolution = 513; break;
            case GameSize.Big: resolution = 1025; break;
            case GameSize.Huge: resolution = 1025; break;
            default: resolution = 33; break;
        }
        

        float offsetX = Mathf.Lerp(0, PRG.GetNextRandom() % (1 << 16), (float)PRG.GetNextRandom() / int.MaxValue); 
        float offsetY = Mathf.Lerp(0, PRG.GetNextRandom() % (1 << 16), (float)PRG.GetNextRandom() / int.MaxValue);
        float scale = (int)mapSize / 30 + (float)(PRG.GetNextRandom() % (1 << 16)) / (1 << 16) * 10;
        loadingPercent++;  
        yield return StartCoroutine(GenerateHeights(resolution, scale, offsetX, offsetY));
    }

    private IEnumerator GenerateHeights(int size, float scale, float offsetX, float offsetY)
    {
        heightMap = new float[size, size];
        for (int x = 0; x < size; ++x)
        {
            for (int y = 0; y < size; ++y)
            {
                float xCoord = (float)x / size * scale + offsetX;
                float yCoord = (float)y / size * scale + offsetY;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                heightMap[x, y] = sample;
            }
            if ((x + 1) % 50 == 0) { loadingPercent++;   yield return null; }
        }
    }

    private IEnumerator CombineMaps()
    {
        int resolution = (int)Mathf.Sqrt(heightMap.Length);

        for (int x = 0; x < resolution; ++x)
        {
            for (int y = 0; y < resolution; ++y)
            {
                int xCoord = (int)Mathf.Lerp(0, (int)mapSize, (float)x / resolution);
                int yCoord = (int)Mathf.Lerp(0, (int)mapSize, (float)y / resolution);
                if (map[xCoord][yCoord].flatland == true)
                {   
                    heightMap[y, x] = 0.4f;
                }

                if (map[xCoord][yCoord].owner != -1 && heightMap[y, x] < 0.35f) // is land, but underwater
                {
                    heightMap[y, x] = Mathf.Lerp(0.35f, 0.45f, heightMap[y, x]);
                }

                if (map[xCoord][yCoord].owner == -1) // is water
                {
                    heightMap[y, x] = Mathf.Lerp(0, 0.35f, heightMap[y, x]);
                }
            }
            if ((x + 1) % 50 == 0) { loadingPercent++;   yield return null; }
        }
    }

    private IEnumerator FixAnomalies()
    {
        int resolution = (int)Mathf.Sqrt(heightMap.Length);

        terrain = (Terrain)FindObjectOfType(typeof(Terrain));
        terrain.terrainData.heightmapResolution = resolution;
        terrain.terrainData.alphamapResolution = resolution - 1;
        for (int k = 0; k < SMOOTH_COUNT; ++k)
        {
            for (int x = 0; x < resolution; ++x)
            {
                for (int y = 0; y < resolution; ++y)
                {
                    int xCoord = (int)Mathf.Lerp(0, (int)mapSize, (float)x / resolution);
                    int yCoord = (int)Mathf.Lerp(0, (int)mapSize, (float)y / resolution);
                    if (!map[xCoord][yCoord].flatland)
                    {
                        SmoothingTerrainAt(y, x, 5);
                    }
                }
                if ((x + 1) % 50 == 0) { loadingPercent++;   yield return null; }
            }
            loadingPercent++;  
            yield return null;
        }

        terrain.terrainData.SetHeights(0, 0, heightMap);
        terrain.terrainData.size = new Vector3((int)mapSize, 20, (int)mapSize);
    }

    private void SmoothingTerrainAt(int i, int j, int radius)
    {
        float sum = 0.0f;
        for (int x = -radius/2; x <= radius/2; ++x)
        {
            for (int y = -radius/2; y <= radius/2; ++y)
            {
                try
                {
                    sum += heightMap[i + x, j + y];
                }
                catch
                {
                    sum += 0;
                }
            }
        }
        heightMap[i, j] = sum / (radius * radius);
    }

    private IEnumerator DistributeResources()
    {
        string[] gameResources = ResourceManager.GetGameResources;

        foreach(string type in gameResources)
        {
            for (int id = 0; id < clumpNumber; ++id)
            {
                if (resourcesLand[id].Count != 0)
                {
                    Quaternion objectRotation = Quaternion.Euler(0, PRG.GetNextRandom() % 360, 0);
                    int index = resourcesLand[id].Count * 1 / 4 + (int)PRG.GetNextRandom() % (resourcesLand[id].Count * 1 / 3);
                    Vector3 objectPosition = resourcesLand[id][index];
                    for (int i = -4; i <= 4; ++i)
                    {
                        for (int j = -4; j <= 4; ++j)
                        {
                            try
                            {
                                resourcesLand[id].RemoveAt(resourcesLand[id].IndexOf(new Vector3(i + objectPosition.x, 0, j + objectPosition.z)));
                            }
                            catch
                            {
                            }
                        }
                    }
                    objectPosition.y = terrain.SampleHeight(objectPosition);
                    GameObject newObject = (GameObject)GameObject.Instantiate(ResourceManager.GetWorldObject(type), objectPosition, objectRotation);
                    Resource resource = newObject.GetComponent<Resource>();
                    resource.ObjectId = ResourceManager.GetNewObjectId();
                }
            }
            loadingPercent++;  
            yield return null;
        }
    }

    private IEnumerator TextureMap()
    {
        int height = terrain.terrainData.alphamapHeight;
        int width = terrain.terrainData.alphamapWidth;
        float[,,] TerrainTextures = new float[height, width, 3];
        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                float terrainHeight = heightMap[i, j];
                float sandPercentage = 0;
                float grassPercentage = 0;
                float mountainPercentage = 0;
                if (terrainHeight <= 0.395f) { sandPercentage = 1; grassPercentage = 0; mountainPercentage = 0; }
                //if (terrainHeight > 0.38f && terrainHeight < 0.4f) { sandPercentage = 0.4f - terrainHeight; grassPercentage = terrainHeight - 0.38f; mountainPercentage = 0; }
                if (terrainHeight >= 0.395f && terrainHeight < 0.405f) { sandPercentage = 0; grassPercentage = 1; mountainPercentage = 0; }
                //if (terrainHeight >= 0.41f) { sandPercentage = 0; grassPercentage = terrainHeight < 0.43f ? (0.43f - terrainHeight) : 0; mountainPercentage = terrainHeight < 0.43f ? (terrainHeight - 0.41f) : 1; }
                if (terrainHeight >= 0.405f) { sandPercentage = 0; grassPercentage = 0; mountainPercentage = 1; }
                Vector3 heights = new Vector3(sandPercentage, grassPercentage, mountainPercentage);
                heights = OneifyVector(heights);
                TerrainTextures[i, j, 0] = heights[0];
                TerrainTextures[i, j, 1] = heights[1];
                TerrainTextures[i, j, 2] = heights[2];
            }
            if ((i + 1) % 50 == 0) { loadingPercent++;   yield return null; }
        }
        terrain.terrainData.terrainLayers = terrainLayers;
        terrain.terrainData.SetAlphamaps(0, 0, TerrainTextures);
        UnityStandardAssets.Water.Water water = (UnityStandardAssets.Water.Water)FindObjectOfType(typeof(UnityStandardAssets.Water.Water));
        water.transform.position = new Vector3((float)mapSize / 2, 3.5f, (float)mapSize / 2);
        water.transform.localScale = new Vector3((int)mapSize, 7, (int)mapSize);
    }

    Vector3 OneifyVector(Vector3 vector)
    {
        float sum = vector.x + vector.y + vector.z;
        return new Vector3(Mathf.Lerp(0, 1, vector.x / sum), Mathf.Lerp(0, 1, vector.y / sum), Mathf.Lerp(0, 1, vector.z / sum));
    }

    private IEnumerator DrawBorders()
    {
        yield return StartCoroutine(GetBorders());
        List<Coroutine> borderMakers = new List<Coroutine>();
        for (int id = 0; id < clumpNumber; ++id)
        {
            Color color = new Color(0.5f, 0.5f, 0.5f);
            if (id < playersNumber)
            {
                color = teamColors[id];
            }
            borderMakers.Add(StartCoroutine(DrawBorder(borders[id], terrain, color, BordersSourceMaterial, "Territory" + id)));
        }
        ((GameManager)FindObjectOfType(typeof(GameManager))).InitOwnership(clumpNumber, playersNumber);
        foreach (Coroutine borderMaker in borderMakers)
        {
            loadingPercent++;  
            yield return null;
            yield return borderMaker;
        }
    }

    private IEnumerator DrawBorder(List<Vector3> borders, Terrain terrain, Color color, Material sourceMaterial, string name)
    {
        Vector3 position = borders[0];
        position.y = terrain.SampleHeight(position) + 0.25f;
        GameObject borderMaker = (GameObject)GameObject.Instantiate(ResourceManager.GetGameObject("BordersMaker"), position, new Quaternion());
        borderMaker.name = name;
        borderMaker.GetComponent<TrailRenderer>().material = new Material(sourceMaterial);
        borderMaker.GetComponent<TrailRenderer>().material.color = color;
        if (terrain)
        {
            for (int i = 1; i < borders.Count; ++i)
            {
                if (terrain)
                {
                    position = borders[i];
                    position.y = terrain.SampleHeight(position) + 0.25f;
                    borderMaker.transform.position = position;
                    yield return null;
                }
            }
            if (terrain)
            {
                position = borders[0];
                position.y = terrain.SampleHeight(position) + 0.25f;
                borderMaker.transform.position = position;
            }
        }
    }

    private IEnumerator GetBorders()
    {
        int[] dx = { 0, 1, 0, -1 };
        int[] dz = { 1, 0, -1, 0 };
        borders = new List<Vector3>[clumpNumber];
        for (int i = 0; i < clumpNumber; ++i)
        {
            borders[i] = new List<Vector3>();
        }
        loadingPercent++;  
        yield return null;
        for (int i = 0; i < (int)mapSize; ++i)
        {
            for (int j = 0; j < (int)mapSize; ++j)
            {
                if (map[i][j].owner != -1)
                {
                    for (int k = 0; k < 4; ++k)
                    {
                        try
                        {
                            if (map[i + dx[k]][j + dz[k]].owner != map[i][j].owner)
                            {
                                borders[map[i][j].owner].Add(new Vector3(i, 0, j));
                                break;
                            }
                        }
                        catch
                        {
                            borders[map[i][j].owner].Add(new Vector3(i, 0, j));
                            break;
                        }
                    }
                }
            }
            if ((i + 1) % 50 == 0) { loadingPercent++;   yield return null; }
        }

        for (int id = 0; id < clumpNumber; ++id)
        {
            List<Vector3> sortedBorder = new List<Vector3>();
            Vector3 startPoint = borders[id][0];
            Vector3 currentPoint = startPoint;
            sortedBorder.Add(currentPoint);
            borders[id].RemoveAt(0);
            while (borders[id].Count > 0)
            {
                Vector3 nextPoint = GetNextBorderPoint(borders[id], currentPoint);
                if (nextPoint != -Vector3.one)
                {
                    sortedBorder.Add(nextPoint);
                    currentPoint = nextPoint;
                    borders[id].Remove(nextPoint);
                }
                else
                {
                    break;
                }
            }
            currentPoint = startPoint;
            while (borders[id].Count > 30)
            {
                Vector3 nextPoint = GetNextBorderPoint(borders[id], currentPoint);
                if (nextPoint != -Vector3.one)
                {
                    sortedBorder.Insert(0, nextPoint);
                    currentPoint = nextPoint;
                    borders[id].Remove(nextPoint);
                }
                else
                {
                    break;
                }
            }
            borders[id] = sortedBorder;
            loadingPercent++;  
            yield return null;
        }
    }

    private Vector3 GetNextBorderPoint(List<Vector3> border, Vector3 currentPoint)
    {
        Vector3 closestPoint = -Vector3.one;
        float distance = int.MaxValue;

        for(int i = 0; i < border.Count; ++i)
        {
            float newDistance = (border[i] - currentPoint).sqrMagnitude;
            if (newDistance < 64 && newDistance < distance)
            {
                closestPoint = border[i];
                distance = newDistance;
            }
        }

        return closestPoint;
    }

    private IEnumerator InitialUnits(bool autoTest)
    {
        string humanPlayer = autoTest ? "Marius" : PlayerManager.GetPlayerName();
        InstantiatePlayer(humanPlayer, playersPositions[0], teamColors[0], true, 0);
        loadingPercent++;  
        yield return null;
        for (int k = 1; k < playersNumber; ++k)
        {
            InstantiatePlayer(PlayerManager.GetComputerNames()[k - 1], playersPositions[k], teamColors[k], false, k);
            yield return null;
        }
        loadingPercent++;  
    }

    private void InstantiatePlayer(string name, Vector3 position, Color teamColor, bool isHuman, int id)
    {
        GameObject playerObject = (GameObject)GameObject.Instantiate(ResourceManager.GetPlayerObject(), position, new Quaternion());
        if (isHuman) Camera.main.transform.root.position = playerObject.transform.position;
        Player player = playerObject.GetComponent<Player>();
        player.isHuman = isHuman;
        player.teamColor = teamColor;
        player.userName = player.name = name;
        player.playerID = id;
        Buildings buildings = player.GetComponentInChildren<Buildings>();
        GameObject townCenterObject = (GameObject)Instantiate(ResourceManager.GetBuilding("TownCenter" + (isHuman ? "" : "AI")), player.transform.position, new Quaternion());
        if (!isHuman)
        {
            townCenterObject.GetComponent<BehaviorParameters>().TeamId = id;
        }
        player.townCenter = townCenterObject.GetComponent<TownCenter>();
        player.townCenter.ObjectId = ResourceManager.GetNewObjectId();
        if (buildings) townCenterObject.transform.parent = buildings.transform;
        player.townCenter.SetPlayer();
        player.townCenter.SetTeamColor();
        player.townCenter.SetPlayingArea(player.GetComponentInChildren<HUD>().GetPlayingArea());
        player.townCenter.CalculateBounds();
        player.townCenter.SetSpawnPoint();
        player.townCenter.FirstUnits(1);
    }

    public List<Vector3>[] GetAllBorders()
    {
        return borders;
    }

    public void ChangeBorder(int borderID, int ownerID)
    {
        GameObject border = GameObject.Find("Territory" + borderID);
        if (ownerID != -1)
        {
            border.GetComponent<TrailRenderer>().material.color = teamColors[ownerID];
        }
        else
        {
            border.GetComponent<TrailRenderer>().material.color = new Color(0.5f, 0.5f, 0.5f);
        }
    }
}
