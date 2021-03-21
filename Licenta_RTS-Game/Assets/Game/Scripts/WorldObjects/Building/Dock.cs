using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using RTS;

public class Dock : Building
{
    protected override void Start()
    {
        base.Start();
        actions = new string[] { "CargoShip" };
        potentialActions = new string[] { "CargoShip", "BattleShip" };
    }

    protected override void Update()
    {
        base.Update();
        if (Ghost)
        {
            if (canBePlaced)
            {
                Vector3 closestWater = GetClosestWaterPoint(this.transform.position);
                transform.LookAt(closestWater);
                CalculateBounds();
            }
            else transform.rotation = new Quaternion();
        } 
        else
        {
            objectName = GetObjectName() + " (" + ResourceManager.GetLevelAlias(player.GetLevel(UpgradeableObjects.Dock)) + ")";
            if (currentlySelected)
            {
                actions = player.GetResearchedObjects(potentialActions);
            }
        }
    }

    private Vector3 GetClosestWaterPoint(Vector3 position)
    {
        Vector3 point = -Vector3.one;

        for (float radius = 1; radius < 50; ++radius)
        {
            for (float k = 0; k < 360; k++)
            {
                Vector3 newPosition = position + new Vector3(radius * Mathf.Cos(2 * Mathf.PI * (float)k / 360.0f), 0, radius * Mathf.Sin(2 * Mathf.PI * (float)k / 360.0f));
                if (terrain.SampleHeight(newPosition) <= 6.8f)
                {
                    return newPosition;
                }
            }
        }

        return point;
    }

    public override void PerformAction(string actionToPerform)
    {
        base.PerformAction(actionToPerform);
        CreateUnit(actionToPerform);
    }
    protected override bool ShouldMakeDecision()
    {
        return false;
    }

    public override void SetRallyPoint(Vector3 position)
    {
        NavGraph waterNav = FindObjectOfType<AstarPath>().data.graphs[1];
        position = GetClosestValidPoint(waterNav, position);
        if (position == -Vector3.one) return;
        rallyPoint = position;
        rallyPoint.y = 7;
        if (player && player.isHuman && currentlySelected)
        {
            RallyPoints flag = player.GetComponentInChildren<RallyPoints>();
            if (flag) flag.transform.position = rallyPoint;
        }
    }

    public override void SetSpawnPoint()
    {
        NavGraph waterNav = FindObjectOfType<AstarPath>().data.graphs[1];
        Vector3 position = GetClosestValidPoint(waterNav, new Vector3(selectionBounds.center.x, 7, selectionBounds.center.z));
        if (position == -Vector3.one) return;
        position.y = 7;
        spawnPoint = position;
        rallyPoint = spawnPoint;
    }

    private Vector3 GetClosestValidPoint(NavGraph navGraph, Vector3 destination)
    {
        int d = 1;
        while (true)
        {
            for (int i = 0; i < 360; i++)
            {
                Vector3 newPosition = destination + new Vector3(d * Mathf.Cos(2 * Mathf.PI * (float)i / 360.0f), 0, d * Mathf.Sin(2 * Mathf.PI * (float)i / 360.0f));
                if (navGraph.GetNearest(newPosition).node.Walkable)
                {

                    return newPosition;
                }
            }
            d++;
            if (d > 50)
            {
                return -Vector3.one;
            }
        }
    }

    public override void SetHoverState(GameObject hoverObject)
    {
        base.SetHoverState(hoverObject);
        if (WorkManager.ObjectIsWater(hoverObject))
        {
            if (player.hud.GetPreviousCursorState() == CursorState.RallyPoint) player.hud.SetCursorState(CursorState.RallyPoint);
        }
    }

    public override void MouseClick(GameObject hitObject, Vector3 hitPoint, Player controller)
    {
        base.MouseClick(hitObject, hitPoint, controller);
        if (WorkManager.ObjectIsWater(hitObject))
        {
            if ((player.hud.GetCursorState() == CursorState.RallyPoint || player.hud.GetPreviousCursorState() == CursorState.RallyPoint) && hitPoint != ResourceManager.InvalidPosition)
            {
                SetRallyPoint(hitPoint);
                player.hud.SetCursorState(CursorState.Select);
            }
        }
    }

    public override string GetObjectName()
    {
        return "Dock";
    }

}
