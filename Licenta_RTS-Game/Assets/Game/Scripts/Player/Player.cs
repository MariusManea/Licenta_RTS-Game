using System.Collections;
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
    public int startSpacing, startSpacingLimit, 
        startCopper, startCopperLimit, 
        startIron, startIronLimit, 
        startOil, startOilLimit, 
        startGold, startGoldLimit,
        startResearchPoint, startResearchPointLimit;
    private Dictionary<UpgradeableObjects, int> levels;
    private List<string> researchableObjects;
    public int universityLevel, warFactoryLevel, refineryLevel, turretLevel, oilPumpLevel, dockLevel,
        workerLevel, harvesterLevel, tankLevel, cargoShipLevel, wonderLevel, convoyTruckLevel, cityHallLevel;
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

    private GameObject oilPileHitted;

    private bool defeat = false;
    private bool defeatEffect = false;


    private string upgradedObject;
    private float universalResearchTime;

    void Awake()
    {
        resources = InitResourceList();
        resourceLimits = InitResourceList();
        universalResearchTime = 0.0f;
        upgradedObject = "";
    }

    // Start is called before the first frame update
    void Start()
    {
        AddStartResourceLimits();
        AddStartResources();
        AddStartLevels();
        hud = GetComponentInChildren<HUD>();
        gameManager = (GameManager)FindObjectOfType(typeof(GameManager));
        levelLoader = (LevelLoader)FindObjectOfType(typeof(LevelLoader));

        
    }

    // Update is called once per frame
    void Update()
    {
        if (defeat)
        {
            if (!defeatEffect)
            {
                GameObject units = GetComponentInChildren<Units>().gameObject;
                GameObject buildings = GetComponentInChildren<Buildings>().gameObject;
                if (units) Destroy(units);
                if (buildings) Destroy(buildings);
                defeatEffect = true;
            }
            return;
        }

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
        list.Add(ResourceType.ResearchPoint, 0);
        return list;
    }

    private void AddStartResourceLimits()
    {
        IncrementResourceLimit(ResourceType.Spacing, startSpacingLimit);
        IncrementResourceLimit(ResourceType.Copper, startCopperLimit);
        IncrementResourceLimit(ResourceType.Iron, startIronLimit);
        IncrementResourceLimit(ResourceType.Oil, startOilLimit);
        IncrementResourceLimit(ResourceType.Gold, startGoldLimit);
        IncrementResourceLimit(ResourceType.ResearchPoint, startResearchPointLimit);
    }

    private void AddStartResources()
    {
        AddResource(ResourceType.Spacing, startSpacing);
        AddResource(ResourceType.Copper, startCopper);
        AddResource(ResourceType.Iron, startIron);
        AddResource(ResourceType.Oil, startOil);
        AddResource(ResourceType.Gold, startGold);
        AddResource(ResourceType.ResearchPoint, startResearchPoint);
    }

    private void AddStartLevels()
    {
        levels = new Dictionary<UpgradeableObjects, int>();
        researchableObjects = new List<string>();
        levels.Add(UpgradeableObjects.CargoShip, cargoShipLevel);
        levels.Add(UpgradeableObjects.ConvoyTruck, convoyTruckLevel);
        levels.Add(UpgradeableObjects.Dock, dockLevel);
        levels.Add(UpgradeableObjects.Harvester, harvesterLevel);
        levels.Add(UpgradeableObjects.OilPump, oilPumpLevel);
        levels.Add(UpgradeableObjects.Refinery, refineryLevel);
        levels.Add(UpgradeableObjects.Tank, tankLevel);
        levels.Add(UpgradeableObjects.CityHall, cityHallLevel);
        levels.Add(UpgradeableObjects.Turret, turretLevel);
        levels.Add(UpgradeableObjects.University, universityLevel);
        levels.Add(UpgradeableObjects.WarFactory, warFactoryLevel);
        levels.Add(UpgradeableObjects.Wonder, wonderLevel);
        levels.Add(UpgradeableObjects.Worker, workerLevel);

        foreach (UpgradeableObjects type in System.Enum.GetValues(typeof(UpgradeableObjects)))
        {
            if (WorkManager.ResearchableObject(type, levels[type] + 1, levels))
            {
                researchableObjects.Add(type.ToString());
            }
        }
    }

    public List<string> GetResearchableObjects()
    {
        return researchableObjects;
    }

    public void UpgradeObject(UpgradeableObjects type)
    {
        if (levels.ContainsKey(type))
            levels[type]++;
        researchableObjects = new List<string>();

        foreach (UpgradeableObjects rsObj in System.Enum.GetValues(typeof(UpgradeableObjects)))
        {
            if (WorkManager.ResearchableObject(rsObj, levels[rsObj] + 1, levels))
            {
                researchableObjects.Add(rsObj.ToString());
            }
        }
        /*switch (type)
        {
            case UpgradeableObjects.University: universityLevel++; break;
            case UpgradeableObjects.WarFactory: warFactoryLevel++; break;
            case UpgradeableObjects.Refinery: refineryLevel++; break;
            case UpgradeableObjects.Turret: turretLevel++; break;
            case UpgradeableObjects.OilPump: oilPumpLevel++; break;
            case UpgradeableObjects.Dock: dockLevel++; break;
            case UpgradeableObjects.Worker: workerLevel++; break;
            case UpgradeableObjects.Tank: tankLevel++; break;
            case UpgradeableObjects.Harvester: harvesterLevel++; break;
            case UpgradeableObjects.CargoShip: cargoShipLevel++; break;
            case UpgradeableObjects.Wonder: wonderLevel++; break;
            case UpgradeableObjects.ConvoyTruck: convoyTruckLevel++; break;
            case UpgradeableObjects.TownHall: townHallLevel++; break;
            default: break;
        }*/
    }

    public bool MaxUpgrade(UpgradeableObjects type)
    {
        switch (type)
        {
            case UpgradeableObjects.University: /*return universityLevel == 5;*/
            case UpgradeableObjects.WarFactory: /*return warFactoryLevel == 5;*/
            case UpgradeableObjects.Refinery: /*return refineryLevel == 5;*/
            case UpgradeableObjects.Turret: /*return turretLevel == 5;*/
            case UpgradeableObjects.OilPump: /*return oilPumpLevel == 5;*/
            case UpgradeableObjects.Dock: /*return dockLevel == 5;*/
            case UpgradeableObjects.CityHall: /*return townHallLevel == 5;*/ return levels[type] == 5;
            case UpgradeableObjects.Worker: /*return workerLevel == 10;*/
            case UpgradeableObjects.Tank: /*return tankLevel == 10;*/
            case UpgradeableObjects.Harvester: /*return harvesterLevel == 10;*/
            case UpgradeableObjects.CargoShip: /*return cargoShipLevel == 10;*/ return levels[type] == 10;
            case UpgradeableObjects.ConvoyTruck: /*return convoyTruckLevel == 1;*/
            case UpgradeableObjects.Wonder: /*return wonderLevel == 1;*/ return levels[type] == 1;
            default: return true;
        }
    }
    
    public Dictionary<UpgradeableObjects, int> GetLevels()
    {
        return levels;
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
        Terrain terrain = (Terrain)FindObjectOfType(typeof(Terrain));

        if (terrain.SampleHeight(tempBuilding.transform.position) < 7)
        {
            return false;
        }
        tempBuilding.CalculateBounds();
        oilPileHitted = null;
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
            if (hit1 && hit2)
            {
                if (hit1.GetComponent<OilPile>() && hit2.GetComponent<OilPile>())
                {
                    oilPileHitted = hit1.transform.parent.gameObject;
                    return true;
                }
                else return false;
            }
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
        float[] cfsy = { 1, 0.85f };
        List<float> heights = new List<float>();
        foreach(float cfx in cfs)
        {
            foreach(float cfz in cfs)
            {
                foreach(float cfy in cfsy)
                {
                    //corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + cfx * ex, cy + cfy * ey, cz + cfz * ez)));
                    //corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + cfx * ex, cy + cfy * ey, cz - cfz * ez)));
                    corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + cfx * ex, cy - cfy * ey, cz + cfz * ez)));
                    //corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - cfx * ex, cy + cfy * ey, cz + cfz * ez)));
                    corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + cfx * ex, cy - cfy * ey, cz - cfz * ez)));
                    corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - cfx * ex, cy - cfy * ey, cz + cfz * ez)));
                    //corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - cfx * ex, cy + cfy * ey, cz - cfz * ez)));
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
        if (oilPileHitted)
        {
            foreach (Renderer renderer in oilPileHitted.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.enabled = false;
            }
            foreach (Collider collider in oilPileHitted.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
            tempBuilding.GetComponent<OilPump>().SetPile(oilPileHitted);
            oilPileHitted = null;
        }
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
        SaveManager.WriteBoolean(writer, "Defeat", defeat);
        SaveManager.WriteBoolean(writer, "DefeatEffect", defeatEffect);
        SaveManager.WriteFloat(writer, "UniversalResearchTime", universalResearchTime);
        SaveManager.WriteString(writer, "UpgradedObject", upgradedObject);
        SaveManager.SavePlayerUpgrades(writer, levels);
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
                        case "Defeat": defeat = (bool)reader.Value; break;
                        case "DefeatEffect": defeatEffect = (bool)reader.Value; break;
                        case "UniversalResearchTime": universalResearchTime = (float)(double)reader.Value; break;
                        case "UpgradedObject": upgradedObject = (string)reader.Value; break;
                        default: break;
                    }
                }
            }
            else if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.StartArray)
            {
                switch (currValue)
                {
                    case "TeamColor": teamColor = LoadManager.LoadColor(reader); break;
                    case "Levels": LoadLevels(reader); break;
                    case "Resources": LoadResources(reader); break;
                    case "Buildings": LoadBuildings(reader); break;
                    case "Units": LoadUnits(reader); break;
                    default: break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject) return;
        }
    }

    private void LoadLevels(JsonTextReader reader)
    {
        if (reader == null) return;
        string currValue = "";
        while(reader.Read())
        {
            if (reader.Value != null)
            {
                if (reader.TokenType == JsonToken.PropertyName) currValue = (string)reader.Value;
                else
                {
                    switch (currValue)
                    {
                        case "CityHall": cityHallLevel = (int)(System.Int64)reader.Value; break;
                        case "University": universityLevel = (int)(System.Int64)reader.Value; break;
                        case "WarFactory": warFactoryLevel = (int)(System.Int64)reader.Value; break;
                        case "Refinery": refineryLevel = (int)(System.Int64)reader.Value; break;
                        case "Worker": workerLevel = (int)(System.Int64)reader.Value; break;
                        case "Harvester": harvesterLevel = (int)(System.Int64)reader.Value; break;
                        case "OilPump": oilPumpLevel = (int)(System.Int64)reader.Value; break;
                        case "Tank": tankLevel = (int)(System.Int64)reader.Value; break;
                        case "Dock": dockLevel = (int)(System.Int64)reader.Value; break;
                        case "CargoShip": cargoShipLevel = (int)(System.Int64)reader.Value; break;
                        case "Turret": turretLevel = (int)(System.Int64)reader.Value; break;
                        case "Wonder": wonderLevel = (int)(System.Int64)reader.Value; break;
                        case "ConvoyTruck": convoyTruckLevel = (int)(System.Int64)reader.Value; break;
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
                        case "ResearchPoint": startResearchPoint = (int)(System.Int64)reader.Value; break;
                        case "ResearchPoint_Limit": startResearchPointLimit = (int)(System.Int64)reader.Value; break;
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

    public string GetUpgradedObject()
    {
        return upgradedObject;
    }
    public void SetUpgradedObject(string nextUpgrade)
    {
        upgradedObject = nextUpgrade;
    }

    public float GetUniversalResearchTime()
    {
        return universalResearchTime;
    }

    public void ResetUniversalResearchTime()
    {
        universalResearchTime = 0.0f;
    }

    public void IncreaseUniversalResearchTime(float delta)
    {
        universalResearchTime += delta;
    }
}
