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

        public static void LoadGame(string filename)
        {
            char separator = Path.DirectorySeparatorChar;
            string path = "SavedGames" + separator + PlayerManager.GetPlayerName() + separator + filename + ".json";
            if (!File.Exists(path))
            {
                Debug.Log("Unable to find " + path + ". Loading will crash, so aborting.");
                return;
            }
            string input;
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
                                    case "GameInfo": LoadGameInfo(reader); break;
                                    case "Sun": LoadLighting(reader); break;
                                    case "Ground": LoadTerrain(reader); break;
                                    case "Camera": LoadCamera(reader); break;
                                    case "Resources": LoadResources(reader); break;
                                    case "Players": LoadPlayers(reader); break;
                                    default: break;
                                }
                            }
                        }
                    }
                }
            }
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

        private static void LoadTerrain(JsonTextReader reader)
        {
            if (reader == null) return;
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
                        if ((string)reader.Value == "Position") position = LoadVector(reader);
                        else if ((string)reader.Value == "Rotation") rotation = LoadQuaternion(reader);
                        else if ((string)reader.Value == "Scale") scale = LoadVector(reader);
                        else if ((string)reader.Value == "Size") size = LoadVector(reader);
                        else if ((string)reader.Value == "Resolution") { reader.Read(); resolution = (int)(double)reader.Value; }
                        else if ((string)reader.Value == "HeightMap") heightMap = LoadHeightMap(reader, resolution + 1);
                        else if ((string)reader.Value == "AlphaMap") alphaMap = LoadAlphaMap(reader, resolution);
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
                    return;
                }
            }
        }

        private static float[,,] LoadAlphaMap(JsonTextReader reader, int resolution)
        {
            float[,,] alphaMap = new float[resolution, resolution, 3];
            for (int i = 0; i < resolution; ++i)
            {
                for (int j = 0; j < resolution; ++j)
                {
                    do
                    {
                        reader.Read();
                    } while (reader.Value == null);
                    alphaMap[i, j, 0] = (float)(double)reader.Value;
                    do
                    {
                        reader.Read();
                    } while (reader.Value == null);
                    alphaMap[i, j, 1] = (float)(double)reader.Value;
                    do
                    {
                        reader.Read();
                    } while (reader.Value == null);
                    alphaMap[i, j, 2] = (float)(double)reader.Value;
                }
            }
            return alphaMap;
        }

        private static float[,] LoadHeightMap(JsonTextReader reader, int resolution)
        {
            float[,] heightMap = new float[resolution, resolution];
            for (int i = 0; i < resolution; ++i)
            {
                for (int j = 0; j < resolution; ++j)
                {
                    do
                    {
                        reader.Read();
                    } while (reader.Value == null);
                    heightMap[i, j] = (float)(double)reader.Value;
                }
            }
            return heightMap;
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
                    return;
                }
            }
        }

        private static void LoadResources(JsonTextReader reader)
        {
            if (reader == null) return;
            string currValue = "", type = "";
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
                    }
                }
                else if (reader.TokenType == JsonToken.EndArray) return;
            }
        }

        private static void LoadPlayers(JsonTextReader reader)
        {
            if (reader == null) return;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    GameObject newObject = (GameObject)GameObject.Instantiate(ResourceManager.GetPlayerObject());
                    Player player = newObject.GetComponent<Player>();
                    player.LoadDetails(reader);
                }
                else if (reader.TokenType == JsonToken.EndArray) return;
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