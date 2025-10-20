using UnityEngine;

public class TogglePause : MonoBehaviour
{
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject infoPanelUI;
    private bool isPaused = false;

    void Start()
    {
        Resume(); // Start the game in unpaused state
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        infoPanelUI.SetActive(true);
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        infoPanelUI.SetActive(false);
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
