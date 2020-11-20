using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RTS;

public class PauseMenu : Menu
{

    private Player player;

    protected override void Start()
    {
        base.Start();
        player = transform.root.GetComponent<Player>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Resume();
    }

    protected override void SetButtons()
    {
        buttons = new string[] { "Resume", "Save Game", "Load Game", "Exit Game" };
    }

    protected override void HandleButton(string text)
    {
        base.HandleButton(text);
        switch (text)
        {
            case "Resume": Resume(); break;
            case "Exit Game": ReturnToMainMenu(); break;
            case "Load Game": LoadGame(); break;
            case "Save Game": SaveGame(); break;
            default: break;
        }
    }

    private void Resume()
    {
        Time.timeScale = 1.0f;
        GetComponent<PauseMenu>().enabled = false;
        if (player) player.GetComponent<UserInput>().enabled = true;
        Cursor.visible = false;
        ResourceManager.MenuOpen = false;
    }

    private void ReturnToMainMenu()
    {
        Cursor.visible = true;
        ResourceManager.LevelName = "";
        SceneManager.LoadScene("MainMenu");
    }

    private void SaveGame()
    {
        GetComponent<PauseMenu>().enabled = false;
        SaveMenu saveMenu = GetComponent<SaveMenu>();
        if (saveMenu)
        {
            saveMenu.enabled = true;
            saveMenu.Activate();
        }
    }


    protected override void HideCurrentMenu()
    {
        GetComponent<PauseMenu>().enabled = false;
    }
}