using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RTS;
using Pathfinding;

public class LevelLoader : MonoSingleton<LevelLoader>
{
    private const int CLUMP_SIZE = 6500;
    private const int SMOOTH_COUNT = 3;

    private int clumpNumber;
    private float chaosLevel;
    private List<Vector3> territories;

    private List<Vector3> playersPositions;

    private List<Vector3>[] resourcesLand;

    public struct MapPoint
    {
        public int owner;
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
                InitNewWorld(true);
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


    void OnLevelWasLoaded()
    {

        if (initialised)
        {
            if (ResourceManager.LevelName != null && ResourceManager.LevelName != "")
            {
                LoadManager.LoadGame(ResourceManager.LevelName);
            }
            else
            {
                if (SceneManager.GetActiveScene().name == "GameScene")
                {
                    Terrain terrain = (Terrain)GameObject.FindObjectOfType(typeof(Terrain));
                    if (terrain)
                    {
                        terrain.terrainData.size = new Vector3((float)mapSize, 20, (float)mapSize);
                    }
                    InitNewWorld(false);
                }

                SetObjectIds();
            }
            Time.timeScale = 1.0f;
            ResourceManager.MenuOpen = false;
           
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                AstarPath.active.data.gridGraph.center = new Vector3((int)mapSize / 2, 0, (int)mapSize / 2);
                AstarPath.active.data.gridGraph.cutCorners = false;
                AstarPath.active.data.gridGraph.SetDimensions((int)mapSize, (int)mapSize, 1);
                AstarPath.active.data.gridGraph.maxSlope = 30;
                AstarPath.active.data.gridGraph.Scan();
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

    private void InitNewWorld(bool autoTest)
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

        // Level Generation

        // Step 1
        PlacePlayers();
        // Step 2
        GrowPlayerLand();
        // Step 3
        AddFlatLands();
        // Step 4
        GenerateHeightMap();
        // Step 5
        CombineMaps();
        // Step 6
        FixAnomalies();
        // Step 7
        DistributeResources();
        // Step 8 (Maybe?)
        // TODO: PlaceTrees();
        // Step 9
        TextureMap();
        // Step 10
        InitialUnits(autoTest);

    }


    private void PlacePlayers()
    {
        playersPositions = GetPlayersPositions();

        map = new MapPoint[(int)mapSize][];
        for (int i = 0; i < (int)mapSize; ++i)
        {
            map[i] = new MapPoint[(int)mapSize];
            for (int j = 0; j < (int)mapSize; ++j)
            {
                map[i][j].owner = -1;
                map[i][j].flatland = false;
            }
        }

        for (int i = 0; i < playersNumber; ++i)
        {
            map[(int)playersPositions[i].x][(int)playersPositions[i].z].owner = i;
        }

        territories = new List<Vector3>(playersPositions);

        for (int dummyPlayer = playersNumber; dummyPlayer < 0.75 / (CLUMP_SIZE / Mathf.Pow((int)mapSize, 2)); ++dummyPlayer)
        {
            Vector3 colonizablePosition = GetColonyPosition(territories);
            map[(int)colonizablePosition.x][(int)colonizablePosition.z].owner = dummyPlayer;
            territories.Add(colonizablePosition);
        }
    }

    private List<Vector3> GetPlayersPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        float outerDiskRadius = ((float)mapSize - 16) / 2;
        float innerDiskRadius = ((float)mapSize * 0.5f + PRG.GetNextRandom() % ((float)mapSize * 0.33f)) / 2;

        float[] angles = GetAngles();

        for (int i = 0; i < playersNumber; ++i)
        {
            float radius = innerDiskRadius + PRG.GetNextRandom() % (outerDiskRadius - innerDiskRadius);
            float X = (float)mapSize / 2 + radius * Mathf.Cos(Mathf.Deg2Rad * angles[i]);
            float Z = (float)mapSize / 2 + radius * Mathf.Sin(Mathf.Deg2Rad * angles[i]);

            positions.Add(new Vector3(X, 0, Z));
        }

        return positions;
    }

    private float[] GetAngles()
    {
        // Generate angles around the center of the map
        float[] angles = new float[playersNumber];
        float optimalDistance = 359.0f / playersNumber;
        for (int i = 0; i < playersNumber; ++i)
        {
            angles[i] = i * optimalDistance + PRG.GetNextRandom() % optimalDistance;
        }
        // Optimize angles
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
            }
        } while (!optimized);

        // Randomize angles positions
        for (int i = 0; i < angles.Length; ++i)
        {
            float temp = angles[i];
            int randomIndex = i + (int)(PRG.GetNextRandom() % (angles.Length - i));
            angles[i] = angles[randomIndex];
            angles[randomIndex] = temp;
        }

        return angles;
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

    private void GrowPlayerLand()
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
        }
       

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

    private void AddFlatLands()
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

    private void GenerateHeightMap()
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

        GenerateHeights(resolution, scale, offsetX, offsetY);
    }

    private void GenerateHeights(int size, float scale, float offsetX, float offsetY)
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
        }
    }

    private void CombineMaps()
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
        }
    }

    private void FixAnomalies()
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
            }
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

    private void DistributeResources()
    {
        string[] gameResources = ResourceManager.GetGameResources;

        foreach(string type in gameResources)
        {
            for (int id = 0; id < clumpNumber; ++id)
            {
                if (resourcesLand[id].Count != 0)
                {
                    Quaternion objectRotation = Quaternion.Euler(0, PRG.GetNextRandom() % 360, 0);
                    int index = resourcesLand[id].Count * 3 / 5 + (int)PRG.GetNextRandom() % (resourcesLand[id].Count * 2 / 5);
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
        }
    }

    private void TextureMap()
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

    private void InitialUnits(bool autoTest)
    {
        string humanPlayer = autoTest ? "Marius" : PlayerManager.GetPlayerName();
        InstantiatePlayer(humanPlayer, playersPositions[0], teamColors[0], true);

        for (int k = 1; k < playersNumber; ++k)
        {
            InstantiatePlayer(PlayerManager.GetComputerNames()[k - 1], playersPositions[k], teamColors[k], false);
        }
    }

    private void InstantiatePlayer(string name, Vector3 position, Color teamColor, bool isHuman)
    {
        GameObject playerObject = (GameObject)GameObject.Instantiate(ResourceManager.GetPlayerObject(), position, new Quaternion());
        if (isHuman) Camera.main.transform.root.position = playerObject.transform.position;
        Player player = playerObject.GetComponent<Player>();
        player.isHuman = isHuman;
        player.teamColor = teamColor;
        player.userName = player.name = name;
        Buildings buildings = player.GetComponentInChildren<Buildings>();
        GameObject townCenterObject = (GameObject)Instantiate(ResourceManager.GetBuilding("TownCenter"), player.transform.position, new Quaternion());
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

}
