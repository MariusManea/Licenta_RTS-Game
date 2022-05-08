using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using Newtonsoft.Json;
public class WorldObjects : MonoBehaviour
{
    public virtual bool IsActive { get { return true; } }

    public string objectName;
    public Texture2D buildImage;
    public int hitPoints, maxHitPoints;

    protected bool canBePlaced;
    protected Player player;
    protected string[] actions = { };
    protected string[] potentialActions = { };
    protected bool currentlySelected = false;
    protected Bounds selectionBounds;
    protected Rect playingArea = new Rect(0.0f, 0.0f, 0.0f, 0.0f);

    protected GUIStyle healthStyle = new GUIStyle();
    protected float healthPercentage = 1.0f;

    private List<Material[]> oldMaterials = new List<Material[]>();

    protected WorldObjects target = null;
    protected bool attacking = false;
    public float weaponRange = 10.0f;
    protected bool aiming = false;
    public float weaponRechargeTime = 1.0f;
    private float currentWeaponChargeTime;

    protected bool movingIntoPosition = false;

    protected bool loadedSavedValues = false;
    private int loadedTargetId = -1;

    public AudioClip attackSound, selectSound, useWeaponSound;
    public float attackVolume = 1.0f, selectVolume = 1.0f, useWeaponVolume = 1.0f;

    private float timeSinceLastDecision = 0.0f, timeBetweenDecisions = 0.1f;
    private float timeSinceLastForcedDecision = 0.0f, timeBetweenForcedDecisions = 2f;
    public float detectionRange = 20.0f;
    protected List<WorldObjects> nearbyObjects;

    protected Terrain terrain;


    protected AudioElement audioElement;

    public int ObjectId { get; set; }
    private Vector3 oldAttackPosition;

    public List<WorldObjects> enemyObjects;

    protected TrainSceneManager trainManager;
    protected virtual void Awake()
    {
        trainManager = GetComponentInParent<TrainSceneManager>();
        if (trainManager)
        {
            terrain = trainManager.terrain;
        } 
        else
        {
            Ground ground = (Ground)GameObject.FindObjectOfType(typeof(Ground));
            terrain = ground.GetComponentInChildren<Terrain>();

        }
       
        selectionBounds = ResourceManager.InvalidBounds;
        CalculateBounds();
    }

    protected virtual void Start()
    {
        SetPlayer();
        timeSinceLastDecision = -2;
        if (player)
        {
            if (loadedSavedValues)
            {
                if (loadedTargetId >= 0) target = player.GetObjectForId(loadedTargetId);
            }
            else
            {
                SetTeamColor();
            }
        }
        InitialiseAudio();
        this.transform.position = new Vector3(this.transform.position.x, terrain.SampleHeight(this.transform.position), this.transform.position.z);
        CalculateBounds();
    }

    protected virtual void Update()
    {
        Vector3 worldObjectPosition = this.transform.position;
        if (trainManager)
        {
            worldObjectPosition -= trainManager.transform.position;
        }

        float y = terrain.SampleHeight(worldObjectPosition);
        if (gameObject.GetComponent<Ship>())
        {
            y = y > 7 ? y : 7;
        }
        this.transform.position = new Vector3(this.transform.position.x, y, this.transform.position.z);
        if (ShouldMakeDecision()) DecideWhatToDo();
        currentWeaponChargeTime += Time.deltaTime;
        if (attacking && !movingIntoPosition && !aiming) PerformAttack();
    }

    protected virtual void OnGUI()
    {
        if (currentlySelected && !ResourceManager.MenuOpen) DrawSelection();
    }

    public bool IsCurrentlySelected()
    {
        return currentlySelected;
    }

    protected virtual bool ShouldMakeDecision()
    {
        if (!attacking && !movingIntoPosition && !aiming)
        {
            //we are not doing anything at the moment
            if (timeSinceLastDecision > timeBetweenDecisions)
            {
                timeSinceLastDecision = 0.0f;
                return true;
            }
            timeSinceLastDecision += Time.deltaTime;
        }
        timeSinceLastForcedDecision += Time.deltaTime;
        if (timeBetweenForcedDecisions < timeSinceLastForcedDecision)
        {
            timeSinceLastForcedDecision = 0.0f;
            nearbyObjects = WorkManager.FindNearbyObjects(transform.position, detectionRange);

        }
        return false;
    }

