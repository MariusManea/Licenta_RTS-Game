using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class CargoShip : Ship
{
    private List<Unit> unitsLoaded;
    public bool readyToUnload;
    protected override void Start()
    {
        base.Start();
        unitsLoaded = new List<Unit>();
        actions = new string[] { };
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

    public void LoadUnit(Unit unit) {
        unitsLoaded.Add(unit);
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
}
