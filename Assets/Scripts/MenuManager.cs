using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject playMenu;
    [SerializeField] private GameObject joinMenu;
    [SerializeField] private GameObject hostMenu;
    [SerializeField] private GameObject usernameMenu;
    [SerializeField] private GameObject authMenu;

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
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
    }
    public void ShowOptionsMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
        playMenu.SetActive(false);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
    }

    public void ShowPlayMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(true);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
    }

    public void ShowJoinMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(false);
        joinMenu.SetActive(true);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
    }

    public void ShowUsernameMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(false);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(true);
        authMenu.SetActive(false);
    }

    public void ShowAuthMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(false);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(true);
    }

    public void ShowHostMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(false);
        joinMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
        hostMenu.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void HideAllMenus()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(false);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
    }
}
