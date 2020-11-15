﻿using Newtonsoft.Json;
using RTSLockstep.Agents;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.Determinism;
using RTSLockstep.Grouping;
using RTSLockstep.Managers;
using RTSLockstep.Managers.GameState;
using RTSLockstep.Player.Commands;
using RTSLockstep.Projectiles;
using RTSLockstep.LSResources;
using System;
using UnityEngine;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;
using RTSLockstep.Integration;

namespace RTSLockstep.Abilities.Essential
{
    [DisallowMultipleComponent]
    public class Attack : ActiveAbility
    {
        #region Properties
        public const long MissModifier = FixedMath.One / 2;

        public AttackGroup MyAttackGroup;
        [HideInInspector]
        public int MyAttackGroupID;

        public bool IsAttackMoving { get; private set; }

        public LSAgent CurrentTarget { get; private set; }

        public Vector2d Destination { get; private set; }

        public virtual Vector3d[] ProjectileOffsets
        {
            get
            {
                if (cachedProjectileOffsets == null)
                {
                    cachedProjectileOffsets = new Vector3d[_secondaryProjectileOffsets.Length + 1];
                    cachedProjectileOffsets[0] = _projectileOffset;
                    for (int i = 0; i < _secondaryProjectileOffsets.Length; i++)
                    {
                        cachedProjectileOffsets[i + 1] = _secondaryProjectileOffsets[i];
                    }
                }
                return cachedProjectileOffsets;
            }
        }

        /// <summary>
        /// The projectile to be fired in OnFire.
        /// </summary>
        /// <value>The current projectile.</value>
        public int CycleCount { get; private set; }

        public static Attack LastAttack;

        public event Action<LSAgent, bool> ExtraOnHit;
        public event Action OnStopAttack;

        private Vector3d[] cachedProjectileOffsets;
        private Health cachedTargetHealth;

        //Stuff for the logic
        private bool inRange;
        private long fastMag;
        private Vector2d targetDirection;
        private long fastRangeToTarget;

        private int basePriority;
        private uint targetVersion;
        private long attackCount;

        private int loadedTargetId = -1;

        [Lockstep(true)]
        private bool IsWindingUp { get; set; }
        private long windupCount;

        private Action<LSAgent> CachedOnHit;

        protected virtual AnimState EngagingAnimState
        {
            get { return AnimState.Engaging; }
        }
        protected virtual AnimImpulse AttackAnimImpulse
        {
            get { return AnimImpulse.Attack; }
        }

        #region variables for quick fix for repathing to target's new position
        const long repathDistance = FixedMath.One * 2;
        FrameTimer repathTimer = new FrameTimer();
        const int repathInterval = LockstepManager.FrameRate * 2;
        int repathRandom;
        #endregion

        #region Serialized Values (Further description in properties)
        [SerializeField]
        protected bool _isOffensive;
        public virtual bool IsOffensive { get { return _isOffensive; } }
        [SerializeField, DataCode("Projectiles")]
        protected string _projectileCode;

        [FixedNumber, SerializeField, Tooltip("Damage of attack")]
        protected long _damage = FixedMath.One;
        public virtual long Damage { get { return _damage; } }
        [SerializeField, FixedNumber, Tooltip("Frames between each attack")]
        protected long _attackSpeed = 1 * FixedMath.One;
        public virtual long AttackSpeed { get { return _attackSpeed; } }
        // Allegiance of the target
        [SerializeField, EnumMask]
        protected AllegianceType _targetAllegiance = AllegianceType.Enemy;

        [SerializeField, Tooltip("Whether or not to require the unit to face the target for attacking")]
        protected bool _trackAttackAngle = true;
        public virtual bool TrackAttackAngle { get { return _trackAttackAngle; } }
        [FixedNumberAngle, SerializeField, Tooltip("The angle in front of the unit that the target must be located in")]
        protected long _attackAngle = FixedMath.TenDegrees;
        public long AttackAngle { get { return _attackAngle; } }

        [SerializeField, Tooltip("Important: With Vector3d, the Z axis represents height!")]
        protected Vector3d _projectileOffset;
        [SerializeField]
        protected Vector3d[] _secondaryProjectileOffsets;
        [SerializeField]
        private bool _cycleProjectiles;
        [SerializeField, FixedNumber]
        protected long _windup = 0;
        [SerializeField]
        protected bool _increasePriority = true;
        #endregion
        #endregion Properties

        protected override void OnSetup()
        {
            basePriority = Agent.Body.Priority;
        }

