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
        Vector3 dirToWater = (transform.position - position).normalized;
        float spawnX = selectionBounds.center.x + dirToWater.x * selectionBounds.extents.x + dirToWater.x * 15;
        float spawnZ = selectionBounds.center.z + dirToWater.z * selectionBounds.extents.z + dirToWater.z * 15;
        position = GetClosestValidPoint(waterNav, new Vector3(spawnX, 7, spawnZ));
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
            for (int i = 0; i < 36; i++)
            {
                Vector3 newPosition = destination + new Vector3(d * Mathf.Cos(2 * Mathf.PI * (float)i / 36.0f), 0, d * Mathf.Sin(2 * Mathf.PI * (float)i / 36.0f));
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

}
