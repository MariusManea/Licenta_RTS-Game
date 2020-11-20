using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using RTS;

public class MainMenu : Menu
{
    protected override void SetButtons()
    {
        buttons = new string[] { "New Game", "Load Game", "Change Player", "Quit Game" };
    }

    protected override void HandleButton(string text)
    {
        base.HandleButton(text);
        switch (text)
        {
            case "New Game": NewGame(); break;
            case "Load Game": LoadGame(); break;
            case "Quit Game": ExitGame(); break;
            case "Change Player": ChangePlayer(); break;
            default: break;
        }
    }

    private void NewGame()
    {
        ResourceManager.MenuOpen = false;
        SceneManager.LoadScene("GameScene");
        //makes sure that the loaded level runs at normal speed
        Time.timeScale = 1.0f;
    }

    private void ChangePlayer()
    {
        GetComponent<MainMenu>().enabled = false;
        GetComponent<SelectPlayerMenu>().enabled = true;
        SelectionList.LoadEntries(PlayerManager.GetPlayerNames());
    }

    void OnLevelWasLoaded()
    {
        Cursor.visible = true;
        if (PlayerManager.GetPlayerName() == "")
        {
            //no player yet selected so enable SetPlayerMenu
            GetComponent<MainMenu>().enabled = false;
            GetComponent<SelectPlayerMenu>().enabled = true;
        }
        else
        {
            //player selected so enable MainMenu
            GetComponent<MainMenu>().enabled = true;
            GetComponent<SelectPlayerMenu>().enabled = false;
        }
    }

    protected override void HideCurrentMenu()
    {
        GetComponent<MainMenu>().enabled = false;
    }
}