        protected override void OnInitialize()
        {
            attackCount = 0;

            IsAttackMoving = false;

            MyAttackGroup = null;
            MyAttackGroupID = -1;

            CurrentTarget = null;

            inRange = false;
            IsFocused = false;

            if (Agent.MyStats.CanMove)
            {
                Agent.MyStats.CachedMove.OnArrive += HandleOnArrive;
            }

            CycleCount = 0;

            repathTimer.Reset(repathInterval);
            repathRandom = LSUtility.GetRandom(repathInterval);

            //caching parameters
            uint spawnVersion = Agent.SpawnVersion;
            LocalAgentController controller = Agent.Controller;
            CachedOnHit = (target) => OnHitTarget(target, spawnVersion, controller);

            if (Agent.GetControllingPlayer() && loadedSavedValues && loadedTargetId >= 0)
            {
                LSAgent obj = Agent.GetControllingPlayer().GetObjectForId(loadedTargetId);
                if (obj.MyAgentType == AgentType.Unit || obj.MyAgentType == AgentType.Structure)
                {
                    CurrentTarget = obj;
                }
            }
        }

        protected override void OnSimulate()
        {
            if (Agent.Tag == AgentTag.Offensive)
            {
                if (attackCount > _attackSpeed)
                {
                    //reset attackCount overcharge if left idle
                    attackCount = _attackSpeed;
                }
                else if (attackCount < _attackSpeed)
                {
                    //charge up attack
                    attackCount += LockstepManager.DeltaTime;
                }

                if (Agent && Agent.IsActive)
                {
                    if ((IsFocused || IsAttackMoving))
                    {
                        BehaveWithTarget();
                    }
                }

                if (Agent.MyStats.CanMove && IsAttackMoving)
                {
                    Agent.MyStats.CachedMove.StartLookingForStopPause();
                }
            }
        }

        protected override void OnExecute(Command com)
        {
            Agent.StopCast(ID);
            IsCasting = true;
            RegisterAttackGroup();
        }

        protected virtual void OnStartAttackMove()
        {
            cachedTargetHealth = CurrentTarget.GetAbility<Health>();

            if (Agent.MyStats.CanMove
                && cachedTargetHealth.IsNotNull()
                && !CheckRange())
            {
                IsAttackMoving = true;
                IsFocused = false;

                Agent.MyStats.CachedMove.StartMove(Destination);
            }
        }

        protected virtual void OnStartWindup()
        {

        }

        protected virtual void OnAttack(LSAgent target)
        {
            if (_cycleProjectiles)
            {
                CycleCount++;
                if (CycleCount >= ProjectileOffsets.Length)
                {
                    CycleCount = 0;
                }

                FullFireProjectile(_projectileCode, ProjectileOffsets[CycleCount], target);
            }
            else
            {
                for (int i = 0; i < ProjectileOffsets.Length; i++)
                {
                    FullFireProjectile(_projectileCode, ProjectileOffsets[i], target);
                }
            }
        }

        protected virtual void OnPrepareProjectile(LSProjectile projectile)
        {

        }

        protected virtual void OnHitTarget(LSAgent target, uint agentVersion, LocalAgentController controller)
        {
            // If the shooter died, certain effects or records can't be completed
            bool isCurrent = Agent.IsNotNull() && agentVersion == Agent.SpawnVersion;
            Health healther = target.GetAbility<Health>();
            AttackerInfo info = new AttackerInfo(isCurrent ? Agent : null, controller);
            healther.TakeDamage(_damage, info);
            CallExtraOnHit(target, isCurrent);
        }

        protected override void OnDeactivate()
        {
            StopAttack(true);
        }

        protected sealed override void OnStopCast()
        {
            StopAttack(true);
        }

        protected virtual bool HardAgentConditional()
        {
            Health health = CurrentTarget.GetAbility<Health>();
            if (health != null)
            {
                if (_damage >= 0)
                {
                    return health.CanLose;
                }
                else
                {
                    return health.CanGain;
                }
            }

            return true;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteUInt(writer, "TargetVersion", targetVersion);
            SaveManager.WriteBoolean(writer, "AttackMoving", IsAttackMoving);
            if (CurrentTarget)
            {
                SaveManager.WriteInt(writer, "TargetID", CurrentTarget.GlobalID);
            }

            SaveManager.WriteBoolean(writer, "Focused", IsFocused);
            SaveManager.WriteBoolean(writer, "InRange", inRange);
            SaveManager.WriteLong(writer, "AttackCount", attackCount);
            SaveManager.WriteLong(writer, "FastRangeToTarget", fastRangeToTarget);
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "TargetVersion":
                    targetVersion = (uint)readValue;
                    break;
                case "AttackMoving":
                    IsAttackMoving = (bool)readValue;
                    break;
                case "TargetID":
                    loadedTargetId = (int)(long)readValue;
                    break;
                case "Focused":
                    IsFocused = (bool)readValue;
                    break;
                case "InRange":
                    inRange = (bool)readValue;
                    break;
                case "AttackCount":
                    attackCount = (long)readValue;
                    break;
                case "FastRangeToTarget":
                    fastRangeToTarget = (long)readValue;
                    break;
                default: break;
            }
        }

