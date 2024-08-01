using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PlayGame : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _dropdown;
    [SerializeField] private GameObject _main, _select;
    [SerializeField] private GameObject _winScreen, _background, _quit, _continue;

    public void Select()
    {
        _main.SetActive(false);
        _select.SetActive(true);
    }

    public void LoadGame()
    {
        GameManager.Size = _dropdown.value + 3;
        SceneManager.LoadSceneAsync(1);
    }

    public void LoadMenu()
    {
        SceneManager.LoadSceneAsync(0);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void Continue()
    {
        GameManager.Ignore = true;
        GameManager.Gamestate = GameState.WaitInput;
        _background.SetActive(false);
        _winScreen.SetActive(false);
        _quit.SetActive(false);
        _continue.SetActive(false);
    }
}