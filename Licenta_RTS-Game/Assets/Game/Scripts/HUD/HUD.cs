using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using System;

public class HUD : MonoBehaviour
{
    private const int ORDERS_BAR_WIDTH = 150;
    private const int RESOURCE_BAR_HEIGHT = 40;
    private const int SELECTION_NAME_HEIGHT = 15;
    private const int ICON_WIDTH = 32;
    private const int ICON_HEIGHT = 32;
    private const int TEXT_WIDTH = 128;
    private const int TEXT_HEIGHT = 32;
    private const int BUILD_IMAGE_WIDTH = 64;
    private const int BUILD_IMAGE_HEIGHT = 64;
    private const int BUTTON_SPACING = 7;
    private const int SCROLL_BAR_WIDTH = 22;
    private const int BUILD_IMAGE_PADDING = 8;

    private int leftPosSelected = 0;
    private int topPosSelected = 0;
    private int widthSelected = 0;
    private int heightSelected = 0;

    private int buildAreaHeight = 0;

    private Player player;
    private CursorState activeCursorState;
    private CursorState previousCursorState;
    private int currentFrame = 0;

    public GUISkin resourceSkin;
    public GUISkin ordersSkin;
    public GUISkin selectBoxSkin;
    public GUISkin mouseCursorSkin;
    public GUISkin multipleSelectionSkin;
    public GUISkin selectedBarSkin;

    public Texture2D activeCursor;
    public Texture2D selectCursor, leftCursor, rightCursor, upCursor, downCursor;
    public Texture2D[] moveCursors, attackCursors, harvestCursors;

    private Dictionary<ResourceType, int> resourceValues, resourceLimits;
    public Texture2D[] resources;
    private Dictionary<ResourceType, Texture2D> resourceImages;

    private WorldObjects lastSelection;
    private float sliderValue;
    public Texture2D buttonHover, buttonClick;
    public Texture2D buildFrame, buildMask;
    public Texture2D smallButtonHover, smallButtonClick;
    public Texture2D rallyPointCursor;
    public Texture2D centerCameraButton;


    public Texture2D healthy, damaged, critical;


    public Texture2D[] resourceHealthBars;

    public GUISkin playerDetailsSkin;

    public AudioClip clickSound;
    public float clickVolume = 1.0f;

    private AudioElement audioElement;

    private bool multipleSelectionActive = false;
    private Vector3 startScreenPos;

    private float multipleSelectionTimer = 0.1f;

    private List<WorldObjects> lastSelectedObjects;
    private Dictionary<Texture2D, int> uniqueSelectedUnits;


    // Start is called before the first frame update
    void Start()
    {
        buildAreaHeight = Screen.height - RESOURCE_BAR_HEIGHT - SELECTION_NAME_HEIGHT - 2 * BUTTON_SPACING;
        resourceValues = new Dictionary<ResourceType, int>();
        resourceLimits = new Dictionary<ResourceType, int>();
        resourceImages = new Dictionary<ResourceType, Texture2D>();
        for (int i = 0; i < resources.Length; i++)
        {
            switch (resources[i].name)
            {
                case "Money":
                    resourceImages.Add(ResourceType.Money, resources[i]);
                    resourceValues.Add(ResourceType.Money, 0);
                    resourceLimits.Add(ResourceType.Money, 0);
                    break;
                case "Power":
                    resourceImages.Add(ResourceType.Power, resources[i]);
                    resourceValues.Add(ResourceType.Power, 0);
                    resourceLimits.Add(ResourceType.Power, 0);
                    break;
                default: break;
            }
        }

        player = transform.parent.GetComponent<Player>();

        ResourceManager.StoreSelectBoxItems(selectBoxSkin, healthy, damaged, critical);

        SetCursorState(CursorState.Select);

        Dictionary<ResourceType, Texture2D> resourceHealthBarTextures = new Dictionary<ResourceType, Texture2D>();
        for (int i = 0; i < resourceHealthBars.Length; i++)
        {
            switch (resourceHealthBars[i].name)
            {
                case "orehealthbar":
                    resourceHealthBarTextures.Add(ResourceType.Ore, resourceHealthBars[i]);
                    break;
                default: break;
            }
        }
        ResourceManager.SetResourceHealthBarTextures(resourceHealthBarTextures);
        List<AudioClip> sounds = new List<AudioClip>();
        List<float> volumes = new List<float>();
        sounds.Add(clickSound);
        volumes.Add(clickVolume);
        audioElement = new AudioElement(sounds, volumes, "HUD", null);
        UnityEngine.Random.InitState(System.DateTime.Now.Second);
    }