        public void OnAttackGroupProcessed(LSAgent currentTarget)
        {
            Agent.Tag = AgentTag.Offensive;

            if (currentTarget.IsNotNull())
            {
                CurrentTarget = currentTarget;
                Destination = CurrentTarget.Body.Position;

                IsFocused = true;
                IsAttackMoving = false;

                targetVersion = CurrentTarget.SpawnVersion;

                fastRangeToTarget = Agent.MyStats.ActionRange + (CurrentTarget.Body.IsNotNull() ? CurrentTarget.Body.Radius : 0) + Agent.Body.Radius;
                fastRangeToTarget *= fastRangeToTarget;

                OnStartAttackMove();
            }
            else
            {
                StopAttack();
            }
        }

        private void RegisterAttackGroup()
        {
            if (AttackGroupHelper.CheckValidAndAlert())
            {
                AttackGroupHelper.LastCreatedGroup.Add(this);
            }
        }

        private void HandleOnArrive()
        {
            if (IsAttackMoving)
            {
                IsFocused = true;
                IsAttackMoving = false;
            }
        }

        private void BehaveWithTarget()
        {
            if (!CurrentTarget.IsActive
                || CurrentTarget.SpawnVersion != targetVersion
                || (_targetAllegiance & Agent.GetAllegiance(CurrentTarget)) == 0
                || CurrentTarget.MyStats.CachedHealth.CurrentHealth == 0)
            {
                // Target's lifecycle has ended
                StopAttack();
            }
            else
            {
                if (!IsWindingUp)
                {
                    if (CheckRange())
                    {
                        if (!inRange)
                        {
                            if (Agent.MyStats.CanMove)
                            {
                                Agent.MyStats.CachedMove.Arrive();
                            }

                            inRange = true;
                        }
                        Agent.Animator.SetState(EngagingAnimState);

                        targetDirection.Normalize(out long mag);
                        bool withinTurn = _trackAttackAngle == false ||
                                          (fastMag != 0 &&
                                          Agent.Body.Forward.Dot(targetDirection.x, targetDirection.y) > 0
                                          && Agent.Body.Forward.Cross(targetDirection.x, targetDirection.y).Abs() <= AttackAngle);
                        bool needTurn = mag != 0 && !withinTurn;
                        if (needTurn && Agent.MyStats.CanTurn)
                        {
                            Agent.MyStats.CachedTurn.StartTurnDirection(targetDirection);
                        }
                        else if (attackCount >= _attackSpeed)
                        {
                            StartWindup();
                        }
                    }
                    else if (Agent.MyStats.CanMove)
                    {
                        bool needsRepath = false;
                        if (!Agent.MyStats.CachedMove.IsMoving
                            && !Agent.MyStats.CachedMove.MoveOnGroupProcessed)
                        {
                            if (Agent.MyStats.CachedMove.IsStuck)
                            {
                                StopAttack();
                            }
                            else
                            {
                                needsRepath = true;
                            }

                            Agent.Body.Priority = basePriority;
                        }
                        else if (!inRange && repathTimer.AdvanceFrame())
                        {
                            if (CurrentTarget.Body.PositionChangedBuffer &&
                                CurrentTarget.Body.Position.FastDistance(Agent.MyStats.CachedMove.Destination.x, Agent.MyStats.CachedMove.Destination.y) >= (repathDistance * repathDistance))
                            {
                                needsRepath = true;
                                //So units don't sync up and path on the same frame
                                repathTimer.AdvanceFrames(repathRandom);
                            }
                        }

                        if (needsRepath)
                        {
                            Agent.MyStats.CachedMove.Destination = CurrentTarget.Body.Position;
                            Agent.MyStats.CachedMove.PauseAutoStop();
                            Agent.MyStats.CachedMove.PauseCollisionStop();
                            OnStartAttackMove();
                        }
                    }

                    if (inRange)
                    {
                        inRange = false;
                    }
                }

                if (IsWindingUp)
                {
                    //TODO: Do we need AgentConditional checks here?
                    windupCount += LockstepManager.DeltaTime;
                    if (Agent.MyStats.CanTurn)
                    {
                        Vector2d targetVector = CurrentTarget.Body.Position - Agent.Body.Position;
                        Agent.MyStats.CachedTurn.StartTurnVector(targetVector);
                    }

                    if (windupCount >= _windup)
                    {
                        windupCount = 0;
                        StartAttack();
                        while (attackCount >= _attackSpeed)
                        {
                            //resetting back down after attack is fired
                            attackCount -= _attackSpeed;
                        }
                        attackCount += _windup;
                        IsWindingUp = false;
                    }
                }
                else
                {
                    windupCount = 0;
                }

                if (Agent.MyStats.CanMove && inRange)
                {
                    Agent.MyStats.CachedMove.PauseAutoStop();
                    Agent.MyStats.CachedMove.PauseCollisionStop();
                }
            }
        }

