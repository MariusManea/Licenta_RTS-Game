using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RTS;

public class NewGameMenu : MonoBehaviour
{
    public GUISkin mySkin;
    private GameSize[] gameTypes;
    private int typeIndex = -1;
    private int numberOfPlayers = 2;

    public AudioClip clickSound;
    public float clickVolume = 1.0f;

    public string seed = "";

    private AudioElement audioElement;

    void Start()
    {
        gameTypes = ResourceManager.GetGameSizes();
        if (gameTypes.Length > 0) typeIndex = 0;
        
        if (clickVolume < 0.0f) clickVolume = 0.0f;
        if (clickVolume > 1.0f) clickVolume = 1.0f;
        List<AudioClip> sounds = new List<AudioClip>();
        List<float> volumes = new List<float>();
        sounds.Add(clickSound);
        volumes.Add(clickVolume);
        audioElement = new AudioElement(sounds, volumes, "GameSizeMenu", null);
    }

    private void PlayClick()
    {
        if (audioElement != null) audioElement.Play(clickSound);
    }

    void OnGUI()
    {
        GUI.skin = mySkin;
        float menuHeight = 450;
        float groupLeft = Screen.width / 2 - ResourceManager.MenuWidth / 2;
        float groupTop = Screen.height / 2 - menuHeight / 2;
        Rect groupRect = new Rect(groupLeft, groupTop, ResourceManager.MenuWidth, menuHeight);
        GUI.BeginGroup(groupRect);
        GUI.Box(new Rect(0, 0, ResourceManager.MenuWidth, menuHeight), "");

        float leftPos = ResourceManager.MenuWidth / 2 - ResourceManager.ButtonWidth;
        float topPos = menuHeight - 5.25f * (ResourceManager.Padding + ResourceManager.ButtonHeight);
        GUI.Label(new Rect(leftPos + ResourceManager.ButtonWidth / 2, topPos - ResourceManager.Padding, ResourceManager.ButtonWidth, ResourceManager.TextHeight), "Seed:");
        float textWidth = ResourceManager.MenuWidth - 2 * ResourceManager.Padding;
        GUI.Label(new Rect(ResourceManager.Padding, topPos + ResourceManager.Padding, textWidth, ResourceManager.TextHeight), "*leave blank for random seed");
        seed = GUI.TextField(new Rect(leftPos, topPos, 2 * ResourceManager.ButtonWidth, ResourceManager.TextHeight), seed, 25);

        leftPos = ResourceManager.MenuWidth / 2 - ResourceManager.ButtonWidth / 2;
        topPos = menuHeight - 2 * (ResourceManager.Padding + ResourceManager.ButtonHeight);
        if (GUI.Button(new Rect(leftPos, topPos, ResourceManager.ButtonWidth, ResourceManager.ButtonHeight), "Play Game"))
        {
            PlayClick();
            ResourceManager.MenuOpen = false;
            LevelLoader levelLoader = (LevelLoader)GameObject.FindObjectOfType(typeof(LevelLoader));
            levelLoader.playersNumber = numberOfPlayers;
            levelLoader.mapSize = gameTypes[typeIndex];
            levelLoader.seed = seed;
            SceneManager.LoadScene("GameScene");
            Time.timeScale = 1.0f;
        }
        topPos += (ResourceManager.Padding + ResourceManager.ButtonHeight);
        if (GUI.Button(new Rect(leftPos, topPos, ResourceManager.ButtonWidth, ResourceManager.ButtonHeight), "Cancel"))
        {
            PlayClick();
            GetComponent<NewGameMenu>().enabled = false;
            GetComponent<MainMenu>().enabled = true;
        }

        float buttonTop = menuHeight - 4 * (ResourceManager.Padding + ResourceManager.ButtonHeight);
        float buttonLeft = ResourceManager.Padding;
        GUI.Label(new Rect(ResourceManager.MenuWidth / 2 - ResourceManager.ButtonWidth / 2, buttonTop - ResourceManager.Padding, ResourceManager.ButtonWidth, ResourceManager.ButtonHeight), "Game Size");
        GUI.Label(new Rect(ResourceManager.MenuWidth / 2 - ResourceManager.ButtonWidth / 2, buttonTop, ResourceManager.ButtonWidth, ResourceManager.ButtonHeight), gameTypes[typeIndex].ToString());
        if (GUI.Button(new Rect(buttonLeft, buttonTop, ResourceManager.ButtonHeight, ResourceManager.ButtonHeight), "<"))
        {
            PlayClick();
            typeIndex -= 1;
            if (typeIndex < 0) typeIndex = gameTypes.Length - 1;
            if (typeIndex == 0 && numberOfPlayers > 4)
            {
                numberOfPlayers = 4;
            }
            if (typeIndex == 1 && numberOfPlayers > 6)
            {
                numberOfPlayers = 6;
            }
            if (typeIndex == 2 && numberOfPlayers > 8)
            {
                numberOfPlayers = 8;
            }
        }
        buttonLeft = ResourceManager.MenuWidth - ResourceManager.Padding - ResourceManager.ButtonHeight;
        if (GUI.Button(new Rect(buttonLeft, buttonTop, ResourceManager.ButtonHeight, ResourceManager.ButtonHeight), ">"))
        {
            PlayClick();
            typeIndex = (typeIndex + 1) % gameTypes.Length;
            if (typeIndex == 0 && numberOfPlayers > 4)
            {
                numberOfPlayers = 4;
            } 
        }

        buttonTop = menuHeight - 3 * (ResourceManager.Padding + ResourceManager.ButtonHeight);
        buttonLeft = ResourceManager.Padding;
        GUI.Label(new Rect(ResourceManager.MenuWidth / 2 - ResourceManager.ButtonWidth / 2, buttonTop - ResourceManager.Padding, ResourceManager.ButtonWidth, ResourceManager.ButtonHeight), "Players Number");
        GUI.Label(new Rect(ResourceManager.MenuWidth / 2 - ResourceManager.ButtonWidth / 2, buttonTop, ResourceManager.ButtonWidth, ResourceManager.ButtonHeight), numberOfPlayers.ToString());
        if (GUI.Button(new Rect(buttonLeft, buttonTop, ResourceManager.ButtonHeight, ResourceManager.ButtonHeight), "<"))
        {
            PlayClick();
            numberOfPlayers -= 1;
            if (numberOfPlayers < 2)
            {
                numberOfPlayers = 10;
                typeIndex = gameTypes.Length - 1;
            }
        }
        buttonLeft = ResourceManager.MenuWidth - ResourceManager.Padding - ResourceManager.ButtonHeight;
        if (GUI.Button(new Rect(buttonLeft, buttonTop, ResourceManager.ButtonHeight, ResourceManager.ButtonHeight), ">"))
        {
            PlayClick();
            numberOfPlayers += 1;
            if (numberOfPlayers > 10) numberOfPlayers = 2;
            if (numberOfPlayers > 4 && typeIndex < 1)
            {
                typeIndex = 1;
            }
            if (numberOfPlayers > 6 && typeIndex < 2)
            {
                typeIndex = 2;
            }
            if (numberOfPlayers > 8 && typeIndex < 3)
            {
                typeIndex = 3;
            }
        }
        GUI.EndGroup();

    }
}