    void OnGUI()
    {
        if (player && player.isHuman)
        {
            DrawPlayerDetails();
            DrawOrdersBar();
            DrawResourceBar();
            DrawMouseCursor();
            DrawMultipleSelectionBox();
            DrawSelectedUnits();
        }
    }

    public class Comparer : IComparer<WorldObjects>
    {
        public int Compare(WorldObjects o1, WorldObjects o2)
        {
            return o1.name.CompareTo(o2.name);
        }
    }

    private void GetUniqueSelectedObjects()
    {
        uniqueSelectedUnits = new Dictionary<Texture2D, int>();
        lastSelectedObjects.Sort(new Comparer());
        foreach(WorldObjects selectedWorldObject in lastSelectedObjects)
        {
            if (selectedWorldObject.GetType().IsSubclassOf(typeof(Unit)) && selectedWorldObject.IsOwnedBy(player))
            {
                if (!uniqueSelectedUnits.ContainsKey(selectedWorldObject.buildImage))
                {
                    uniqueSelectedUnits.Add(selectedWorldObject.buildImage, 1);
                }
                else
                {
                    uniqueSelectedUnits[selectedWorldObject.buildImage]++;
                }
            }
        }
    }

    private void DrawSelectedUnits()
    {
        if (player && player.isHuman && player.SelectedObjects != null)
        {
            
            if (lastSelectedObjects != player.SelectedObjects) {
                lastSelectedObjects = player.SelectedObjects;
                GetUniqueSelectedObjects();
            }
            if (uniqueSelectedUnits.Count > 0)
            {
                GUI.skin = selectedBarSkin;
                int N = uniqueSelectedUnits.Count;
                float leftPos = Screen.width - ORDERS_BAR_WIDTH - N * ResourceManager.SelectedUnitButtonDimension - (N - 1) * ResourceManager.SelectedUnitButtonSpacing;
                float topPos = Screen.height - 3 / 2.0f * ResourceManager.SelectedUnitButtonDimension;
                float width = N * ResourceManager.SelectedUnitButtonDimension + (N - 1) * ResourceManager.SelectedUnitButtonSpacing;
                float height = ResourceManager.SelectedUnitButtonDimension;
                leftPosSelected = (int)(leftPos - ResourceManager.SelectedUnitButtonSpacing);
                topPosSelected = (int)(topPos - ResourceManager.SelectedUnitButtonSpacing);
                widthSelected = (int)(width + 2 * ResourceManager.SelectedUnitButtonSpacing);
                heightSelected = (int)(height + 2 * ResourceManager.SelectedUnitButtonSpacing);
                GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
                GUI.Box(new Rect(leftPosSelected, topPosSelected, widthSelected, heightSelected), "");
                foreach (Texture2D uniqueUnit in uniqueSelectedUnits.Keys)
                {
                    GUIContent content = new GUIContent(uniqueSelectedUnits[uniqueUnit].ToString(), uniqueUnit);
                    if (GUI.Button(new Rect(leftPos, topPos, ResourceManager.SelectedUnitButtonDimension, ResourceManager.SelectedUnitButtonDimension), content))
                    {
                        Event e = Event.current;
                        if (Event.current.button == 0)
                        {
                            List<WorldObjects> newList = new List<WorldObjects>();
                            foreach (WorldObjects selWorldObject in player.SelectedObjects)
                            {
                                if (selWorldObject.buildImage == uniqueUnit)
                                {
                                    newList.Add(selWorldObject);
                                }
                                else
                                {
                                    selWorldObject.SetSelection(false, GetPlayingArea());
                                }
                            }
                            player.SelectedObjects = newList;
                        }
                        if (Event.current.button == 1)
                        {
                            for (int i = 0; i < lastSelectedObjects.Count; i++)
                            {
                                WorldObjects selWorldObject = lastSelectedObjects[i];
                                if (selWorldObject.buildImage == uniqueUnit)
                                {
                                    selWorldObject.SetSelection(false, GetPlayingArea());
                                    lastSelectedObjects.Remove(selWorldObject);
                                    break;
                                }
                            }
                            if (lastSelectedObjects.Count == 0)
                            {
                                player.SelectedObjects = null;
                            }
                            else
                            {
                                player.SelectedObjects = new List<WorldObjects>(lastSelectedObjects);
                            }
                        }
                    }
                    leftPos += ResourceManager.SelectedUnitButtonDimension + ResourceManager.SelectedUnitButtonSpacing;
                }
                GUI.EndGroup();
            }
        }
        else
        {
            leftPosSelected = topPosSelected = widthSelected = heightSelected = 0;
        }
    }

