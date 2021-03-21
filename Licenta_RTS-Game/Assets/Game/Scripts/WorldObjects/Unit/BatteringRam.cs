using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using Newtonsoft.Json;


public class BatteringRam : Unit
{
    private Quaternion aimRotation;
    private float weaponAimSpeed = 3;
    public int siegeDamage;

    protected override void Update()
    {
        base.Update();
        if (aiming)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, aimRotation, weaponAimSpeed);
            CalculateBounds();
            //sometimes it gets stuck exactly 180 degrees out in the calculation and does nothing, this check fixes that
            Quaternion inverseAimRotation = new Quaternion(-aimRotation.x, -aimRotation.y, -aimRotation.z, -aimRotation.w);
            if (transform.rotation == aimRotation || transform.rotation == inverseAimRotation)
            {
                aiming = false;
            }
        }
        objectName = GetObjectName() + " (" + ResourceManager.GetLevelAlias(player.GetLevel(UpgradeableObjects.BatteringRam)) + ")";
    }
    protected override void AimAtTarget()
    {
        base.AimAtTarget();
        aimRotation = Quaternion.LookRotation(target.transform.position - transform.position);
    }
    protected override void UseWeapon()
    {
        base.UseWeapon();
        Building targetedBuilding = target as Building;
        if (targetedBuilding)
        {
            targetedBuilding.TakeDamage(siegeDamage + 10 * (player.GetLevel(UpgradeableObjects.BatteringRam) - 1));
        } else
        {
            target.TakeDamage((int)(0.15f * siegeDamage + 1.5f * (player.GetLevel(UpgradeableObjects.BatteringRam) - 1)));
        }
        animController.Play("fire");
    }
    public override bool CanAttack()
    {
        return true;
    }
    public override void SaveDetails(JsonWriter writer)
    {
        base.SaveDetails(writer);
        SaveManager.WriteQuaternion(writer, "AimRotation", aimRotation);
    }

    protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        base.HandleLoadedProperty(reader, propertyName, readValue);
        switch (propertyName)
        {
            case "AimRotation": aimRotation = LoadManager.LoadQuaternion(reader); break;
            default: break;
        }
    }
    public override string GetObjectName()
    {
        return "Battering Ram";
    }
}
