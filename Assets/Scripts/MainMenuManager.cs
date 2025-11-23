using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The name of your gameplay scene (e.g., 'Level1')")]
    [SerializeField] private string _gameplaySceneName = "GameLevel";

    private void Start()
    {
        // Ensure cursor is visible when returning to menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnStartClicked()
    {
        // Load the actual game
        SceneManager.LoadScene(_gameplaySceneName);
    }

    public void OnControlsClicked()
    {
        Debug.Log("Open Controls Panel here (Active/Inactive logic)");
        // If you have a controls panel, reference it and .SetActive(true) here
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}