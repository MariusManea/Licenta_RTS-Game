using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using RTS;
using Unity.MLAgents.Sensors;
using Pathfinding;

public class AgentRTS : Agent
{
    public Color team;
    public Entity entity;

    [HideInInspector]
    public Player owner;
    [HideInInspector]
    public Unit unitController;
    [HideInInspector]
    public Building buildingController;
    private bool moveable;
    BehaviorParameters m_BehaviorParameters;
    public Building nextProj;

    // Heuristic helpers
    public int myID;
    public static int globalID = 0;
    public static int currentID;
    private const string FORWARD = "forward";
    private const string BACKWARD = "backward";
    private const string IDLE_BUILDING = "idleBuilding";
    private const string IDLE_UNIT = "idleUnit";
    private const string ATTACKING_UNIT = "attackingUnit";
    private const string WORKER = "worker";
    private const string HARVESTER = "harvester";
    private const string CARGO = "cargo";
    private const string HALL = "hall";
    private const string DOCK = "dock";
    private const string REFINERY = "refinery";
    private const string WAR_FACTORY = "warFactory";
    private const string UNIVERSITY = "university";
    private Dictionary<string, int> branches = new Dictionary<string, int>
    {
        {FORWARD, 3 },
        {BACKWARD, 3 },
        {IDLE_BUILDING, 1 },
        {IDLE_UNIT, 2 },
        {ATTACKING_UNIT, 3 },
        {WORKER, 11 },
        {HARVESTER, 5 },
        {CARGO, 2},
        {HALL, 3},
        {DOCK, 3 },
        {REFINERY, 2 },
        {WAR_FACTORY, 4 },
        {UNIVERSITY, 16}
    };
    private bool ready = false;

    private AstarPath graph;
    private NavGraph waterGraph;
    private int canPlaceCount = 0;
    private float unableToPlaceTimer;

    public TrainSceneManager AITrainSceneManager;

    public void Awake()
    {
        AITrainSceneManager = FindObjectOfType<TrainSceneManager>();
    }

