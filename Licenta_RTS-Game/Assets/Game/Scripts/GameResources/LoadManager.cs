using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using RTS;
using System;

namespace RTS
{
    
    public static class LoadManager
    {
        private static int clumps;
        public static int loadingProgress;
        public static int loadingCap;
        public static IEnumerator LoadGame(string filename, LevelLoader loader)
        {
            char separator = Path.DirectorySeparatorChar;
            string path = "SavedGames" + separator + PlayerManager.GetPlayerName() + separator + filename + ".json";
            if (!File.Exists(path))
            {
                Debug.Log("Unable to find " + path + ". Loading will crash, so aborting.");
                yield break;
            }
            string input = "";
            using (StreamReader sr = new StreamReader(path))
            {
                input = sr.ReadToEnd();
            }
            if (input != null)
            {
                //parse contents of file
                using (JsonTextReader reader = new JsonTextReader(new StringReader(input)))
                {
                    while (reader.Read())
                    {
                        if (reader.Value != null)
                        {
                            if (reader.TokenType == JsonToken.PropertyName)
                            {
                                string property = (string)reader.Value;
                                switch (property)
                                {
                                    case "GameInfo": LoadGameInfo(reader); loadingProgress++; loader.loadingStep++; yield return null; break;
                                    case "Sun": LoadLighting(reader); loadingProgress += 3; loader.loadingStep++; yield return null; break;
                                    case "Ground": yield return loader.StartCoroutine(LoadTerrain(reader)); loader.loadingStep++; yield return null; break;
                                    case "Borders": yield return loader.StartCoroutine(LoadBorders(reader, loader)); loader.loadingStep++; yield return null; break;
                                    case "Camera": LoadCamera(reader); loadingProgress += 3; yield return null; loader.loadingStep++; break;
                                    case "Resources": yield return loader.StartCoroutine(LoadResources(reader)); loader.loadingStep++; yield return null; break;
                                    case "Players": yield return loader.StartCoroutine(LoadPlayers(reader, loader)); yield return null; break;
                                    default: break;
                                }
                            }
                        }
                    }
                }
            }
            loader.gameObject.GetComponent<LoadingScreen>().enabled = false;
        }

