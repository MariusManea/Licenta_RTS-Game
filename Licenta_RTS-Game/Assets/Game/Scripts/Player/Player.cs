﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using Pathfinding;
using Newtonsoft.Json;
using System.Linq;

public class Player : MonoBehaviour
{
    public TownCenter townCenter;
    // Resources
    public int startSpacing, startSpacingLimit, startCopper, startCopperLimit, startIron, startIronLimit, startOil, startOilLimit, startGold, startGoldLimit;
    private Dictionary<ResourceType, int> resources, resourceLimits;

    public string userName;
    public bool isHuman;
    public int playerID;

    public HUD hud;

    public Material notAllowedMaterial, allowedMaterial;

    private Building tempBuilding;
    private Unit tempCreator;
    private bool findingPlacement = false;

    public Color teamColor;

    public List<WorldObjects> SelectedObjects { get; set; }
    public bool centerToBase;
    private GameManager gameManager;
    private LevelLoader levelLoader;

    private bool defeat = false;

    void Awake()
    {
        resources = InitResourceList();
        resourceLimits = InitResourceList();
        AddStartResourceLimits();
        AddStartResources();
    }

    // Start is called before the first frame update
    void Start()
    {
        hud = GetComponentInChildren<HUD>();
        gameManager = (GameManager)FindObjectOfType(typeof(GameManager));
        levelLoader = (LevelLoader)FindObjectOfType(typeof(LevelLoader));

        
    }

    // Update is called once per frame
    void Update()
    {
        if (isHuman)
        {
            hud.SetResourceValues(resources, resourceLimits);
        }
        if (findingPlacement)
        {
            tempBuilding.CalculateBounds();
            if (CanPlaceBuilding()) tempBuilding.SetTransparentMaterial(allowedMaterial, false, true);
            else tempBuilding.SetTransparentMaterial(notAllowedMaterial, false);
        }
    }

    private Dictionary<ResourceType, int> InitResourceList()
    {
        Dictionary<ResourceType, int> list = new Dictionary<ResourceType, int>();
        list.Add(ResourceType.Spacing, 0);
        list.Add(ResourceType.Copper, 0);
        list.Add(ResourceType.Iron, 0);
        list.Add(ResourceType.Oil, 0);
        list.Add(ResourceType.Gold, 0);
        return list;
    }

    private void AddStartResourceLimits()
    {
        IncrementResourceLimit(ResourceType.Spacing, startSpacingLimit);
        IncrementResourceLimit(ResourceType.Copper, startCopperLimit);
        IncrementResourceLimit(ResourceType.Iron, startIronLimit);
        IncrementResourceLimit(ResourceType.Oil, startOilLimit);
        IncrementResourceLimit(ResourceType.Gold, startGoldLimit);
    }

    private void AddStartResources()
    {
        AddResource(ResourceType.Spacing, startSpacing);
        AddResource(ResourceType.Copper, startCopper);
        AddResource(ResourceType.Iron, startIron);
        AddResource(ResourceType.Oil, startOil);
        AddResource(ResourceType.Gold, startGold);
    }

    public void AddResource(ResourceType type, int amount)
    {
        resources[type] += amount;
        if (resources[type] > resourceLimits[type])
        {
            resources[type] = resourceLimits[type];
        }
    }

    public bool IsFull(ResourceType type)
    {
        return resources[type] >= resourceLimits[type];
    }

    public void IncrementResourceLimit(ResourceType type, int amount)
    {
        resourceLimits[type] += amount;
    }

    public void AddUnit(string unitName, Vector3 spawnPoint, Vector3 rallyPoint, Quaternion rotation, Building creator)
    {
        Units units = GetComponentInChildren<Units>();
        GameObject newUnit;
        try
        {
            newUnit = (GameObject)Instantiate(ResourceManager.GetUnit(unitName), spawnPoint, rotation);
        }
        catch
        {
            return;
        }
        newUnit.GetComponent<WorldObjects>().SetPlayer();
        newUnit.transform.parent = units.transform;
        Unit unitObject = newUnit.GetComponent<Unit>();
        if (unitObject && spawnPoint != rallyPoint) unitObject.StartMove(rallyPoint);

        if (unitObject)
        {
            unitObject.SetBuilding(creator);
            unitObject.ObjectId = ResourceManager.GetNewObjectId();
            if (spawnPoint != rallyPoint) unitObject.StartMove(rallyPoint);
        }
    }