    public void Start()
    {
        myID = globalID++;
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (AITrainSceneManager)
        {
            team = AITrainSceneManager.teamColors[m_BehaviorParameters.TeamId];
        }
        else
        {
            team = FindObjectOfType<LevelLoader>().teamColors[m_BehaviorParameters.TeamId];
        }
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player.teamColor == team)
            {
                owner = player;
                break;
            }
        }
        unitController = gameObject.GetComponent<Unit>();
        buildingController = gameObject.GetComponent<Building>();

        if (unitController)
        {
            moveable = true;
        }
        else
        {
            moveable = false;
        }
        graph = FindObjectOfType<AstarPath>();
        if (!graph)
        {
            TrainSceneManager AISceneManager = GetComponentInParent<TrainSceneManager>();
            if (AISceneManager) graph = AISceneManager.graph;
        }
        waterGraph = AstarPath.active.graphs[1];
    }

    public override void Initialize()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (AITrainSceneManager)
        {
            team = AITrainSceneManager.teamColors[m_BehaviorParameters.TeamId];
        }
        else
        {
            team = FindObjectOfType<LevelLoader>().teamColors[m_BehaviorParameters.TeamId];
        }
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player.teamColor == team)
            {
                owner = player;
                break;
            }
        }
        unitController = gameObject.GetComponent<Unit>();
        buildingController = gameObject.GetComponent<Building>();

        if (unitController)
        {
            moveable = true;
        } 
        else
        {
            moveable = false;
        }
        graph = FindObjectOfType<AstarPath>();
        if (!graph)
        {
            TrainSceneManager AISceneManager = GetComponentInParent<TrainSceneManager>();
            if (AISceneManager) graph = AISceneManager.graph;
        }
        waterGraph = AstarPath.active.graphs[1];
    }

    public override void OnEpisodeBegin()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (AITrainSceneManager)
        {
            team = AITrainSceneManager.teamColors[m_BehaviorParameters.TeamId];
        }
        else
        {
            team = FindObjectOfType<LevelLoader>().teamColors[m_BehaviorParameters.TeamId];
        }
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player.teamColor == team)
            {
                owner = player;
                break;
            }
        }
        unitController = gameObject.GetComponent<Unit>();
        buildingController = gameObject.GetComponent<Building>();

        if (unitController)
        {
            moveable = true;
        }
        else
        {
            moveable = false;
        }
        graph = FindObjectOfType<AstarPath>();
        if (!graph)
        {
            TrainSceneManager AISceneManager = GetComponentInParent<TrainSceneManager>();
            if (AISceneManager) graph = AISceneManager.graph;
        }
        waterGraph = AstarPath.active.graphs[1];
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (moveable)
        {

            // Units
            sensor.AddObservation(unitController.hitPoints);
            List<WorldObjects> nearbyObjects = unitController.GetNearbyObjects();
            if (nearbyObjects == null) nearbyObjects = unitController.SetNearbyObjects();
            // #friends
            int nFriends = 1;
            // #enemies
            int nEnemies = 0;
            // distance to closest friend
            float friendDistance = int.MaxValue;
            // distance to closest enemy
            float enemyDistance = int.MaxValue;
            // distance to closest resource
            float resourceDistance = int.MaxValue;
            // distance to closest friendly Hall
            float hallDistance = int.MaxValue;
            // distance to closest Oil Pile
            float oilDistance = int.MaxValue;
            if (nearbyObjects != null)
            {
                foreach (WorldObjects worldObj in nearbyObjects)
                {
                    if (worldObj == null) continue;
                    float dist = Vector3.Distance(this.transform.position, worldObj.transform.position);
                    if (worldObj.IsOwnedBy(owner))
                    {
                        nFriends++;
                        if (worldObj.GetObjectName() == entity.ToString())
                        {
                            if (dist < friendDistance)
                            {
                                friendDistance = dist;
                            }
                        }
                        if (worldObj.GetObjectName() == "Town Center" || worldObj.GetObjectName() == "City Hall")
                        {
                            if (dist < hallDistance)
                            {
                                hallDistance = dist;
                            }
                        }
                    }
                    else
                    {
                        if (worldObj.CanAttack())
                        {
                            nEnemies++;
                            if (dist < enemyDistance)
                            {
                                enemyDistance = dist;
                            }
                        }
                    }
                    if (WorkManager.ObjectIsOre(worldObj.gameObject))
                    {
                        if (dist < resourceDistance)
                        {
                            resourceDistance = dist;
                        }
                    }
                    if (WorkManager.ObjectIsOil(worldObj.gameObject))
                    {
                        if (dist < oilDistance)
                        {
                            oilDistance = dist;
                        }
                    }
                }
            }
            sensor.AddObservation(nFriends - nEnemies);
            sensor.AddObservation(enemyDistance);
            if (entity == Entity.Worker || entity == Entity.Tank || entity == Entity.BattleShip || entity == Entity.BatteringRam)
            {
                sensor.AddObservation((friendDistance != int.MaxValue) ? friendDistance : 0);
            } 
            if (entity == Entity.Worker)
            {
                sensor.AddObservation(hallDistance);
                sensor.AddObservation(oilDistance);
                sensor.AddObservation(owner.builds);
                sensor.AddObservation(owner.nextProj != null ? owner.nextProj.UnderConstruction() : false);
                sensor.AddObservation(owner.nextProj != null ? (owner.nextProj.UnderConstruction() ? Vector3.Distance(transform.position, owner.nextProj.transform.position) : 99999) : 99999);
            } 
            if (entity == Entity.Harvester || entity == Entity.RustyHarvester)
            {
                sensor.AddObservation(resourceDistance);
                sensor.AddObservation(owner.GetResourceAmount(ResourceType.Copper));
                sensor.AddObservation(owner.GetResourceAmount(ResourceType.Iron));
                sensor.AddObservation(owner.GetResourceAmount(ResourceType.Gold));
            } 
        }
        else
        {
            sensor.AddObservation(buildingController.hitPoints);
            // Buildings
            sensor.AddObservation(owner.GetResourceAmount(ResourceType.Spacing));
            sensor.AddObservation(owner.GetResourceAmount(ResourceType.Copper));
            sensor.AddObservation(owner.GetResourceAmount(ResourceType.Iron));
            sensor.AddObservation(owner.GetResourceAmount(ResourceType.Oil));
            sensor.AddObservation(owner.GetResourceAmount(ResourceType.Gold));
            if (entity == Entity.University)
            {
                sensor.AddObservation(owner.GetResourceAmount(ResourceType.ResearchPoint));
            }
        }
    }

    private string GetBranchName()
    {
        switch(entity)
        {
            case Entity.Worker:
                return WORKER;
            case Entity.Harvester:
            case Entity.RustyHarvester:
                return HARVESTER;
            case Entity.Tank:
            case Entity.BatteringRam:
            case Entity.BattleShip:
                return ATTACKING_UNIT;
            case Entity.CargoShip:
                return CARGO;
            case Entity.ConvoyTruck:
                return IDLE_UNIT;
            case Entity.TownCenter:
            case Entity.CityHall:
                return HALL;
            case Entity.Dock:
                return DOCK;
            case Entity.Refinery:
                return REFINERY;
            case Entity.WarFactory:
                return WAR_FACTORY;
            case Entity.University:
                return UNIVERSITY;
            default:
                return IDLE_BUILDING;
        }
    }

    public void Update()
    {
        // For heuristic use only
        {
            if (myID == 0)
            {
                if (Input.GetKeyDown(KeyCode.X))
                {
                    currentID = (currentID + 1) % globalID;
                }
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    currentID--;
                    if (currentID < 0) currentID = globalID - 1;
                }
            }
            if (!ready)
            {
                if (currentID == myID)
                {
                    Debug.Log(entity + " " + currentID);
                    ready = true;
                }
                else
                {
                    ready = false;
                }
            }
            else
            {
                if (currentID != myID)
                {
                    ready = false;
                }
            }
        }

        if (entity == Entity.Worker)
        {
            // search for dock or oil pump placement (special needs)
            if (owner.IsFindingBuildingLocation())
            {
                Building tempBuilding = owner.GetTempBuilding();
                if (tempBuilding.GetObjectName() == "Oil Pump")
                {
                    List<WorldObjects> nearbyObjects = unitController.GetNearbyObjects();
                    if (nearbyObjects == null) nearbyObjects = unitController.SetNearbyObjects();
                    foreach (WorldObjects nearby in nearbyObjects)
                    {
                        if (nearby == null) continue;
                        if (WorkManager.ObjectIsOil(nearby.gameObject)) {
                            owner.SetTempBuildingLocation(nearby.transform.position);
                            break;
                        }
                    }
                }
                if (tempBuilding.GetObjectName() == "Dock")
                {
                    Vector3 position = GetClosestValidDockPoint(waterGraph, transform.position);
                    if (position != -Vector3.one)
                    {
                        owner.SetTempBuildingLocation(position);
                    }
                }
            }
        }
    }

    public void FixedUpdate()
    {
        if (entity == Entity.Worker)
            if (owner.IsFindingBuildingLocation())
            {
                if (owner.CanPlaceBuilding())
                {
                    canPlaceCount++;
                    if (canPlaceCount > 5)
                    {
                        nextProj = owner.StartConstruction();
                        AddReward(1f);
                        canPlaceCount = 0;
                    }
                }
                else
                {
                    unableToPlaceTimer += Time.deltaTime;
                    if (unableToPlaceTimer > 5.0f)
                    {
                        owner.TryCancelBuilding(GetComponent<Worker>());
                        unableToPlaceTimer = 0;
                    }
                }
            }
    }

    private void MoveUnit(Unit controller, int forwardMove, int sideMove)
    {
        if (entity == Entity.Harvester) return;
        if (entity == Entity.Tank)
        {
            if (controller.enemyObjects != null && controller.enemyObjects.Count > 0)
            {
                WorldObjects closestObject = WorkManager.FindNearestWorldObjectInListToPosition(controller.enemyObjects, transform.position);
                if (closestObject)
                {
                    controller.BeginAttack(closestObject);
                    return;
                }
            }
        }
        if (forwardMove != 0 || sideMove != 0)
        {
            AddReward(-0.005f);
            if (entity == Entity.Worker && owner.IsBuilding())
            {
                AddReward(-10f);
                if (owner.nextProj != null && owner.nextProj.UnderConstruction())
                {
                    (controller as Worker).SetBuilding(owner.nextProj);
                    return;
                }
            }
            Vector3 target = controller.transform.position;
            target += Vector3.forward * (forwardMove == 1 ? 5f : (forwardMove == 2 ? -5f : 0f));
            target += Vector3.right * (sideMove == 1 ? -5f : (sideMove == 2 ? 5f : 0f));
            controller.StartMove(target);
        }
        if (entity == Entity.RustyHarvester || entity == Entity.Harvester)
        {
            if ((controller as Harvester).harvesting || (controller as Harvester).emptying)
            {
                AddReward(-0.5f);
            }
        }
    }

    private void ConstructBuilding(Worker controller, int buildType)
    {
        if (buildType <= 0)
            return;
        if (buildType == 1)
        {
            // load in cargo
            return;
        }
        if (buildType == 2)
        {
            Buildings friendlyBuildings = owner.GetComponentInChildren<Buildings>();
            Building[] buildingsList = friendlyBuildings.GetComponentsInChildren<Building>();
            float closestDistance = int.MaxValue;
            Building helpProj = null;
            foreach(Building building in buildingsList)
            {
                if (building.UnderConstruction())
                {
                    float dist = Vector3.Distance(transform.position, building.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        helpProj = building;
                    }
                }
            }
            if (helpProj != null)
            {
                controller.StartMove(helpProj.transform.position, helpProj.gameObject);
                AddReward(1f);
            }
            return;
        }
        string buildName = controller.GetPotentialActions()[buildType - 3];
        controller.PerformAction(buildName);
        AddReward(2f);
        if (buildName == "CityHall") AddReward(2f);
    }

    private void HarvestResources(Harvester controller, int resourceType)
    {
        if (resourceType == 1)
        {
            // load in cargo
            return;
        }
        ResourceType resourceToHarvest = ResourceType.Unknown;
        switch (resourceType)
        {
            case 2: resourceToHarvest = ResourceType.CopperOre; break;
            case 3: resourceToHarvest = ResourceType.IronOre; break;
            case 4: resourceToHarvest = ResourceType.GoldOre; break;
            default: break;
        }
        if (resourceToHarvest == ResourceType.Unknown) return;
        if (controller.harvesting || controller.emptying) return;
        List<WorldObjects> nearbyObjects = WorkManager.FindNearbyObjects(this.transform.position, 125);
        float closestDistance = int.MaxValue;
        Resource closestResource = null;
        foreach (WorldObjects obj in nearbyObjects)
        {
            if (obj == null) continue;
            if (WorkManager.ObjectIsOre(obj.gameObject))
            {
                if ((obj as Resource).GetResourceType() == resourceToHarvest)
                {
                    float dist = Vector3.Distance(obj.transform.position, this.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestResource = obj as Resource;
                    }
                }
            }
        }
        if (closestResource)
        {
            controller.StartHarvest(closestResource);
        }
    }

    private void UnitController(Unit controller, ActionSegment<int> act)
    {
        var forwardMove = act[0]; // move forward
        var sideMove = act[1]; // move backward
        var ownAction = act[2];


        /* 
         * 0 = Do nothing
         * Worker - build
         * 1 = Load in Cargo; 2 = Help Build; 3 = City Hall; 4 = University; 5 = Refinery; 6 = Oil Pump; 7 = War Factory; 8 = Turret; 9 = Wonder; 10 = Dock
         * Attackers (Tank, BatteringRam, BattleShip) - attack
         * 1 = Load in Cargo (except BattleShip); enemy; 2 = Attack closest;
         * Harvester - harvest
         * 1 = Load in Cargo; 2 = Harvest Copper; 3 = Harvest Iron; 4 = Harvest Gold;
         * Cargo - unload
         * 1 = Unload units
         */
        if (forwardMove != 0 || sideMove != 0)
        {
            MoveUnit(controller, forwardMove, sideMove);
        }
        if ((entity == Entity.Worker && ownAction == 1) ||
            ((entity == Entity.Harvester || entity == Entity.RustyHarvester) && ownAction == 1) ||
            ((entity == Entity.Tank || entity == Entity.BatteringRam) && ownAction == 1) ||
            (entity == Entity.ConvoyTruck && ownAction == 1) ||
            (entity == Entity.CargoShip && ownAction == 1))
        {
            // Load Unit
            if (entity != Entity.CargoShip && entity != Entity.BattleShip)
            {

                List<WorldObjects> nearbyObjects = controller.GetNearbyObjects();
                if (nearbyObjects == null) nearbyObjects = controller.SetNearbyObjects();
                float closestDistance = int.MaxValue;
                WorldObjects closestCargo = null;
                foreach (WorldObjects obj in nearbyObjects)
                {

                    if (obj)
                    {
                        if (WorkManager.ObjectIsCargo(obj.gameObject))
                        {
                            float dist = Vector3.Distance(obj.transform.position, this.transform.position);
                            if (dist < closestDistance)
                            {
                                closestDistance = dist;
                                closestCargo = obj;
                            }
                        }
                    }
                }
                if (closestCargo == null)
                {
                    AddReward(-2);
                }
                else
                {
                    AddReward(-0.04f);
                    controller.LoadUnitIntoCargo(closestCargo.gameObject, closestCargo.transform.position);
                }
            } else
            {
                if (entity == Entity.CargoShip)
                {
                    (controller as CargoShip).UnloadUnits();
                    AddReward(-0.1f);
                } 
                else
                {
                    AddReward(-5);
                }
            }
            return;
        }
        switch (entity)
        {
            case Entity.Worker:
                ConstructBuilding(controller as Worker, ownAction);
                break;
            case Entity.Harvester:
            case Entity.RustyHarvester:
                HarvestResources(controller as Harvester, ownAction);
                break;
            case Entity.Tank:
            case Entity.BatteringRam:
            case Entity.BattleShip:
                AttackEnemy(controller, ownAction);
                break;
            default:
                break;
        }
    }

    private void AttackEnemy(Unit controller, int ownAction)
    {
        if (ownAction <= 1)
        {
            // nothing or load in cargo
            return;
        }
        if (ownAction == 2)
        {
            List<WorldObjects> nearbyObjects = controller.GetNearbyObjects();
            if (nearbyObjects == null) controller.SetNearbyObjects();
            float closestDistance = int.MaxValue;
            float closestUnitDistance = int.MaxValue;
            WorldObjects closestObject = null;
            Unit closestUnit = null;
            foreach (WorldObjects obj in nearbyObjects)
            {
                if (obj == null) continue;
                if (WorkManager.ObjectIsOre(obj.gameObject))
                {
                    if (!obj.IsOwnedBy(owner))
                    {
                        float dist = Vector3.Distance(obj.transform.position, this.transform.position);
                        if (obj.GetComponent<Building>() && dist < closestDistance)
                        {
                            closestDistance = dist;
                            closestObject = obj;
                        }
                        if (obj.GetComponent<Unit>() && dist < closestUnitDistance)
                        {
                            closestUnitDistance = dist;
                            closestUnit = obj as Unit;
                        }
                    }
                }
            }
            if (closestUnit != null)
            {
                AddReward(1f);
                controller.BeginAttack(closestUnit);
            } else
            {
                if (closestObject != null)
                {
                    controller.BeginAttack(closestObject);
                }
            }
        }
    }

    private void BuildingController(Building controller, ActionSegment<int> act)
    {
        int ownAction = act[2];
        /*
         * Buildings
         * 1,2,3... = Execute its actions
         */
        if (ownAction != 0 && !controller.UnderConstruction())
        {
            string unitName = controller.GetPotentialActions()[ownAction - 1];
            controller.PerformAction(unitName);
            if (unitName != "RustyHarvester")
            {
                AddReward(0.5f);
            }
            else
            {
                AddReward(-1f);
            }
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        WorldObjects obj = (unitController != null) ? unitController as WorldObjects : buildingController;
        List<string> allowed = new List<string>(obj.GetActions());
        int idx = (entity == Entity.Worker) ? 3 : ((moveable ? 2 : 1));
        foreach (string act in obj.GetPotentialActions())
        {
            if (!allowed.Contains(act) || (owner.IsFindingBuildingLocation() && entity == Entity.Worker) || !WorkManager.NotToMany(owner, act))
            {
                actionMask.SetActionEnabled(2, idx, false);
            } 
            else
            {
                actionMask.SetActionEnabled(2, idx, true);
            }
            idx++;
        }        
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        /*
            0 - up/down (3)
            1 - left/right (3)
            2 - cladiri care nu fac nimic (1)
            3 - trupe care nu fac nimic (2)
            4 - trupe care doar ataca (3)
            5 - worker (11)
            6 - harvester (5)
            7 - cargo (2)
            8 - hall (3)
            9 - dock (3)
            10 - refinery (2)
            11 - warfactory (4)
            12 - university (16)

            mlagents-learn config/RTSAgent.yaml --initialize-from=RTSAgent --run-id=RTSAgent
         */
        if (moveable)
        {
            UnitController(unitController, actionBuffers.DiscreteActions);
        }
        else
        {
            BuildingController(buildingController, actionBuffers.DiscreteActions);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (myID != currentID) return;
        ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;
        discreteActionsOut[1] = 0;
        discreteActionsOut[2] = 0;
        if (Input.GetKey(KeyCode.L))
        {
            discreteActionsOut[1] = 2;
        }
        if (Input.GetKey(KeyCode.I))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.J))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.K))
        {
            discreteActionsOut[0] = 2;
        }
        int branchSize = branches[GetBranchName()];
        int actionCode = 0;
        if (Input.GetKeyDown(KeyCode.Alpha0)) actionCode = 0 % branchSize;
        if (Input.GetKeyDown(KeyCode.Alpha1)) actionCode = 1 % branchSize;
        if (Input.GetKeyDown(KeyCode.Alpha2)) actionCode = 2 % branchSize;
        if (Input.GetKeyDown(KeyCode.Alpha3)) actionCode = 3 % branchSize;
        if (Input.GetKeyDown(KeyCode.Alpha4)) actionCode = 4 % branchSize;
        if (Input.GetKeyDown(KeyCode.Alpha5)) actionCode = 5 % branchSize;
        if (Input.GetKeyDown(KeyCode.Alpha6)) actionCode = 6 % branchSize;
        if (Input.GetKeyDown(KeyCode.Alpha7)) actionCode = 7 % branchSize;
        if (Input.GetKeyDown(KeyCode.Alpha8)) actionCode = 8 % branchSize;
        if (Input.GetKeyDown(KeyCode.Alpha9)) actionCode = 9 % branchSize;
        if (Input.GetKeyDown(KeyCode.A)) actionCode = 10 % branchSize;
        if (Input.GetKeyDown(KeyCode.B)) actionCode = 11 % branchSize;
        if (Input.GetKeyDown(KeyCode.C)) actionCode = 12 % branchSize;
        if (Input.GetKeyDown(KeyCode.D)) actionCode = 13 % branchSize;
        if (Input.GetKeyDown(KeyCode.E)) actionCode = 14 % branchSize;
        if (Input.GetKeyDown(KeyCode.F)) actionCode = 15 % branchSize;
        if (actionCode != 0)
        {
            Debug.Log("Action: " + actionCode);
        }
        discreteActionsOut[2] = actionCode;
    }

    private void OnDestroy()
    {
        Building bld = GetComponent<Building>();
        if (bld)
        {
            if (bld.Ghost) return;
        }
        SetReward(-10);
    }

    private Vector3 GetClosestValidDockPoint(NavGraph navGraph, Vector3 position)
    {
        int d = 1;
        while (true)
        {
            for (int i = 0; i < 36; i++)
            {
                Vector3 newPosition = position + new Vector3(d * Mathf.Cos(2 * Mathf.PI * (float)i / 36.0f), 0, d * Mathf.Sin(2 * Mathf.PI * (float)i / 36.0f));
                if (navGraph.GetNearest(newPosition).node.Walkable)
                {

                    return newPosition;
                }
            }
            d++;
            if (d > 75)
            {
                return -Vector3.one;
            }
        }
    }


}
