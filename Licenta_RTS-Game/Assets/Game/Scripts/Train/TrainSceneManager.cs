using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/* Seed: 151646380 */

public class TrainSceneManager : MonoBehaviour
{
    public VictoryCondition[] victoryConditions;
    public int[] territoriesOwner;
    public GameObject SceneContents;
    public GameObject SceneContentsPrefab;
    public List<Vector3>[] borders;
    private MapPoint[][] map;
    public Color[] teamColors;
    public AstarPath graph;
    public Terrain terrain;

    public struct MapPoint
    {
        public int owner;
        public int id;
        public bool flatland;
    }

    private void Awake()
    {
        terrain = FindObjectOfType<GraphInitializer>().terrain;
    }

    void Start()
    {
        using (StreamReader fileReader = File.OpenText("MapData.txt"))
        {
            int mapSize = System.Int32.Parse(fileReader.ReadLine());
            map = new MapPoint[mapSize][];
            for (int i = 0; i < mapSize; ++i)
            {
                map[i] = new MapPoint[mapSize];
                for (int j = 0; j < mapSize; ++j)
                {
                    string line = fileReader.ReadLine();
                    string[] lineArray = line.Split(' ');
                    map[i][j].id = System.Int32.Parse(lineArray[0]);
                    map[i][j].owner = System.Int32.Parse(lineArray[1]);
                    map[i][j].flatland = System.Boolean.Parse(lineArray[2]);
                }
            }
        }
        using (StreamReader fileReader = File.OpenText("BorderData.txt"))
        {
            int bordersLength = System.Int32.Parse(fileReader.ReadLine());
            borders = new List<Vector3>[bordersLength];
            for (int i = 0; i < bordersLength; ++i)
            {
                int borderCount = System.Int32.Parse(fileReader.ReadLine());
                borders[i] = new List<Vector3>();
                for (int j = 0; j < borderCount; ++j)
                {
                    string line = fileReader.ReadLine();
                    string[] lineArray = line.Split(' ');
                    borders[i].Add(new Vector3(System.Int32.Parse(lineArray[0]), System.Int32.Parse(lineArray[1]), System.Int32.Parse(lineArray[2])));
                }
            }
        }
    }

    void Update()
    {
        if (victoryConditions != null)
        {
            foreach (VictoryCondition victoryCondition in victoryConditions)
            {
                if (victoryCondition != null && victoryCondition.GameFinished())
                {
                    Destroy(SceneContents);
                    SceneContents = Instantiate(SceneContentsPrefab, this.transform);
                }
            }
        }
    }

    public void SetOwner(int id, int owner)
    {
        territoriesOwner[id] = owner;
    }

    public int GetOwner(int id)
    {
        try
        {
            return territoriesOwner[id];
        }
        catch
        {
            return -2;
        }
    }

    public int GetNumberOfTerritories()
    {
        return territoriesOwner.Length;
    }

    public List<Vector3>[] GetAllBorders()
    {
        return borders;
    }
}
