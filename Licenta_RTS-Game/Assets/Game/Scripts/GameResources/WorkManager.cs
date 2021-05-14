using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTS
{
    public static class WorkManager
    {
        public static Rect CalculateSelectionBox(Bounds selectionBounds, Rect playingArea)
        {
            //shorthand for the coordinates of the centre of the selection bounds
            float cx = selectionBounds.center.x;
            float cy = selectionBounds.center.y;
            float cz = selectionBounds.center.z;
            //shorthand for the coordinates of the extents of the selection bounds
            float ex = selectionBounds.extents.x;
            float ey = selectionBounds.extents.y;
            float ez = selectionBounds.extents.z;

            //Determine the screen coordinates for the corners of the selection bounds
            List<Vector3> corners = new List<Vector3>();
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + ex, cy + ey, cz + ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + ex, cy + ey, cz - ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + ex, cy - ey, cz + ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - ex, cy + ey, cz + ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + ex, cy - ey, cz - ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - ex, cy - ey, cz + ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - ex, cy + ey, cz - ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - ex, cy - ey, cz - ez)));

            //Determine the bounds on screen for the selection bounds
            Bounds screenBounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Count; i++)
            {
                screenBounds.Encapsulate(corners[i]);
            }

            //Screen coordinates start in the bottom left corner, rather than the top left corner
            //this correction is needed to make sure the selection box is drawn in the correct place
            float selectBoxTop = playingArea.height - (screenBounds.center.y + screenBounds.extents.y);
            float selectBoxLeft = screenBounds.center.x - screenBounds.extents.x;
            float selectBoxWidth = 2 * screenBounds.extents.x;
            float selectBoxHeight = 2 * screenBounds.extents.y;

            return new Rect(selectBoxLeft, selectBoxTop, selectBoxWidth, selectBoxHeight);
        }

        public static GameObject FindHitObject(Vector3 origin)
        {
            Ray ray = Camera.main.ScreenPointToRay(origin);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) return hit.collider.gameObject;
            return null;
        }
        public static Vector3 FindHitPoint(Vector3 origin)
        {
            Ray ray = Camera.main.ScreenPointToRay(origin);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) return hit.point;
            return ResourceManager.InvalidPosition;
        }

        public static ResourceType GetResourceType(string type)
        {
            switch (type)
            {
                case "Spacing": return ResourceType.Spacing;
                case "Copper": return ResourceType.Copper;
                case "Iron": return ResourceType.Iron;
                case "Oil": return ResourceType.Oil;
                case "Gold": return ResourceType.Gold;
                case "CopperOre": return ResourceType.CopperOre;
                case "IronOre": return ResourceType.IronOre;
                case "OilDeposit": return ResourceType.OilDeposit;
                case "GoldOre": return ResourceType.GoldOre;
                case "ResearchPoint": return ResourceType.ResearchPoint;
                default: return ResourceType.Unknown;
            }
        }

        public static ResourceType GetResourceHarvested(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.CopperOre: return ResourceType.Copper;
                case ResourceType.IronOre: return ResourceType.Iron;
                case ResourceType.OilDeposit: return ResourceType.Oil;
                case ResourceType.GoldOre: return ResourceType.Gold;
                default: return ResourceType.Unknown;
            }
        }

        public static bool ObjectIsGround(GameObject obj)
        {
            return obj != null && (obj.name == "Ground" || obj.name == "Ground(Clone)" || obj.name == "GroundHolder" || obj.name == "GroundHolder(Clone)");
        }

        public static bool ObjectIsWater(GameObject obj)
        {
            return obj != null && (obj.name == "Water" || obj.layer == 4);
        }

        public static bool ObjectIsCargo(GameObject obj)
        {
            return obj != null && (obj.transform.parent != null && obj.transform.parent.GetComponent<CargoShip>() != null);
        }

        public static bool ObjectIsOil (GameObject obj)
        {
            return obj.GetComponent<OilPile>() != null || obj.GetComponentInChildren<OilPile>() != null;
        }

        public static bool ObjectIsOre(GameObject obj)
        {
            return obj.GetComponent<Ore>() != null || obj.GetComponentInChildren<Ore>() != null;
        }

        public static List<WorldObjects> FindNearbyObjects(Vector3 position, float range)
        {
            Collider[] hitColliders = Physics.OverlapSphere(position, range);
            HashSet<int> nearbyObjectIds = new HashSet<int>();
            List<WorldObjects> nearbyObjects = new List<WorldObjects>();
            for (int i = 0; i < hitColliders.Length; i++)
            {
                Transform parent = hitColliders[i].transform.parent;
                if (parent)
                {
                    WorldObjects parentObject = parent.GetComponent<WorldObjects>();
                    if (parentObject && !nearbyObjectIds.Contains(parentObject.ObjectId))
                    {
                        nearbyObjectIds.Add(parentObject.ObjectId);
                        nearbyObjects.Add(parentObject);
                    }
                }
            }
            return nearbyObjects;
        }
        public static WorldObjects FindNearestWorldObjectInListToPosition(List<WorldObjects> objects, Vector3 position)
        {
            if (objects == null || objects.Count == 0) return null;
            WorldObjects nearestObject = objects[0];
            float distanceToNearestObject = Vector3.SqrMagnitude(position - nearestObject.transform.position);
            for (int i = 1; i < objects.Count; i++)
            {
                float distanceToObject = Vector3.SqrMagnitude(position - objects[i].transform.position);
                if (distanceToObject < distanceToNearestObject)
                {
                    distanceToNearestObject = distanceToObject;
                    nearestObject = objects[i];
                }
            }
            return nearestObject;
        }

        public static bool IsWorldObjectNearby(WorldObjects target, Vector3 position)
        {
            if (target == null) return false;
            float distance = Vector3.SqrMagnitude(position - target.transform.position);
            return distance < 225;
        }


        // conditii:
        // Dock_1: University_2
        // Turret_1: University_2 WarFactory_3
        // Wonder: University_5 Worker_10 Refinery_5 OilPump_5
        // ConvoyTruck: University_5 WarFactory_5 OilPump_5 Harvester_10
        // CargoShip_1: Dock_1
        // Refinery, WarFactory, Dock si TownHall permit pentru fiecare nivel de upgrade cate 2 nivele pentru Harvester, Tank, Cargoship, respectiv Worker
        // Pentru Tank si CargoShip se ia in paralel si OilPump



        public static bool ResearchableObject(UpgradeableObjects type, int desiredLevel, Dictionary<UpgradeableObjects, int> levels)
        {
            switch (type)
            {
                case UpgradeableObjects.Dock:
                    {
                        if (desiredLevel == 1 && levels[UpgradeableObjects.University] >= 2)
                            return true;
                        else
                            return desiredLevel != 1;
                    }
                case UpgradeableObjects.CargoShip:
                    {
                        return desiredLevel <= 2 * levels[UpgradeableObjects.Dock] && desiredLevel <= 2 * levels[UpgradeableObjects.OilPump];
                    }
                case UpgradeableObjects.Turret:
                    {
                        if (desiredLevel == 1 && levels[UpgradeableObjects.University] >= 2 && levels[UpgradeableObjects.WarFactory] >= 3)
                            return true;
                        else
                            return desiredLevel != 1;
                    }
                case UpgradeableObjects.Tank:
                    {
                        return desiredLevel <= 2 * levels[UpgradeableObjects.WarFactory] && desiredLevel <= 2 * levels[UpgradeableObjects.OilPump];
                    }
                case UpgradeableObjects.Worker:
                    {
                        return desiredLevel <= 2 * levels[UpgradeableObjects.CityHall];
                    }
                case UpgradeableObjects.Harvester:
                    {
                        return desiredLevel <= 2 * levels[UpgradeableObjects.Refinery];
                    }
                case UpgradeableObjects.Wonder:
                    {
                        return desiredLevel == 1 && levels[UpgradeableObjects.Worker] == 10 &&
                            levels[UpgradeableObjects.University] == 5 &&
                            levels[UpgradeableObjects.Refinery] == 5 &&
                            levels[UpgradeableObjects.OilPump] == 5;
                    }
                case UpgradeableObjects.ConvoyTruck:
                    {
                        return desiredLevel == 1 && levels[UpgradeableObjects.Harvester] == 10 &&
                            levels[UpgradeableObjects.University] == 5 &&
                            levels[UpgradeableObjects.WarFactory] == 5 &&
                            levels[UpgradeableObjects.OilPump] == 5;
                    }
                case UpgradeableObjects.BatteringRam:
                    {
                        if (desiredLevel == 1)
                        {
                            return levels[UpgradeableObjects.University] >= 3 &&
                                levels[UpgradeableObjects.WarFactory] >= 3;
                        } 
                        else
                        {
                            return desiredLevel <= levels[UpgradeableObjects.WarFactory];
                        }
                    }
                case UpgradeableObjects.BattleShip:
                    {
                        if (desiredLevel == 1)
                        {
                            return levels[UpgradeableObjects.University] >= 3 &&
                                levels[UpgradeableObjects.Dock] >= 3 &&
                                levels[UpgradeableObjects.OilPump] >= 2;
                        }
                        else
                        {
                            return desiredLevel <= levels[UpgradeableObjects.Dock] && desiredLevel <= levels[UpgradeableObjects.OilPump];
                        }
                    }
                default: return true;
            }
        }
    }
}