    protected virtual void DecideWhatToDo()
    {
        //determine what should be done by the world object at the current point in time
        Vector3 currentPosition = transform.position;
        nearbyObjects = WorkManager.FindNearbyObjects(currentPosition, detectionRange);

        if (CanAttack())
        {
            enemyObjects = new List<WorldObjects>();
            foreach (WorldObjects nearbyObject in nearbyObjects)
            {
                Resource resource = nearbyObject.GetComponent<Resource>();
                if (resource) continue;
                Building building = nearbyObject.GetComponent<Building>();
                if (building && building.Ghost) continue;
                if (nearbyObject.GetPlayer() != player) enemyObjects.Add(nearbyObject);
            }
            WorldObjects closestObject = WorkManager.FindNearestWorldObjectInListToPosition(enemyObjects, currentPosition);
            if (closestObject) BeginAttack(closestObject);
        }
    }

    protected virtual void InitialiseAudio()
    {
        List<AudioClip> sounds = new List<AudioClip>();
        List<float> volumes = new List<float>();
        if (attackVolume < 0.0f) attackVolume = 0.0f;
        if (attackVolume > 1.0f) attackVolume = 1.0f;
        sounds.Add(attackSound);
        volumes.Add(attackVolume);
        if (selectVolume < 0.0f) selectVolume = 0.0f;
        if (selectVolume > 1.0f) selectVolume = 1.0f;
        sounds.Add(selectSound);
        volumes.Add(selectVolume);
        if (useWeaponVolume < 0.0f) useWeaponVolume = 0.0f;
        if (useWeaponVolume > 1.0f) useWeaponVolume = 1.0f;
        sounds.Add(useWeaponSound);
        volumes.Add(useWeaponVolume);
        audioElement = new AudioElement(sounds, volumes, objectName + ObjectId, this.transform);
    }

    public void SetPlayer()
    {
        player = GetComponentInParent<Player>();
    }
    public Player GetPlayer()
    {
        return player;
    }

    public virtual bool IsCargo()
    {
        return false;
    }

    public virtual void InitiateUnload()
    {

    }
    public virtual void CancelUnload()
    {

    }        

    public void SetTeamColor()
    {
        TeamColor[] teamColors = GetComponentsInChildren<TeamColor>();
        foreach (TeamColor teamColor in teamColors) teamColor.GetComponent<Renderer>().materials[teamColor.matIndex].color = player.teamColor;
    }

    protected virtual void DrawSelectionBox(Rect selectBox)
    {
        GUI.Box(selectBox, "");
        CalculateCurrentHealth(0.35f, 0.65f);
        DrawHealthBar(selectBox, "");
    }

    private void DrawSelection()
    {
        GUI.skin = ResourceManager.SelectBoxSkin;
        Rect selectBox = WorkManager.CalculateSelectionBox(selectionBounds, playingArea);
        //Draw the selection box around the currently selected object, within the bounds of the playing area
        GUI.BeginGroup(playingArea);
        DrawSelectionBox(selectBox);
        GUI.EndGroup();
    }

    public virtual void SetSelection(bool selected, Rect playingArea)
    {
        currentlySelected = selected;
        if (selected)
        {
            this.playingArea = playingArea;
            if (audioElement != null) audioElement.Play(selectSound);
        }
    }

    public Bounds GetSelectionBounds()
    {
        return selectionBounds;
    }

    public string[] GetActions()
    {
        return actions;
    }

    public string[] GetPotentialActions()
    {
        return potentialActions;
    }

    public bool HasActions()
    {
        return (actions != null) && (actions.Length > 0);
    }

    public virtual void PerformAction(string actionToPerform)
    {
        //it is up to children with specific actions to determine what to do with each of those actions
    }

    public virtual void MouseClick(GameObject hitObject, Vector3 hitPoint, Player controller)
    {
        //only handle input if currently selected
        if (currentlySelected && hitObject && !WorkManager.ObjectIsGround(hitObject))
        {
            WorldObjects worldObject = hitObject.transform.parent.GetComponent<WorldObjects>();
            //clicked on another selectable object
            if (worldObject)
            {
                Resource resource = hitObject.transform.parent.GetComponent<Resource>();
                if (resource && resource.isEmpty()) return;
                Player owner = hitObject.transform.root.GetComponent<Player>();
                if (owner)
                { //the object is controlled by a player
                    if (player && player.isHuman)
                    { //this object is controlled by a human player
                      //start attack if object is not owned by the same player and this object can attack, else select
                        if (player.userName != owner.userName && CanAttack())
                        {
                            BeginAttack(worldObject);
                        }
                        else
                        {
                            if (!CanAttack())
                            {
                                bool foundAnAttacker = false;
                                foreach (WorldObjects selWO in player.SelectedObjects)
                                {
                                    if (selWO.CanAttack())
                                    {
                                        foundAnAttacker = true;
                                        break;
                                    }
                                }
                                if (!foundAnAttacker)
                                {
                                    ChangeSelection(worldObject, controller);
                                }
                            }
                            else
                            {
                                ChangeSelection(worldObject, controller);
                            }
                        }
                    }
                    else ChangeSelection(worldObject, controller);
                }
                else ChangeSelection(worldObject, controller);
            }
        }
    }

