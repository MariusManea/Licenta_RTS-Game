﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class Turret : Building
{

    private Quaternion aimRotation;
    public float weaponAimSpeed;

    protected override void Start()
    {
        base.Start();
        detectionRange = weaponRange;
    }

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
        if (!needsBuilding && !Ghost)
        {
            objectName = GetObjectName() + " (" + ResourceManager.GetLevelAlias(player.GetLevel(UpgradeableObjects.Turret)) + ")";
        }
    }

    public override bool CanAttack()
    {
        if (UnderConstruction() || hitPoints == 0) return false;
        return true;
    }

    protected override void UseWeapon()
    {
        base.UseWeapon();
        Vector3 spawnPoint = transform.position;
        spawnPoint.x += (2.6f * transform.forward.x);
        spawnPoint.y += 1.0f;
        spawnPoint.z += (2.6f * transform.forward.z);
        GameObject gameObject = (GameObject)Instantiate(ResourceManager.GetGameObject("TurretProjectile"), spawnPoint, transform.rotation);
        Projectile projectile = gameObject.GetComponentInChildren<Projectile>();
        projectile.SetRange(0.9f * weaponRange);
        projectile.SetTarget(target);
        projectile.damage += 10 * (player.GetLevel(UpgradeableObjects.Turret) - 1);
    }

    protected override void AimAtTarget()
    {
        base.AimAtTarget();
        aimRotation = Quaternion.LookRotation(target.transform.position - transform.position);
    }

    public override string GetObjectName()
    {
        return "Turret";
    }
}