using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject playMenu;
    [SerializeField] private GameObject joinMenu;
    [SerializeField] private GameObject usernameMenu;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
        playMenu.SetActive(false);
        joinMenu.SetActive(false);
        usernameMenu.SetActive(false);
    }
    public void ShowOptionsMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
        playMenu.SetActive(false);
        joinMenu.SetActive(false);
        usernameMenu.SetActive(false);
    }

    public void ShowPlayMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(true);
        joinMenu.SetActive(false);
        usernameMenu.SetActive(false);
    }

    public void ShowJoinMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(false);
        joinMenu.SetActive(true);
        usernameMenu.SetActive(false);
    }

    public void ShowUsernameMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(false);
        joinMenu.SetActive(false);
        usernameMenu.SetActive(true);
    }
}