    public void CreateBuilding(string buildingName, Vector3 buildPoint, Unit creator, Rect playingArea)
    {
        GameObject newBuilding = (GameObject)Instantiate(ResourceManager.GetBuilding(buildingName), buildPoint, new Quaternion());
        if (IsFindingBuildingLocation() && tempBuilding)
        {
            Destroy(tempBuilding.gameObject);
            tempBuilding = null;

        }
        tempBuilding = newBuilding.GetComponent<Building>();
        if (tempBuilding)
        {
            tempBuilding.ObjectId = ResourceManager.GetNewObjectId();
            tempBuilding.hitPoints = 0;
            tempBuilding.Ghost = true;
            tempCreator = creator;
            findingPlacement = true;
            tempBuilding.SetTransparentMaterial(notAllowedMaterial, true);
            tempBuilding.SetColliders(false);
            tempBuilding.SetPlayingArea(playingArea);
        }
        else Destroy(newBuilding);
    }

    public bool IsFindingBuildingLocation()
    {
        return findingPlacement;
    }

    public void FindBuildingLocation()
    {
        Vector3 newLocation = WorkManager.FindHitPoint(Input.mousePosition);
        newLocation.y = 0;
        tempBuilding.transform.position = newLocation;
    }

    public bool CanPlaceBuilding()
    {
        if (tempBuilding.transform.position.y < 7)
        {
            return false;
        }
        tempBuilding.CalculateBounds();
        if (tempBuilding.GetComponent<OilPump>())
        {
            //shorthand for the coordinates of the center of the selection bounds
            float pcx = tempBuilding.transform.position.x;
            float pcy = tempBuilding.transform.position.y;
            float pcz = tempBuilding.transform.position.z;
            //shorthand for the coordinates of the extents of the selection box
            float pex = tempBuilding.transform.localScale.x / 2;
            float pez = tempBuilding.transform.localScale.z / 2;

            Vector3 point1 = Camera.main.WorldToScreenPoint(new Vector3(pcx + 0.5f * pex, pcy, pcz + 0.5f * pez));
            Vector3 point2 = Camera.main.WorldToScreenPoint(new Vector3(pcx - 0.5f * pex, pcy, pcz - 0.5f * pez));

            GameObject hit1 = WorkManager.FindHitObject(point1);
            GameObject hit2 = WorkManager.FindHitObject(point2);

            if (hit1.GetComponent<OilPile>() && hit2.GetComponent<OilPile>()) return true;
            else return false;
        }

        if (tempBuilding.GetComponent<CityHall>())
        {
            int count = gameManager.GetNumberOfTerritories();
            List<Vector3>[] borders = levelLoader.GetAllBorders();
            for (int i = 0; i < count; ++i)
            {
                if (Poly.ContainsPoint(borders[i], tempBuilding.transform.position))
                {
                    int owner = gameManager.GetOwner(i);
                    if (owner != -1)
                    {
                        return false;
                    } 
                    else
                    {
                        tempBuilding.GetComponent<CityHall>().SetStats(playerID, i);
                    }
                    break;
                }
            }
        }
        


        Bounds placeBounds = tempBuilding.GetSelectionBounds();
        //shorthand for the coordinates of the center of the selection bounds
        float cx = placeBounds.center.x;
        float cy = placeBounds.center.y;
        float cz = placeBounds.center.z;
        //shorthand for the coordinates of the extents of the selection box
        float ex = placeBounds.extents.x;
        float ey = placeBounds.extents.y;
        float ez = placeBounds.extents.z;

        //Determine the screen coordinates for the corners of the selection bounds
        List<Vector3> corners = new List<Vector3>();
        float[] cfs = { 0.05f, 0.33f, 0.66f, 1};
        Terrain terrain = (Terrain)FindObjectOfType(typeof(Terrain));
        List<float> heights = new List<float>();
        foreach(float cfx in cfs)
        {
            foreach(float cfz in cfs)
            {
                foreach(float cfy in cfs)
                {
                    corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + cfx * ex, cy + cfy * ey, cz + cfz * ez)));
                    corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + cfx * ex, cy + cfy * ey, cz - cfz * ez)));
                    corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + cfx * ex, cy - cfy * ey, cz + cfz * ez)));
                    corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - cfx * ex, cy + cfy * ey, cz + cfz * ez)));
                    corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + cfx * ex, cy - cfy * ey, cz - cfz * ez)));
                    corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - cfx * ex, cy - cfy * ey, cz + cfz * ez)));
                    corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - cfx * ex, cy + cfy * ey, cz - cfz * ez)));
                    corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - cfx * ex, cy - cfy * ey, cz - cfz * ez)));
                }
                heights.Add(terrain.SampleHeight(new Vector3(cx + cfx * ex, 0, cz + cfz * ez)));
                heights.Add(terrain.SampleHeight(new Vector3(cx + cfx * ex, 0, cz - cfz * ez)));
                heights.Add(terrain.SampleHeight(new Vector3(cx - cfx * ex, 0, cz + cfz * ez)));
                heights.Add(terrain.SampleHeight(new Vector3(cx - cfx * ex, 0, cz - cfz * ez)));
            }
        }
        if (!tempBuilding.GetComponent<Dock>())
        {
            if (heights.Max() - heights.Min() > (tempBuilding.GetComponent<Dock>() != null ? 2 : 1.5f)) return false;
        }
        else { if (tempBuilding.transform.position.y >= 8) return false; }
        bool dockOnWater = false;
        bool dockOnGround = false;
        foreach (Vector3 corner in corners)
        {
            GameObject hitObject = WorkManager.FindHitObject(corner);
            if (tempBuilding.GetComponent<Dock>())
            {
                if (WorkManager.ObjectIsGround(hitObject)) dockOnGround = true;
                else if (WorkManager.ObjectIsWater(hitObject)) dockOnWater = true;
            }
            if (hitObject && !WorkManager.ObjectIsGround(hitObject))
            {
                WorldObjects worldObject = hitObject.transform.parent.GetComponent<WorldObjects>();
                if (worldObject && placeBounds.Intersects(worldObject.GetSelectionBounds())) return false;
            }
        }
        if (tempBuilding.GetComponent<Dock>())
        {
            return dockOnWater && dockOnGround;
        }
        return true;
    }
    public void StartConstruction()
    {
        findingPlacement = false;
        Buildings buildings = GetComponentInChildren<Buildings>();
        if (buildings) tempBuilding.transform.parent = buildings.transform;
        tempBuilding.Ghost = false;
        tempBuilding.SetPlayer();
        tempBuilding.SetColliders(true);
        tempBuilding.GetComponentInChildren<DynamicGridObstacle>().DoUpdateGraphs();
        AstarPath.active.FlushGraphUpdates();
        tempCreator.SetBuilding(tempBuilding);
        foreach(WorldObjects selectedWorldObject in SelectedObjects)
        {
            if (selectedWorldObject.GetType() == typeof(Worker))
            {
                ((Worker)selectedWorldObject).SetBuilding(tempBuilding);
            }
        }
        tempBuilding.StartConstruction();

        ResourceManager.Cost cost = ResourceManager.GetCost(tempBuilding.GetObjectName());
        AddResource(ResourceType.Spacing, cost.spacing);
        RemoveResource(ResourceType.Copper, cost.copper);
        RemoveResource(ResourceType.Iron, cost.iron);
        RemoveResource(ResourceType.Oil, cost.oil);
        RemoveResource(ResourceType.Gold, cost.gold);
    }

    public void CancelBuildingPlacement()
    {
        findingPlacement = false;
        Destroy(tempBuilding.gameObject);
        tempBuilding = null;
        tempCreator = null;
    }

    public virtual void SaveDetails(JsonWriter writer)
    {
        SaveManager.WriteString(writer, "Username", userName);
        SaveManager.WriteBoolean(writer, "Human", isHuman);
        SaveManager.WriteFloat(writer, "PlayerID", playerID);
        SaveManager.WriteColor(writer, "TeamColor", teamColor);
        SaveManager.SavePlayerResources(writer, resources, resourceLimits);
        SaveManager.SavePlayerBuildings(writer, GetComponentsInChildren<Building>());
        SaveManager.SavePlayerUnits(writer, GetComponentsInChildren<Unit>());
    }

    public WorldObjects GetObjectForId(int id)
    {
        WorldObjects[] objects = GameObject.FindObjectsOfType(typeof(WorldObjects)) as WorldObjects[];
        foreach (WorldObjects obj in objects)
        {
            if (obj.ObjectId == id) return obj;
        }
        return null;
    }

    public void LoadDetails(JsonTextReader reader)
    {
        if (reader == null) return;
        string currValue = "";
        while (reader.Read())
        {
            if (reader.Value != null)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    currValue = (string)reader.Value;
                }
                else
                {
                    switch (currValue)
                    {
                        case "Username": userName = (string)reader.Value; gameObject.name = userName; break;
                        case "Human": isHuman = (bool)reader.Value; break;
                        case "PlayerID": playerID = (int)(double)reader.Value; break;
                        default: break;
                    }
                }
            }
            else if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.StartArray)
            {
                switch (currValue)
                {
                    case "TeamColor": teamColor = LoadManager.LoadColor(reader); break;
                    case "Resources": LoadResources(reader); break;
                    case "Buildings": LoadBuildings(reader); break;
                    case "Units": LoadUnits(reader); break;
                    default: break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject) return;
        }
    }

    private void LoadResources(JsonTextReader reader)
    {
        if (reader == null) return;
        string currValue = "";
        while (reader.Read())
        {
            if (reader.Value != null)
            {
                if (reader.TokenType == JsonToken.PropertyName) currValue = (string)reader.Value;
                else
                {
                    switch (currValue)
                    {
                        case "Spacing": startSpacing = (int)(System.Int64)reader.Value; break;
                        case "Spacing_Limit": startSpacingLimit = (int)(System.Int64)reader.Value; break;
                        case "Copper": startCopper = (int)(System.Int64)reader.Value; break;
                        case "Copper_Limit": startCopperLimit = (int)(System.Int64)reader.Value; break;
                        case "Iron": startIron = (int)(System.Int64)reader.Value; break;
                        case "Iron_Limit": startIronLimit = (int)(System.Int64)reader.Value; break;
                        case "Oil": startOil = (int)(System.Int64)reader.Value; break;
                        case "Oil_Limit": startOilLimit = (int)(System.Int64)reader.Value; break;
                        case "Gold": startGold = (int)(System.Int64)reader.Value; break;
                        case "Gold_Limit": startGoldLimit = (int)(System.Int64)reader.Value; break;
                        default: break;
                    }
                }
            }
            else if (reader.TokenType == JsonToken.EndArray)
            {
                return;
            }
        }
    }
    private void LoadBuildings(JsonTextReader reader)
    {
        if (reader == null) return;
        Buildings buildings = GetComponentInChildren<Buildings>();
        string currValue = "", type = "";
        while (reader.Read())
        {
            if (reader.Value != null)
            {
                if (reader.TokenType == JsonToken.PropertyName) currValue = (string)reader.Value;
                else if (currValue == "Type")
                {
                    type = (string)reader.Value;
                    GameObject newObject = (GameObject)GameObject.Instantiate(ResourceManager.GetBuilding(type));
                    if (type == "TownCenter")
                    {
                        townCenter = newObject.GetComponent<TownCenter>();
                    }
                    Building building = newObject.GetComponent<Building>();
                    building.LoadDetails(reader);
                    building.transform.parent = buildings.transform;
                    building.SetPlayer();
                    building.SetTeamColor();
                    if (building.UnderConstruction())
                    {
                        building.SetTransparentMaterial(allowedMaterial, true);
                    }
                }
            }
            else if (reader.TokenType == JsonToken.EndArray) return;
        }
    }
    private void LoadUnits(JsonTextReader reader)
    {
        if (reader == null) return;
        Units units = GetComponentInChildren<Units>();
        string currValue = "", type = "";
        while (reader.Read())
        {
            if (reader.Value != null)
            {
                if (reader.TokenType == JsonToken.PropertyName) currValue = (string)reader.Value;
                else if (currValue == "Type")
                {
                    type = (string)reader.Value;
                    GameObject newObject = (GameObject)GameObject.Instantiate(ResourceManager.GetUnit(type));
                    Unit unit = newObject.GetComponent<Unit>();
                    unit.transform.parent = units.transform;
                    unit.SetPlayer();
                    unit.SetTeamColor();
                    unit.LoadDetails(reader);
                }
            }
            else if (reader.TokenType == JsonToken.EndArray) return;
        }
    }

    public bool IsDead()
    {
        Building[] buildings = GetComponentsInChildren<Building>();
        Unit[] units = GetComponentsInChildren<Unit>();
        if (buildings != null && buildings.Length > 0) return false;
        if (units != null && units.Length > 0) return false;
        return true;
    }
    public int GetResourceAmount(ResourceType type)
    {
        if (type == ResourceType.Spacing) return resourceLimits[type] - resources[type];
        return resources[type];
    }
    public void RemoveResource(ResourceType type, int amount)
    {
        resources[type] -= amount;
    }

    public ResourceManager.Cost AvailableResources()
    {
        return new ResourceManager.Cost(resourceLimits[ResourceType.Spacing] - resources[ResourceType.Spacing], resources[ResourceType.Copper],
            resources[ResourceType.Iron], resources[ResourceType.Oil], resources[ResourceType.Gold]);
    }

    public void GameLost()
    {
        defeat = true;
    }
}