        private bool CheckRange()
        {
            targetDirection = CurrentTarget.Body.Position - Agent.Body.Position;
            fastMag = targetDirection.FastMagnitude();

            return fastMag <= fastRangeToTarget;
        }

        private void StartWindup()
        {
            windupCount = 0;
            IsWindingUp = true;
            Agent.ApplyImpulse(AttackAnimImpulse);
            OnStartWindup();
        }

        private void CallExtraOnHit(LSAgent agent, bool isCurrent)
        {
            ExtraOnHit?.Invoke(agent, isCurrent);
        }

        private void StartAttack()
        {
            if (Agent.MyStats.CanMove)
            {
                // we don't want to be able to fire and move!
                IsAttackMoving = false;
                Agent.MyStats.CachedMove.StopMove();
            }
            Agent.Body.Priority = _increasePriority ? basePriority + 1 : basePriority;

            OnAttack(CurrentTarget);
        }

        private LSProjectile FullFireProjectile(string projectileCode, Vector3d projOffset, LSAgent target)
        {
            LSProjectile proj = PrepareProjectile(projectileCode, projOffset, target);
            FireProjectile(proj);
            return proj;
        }

        private LSProjectile PrepareProjectile(string projectileCode, Vector3d projOffset, LSAgent target)
        {
            LastAttack = this;
            LSProjectile currentProjectile = ProjectileManager.Create(
                                                 projectileCode,
                                                 Agent,
                                                 projOffset,
                                                 _targetAllegiance,
                                                 (other) =>
                                                 {
                                                     Health healther = other.GetAbility<Health>();
                                                     return healther.IsNotNull() && healther.CurrentHealth > 0;

                                                 },
                                                 CachedOnHit);

            switch (currentProjectile.TargetingBehavior)
            {
                case TargetingType.Homing:
                    currentProjectile.InitializeHoming(target);
                    break;
                case TargetingType.Timed:
                    currentProjectile.InitializeTimed(target);
                    break;
                case TargetingType.Positional:
                    currentProjectile.InitializePositional(target.Body.Position.ToVector3d(target.Body.HeightPos));
                    break;
                case TargetingType.Directional:
                    //TODO
                    throw new Exception("Not implemented yet.");
                    //break;
            }
            OnPrepareProjectile(currentProjectile);

            return currentProjectile;
        }

        private LSProjectile PrepareProjectile(string projectileCode, Vector3d projOffset, Vector3d targetPos)
        {
            LSProjectile currentProjectile = ProjectileManager.Create(
                                                 projectileCode,
                                                 Agent,
                                                 projOffset,
                                                 _targetAllegiance,
                                                 (other) =>
                                                 {
                                                     Health healther = other.GetAbility<Health>();
                                                     return healther.IsNotNull() && healther.CurrentHealth > 0;

                                                 },
                                                 CachedOnHit);

            switch (currentProjectile.TargetingBehavior)
            {
                case TargetingType.Timed:
                    currentProjectile.InitializeTimed(targetPos);
                    break;
                case TargetingType.Positional:
                    currentProjectile.InitializePositional(targetPos);
                    break;
                case TargetingType.Directional:
                    //TODO
                    throw new Exception("Not implemented yet.");
                    //break;
            }

            return currentProjectile;
        }

        private void FireProjectile(LSProjectile projectile)
        {
            ProjectileManager.Fire(projectile);
        }

        private void StopAttack(bool complete = false)
        {
            inRange = false;
            IsWindingUp = false;
            IsFocused = false;

            if (MyAttackGroup.IsNotNull())
            {
                MyAttackGroup.Remove(this);
            }

            IsAttackMoving = false;

            if (complete)
            {
                Agent.Tag = AgentTag.None;
            }
            else if (CurrentTarget.IsNotNull())
            {
                if (Agent.MyStats.CanMove && !inRange)
                {
                    Agent.MyStats.CachedMove.StopMove();
                }
            }

            CurrentTarget = null;

            IsCasting = false;

            Agent.Body.Priority = basePriority;

            OnStopAttack?.Invoke();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Agent.IsNull() || !Agent.IsActive)
            {
                return;
            }

            if (Agent.Body.IsNull())
            {
                Debug.Log(Agent.gameObject);
            }

            Gizmos.DrawWireSphere(Application.isPlaying ? Agent.Body._visualPosition : transform.position, Agent.MyStats.ActionRange.ToFloat());
        }
#endif
    }
}