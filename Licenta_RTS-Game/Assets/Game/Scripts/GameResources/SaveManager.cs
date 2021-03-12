using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;

namespace RTS
{
    public static class SaveManager
    {

        public static void SaveGame(string filename)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            Directory.CreateDirectory("SavedGames");
            char separator = Path.DirectorySeparatorChar;
            string path = "SavedGames" + separator + PlayerManager.GetPlayerName() + separator + filename + ".json";
            using (StreamWriter sw = new StreamWriter(path))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.WriteStartObject();
                    SaveGameDetails(writer);
                    writer.WriteEndObject();
                }
            }
        }

        private static void SaveGameDetails(JsonWriter writer)
        {
            SaveGameInfo(writer);
            SaveLighting(writer);
            SaveTerrain(writer);
            SaveBorders(writer);
            SaveCamera(writer);
            SaveResources(writer);
            SavePlayers(writer);
        }


        public static void WriteVector(JsonWriter writer, string name, Vector3 vector)
        {
            if (writer == null) return;

            writer.WritePropertyName(name);
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(vector.x);
            writer.WritePropertyName("y");
            writer.WriteValue(vector.y);
            writer.WritePropertyName("z");
            writer.WriteValue(vector.z);
            writer.WriteEndObject();
        }

        public static void WriteQuaternion(JsonWriter writer, string name, Quaternion quaternion)
        {
            if (writer == null) return;

            writer.WritePropertyName(name);
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(quaternion.x);
            writer.WritePropertyName("y");
            writer.WriteValue(quaternion.y);
            writer.WritePropertyName("z");
            writer.WriteValue(quaternion.z);
            writer.WritePropertyName("w");
            writer.WriteValue(quaternion.w);
            writer.WriteEndObject();
        }

        private static void SaveGameInfo(JsonWriter writer)
        {
            LevelLoader levelLoader = (LevelLoader)GameManager.FindObjectOfType(typeof(LevelLoader));
            if (writer == null || levelLoader == null) return;

            writer.WritePropertyName("GameInfo");
            writer.WriteStartObject();

            WriteFloat(writer, "PlayersNumber", levelLoader.playersNumber);
            WriteString(writer, "Seed", levelLoader.seed);

            writer.WriteEndObject();
        }

        private static void SaveLighting(JsonWriter writer)
        {
            Sun sun = (Sun)GameObject.FindObjectOfType(typeof(Sun));
            if (writer == null || sun == null) return;

            writer.WritePropertyName("Sun");
            writer.WriteStartObject();

            WriteVector(writer, "Position", sun.transform.position);
            WriteQuaternion(writer, "Rotation", sun.transform.rotation);
            WriteVector(writer, "Scale", sun.transform.localScale);

            writer.WriteEndObject();
        }

        private static void SaveTerrain(JsonWriter writer)
        {
            //needs to be adapted for terrain once if that gets implemented
            Ground ground = (Ground)GameObject.FindObjectOfType(typeof(Ground));
            if (writer == null || ground == null) return;

            writer.WritePropertyName("Ground");
            writer.WriteStartObject();

            WriteVector(writer, "Position", ground.transform.position);
            WriteQuaternion(writer, "Rotation", ground.transform.rotation);
            WriteVector(writer, "Scale", ground.transform.localScale);
            TerrainData terrainData = ground.GetComponentInChildren<Terrain>().terrainData;
            WriteVector(writer, "Size", terrainData.size);
            WriteFloat(writer, "Resolution", terrainData.alphamapResolution);
            writer.WritePropertyName("HeightMap");
            writer.WriteStartArray();

            float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

            for(int i = 0; i < terrainData.heightmapResolution; ++i)
            {
                for (int j = 0; j < terrainData.heightmapResolution; ++j)
                {
                    writer.WriteValue(heightMap[i, j]);
                }
            }

            writer.WriteEndArray();

            writer.WritePropertyName("AlphaMap");
            writer.WriteStartArray();
            float[,,] alphamap = terrainData.GetAlphamaps(0, 0, terrainData.alphamapResolution, terrainData.alphamapResolution);
            for (int i = 0; i < terrainData.alphamapResolution; ++i)
            {
                for (int j = 0; j < terrainData.alphamapResolution; ++j)
                {
                    writer.WriteValue(alphamap[i, j, 0]);
                    writer.WriteValue(alphamap[i, j, 1]);
                    writer.WriteValue(alphamap[i, j, 2]);
                }
            }

            writer.WriteEndArray();


            writer.WriteEndObject();
        }

        private static void SaveBorders(JsonWriter writer)
        {
            LevelLoader levelLoader = (LevelLoader)GameObject.FindObjectOfType(typeof(LevelLoader));
            List<Vector3>[] borders = levelLoader.GetAllBorders();
            writer.WritePropertyName("Borders");
            writer.WriteStartObject();
            WriteFloat(writer, "Size", borders.Length);
            writer.WritePropertyName("Array");
            writer.WriteStartArray();
            for (int i = 0; i < borders.Length; ++i)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Territory");
                writer.WriteStartArray();
                for (int j = 0; j < borders[i].Count; ++j)
                {
                    writer.WriteStartObject();
                    WriteVector(writer, j.ToString(), borders[i][j]);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        private static void SaveCamera(JsonWriter writer)
        {
            if (writer == null) return;

            writer.WritePropertyName("Camera");
            writer.WriteStartObject();

            Transform cameraTransform = Camera.main.transform.root;
            WriteVector(writer, "Position", cameraTransform.position);
            WriteQuaternion(writer, "Rotation", cameraTransform.rotation);
            WriteVector(writer, "Scale", cameraTransform.localScale);

            writer.WriteEndObject();
        }

        private static void SaveResources(JsonWriter writer)
        {
            Resource[] resources = GameObject.FindObjectsOfType(typeof(Resource)) as Resource[];
            if (writer == null || resources == null) return;

            writer.WritePropertyName("Resources");
            writer.WriteStartArray();

            foreach (Resource resource in resources)
            {
                SaveWorldObject(writer, resource);
            }

            writer.WriteEndArray();
        }

        public static void SaveWorldObject(JsonWriter writer, WorldObjects worldObject)
        {
            if (writer == null || worldObject == null) return;

            writer.WriteStartObject();
            worldObject.SaveDetails(writer);
            writer.WriteEndObject();
            if (worldObject.GetComponent<CargoShip>() != null)
            {
                List<Unit> loadedUnits = worldObject.GetComponent<CargoShip>().GetLoadedUnits();
                foreach (WorldObjects unit in loadedUnits)
                {
                    writer.WriteStartObject();
                    unit.SaveDetails(writer);
                    writer.WriteEndObject();
                }
            }
        }

        private static void SavePlayers(JsonWriter writer)
        {
            Player[] players = GameObject.FindObjectsOfType(typeof(Player)) as Player[];
            if (writer == null || players == null) return;

            writer.WritePropertyName("Players");
            writer.WriteStartArray();

            foreach (Player player in players)
            {
                writer.WriteStartObject();
                player.SaveDetails(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }


        public static void WriteString(JsonWriter writer, string name, string entry)
        {
            if (writer == null) return;

            writer.WritePropertyName(name);
            //make sure no bracketed values get stored (e.g. Tank(Clone) becomes Tank)
            if (entry.Contains("(")) writer.WriteValue(entry.Substring(0, entry.IndexOf("(")));
            else writer.WriteValue(entry);
        }

        public static void WriteInt(JsonWriter writer, string name, int amount)
        {
            if (writer == null) return;
            writer.WritePropertyName(name);
            writer.WriteValue(amount);
        }

        public static void WriteFloat(JsonWriter writer, string name, float amount)
        {
            if (writer == null) return;

            writer.WritePropertyName(name);
            writer.WriteValue(amount);
        }

        public static void WriteBoolean(JsonWriter writer, string name, bool state)
        {
            if (writer == null) return;

            writer.WritePropertyName(name);
            writer.WriteValue(state);
        }

        public static void WriteColor(JsonWriter writer, string name, Color color)
        {
            if (writer == null) return;

            writer.WritePropertyName(name);
            writer.WriteStartObject();
            writer.WritePropertyName("r");
            writer.WriteValue(color.r);
            writer.WritePropertyName("g");
            writer.WriteValue(color.g);
            writer.WritePropertyName("b");
            writer.WriteValue(color.b);
            writer.WritePropertyName("a");
            writer.WriteValue(color.a);
            writer.WriteEndObject();
        }

        public static void SavePlayerUpgrades(JsonWriter writer, Dictionary<UpgradeableObjects, int> levels)
        {
            if (writer == null) return;

            writer.WritePropertyName("Levels");
            writer.WriteStartArray();
            foreach(KeyValuePair<UpgradeableObjects, int> pair in levels)
            {
                writer.WriteStartObject();
                WriteInt(writer, pair.Key.ToString(), pair.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        public static void SavePlayerResources(JsonWriter writer, Dictionary<ResourceType, int> resources, Dictionary<ResourceType, int> resourceLimits)
        {
            if (writer == null) return;

            writer.WritePropertyName("Resources");
            writer.WriteStartArray();
            foreach (KeyValuePair<ResourceType, int> pair in resources)
            {
                writer.WriteStartObject();
                WriteInt(writer, pair.Key.ToString(), pair.Value);
                writer.WriteEndObject();
            }
            foreach (KeyValuePair<ResourceType, int> pair in resourceLimits)
            {
                writer.WriteStartObject();
                WriteInt(writer, pair.Key.ToString() + "_Limit", pair.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        public static void SavePlayerBuildings(JsonWriter writer, Building[] buildings)
        {
            if (writer == null) return;

            writer.WritePropertyName("Buildings");
            writer.WriteStartArray();
            foreach (Building building in buildings)
            {
                SaveWorldObject(writer, building);
            }
            writer.WriteEndArray();
        }

        public static void SavePlayerUnits(JsonWriter writer, Unit[] units)
        {
            if (writer == null) return;

            writer.WritePropertyName("Units");
            writer.WriteStartArray();
            foreach (Unit unit in units)
            {
                SaveWorldObject(writer, unit);
            }
            writer.WriteEndArray();
        }

        public static void WriteStringArray(JsonWriter writer, string name, string[] values)
        {
            if (writer == null) return;

            writer.WritePropertyName(name);
            writer.WriteStartArray();
            foreach (string v in values)
            {
                writer.WriteValue(v);
            }
            writer.WriteEndArray();
        }

        public static void WriteRect(JsonWriter writer, string name, Rect rect)
        {
            if (writer == null) return;

            writer.WritePropertyName(name);
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(rect.x);
            writer.WritePropertyName("y");
            writer.WriteValue(rect.y);
            writer.WritePropertyName("width");
            writer.WriteValue(rect.width);
            writer.WritePropertyName("height");
            writer.WriteValue(rect.height);
            writer.WriteEndObject();
        }
    }
}
