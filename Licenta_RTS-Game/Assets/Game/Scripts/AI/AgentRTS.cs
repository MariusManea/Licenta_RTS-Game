using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using RTS;
using Unity.MLAgents.Sensors;

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

    private int buildID = 0;

    BehaviorParameters m_BehaviorParameters;

    public void Start()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        team = FindObjectOfType<LevelLoader>().teamColors[m_BehaviorParameters.TeamId];
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
    }

    public override void Initialize()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        team = FindObjectOfType<LevelLoader>().teamColors[m_BehaviorParameters.TeamId];
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
    }

    public override void OnEpisodeBegin()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        team = FindObjectOfType<LevelLoader>().teamColors[m_BehaviorParameters.TeamId];
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
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation((int)entity);
        if (moveable)
        {

            // Units
            sensor.AddObservation(unitController.hitPoints);
            List<WorldObjects> nearbyObjects = WorkManager.FindNearbyObjects(this.transform.position, 250);
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

            foreach (WorldObjects worldObj in nearbyObjects)
            {
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
            sensor.AddObservation(nFriends - nEnemies);
            sensor.AddObservation(enemyDistance);
            if (entity == Entity.Worker || entity == Entity.Tank || entity == Entity.BattleShip || entity == Entity.BatteringRam)
            {
                sensor.AddObservation((friendDistance != int.MaxValue) ? friendDistance : 0);
            } 
            else
            {
                sensor.AddObservation(0);
            }
            if (entity == Entity.Worker)
            {
                sensor.AddObservation(hallDistance);
                sensor.AddObservation(oilDistance);
            } 
            else
            {
                sensor.AddObservation(0);
                sensor.AddObservation(0);
            }
            if (entity == Entity.Harvester || entity == Entity.RustyHarvester)
            {
                sensor.AddObservation(resourceDistance);
                sensor.AddObservation(owner.GetResourceAmount(ResourceType.Copper));
                sensor.AddObservation(owner.GetResourceAmount(ResourceType.Iron));
                sensor.AddObservation(owner.GetResourceAmount(ResourceType.Gold));
            } 
            else
            {
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
            }
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
        }
        else
        {
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
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
            else
            {
                sensor.AddObservation(0);
            }
        }
    }

    public void FixedUpdate()
    {
        if (owner.IsFindingBuildingLocation())
        {
            if (owner.CanPlaceBuilding())
            {
                buildID = 0;
                owner.StartConstruction();
                AddReward(-0.5f);
            }
        }
    }

    private void MoveUnit(Unit controller, int forwardMove, int sideMove)
    {
        AddReward(-0.005f);
        Vector3 target = controller.transform.position;
        target += Vector3.forward * (forwardMove == 1 ? 5f : (forwardMove == 2 ? -5f : 0f));
        target += Vector3.right * (sideMove == 1 ? -5f : (sideMove == 2 ? 5f : 0f));
        controller.StartMove(target);
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
            }
            return;
        }
        string buildName = controller.GetPotentialActions()[buildType - 3];
        controller.PerformAction(buildName);
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
        List<WorldObjects> nearbyObjects = WorkManager.FindNearbyObjects(this.transform.position, 250);
        float closestDistance = int.MaxValue;
        Resource closestResource = null;
        foreach (WorldObjects obj in nearbyObjects)
        {
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
        var idleUnits = act[3];
        var attackingUnits = act[4];
        var workerActions = act[5];
        var harvesterActions = act[6];
        var cargoActions = act[7];


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
        if ((entity == Entity.Worker && workerActions == 1) ||
            ((entity == Entity.Harvester || entity == Entity.RustyHarvester) && harvesterActions == 1) ||
            ((entity == Entity.Tank || entity == Entity.BatteringRam) && attackingUnits == 1) ||
            (entity == Entity.ConvoyTruck && idleUnits == 1) ||
            (entity == Entity.CargoShip && cargoActions == 1))
        {
            // Load Unit
            if (entity != Entity.CargoShip && entity != Entity.BattleShip)
            {
                List<WorldObjects> nearbyObjects = WorkManager.FindNearbyObjects(this.transform.position, 250);
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
                ConstructBuilding(controller as Worker, workerActions);
                break;
            case Entity.Harvester:
                HarvestResources(controller as Harvester, harvesterActions);
                break;
            case Entity.Tank:
            case Entity.BatteringRam:
            case Entity.BattleShip:
                AttackEnemy(controller, attackingUnits);
                break;
            default:
                break;
        }
    }

    private void AttackEnemy(object controller, int ownAction)
    {
        if (ownAction <= 1)
        {
            // nothing or load in cargo
            return;
        }
        if (ownAction == 2)
        {
            List<WorldObjects> nearbyObjects = WorkManager.FindNearbyObjects(this.transform.position, 250);
            float closestDistance = int.MaxValue;
            float closestUnitDistance = int.MaxValue;
            WorldObjects closestObject = null;
            Unit closestUnit = null;
            foreach (WorldObjects obj in nearbyObjects)
            {
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
                (controller as WorldObjects).BeginAttack(closestUnit);
            } else
            {
                if (closestObject != null)
                {
                    (controller as WorldObjects).BeginAttack(closestObject);
                }
            }
        }
    }

    private void BuildingController(Building controller, ActionSegment<int> act)
    {
        var idleBuilding = act[2];
        var hallActions = act[8];
        var dockActions = act[9];
        var refineryActions = act[10];
        var warFactoryActions = act[11];
        var universityActions = act[12];

        var ownAction = idleBuilding;
        switch (entity)
        {
            case Entity.TownCenter:
            case Entity.CityHall:
                ownAction = hallActions;
                break;
            case Entity.Dock:
                ownAction = dockActions;
                break;
            case Entity.Refinery:
                ownAction = refineryActions;
                break;
            case Entity.WarFactory:
                ownAction = warFactoryActions;
                break;
            case Entity.University:
                ownAction = universityActions;
                break;
            default:
                ownAction = idleBuilding;
                break;
        }
        /*
         * Buildings
         * 1,2,3... = Execute its actions
         */
        if (ownAction != 0)
        {
            string unitName = controller.GetPotentialActions()[ownAction - 1];
            controller.PerformAction(unitName);
            if (unitName != "RustyHarvester")
            {
                AddReward(0.5f);
            }
            else
            {
                AddReward(-0.01f);
            }
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        WorldObjects obj = (unitController != null) ? unitController as WorldObjects : buildingController;
        List<int> mask = new List<int>();
        List<string> allowed = new List<string>(obj.GetActions());
        int idx = 2;
        foreach (string act in obj.GetPotentialActions())
        {
            if (!allowed.Contains(act) || (owner.IsFindingBuildingLocation() && entity == Entity.Worker))
            {
                mask.Add(idx);
            }
            idx++;
        }
        if (moveable)
        {
            switch (entity) {
                case Entity.ConvoyTruck:
                    actionMask.WriteMask(3, mask);
                    break;
                case Entity.Tank:
                case Entity.BatteringRam:
                case Entity.BattleShip:
                    actionMask.WriteMask(4, mask);
                    break;
                case Entity.Worker:
                    actionMask.WriteMask(5, mask);
                    break;
                case Entity.Harvester:
                    actionMask.WriteMask(6, mask);
                    break;
                case Entity.CargoShip:
                    actionMask.WriteMask(7, mask);
                    break;
                case Entity.TownCenter:
                case Entity.CityHall:
                    actionMask.WriteMask(8, mask);
                    break;
                case Entity.Dock:
                    actionMask.WriteMask(9, mask);
                    break;
                case Entity.Refinery:
                    actionMask.WriteMask(10, mask);
                    break;
                case Entity.WarFactory:
                    actionMask.WriteMask(11, mask);
                    break;
                case Entity.University:
                    actionMask.WriteMask(12, mask);
                    break;
                default:
                    actionMask.WriteMask(2, mask);
                    break;
            }
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
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;
        discreteActionsOut[1] = 0;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 2;
        }
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            buildID = (buildID + 1) % 10;
        }
        discreteActionsOut[2] = buildID;
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

}
