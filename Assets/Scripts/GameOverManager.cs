using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TextMeshProUGUI _finalScoreText;
    [SerializeField] private TextMeshProUGUI _highScoreText;

    [Header("Scene Configuration")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
    }

    public void TriggerGameOver()
    {
        if (_gameOverPanel == null) return;

        // 1. Show Panel
        _gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. Update Score Texts
        if (ScoreManager.Instance != null)
        {
            int current = ScoreManager.Instance.GetCurrentScore();
            int high = ScoreManager.Instance.GetHighScore();

            if (_finalScoreText != null)
                _finalScoreText.text = $"SCORE: {current}";

            if (_highScoreText != null)
                _highScoreText.text = $"BEST: {high}";
        }
    }

    public void OnRestartClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuSceneName);
    }
}