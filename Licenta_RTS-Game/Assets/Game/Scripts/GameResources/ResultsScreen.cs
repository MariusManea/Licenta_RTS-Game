using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RTS;

public class ResultsScreen : MonoBehaviour
{
    public GUISkin skin;
    public AudioClip clickSound;
    public float clickVolume = 1.0f;
    public Texture2D logo;

    private AudioElement audioElement;
    private Player winner;
    private VictoryCondition metVictoryCondition;

    void Start()
    {
        List<AudioClip> sounds = new List<AudioClip>();
        List<float> volumes = new List<float>();
        sounds.Add(clickSound);
        volumes.Add(clickVolume);
        audioElement = new AudioElement(sounds, volumes, "ResultsScreen", null);
    }

    void OnGUI()
    {
        GUI.skin = skin;

        GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));

        //display 
        float padding = ResourceManager.Padding;
        float itemHeight = ResourceManager.ButtonHeight;
        float buttonWidth = ResourceManager.ButtonWidth;
        float leftPos = padding;
        float topPos = Screen.height / 2 - 2 * itemHeight;
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
        GUI.DrawTexture(new Rect(Screen.width / 2 - ResourceManager.LogoWidth / 2, topPos - 4 * itemHeight, ResourceManager.LogoWidth, ResourceManager.HeaderHeight), logo);
        topPos += itemHeight;
        string message = "Game Over";
        if (winner) message = "Congratulations " + winner.userName + "! You have won by " + metVictoryCondition.GetDescription();
        GUI.Label(new Rect(leftPos, topPos, Screen.width - 2 * padding, 2 * itemHeight), message);
        leftPos = Screen.width / 2 - 4 * padding / 2 - buttonWidth;
        topPos += 2 * itemHeight + 2 * padding;
        if (GUI.Button(new Rect(leftPos, topPos, buttonWidth, itemHeight), "New Game"))
        {
            PlayClick();
            //makes sure that the loaded level runs at normal speed
            Time.timeScale = 1.0f;
            ResourceManager.MenuOpen = false;
            SceneManager.LoadScene("GameScene");
        }
        leftPos += 4 * padding + buttonWidth;
        if (GUI.Button(new Rect(leftPos, topPos, buttonWidth, itemHeight), "Main Menu"))
        {
            ResourceManager.LevelName = "";
            SceneManager.LoadScene("MainMenu");
            Cursor.visible = true;
        }

        GUI.EndGroup();
    }

    private void PlayClick()
    {
        if (audioElement != null) audioElement.Play(clickSound);
    }

    public void SetMetVictoryCondition(VictoryCondition victoryCondition)
    {
        if (!victoryCondition) return;
        metVictoryCondition = victoryCondition;
        winner = metVictoryCondition.GetWinner();
    }
}