    public void ActivateMultipleSelection()
    {
        multipleSelectionActive = true;
        startScreenPos = Input.mousePosition;
    }

    private void DrawMultipleSelectionBox()
    {
        if (player && player.isHuman)
        {
            if (Input.GetMouseButtonUp(0))
            {
                multipleSelectionActive = false;
            }
            if (multipleSelectionActive)
            {
                GUI.skin = multipleSelectionSkin;
                GUI.BeginGroup(GetPlayingArea());
                float leftPos = Mathf.Min(startScreenPos.x, Input.mousePosition.x) - ResourceManager.MultipleSelectionOffsetX;
                float topPos = Screen.height - Mathf.Max(startScreenPos.y, Input.mousePosition.y) - ResourceManager.MultipleSelectionOffsetY;
                float width = Mathf.Abs(startScreenPos.x - Input.mousePosition.x);
                float height = Mathf.Abs(startScreenPos.y - Input.mousePosition.y);
                GUI.Box(new Rect(leftPos, topPos, width, height), "");
                GUI.EndGroup();
                Bounds multipleSelectionBounds = new Bounds(new Vector3(leftPos + width / 2, topPos + height / 2, 0), new Vector3(width, height, 0));
                if (multipleSelectionTimer < 0)
                {
                    GetSelectedUnits(multipleSelectionBounds);
                    multipleSelectionTimer = 0.1f;
                }
                else
                {
                    multipleSelectionTimer -= Time.deltaTime;
                }
            }
        }
    }