        public static Vector3 LoadVector(JsonTextReader reader)
        {
            Vector3 position = new Vector3(0, 0, 0);
            if (reader == null) return position;
            string currVal = "";
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName) currVal = (string)reader.Value;
                    else
                    {
                        switch (currVal)
                        {
                            case "x": position.x = (float)(double)reader.Value; break;
                            case "y": position.y = (float)(double)reader.Value; break;
                            case "z": position.z = (float)(double)reader.Value; break;
                            default: break;
                        }
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject) return position;
            }
            return position;
        }

        public static Quaternion LoadQuaternion(JsonTextReader reader)
        {
            Quaternion rotation = new Quaternion(0, 0, 0, 0);
            if (reader == null) return rotation;
            string currVal = "";
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName) currVal = (string)reader.Value;
                    else
                    {
                        switch (currVal)
                        {
                            case "x": rotation.x = (float)(double)reader.Value; break;
                            case "y": rotation.y = (float)(double)reader.Value; break;
                            case "z": rotation.z = (float)(double)reader.Value; break;
                            case "w": rotation.w = (float)(double)reader.Value; break;
                            default: break;
                        }
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject) return rotation;
            }
            return rotation;
        }

        private static void LoadGameInfo(JsonTextReader reader)
        {
            if (reader == null) return;
            LevelLoader levelLoader = (LevelLoader)GameObject.FindObjectOfType(typeof(LevelLoader));
            string currVal = "";
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName) currVal = (string)reader.Value;
                    else
                    {
                        switch (currVal)
                        {
                            case "PlayersNumber": levelLoader.playersNumber = (int)(double)reader.Value; break;
                            case "Seed": levelLoader.seed = (string)reader.Value; break;
                            case "Clumps":
                                {
                                    clumps = (int)(System.Int64)reader.Value;
                                    loadingCap = 13 * levelLoader.playersNumber + 8 + 7 + 2 * clumps + (4 * clumps / 50 + 2);
                                    break;
                                }
                            default: break;
                        }
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject) return;
            }
        }

        private static void LoadLighting(JsonTextReader reader)
        {
            if (reader == null) return;
            Vector3 position = new Vector3(0, 0, 0), scale = new Vector3(1, 1, 1);
            Quaternion rotation = new Quaternion(0, 0, 0, 0);
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if ((string)reader.Value == "Position") position = LoadVector(reader);
                        else if ((string)reader.Value == "Rotation") rotation = LoadQuaternion(reader);
                        else if ((string)reader.Value == "Scale") scale = LoadVector(reader);
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    GameObject sun = (GameObject)GameObject.Instantiate(ResourceManager.GetGameObject("Sun"), position, rotation);
                    sun.transform.localScale = scale;
                    return;
                }
            }
        }

        private static IEnumerator LoadTerrain(JsonTextReader reader)
        {
            if (reader == null) yield break;
            Vector3 position = new Vector3(0, 0, 0), scale = new Vector3(1, 1, 1), size = new Vector3(1000, 20, 1000);
            Quaternion rotation = new Quaternion(0, 0, 0, 0);
            float[,] heightMap = null;
            float[,,] alphaMap = null;
            int resolution = 0;
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if ((string)reader.Value == "Position") { position = LoadVector(reader); loadingProgress++; yield return null; }
                        else if ((string)reader.Value == "Rotation") { rotation = LoadQuaternion(reader); loadingProgress++; yield return null; }
                        else if ((string)reader.Value == "Scale") { scale = LoadVector(reader); loadingProgress++; yield return null; }
                        else if ((string)reader.Value == "Size") { size = LoadVector(reader); loadingProgress++; yield return null; }
                        else if ((string)reader.Value == "Resolution") { reader.Read(); resolution = (int)(double)reader.Value; loadingProgress++; yield return null; }
                        else if ((string)reader.Value == "HeightMap") { heightMap = LoadHeightMap(reader, resolution + 1); loadingProgress++; yield return null; }
                        else if ((string)reader.Value == "AlphaMap") { alphaMap = LoadAlphaMap(reader, resolution); loadingProgress++; yield return null; }
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    GameObject ground = (GameObject)GameObject.Instantiate(ResourceManager.GetGameObject("GroundHolder"), position, rotation);
                    ground.transform.localScale = scale;
                    TerrainData terrainData = ground.GetComponentInChildren<Terrain>().terrainData;
                    terrainData.alphamapResolution = resolution;
                    terrainData.heightmapResolution = resolution + 1;
                    terrainData.SetHeights(0, 0, heightMap);
                    terrainData.SetAlphamaps(0, 0, alphaMap);
                    terrainData.size = size;
                    UnityStandardAssets.Water.Water water = ground.GetComponentInChildren<UnityStandardAssets.Water.Water>();
                    water.transform.position = new Vector3(size.x / 2, 3.5f, size.z / 2);
                    water.transform.localScale = new Vector3(size.x, 7, size.x);
                    ((LevelLoader)GameObject.FindObjectOfType(typeof(LevelLoader))).mapSize = (GameSize)size.x;
                    loadingProgress++;
                    yield break;
                }
            }
        }

        private static IEnumerator LoadBorders(JsonTextReader reader, LevelLoader loader)
        {
            if (reader == null) yield break;
            int size = 0;
            int index = 0;
            List<Vector3>[] borders = null;
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if ((string)reader.Value == "Size")
                        {
                            do
                            {
                                reader.Read();
                            } while (reader.Value == null);
                            size = (int)(double)reader.Value;
                            borders = new List<Vector3>[size];
                        }
                        else if (((string)reader.Value).Contains("Territory"))
                        {
                            borders[index] = LoadBordersArray(reader, size);
                            index++;
                            loadingProgress++;
                            yield return null;
                        }
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    yield return loader.StartCoroutine(loader.LoadBorders(borders));
                    yield return null;
                    yield break;
                }
            }
        }

        private static List<Vector3> FromArrayToVectorList(float[,] fromArray)
        {
            List<Vector3> toList = new List<Vector3>();
            for (int i = 0; i < fromArray.Length / 3; i++)
            {
                toList.Add(new Vector3(fromArray[i, 0], fromArray[i, 1], fromArray[i, 2]));
            }
            return toList;
        }

        private static List<Vector3> LoadBordersArray(JsonTextReader reader, int size)
        {
            if (reader == null) return null;
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    string borderCompressed = (string)reader.Value;
                    return FromArrayToVectorList(DataForamatter<float[,]>.LoadAnArrayFromString(borderCompressed));
                }
            }
            return null;
        }

        private static float[,,] LoadAlphaMap(JsonTextReader reader, int resolution)
        {
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    string alphamapCompressed = (string)reader.Value;
                    float[,,] alphaMap = DataForamatter<float[,,]>.LoadAnArrayFromString(alphamapCompressed);
                    return alphaMap;
                }
            }
            return null;
        }

        private static float[,] LoadHeightMap(JsonTextReader reader, int resolution)
        {
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    string heightMapCompressed = (string)reader.Value;

                    float[,] heightMap = DataForamatter<float[,]>.LoadAnArrayFromString(heightMapCompressed);
                    return heightMap;
                }
            }
            return null;
        }

        private static void LoadCamera(JsonTextReader reader)
        {
            if (reader == null) return;
            Vector3 position = new Vector3(0, 0, 0), scale = new Vector3(1, 1, 1);
            Quaternion rotation = new Quaternion(0, 0, 0, 0);
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if ((string)reader.Value == "Position") position = LoadVector(reader);
                        else if ((string)reader.Value == "Rotation") rotation = LoadQuaternion(reader);
                        else if ((string)reader.Value == "Scale") scale = LoadVector(reader);
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    GameObject camera = Camera.main.transform.root.gameObject;
                    camera.transform.position = position;
                    camera.transform.rotation = rotation;
                    camera.transform.localScale = scale;
                    loadingProgress++;
                    return;
                }
            }
        }

        private static IEnumerator LoadResources(JsonTextReader reader)
        {
            if (reader == null) yield break;
            string currValue = "", type = "";
            int count = 1;
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName) currValue = (string)reader.Value;
                    else if (currValue == "Type")
                    {
                        type = (string)reader.Value;
                        GameObject newObject = (GameObject)GameObject.Instantiate(ResourceManager.GetWorldObject(type));
                        Resource resource = newObject.GetComponent<Resource>();
                        resource.LoadDetails(reader);
                        count++;
                        if (count % 50 == 0) { loadingProgress++; yield return null; }
                    }
                }
                else if (reader.TokenType == JsonToken.EndArray) { loadingProgress++; yield break; }
            }
        }

        private static IEnumerator LoadPlayers(JsonTextReader reader, LevelLoader loader)
        {
            if (reader == null) yield break;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    GameObject newObject = (GameObject)GameObject.Instantiate(ResourceManager.GetPlayerObject());
                    Player player = newObject.GetComponent<Player>();
                    yield return loader.StartCoroutine(player.LoadDetails(reader));
                    loadingProgress++;
                    yield return null;
                }
                else if (reader.TokenType == JsonToken.EndArray) yield break;
            }
        }

        public static Color LoadColor(JsonTextReader reader)
        {
            Color color = new Color(0, 0, 0, 0);
            string currVal = "";
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName) currVal = (string)reader.Value;
                    else
                    {
                        switch (currVal)
                        {
                            case "r": color.r = (float)(double)reader.Value; break;
                            case "g": color.g = (float)(double)reader.Value; break;
                            case "b": color.b = (float)(double)reader.Value; break;
                            case "a": color.a = (float)(double)reader.Value; break;
                            default: break;
                        }
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject) return color;
            }
            return color;
        }

        public static List<string> LoadStringArray(JsonTextReader reader)
        {
            List<string> values = new List<string>();
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    values.Add((string)reader.Value);
                }
                else if (reader.TokenType == JsonToken.EndArray) return values;
            }
            return values;
        }
        public static Rect LoadRect(JsonTextReader reader)
        {
            Rect rect = new Rect(0, 0, 0, 0);
            if (reader == null) return rect;
            string currVal = "";
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName) currVal = (string)reader.Value;
                    else
                    {
                        switch (currVal)
                        {
                            case "x": rect.x = (float)(double)reader.Value; break;
                            case "y": rect.y = (float)(double)reader.Value; break;
                            case "width": rect.width = (float)(double)reader.Value; break;
                            case "height": rect.height = (float)(double)reader.Value; break;
                            default: break;
                        }
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject) return rect;
            }
            return rect;
        }

    }
}