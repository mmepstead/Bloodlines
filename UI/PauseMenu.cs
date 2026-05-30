using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused = false;
    public GameObject pauseMenuUI;
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            
            // Toggle pause menu visibility here
            if(IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        // Hide pause menu UI here
        pauseMenuUI.SetActive(false);
    }

    void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        // Show pause menu UI here
        pauseMenuUI.SetActive(true);
    }
}