    private void GetSelectedUnits(Bounds bounds)
    {
        Unit[] units = GameObject.FindObjectsOfType(typeof(Unit)) as Unit[];
        List<Unit> ownedUnits = new List<Unit>();
        foreach (Unit unit in units)
        {
            if (unit.IsOwnedBy(player))
            {
                ownedUnits.Add(unit);
            }
        }
        player.SelectedObjects = new List<WorldObjects>();
        foreach (Unit unit in ownedUnits)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
            screenPos.x -= ResourceManager.MultipleSelectionOffsetX;
            screenPos.y = Screen.height - screenPos.y - ResourceManager.MultipleSelectionOffsetY;
            screenPos.z = 0;
            if (bounds.Contains(screenPos))
            {
                // Unit is selected
                player.SelectedObjects.Add(unit);
                unit.SetSelection(true, GetPlayingArea());
            }
            else
            {
                unit.SetSelection(false, GetPlayingArea());
            }
        }
        if (player.SelectedObjects.Count == 0)
        {
            player.SelectedObjects = null;
        }
    }

    private void PlayClick()
    {
        if (audioElement != null) audioElement.Play(clickSound);
    }
    private void DrawPlayerDetails()
    {
        GUI.skin = playerDetailsSkin;
        GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
        float height = ResourceManager.TextHeight;
        float leftPos = ResourceManager.Padding;
        float topPos = Screen.height - height - ResourceManager.Padding;
        Texture2D avatar = PlayerManager.GetPlayerAvatar();
        if (avatar)
        {
            //we want the texture to be drawn square at all times
            GUI.DrawTexture(new Rect(leftPos, topPos, height, height), avatar);
            leftPos += height + ResourceManager.Padding;
        }
        float minWidth = 0, maxWidth = 0;
        string playerName = PlayerManager.GetPlayerName();
        playerDetailsSkin.GetStyle("label").CalcMinMaxWidth(new GUIContent(playerName), out minWidth, out maxWidth);
        GUI.Label(new Rect(leftPos, topPos, maxWidth, height), playerName);
        leftPos += maxWidth + ResourceManager.Padding;
        if (player != null && player.townCenter != null)
        {
            if ((player.townCenter.transform.position - Camera.main.transform.root.position).sqrMagnitude > 10000)
            {
                if (GUI.Button(new Rect(leftPos, topPos - (1.5f * ResourceManager.ButtonHeight - height) / 2, 1.5f * ResourceManager.ButtonHeight, 1.5f * ResourceManager.ButtonHeight), centerCameraButton))
                {
                    player.centerToBase = true;
                }
            }
        }
        GUI.EndGroup();
    }

    private WorldObjects WorldObjectWithActions(List<WorldObjects> selectedObjects)
    {
        foreach (WorldObjects selectedWorldObject in selectedObjects)
        {
            if (selectedWorldObject.HasActions())
            {
                return selectedWorldObject;
            }
        }
        return null;
    }

    private void DrawOrdersBar()
    {
        GUI.skin = ordersSkin;
        GUI.BeginGroup(new Rect(Screen.width - ORDERS_BAR_WIDTH - BUILD_IMAGE_WIDTH, RESOURCE_BAR_HEIGHT, ORDERS_BAR_WIDTH + BUILD_IMAGE_WIDTH, Screen.height - RESOURCE_BAR_HEIGHT));
        GUI.Box(new Rect(BUILD_IMAGE_WIDTH + SCROLL_BAR_WIDTH, 0, ORDERS_BAR_WIDTH, Screen.height - RESOURCE_BAR_HEIGHT), "");
        string selectionName = "";
        if (player.SelectedObjects != null)
        {
            WorldObjects selectedObjectWithActions = WorldObjectWithActions(player.SelectedObjects);
            if (selectedObjectWithActions != null)
            {
                selectionName = selectedObjectWithActions.objectName;
                if (selectedObjectWithActions.IsOwnedBy(player))
                {
                    //reset slider value if the selected object has changed
                    if (lastSelection && lastSelection != selectedObjectWithActions) sliderValue = 0.0f;
                    if (selectedObjectWithActions.IsActive) DrawActions(selectedObjectWithActions.GetActions());
                    //store the current selection
                    lastSelection = selectedObjectWithActions;
                    Building selectedBuilding = lastSelection.GetComponent<Building>();
                    if (selectedBuilding)
                    {
                        DrawBuildQueue(selectedBuilding.getBuildQueueValues(), selectedBuilding.getBuildPercentage());
                        DrawStandardBuildingOptions(selectedBuilding);
                    }
                }
            }
        }
        if (!selectionName.Equals(""))
        {
            int leftPos = BUILD_IMAGE_WIDTH + SCROLL_BAR_WIDTH / 2;
            int topPos = buildAreaHeight + BUTTON_SPACING;
            GUI.Label(new Rect(leftPos, topPos, ORDERS_BAR_WIDTH, SELECTION_NAME_HEIGHT), selectionName);
        }
        GUI.EndGroup();
    }

    private void DrawStandardBuildingOptions(Building building)
    {
        GUIStyle buttons = new GUIStyle();
        buttons.hover.background = smallButtonHover;
        buttons.active.background = smallButtonClick;
        GUI.skin.button = buttons;
        int leftPos = BUILD_IMAGE_WIDTH + SCROLL_BAR_WIDTH + BUTTON_SPACING;
        int topPos = buildAreaHeight - BUILD_IMAGE_HEIGHT / 2;
        int width = BUILD_IMAGE_WIDTH / 2;
        int height = BUILD_IMAGE_HEIGHT / 2;
        if (building.Sellable())
        {
            if (GUI.Button(new Rect(leftPos, topPos, width, height), building.sellImage))
            {
                PlayClick();
                building.Sell();
            }
        }
        if (building.hasSpawnPoint())
        {
            leftPos += width + BUTTON_SPACING;
            if (GUI.Button(new Rect(leftPos, topPos, width, height), building.rallyPointImage))
            {
                PlayClick();
                if (activeCursorState != CursorState.RallyPoint && previousCursorState != CursorState.RallyPoint) SetCursorState(CursorState.RallyPoint);
                else
                {
                    //dirty hack to ensure toggle between RallyPoint and not works ...
                    SetCursorState(CursorState.PanRight);
                    SetCursorState(CursorState.Select);
                }
            }
        }
    }

    private void DrawBuildQueue(string[] buildQueue, float buildPercentage)
    {
        for (int i = 0; i < buildQueue.Length; i++)
        {
            float topPos = i * BUILD_IMAGE_HEIGHT - (i + 1) * BUILD_IMAGE_PADDING;
            Rect buildPos = new Rect(BUILD_IMAGE_PADDING, topPos, BUILD_IMAGE_WIDTH, BUILD_IMAGE_HEIGHT);
            GUI.DrawTexture(buildPos, ResourceManager.GetBuildImage(buildQueue[i]));
            GUI.DrawTexture(buildPos, buildFrame);
            topPos += BUILD_IMAGE_PADDING;
            float width = BUILD_IMAGE_WIDTH - 2 * BUILD_IMAGE_PADDING;
            float height = BUILD_IMAGE_HEIGHT - 2 * BUILD_IMAGE_PADDING;
            if (i == 0)
            {
                //shrink the build mask on the item currently being built to give an idea of progress
                topPos += height * buildPercentage;
                height *= (1 - buildPercentage);
            }
            GUI.DrawTexture(new Rect(2 * BUILD_IMAGE_PADDING, topPos, width, height), buildMask);
        }
    }

    private void DrawActions(string[] actions)
    {
        GUIStyle buttons = new GUIStyle();
        buttons.hover.background = buttonHover;
        buttons.active.background = buttonClick;
        GUI.skin.button = buttons;
        int numActions = actions.Length;
        //define the area to draw the actions inside
        GUI.BeginGroup(new Rect(BUILD_IMAGE_WIDTH, 0, ORDERS_BAR_WIDTH, buildAreaHeight));
        //draw scroll bar for the list of actions if need be
        if (numActions >= MaxNumRows(buildAreaHeight)) DrawSlider(buildAreaHeight, numActions / 2.0f);
        //display possible actions as buttons and handle the button click for each
        for (int i = 0; i < numActions; i++)
        {
            int column = i % 2;
            int row = i / 2;
            Rect pos = GetButtonPos(row, column);
            Texture2D action = ResourceManager.GetBuildImage(actions[i]);
            if (action)
            {
                //create the button and handle the click of that button
                if (GUI.Button(pos, action))
                {
                    if (player.SelectedObjects != null)
                    {
                        foreach(WorldObjects selectedWorldObject in player.SelectedObjects)
                        {
                            if (selectedWorldObject.GetType() == player.SelectedObjects[0].GetType())
                            {
                                selectedWorldObject.PerformAction(actions[i]);
                            }
                        }
                        PlayClick();
                    }
                }
            }
        }
        GUI.EndGroup();
    }

    private int MaxNumRows(int areaHeight)
    {
        return areaHeight / BUILD_IMAGE_HEIGHT;
    }

    private Rect GetButtonPos(int row, int column)
    {
        int left = SCROLL_BAR_WIDTH + column * BUILD_IMAGE_WIDTH;
        float top = row * BUILD_IMAGE_HEIGHT - sliderValue * BUILD_IMAGE_HEIGHT;
        return new Rect(left, top, BUILD_IMAGE_WIDTH, BUILD_IMAGE_HEIGHT);
    }

    private void DrawSlider(int groupHeight, float numRows)
    {
        //slider goes from 0 to the number of rows that do not fit on screen
        sliderValue = GUI.VerticalSlider(GetScrollPos(groupHeight), sliderValue, 0.0f, numRows - MaxNumRows(groupHeight));
    }

    private Rect GetScrollPos(int groupHeight)
    {
        return new Rect(BUTTON_SPACING, BUTTON_SPACING, SCROLL_BAR_WIDTH, groupHeight - 2 * BUTTON_SPACING);
    }

    private void DrawResourceBar()
    {
        GUI.skin = resourceSkin;
        GUI.BeginGroup(new Rect(0, 0, Screen.width, RESOURCE_BAR_HEIGHT));
        GUI.Box(new Rect(0, 0, Screen.width, RESOURCE_BAR_HEIGHT), "");
        int topPos = 4, iconLeft = 4, textLeft = 20;
        DrawResourceIcon(ResourceType.Money, iconLeft, textLeft, topPos);
        iconLeft += TEXT_WIDTH;
        textLeft += TEXT_WIDTH;
        DrawResourceIcon(ResourceType.Power, iconLeft, textLeft, topPos);
        int padding = 7;
        int buttonWidth = ORDERS_BAR_WIDTH - 2 * padding - SCROLL_BAR_WIDTH;
        int buttonHeight = RESOURCE_BAR_HEIGHT - 2 * padding;
        int leftPos = Screen.width - ORDERS_BAR_WIDTH / 2 - buttonWidth / 2 + SCROLL_BAR_WIDTH / 2;
        Rect menuButtonPosition = new Rect(leftPos, padding, buttonWidth, buttonHeight);

        if (GUI.Button(menuButtonPosition, "Menu"))
        {
            PlayClick();
            Time.timeScale = 0.0f;
            PauseMenu pauseMenu = GetComponent<PauseMenu>();
            if (pauseMenu) pauseMenu.enabled = true;
            UserInput userInput = player.GetComponent<UserInput>();
            if (userInput) userInput.enabled = false;
            Cursor.visible = true;
            ResourceManager.MenuOpen = true;
        }
        GUI.EndGroup();
    }
    private void DrawResourceIcon(ResourceType type, int iconLeft, int textLeft, int topPos)
    {
        Texture2D icon = resourceImages[type];
        string text = resourceValues[type].ToString() + "/" + resourceLimits[type].ToString();
        GUI.DrawTexture(new Rect(iconLeft, topPos, ICON_WIDTH, ICON_HEIGHT), icon);
        GUI.Label(new Rect(textLeft, topPos, TEXT_WIDTH, TEXT_HEIGHT), text);
    }

    public bool MouseInBounds()
    {
        //Screen coordinates start in the lower-left corner of the screen
        //not the top-left of the screen like the drawing coordinates do
        Vector3 mousePos = Input.mousePosition;
        bool insideWidth = mousePos.x >= 0 && mousePos.x <= Screen.width - ORDERS_BAR_WIDTH;
        bool insideHeight = mousePos.y >= 0 && mousePos.y <= Screen.height - RESOURCE_BAR_HEIGHT;
        bool outsideSelectedUnits = mousePos.x < leftPosSelected || (Screen.height - mousePos.y) < topPosSelected || mousePos.x > (leftPosSelected + widthSelected) || (Screen.height - mousePos.y) > (topPosSelected + heightSelected);
        return insideWidth && insideHeight && outsideSelectedUnits;
    }

    public Rect GetPlayingArea()
    {
        return new Rect(0, RESOURCE_BAR_HEIGHT, Screen.width - ORDERS_BAR_WIDTH, Screen.height - RESOURCE_BAR_HEIGHT);
    }

    private void DrawMouseCursor()
    {
        bool mouseOverHud = !MouseInBounds() && activeCursorState != CursorState.PanRight && activeCursorState != CursorState.PanUp;
        if (mouseOverHud)
        {
            Cursor.visible = true;
        }
        else
        {
            Cursor.visible = false;
            if (!player.IsFindingBuildingLocation())
            {
                GUI.skin = mouseCursorSkin;
                GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
                UpdateCursorAnimation();
                Rect cursorPosition = GetCursorDrawPosition();
                GUI.Label(cursorPosition, activeCursor);
                GUI.EndGroup();
            }
        }
    }

    private void UpdateCursorAnimation()
    {
        //sequence animation for cursor (based on more than one image for the cursor)
        //change once per second, loops through array of images
        if (activeCursorState == CursorState.Move)
        {
            currentFrame = (int)Time.time % moveCursors.Length;
            activeCursor = moveCursors[currentFrame];
        }
        else if (activeCursorState == CursorState.Attack)
        {
            currentFrame = (int)Time.time % attackCursors.Length;
            activeCursor = attackCursors[currentFrame];
        }
        else if (activeCursorState == CursorState.Harvest)
        {
            currentFrame = (int)Time.time % harvestCursors.Length;
            activeCursor = harvestCursors[currentFrame];
        }
    }

    private Rect GetCursorDrawPosition()
    {
        //set base position for custom cursor image
        float leftPos = Input.mousePosition.x;
        float topPos = Screen.height - Input.mousePosition.y; //screen draw coordinates are inverted
                                                              //adjust position base on the type of cursor being shown
        if (activeCursorState == CursorState.PanRight) leftPos -= activeCursor.width;
        else if (activeCursorState == CursorState.PanDown) topPos -= activeCursor.height;
        else if (activeCursorState == CursorState.Move || activeCursorState == CursorState.Select || activeCursorState == CursorState.Harvest)
        {
            topPos -= activeCursor.height / 2;
            leftPos -= activeCursor.width / 2;
        }
        else if (activeCursorState == CursorState.RallyPoint) topPos -= activeCursor.height;
        return new Rect(leftPos, topPos, activeCursor.width, activeCursor.height);
    }

    public CursorState GetCursorState()
    {
        return activeCursorState;
    }

    public CursorState GetPreviousCursorState()
    {
        return previousCursorState;
    }

    public void SetCursorState(CursorState newState)
    {
        if (activeCursorState != newState) previousCursorState = activeCursorState;
        activeCursorState = newState;
        switch (newState)
        {
            case CursorState.Select:
                activeCursor = selectCursor;
                break;
            case CursorState.Attack:
                currentFrame = (int)Time.time % attackCursors.Length;
                activeCursor = attackCursors[currentFrame];
                break;
            case CursorState.Harvest:
                currentFrame = (int)Time.time % harvestCursors.Length;
                activeCursor = harvestCursors[currentFrame];
                break;
            case CursorState.Move:
                currentFrame = (int)Time.time % moveCursors.Length;
                activeCursor = moveCursors[currentFrame];
                break;
            case CursorState.PanLeft:
                activeCursor = leftCursor;
                break;
            case CursorState.PanRight:
                activeCursor = rightCursor;
                break;
            case CursorState.PanUp:
                activeCursor = upCursor;
                break;
            case CursorState.PanDown:
                activeCursor = downCursor;
                break;
            case CursorState.RallyPoint:
                activeCursor = rallyPointCursor;
                break;
            default: break;
        }
    }

    public void SetResourceValues(Dictionary<ResourceType, int> resourceValues, Dictionary<ResourceType, int> resourceLimits)
    {
        this.resourceValues = resourceValues;
        this.resourceLimits = resourceLimits;
    }
}