    public virtual void BeginAttack(WorldObjects target)
    {
        if (audioElement != null) audioElement.Play(attackSound);
        this.target = target;
        if (TargetInRange())
        {
            attacking = true;
            PerformAttack();
        }
        else AdjustPosition();
    }

    private bool TargetInRange()
    {
        Vector3 targetLocation = target.transform.position;
        Vector3 direction = targetLocation - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude < weaponRange * weaponRange)
        {
            return true;
        }
        return false;
    }

    private void AdjustPosition()
    {
        Unit self = this as Unit;
        if (self)
        {
            movingIntoPosition = true;
            Vector3 attackPosition = FindNearestAttackPosition();
            if (attackPosition != oldAttackPosition)
            {
                self.StartMove(attackPosition);
                attacking = true;
            }
            oldAttackPosition = attackPosition;
        }
        else attacking = false;
    }

    private Vector3 FindNearestAttackPosition()
    {
        Vector3 targetLocation = target.transform.position;
        Vector3 direction = targetLocation - transform.position;
        float targetDistance = direction.magnitude;
        float distanceToTravel = targetDistance - (0.8f * weaponRange);
        Vector3 attackPosition = Vector3.Lerp(transform.position, targetLocation, distanceToTravel / targetDistance);
        return ((Unit)this).graph.GetNearest(attackPosition).position;
    }

    private void PerformAttack()
    {
        if (!target)
        {
            attacking = false;
            return;
        }
        if (!TargetInRange()) AdjustPosition();
        else if (!TargetInFrontOfWeapon()) AimAtTarget();
        else if (ReadyToFire()) UseWeapon();
    }

    private bool TargetInFrontOfWeapon()
    {
        Vector3 targetLocation = target.transform.position;
        Vector3 direction = targetLocation - transform.position;
        if (direction.normalized == transform.forward.normalized) return true;
        else return false;
    }

    protected virtual void AimAtTarget()
    {
        aiming = true;
        //this behaviour needs to be specified by a specific object
    }

    private bool ReadyToFire()
    {
        if (currentWeaponChargeTime >= weaponRechargeTime) return true;
        return false;
    }

    protected virtual void UseWeapon()
    {
        if (audioElement != null && Time.timeScale > 0) { audioElement.Stop(useWeaponSound); audioElement.Play(useWeaponSound); }
        currentWeaponChargeTime = 0.0f;
        if (!player.isHuman)
        {
            GetComponent<AgentRTS>().AddReward(0.5f);
        }
        //this behaviour needs to be specified by a specific object
    }

    protected void ChangeSelection(WorldObjects worldObject, Player controller)
    {
        //this should be called by the following line, but there is an outside chance it will not
        SetSelection(false, playingArea);
        if (controller.SelectedObjects != null)
        {
            foreach (WorldObjects selectedWorldObject in controller.SelectedObjects)
            {
                selectedWorldObject.SetSelection(false, playingArea);
            }
        }
        controller.SelectedObjects = null;
        controller.SelectedObjects = new List<WorldObjects>();
        controller.SelectedObjects.Add(worldObject);
        worldObject.SetSelection(true, controller.hud.GetPlayingArea());
    }

    public void CalculateBounds()
    {
        selectionBounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            selectionBounds.Encapsulate(r.bounds);
        }
    }

    public virtual void SetHoverState(GameObject hoverObject)
    {
        //only handle input if owned by a human player and currently selected
        if (player && player.isHuman && currentlySelected)
        {
            //something other than the ground is being hovered over
            if (!WorkManager.ObjectIsGround(hoverObject))
            {
                Player owner = hoverObject.transform.root.GetComponent<Player>();
                Unit unit = hoverObject.transform.parent.GetComponent<Unit>();
                Building building = hoverObject.transform.parent.GetComponent<Building>();
                if (owner)
                { //the object is owned by a player
                    if (owner.userName == player.userName) player.hud.SetCursorState(CursorState.Select);
                    else if (CanAttack()) player.hud.SetCursorState(CursorState.Attack);
                    else player.hud.SetCursorState(CursorState.Select);
                }
                else if (unit || building && CanAttack()) player.hud.SetCursorState(CursorState.Attack);
                else player.hud.SetCursorState(CursorState.Select);
            }
        }
    }

    public virtual bool CanAttack()
    {
        //default behaviour needs to be overidden by children
        return false;
    }

    public bool IsOwnedBy(Player owner)
    {
        if (player && player.Equals(owner))
        {
            return true;
        }
        else
        {
            return false;
        }
    }



    protected virtual void CalculateCurrentHealth(float lowSplit, float highSplit)
    {
        healthPercentage = (float)hitPoints / (float)maxHitPoints;
        if (healthPercentage > highSplit) healthStyle.normal.background = ResourceManager.HealthyTexture;
        else if (healthPercentage > lowSplit) healthStyle.normal.background = ResourceManager.DamagedTexture;
        else healthStyle.normal.background = ResourceManager.CriticalTexture;
    }

    protected void DrawHealthBar(Rect selectBox, string label)
    {
        healthStyle.padding.top = -20;
        healthStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(selectBox.x, selectBox.y - 7, selectBox.width * healthPercentage, 5), label, healthStyle);
    }

    public void SetColliders(bool enabled)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders) collider.enabled = enabled;
    }

    public void SetTransparentMaterial(Material material, bool storeExistingMaterial, bool placeable = false)
    {
        if (storeExistingMaterial) oldMaterials.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (storeExistingMaterial) oldMaterials.Add(renderer.materials);
            Material[] newMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < renderer.materials.Length; ++i)
                newMaterials[i] = material;
            renderer.materials = newMaterials;
        }
        canBePlaced = placeable;
    }

    public void RestoreMaterials()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (oldMaterials.Count == renderers.Length)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].materials = oldMaterials[i];
            }
        }
    }

    public void SetPlayingArea(Rect playingArea)
    {
        this.playingArea = playingArea;
    }

    public void TakeDamage(int damage)
    {
        hitPoints -= damage;
        if (hitPoints <= 0) Destroy(gameObject);
    }

    public virtual void SaveDetails(JsonWriter writer)
    {
        SaveManager.WriteString(writer, "Type", name);
        SaveManager.WriteString(writer, "Name", objectName);
        SaveManager.WriteInt(writer, "Id", ObjectId);
        SaveManager.WriteVector(writer, "Position", transform.position);
        SaveManager.WriteQuaternion(writer, "Rotation", transform.rotation);
        SaveManager.WriteVector(writer, "Scale", transform.localScale);
        SaveManager.WriteInt(writer, "HitPoints", hitPoints);
        SaveManager.WriteBoolean(writer, "Attacking", attacking);
        SaveManager.WriteBoolean(writer, "MovingIntoPosition", movingIntoPosition);
        SaveManager.WriteBoolean(writer, "Aiming", aiming);
        if (attacking)
        {
            //only save if attacking so that we do not end up storing massive numbers for no reason
            SaveManager.WriteFloat(writer, "CurrentWeaponChargeTime", currentWeaponChargeTime);
        }
        if (target != null) SaveManager.WriteInt(writer, "TargetId", target.ObjectId);
    }

    public void LoadDetails(JsonTextReader reader)
    {
        while (reader.Read())
        {
            if (reader.Value != null)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = (string)reader.Value;
                    reader.Read();
                    HandleLoadedProperty(reader, propertyName, reader.Value);
                }
            }
            else if (reader.TokenType == JsonToken.EndObject)
            {
                //loaded position invalidates the selection bounds so they must be recalculated
                selectionBounds = ResourceManager.InvalidBounds;
                CalculateBounds();
                loadedSavedValues = true;
                return;
            }
        }
        //loaded position invalidates the selection bounds so they must be recalculated
        selectionBounds = ResourceManager.InvalidBounds;
        CalculateBounds();
        loadedSavedValues = true;
    }

    protected virtual void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        switch (propertyName)
        {
            case "Name": objectName = (string)readValue; break;
            case "Id": ObjectId = (int)(System.Int64)readValue; break;
            case "Position": transform.localPosition = LoadManager.LoadVector(reader); break;
            case "Rotation": transform.localRotation = LoadManager.LoadQuaternion(reader); break;
            case "Scale": transform.localScale = LoadManager.LoadVector(reader); break;
            case "HitPoints": hitPoints = (int)(System.Int64)readValue; break;
            case "Attacking": attacking = (bool)readValue; break;
            case "MovingIntoPosition": movingIntoPosition = (bool)readValue; break;
            case "Aiming": aiming = (bool)readValue; break;
            case "CurrentWeaponChargeTime": currentWeaponChargeTime = (float)(double)readValue; break;
            case "TargetId": loadedTargetId = (int)(System.Int64)readValue; break;
            default: break;
        }
    }

    public virtual string GetObjectName()
    {
        return objectName;
    }

    public List<WorldObjects> GetNearbyObjects()
    {
        return nearbyObjects;
    }

    public List<WorldObjects> SetNearbyObjects() {
        nearbyObjects = WorkManager.FindNearbyObjects(transform.position, detectionRange);
        return nearbyObjects;
    }
}
