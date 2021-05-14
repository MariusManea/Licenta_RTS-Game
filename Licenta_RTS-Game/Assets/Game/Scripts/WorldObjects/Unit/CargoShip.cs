using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using Pathfinding;
using Newtonsoft.Json;

public class CargoShip : Ship
{
    private List<Unit> unitsLoaded;
    private List<int> unitsLoadedID;
    private bool readyToUnload;
    private bool unloading;

    protected override void Start()
    {
        base.Start();
        unitsLoaded = new List<Unit>();
        actions = new string[] { };
        if (player && loadedSavedValues && unitsLoadedID != null)
        {
            foreach (int id in unitsLoadedID)
            {
                LoadUnit(player.GetObjectForId(id).GetComponent<Unit>());
            }
        }
        unitsLoadedID = null;
    }

    protected override void Update()
    {
        base.Update();
        actions = new string[unitsLoaded.Count];
        for (int i = 0; i < unitsLoaded.Count; ++i)
        {
            actions[i] = unitsLoaded[i].GetType().ToString();
        }
        if (!moving && !rotating)
        {
            if (readyToUnload && unloading)
            {
                NavGraph landNav = FindObjectOfType<AstarPath>().data.graphs[0];
                Vector3 unloadPosition = GetClosestValidUnloadPoint(landNav, transform.position);

                foreach(Unit loadedUnit in unitsLoaded)
                {
                    loadedUnit.gameObject.SetActive(true);
                    loadedUnit.transform.parent = this.transform.parent;
                    loadedUnit.transform.position = unloadPosition;
                    loadedUnit.StartMove(unloadPosition);
                }
                unitsLoaded.Clear();
                readyToUnload = false;
                unloading = false;
                player.hud.SetCursorState(CursorState.Move);
            }
        }
        objectName = GetObjectName() + " (" + ResourceManager.GetLevelAlias(player.GetLevel(UpgradeableObjects.CargoShip)) + ")";
    }

    public override bool IsCargo()
    {
        return true;
    }

    public override void InitiateUnload()
    {
        base.InitiateUnload();
        readyToUnload = true;
    }

    public override void CancelUnload()
    {
        base.CancelUnload();
        readyToUnload = false;
    }

    private int BonusCapacity(int level)
    {
        int capacity = 0;
        for (int i = 2; i <= level; ++i)
        {
            capacity += i;
        }
        return capacity;
    }

    public void LoadUnit(Unit unit) {
        if (unitsLoaded.Count < loadCapacity + BonusCapacity(player.GetLevel(UpgradeableObjects.CargoShip)))
        {
            unitsLoaded.Add(unit);
            if (unit.IsCurrentlySelected())
            {
                unit.SetSelection(false, playingArea);
                player.SelectedObjects.Remove(unit.GetComponent<WorldObjects>());
                if (player.SelectedObjects.Count == 0) player.SelectedObjects = null;
                else player.SelectedObjects = new List<WorldObjects>(player.SelectedObjects.ToArray());
            }
            unit.transform.parent = this.transform;
            unit.transform.localPosition = Vector3.zero;
            unit.loadingTarget = null;
            unit.gameObject.SetActive(false);
        }
    }

    public override void SetHoverState(GameObject hoverObject)
    {
        bool doBase = true;
        if (WorkManager.ObjectIsGround(hoverObject))
        {
            if (player.hud.GetPreviousCursorState() == CursorState.Unload) { player.hud.SetCursorState(CursorState.Unload); doBase = false; }
        }
        if (doBase) base.SetHoverState(hoverObject);
    }

    public override void MouseClick(GameObject hitObject, Vector3 hitPoint, Player controller)
    {
        base.MouseClick(hitObject, hitPoint, controller);
        if (player.hud.GetPreviousCursorState() == CursorState.Unload)
        {
            if (readyToUnload)
            {
                player.hud.SetCursorState(CursorState.Move);
                unloading = true;
            }
        } 
        else
        {
            readyToUnload = false;
            unloading = false;
        }
    }

    public void UnloadUnits()
    {
        readyToUnload = true;
        unloading = true;
    }

    private Vector3 GetClosestValidUnloadPoint(NavGraph navGraph, Vector3 position)
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
            if (d > 50)
            {
                return -Vector3.one;
            }
        }
    }

    public override void SaveDetails(JsonWriter writer)
    {
        base.SaveDetails(writer);
        SaveManager.WriteBoolean(writer, "UnloadingReady", readyToUnload);
        SaveManager.WriteBoolean(writer, "Unloading", unloading);
        SaveManager.WriteInt(writer, "LoadCapacity", loadCapacity);
        writer.WritePropertyName("LoadedUnits");
        writer.WriteStartArray();
        foreach (Unit loadedUnit in unitsLoaded)
        {
            writer.WriteValue(loadedUnit.ObjectId);
        }
        writer.WriteEndArray();
    }

    protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        base.HandleLoadedProperty(reader, propertyName, readValue);
        switch (propertyName)
        {
            case "UnloadingReady": readyToUnload = (bool)readValue; break;
            case "Unloading": unloading = (bool)readValue; break;
            case "LoadCapacity": loadCapacity = (int)(System.Int64)readValue; break;
            case "LoadedUnits":
                unitsLoadedID = new List<int>();
                while (reader.Read())
                {
                    if (reader.Value != null)
                    {
                        unitsLoadedID.Add((int)(System.Int64)reader.Value);
                    }
                    else if (reader.TokenType == JsonToken.EndArray) break;
                }
                break;
        }
    }

    public List<Unit> GetLoadedUnits()
    {
        return unitsLoaded;
    }

    public override string GetObjectName()
    {
        return "Cargo Ship";
    }
}
