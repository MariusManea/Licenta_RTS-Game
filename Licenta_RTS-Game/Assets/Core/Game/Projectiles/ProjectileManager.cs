﻿using UnityEngine;
using RTSLockstep.Utility.FastCollections;
using System;
using RTSLockstep.Data;
using System.Collections.Generic;
using RTSLockstep.Agents;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.LSResources;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;

namespace RTSLockstep.Projectiles
{
    public static class ProjectileManager
    {
        public const int MaxProjectiles = 1 << 13;
        private static string[] AllProjCodes;
        private static readonly Dictionary<string, IProjectileData> CodeDataMap = new Dictionary<string, IProjectileData>();

        private static FastBucket<LSProjectile> NDProjectileBucket = new FastBucket<LSProjectile>();

        private static readonly Dictionary<string, FastStack<LSProjectile>> ProjectilePool = new Dictionary<string, FastStack<LSProjectile>>();
        private static FastBucket<LSProjectile> ProjectileBucket = new FastBucket<LSProjectile>();

        public static void Setup()
        {
            if (LSDatabaseManager.TryGetDatabase(out IProjectileDataProvider prov))
            {
                IProjectileData[] projectileData = prov.ProjectileData;
                for (int i = 0; i < projectileData.Length; i++)
                {
                    IProjectileData item = projectileData[i];
                    CodeDataMap.Add(item.Name, item);
                    ProjectilePool.Add(item.Name, new FastStack<LSProjectile>());
                }
            }
        }

        public static void Initialize()
        {
        }

        public static void Simulate()
        {
            for (int i = ProjectileBucket.PeakCount - 1; i >= 0; i--)
            {
                if (ProjectileBucket.arrayAllocation[i])
                {
                    ProjectileBucket[i].Simulate();
                }
            }

            for (int i = NDProjectileBucket.PeakCount - 1; i >= 0; i--)
            {
                if (NDProjectileBucket.arrayAllocation[i])
                {
                    NDProjectileBucket[i].Simulate();
                }
            }
        }

        public static void Visualize()
        {
            for (int i = ProjectileBucket.PeakCount - 1; i >= 0; i--)
            {
                if (ProjectileBucket.arrayAllocation[i])
                {
                    if (ProjectileBucket[i].IsNotNull())
                    {
                        ProjectileBucket[i].Visualize();
                    }
                }
            }
            VisualizeBucket(NDProjectileBucket);
        }

        private static void VisualizeBucket(FastBucket<LSProjectile> bucket)
        {
            for (int i = bucket.PeakCount - 1; i >= 0; i--)
            {
                if (bucket.arrayAllocation[i])
                {
                    bucket[i].Visualize();
                }
            }
        }

        public static void Deactivate()
        {
            for (int i = ProjectileBucket.PeakCount - 1; i >= 0; i--)
            {
                if (ProjectileBucket.arrayAllocation[i])
                {
                    EndProjectile(ProjectileBucket[i]);
                }
            }

            for (int i = NDProjectileBucket.PeakCount - 1; i >= 0; i--)
            {
                if (NDProjectileBucket.arrayAllocation[i])
                {
                    EndProjectile(NDProjectileBucket[i]);
                }
            }
        }

        public static int GetStateHash()
        {
            int hash = 23;
            for (int i = ProjectileBucket.PeakCount - 1; i >= 0; i--)
            {
                if (ProjectileBucket.arrayAllocation[i])
                {
                    LSProjectile proj = ProjectileBucket[i];
                    hash ^= proj.GetStateHash();
                }
            }

            return hash;
        }

        private static LSProjectile NewProjectile(string projCode)
        {
            IProjectileData projData = CodeDataMap[projCode];
            if (projData.GetProjectile().gameObject.IsNotNull())
            {
                var curProj = UnityEngine.Object.Instantiate(projData.GetProjectile().gameObject).GetComponent<LSProjectile>();
                if (curProj.IsNotNull())
                {
                    curProj.Setup(projData);
                    return curProj;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public static LSProjectile Create(string projCode, LSAgent source, Vector3d offset, AllegianceType targetAllegiance, Func<LSAgent, bool> agentConditional, Action<LSAgent> hitEffect)
        {
            Vector2d relativePos = offset.ToVector2d();
            Vector2d worldPos = relativePos.Rotated(source.Body.Rotation) + source.Body.Position;
            Vector3d pos = new Vector3d(worldPos.x, worldPos.y, offset.z + source.Body.HeightPos);
            LocalAgentController sourceController = source.Controller;
            LSProjectile proj = Create(
                projCode,
                pos,
                agentConditional,
                (bite) =>
                {
                    return ((sourceController.GetAllegiance(bite) & targetAllegiance) != 0);
                },
                hitEffect);

            return proj;
        }

        public static LSProjectile Create(string projCode, Vector3d position, Func<LSAgent, bool> agentConditional, Func<byte, bool> bucketConditional, Action<LSAgent> onHit)
        {
            var curProj = RawCreate(projCode);

            int id = ProjectileBucket.Add(curProj);
            if (curProj.IsNotNull())
            {
                curProj.Prepare(id, position, agentConditional, bucketConditional, onHit, true);
            }

            return curProj;
        }

        private static LSProjectile RawCreate(string projCode)
        {
            if (!ProjectilePool.ContainsKey(projCode))
            {
                Debug.Log(projCode + " fired by " + Attack.LastAttack + " Caused boom");
                return null;
            }

            FastStack<LSProjectile> pool = ProjectilePool[projCode];
            LSProjectile curProj;
            if (pool.Count > 0)
            {
                curProj = pool.Pop();
            }
            else
            {
                curProj = NewProjectile(projCode);
            }

            return curProj;
        }

        public static void Fire(LSProjectile projectile)
        {
            if (projectile.IsNotNull())
            {
                projectile.LateInit();
            }
        }

        /// <summary>
        /// Non-deterministic
        /// </summary>
        /// <returns>The create and fire.</returns>
        /// <param name="curProj">Current proj.</param>
        /// <param name="projCode">Proj code.</param>
        /// <param name="position">Position.</param>
        /// <param name="direction">Direction.</param>
        /// <param name="gravity">If set to <c>true</c> gravity.</param>
        public static LSProjectile NDCreateAndFire(string projCode, Vector3d position, Vector3d direction, bool gravity = false)
        {
            LSProjectile curProj = RawCreate(projCode);
            int id = NDProjectileBucket.Add(curProj);
            curProj.Prepare(id, position, (a) => false, (a) => false, (a) => { }, false);
            curProj.InitializeFree(direction, (a) => false, gravity);
            Fire(curProj);
            return curProj;
        }

        public static void EndProjectile(LSProjectile projectile)
        {
            if (projectile.Deterministic)
            {
                int id = projectile.ID;
                if (!ProjectileBucket.SafeRemoveAt(id, projectile))
                {
                    Debug.Log("BOO! This is a terrible bug.");
                }
            }
            else
            {
                if (!NDProjectileBucket.SafeRemoveAt(projectile.ID, projectile))
                {
                    Debug.Log("BOO! This is a terrible bug.");
                }
            }
            CacheProjectile(projectile);
            projectile.Deactivate();
        }

        #region ID and allocation management
        private static void CacheProjectile(LSProjectile projectile)
        {
            ProjectilePool[projectile.MyProjCode].Add(projectile);
            /*if (projectile.ID == PeakCount - 1)
			{
				PeakCount--;
				for (int i = projectile.ID - 1; i >= 0; i--)
				{
					if (ProjectileActive[i] == false)
					{
						PeakCount--;
					}
					else {
						break;
					}
				}
			}*/
        }
        #endregion
    }
}