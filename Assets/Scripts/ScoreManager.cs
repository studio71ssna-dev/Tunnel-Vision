using UnityEngine;
using TMPro;


public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _scoreTextHUD; // Drag your HUD Score Text here
    [SerializeField] private string _prefix = "SCORE: ";

    private int _currentScore = 0;
    private int _highScore = 0;

    private void Awake()
    {

        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        _highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AddScore(int amount)
    {
        _currentScore += amount;
        UpdateUI();
    }

    public int GetCurrentScore() => _currentScore;

    public int GetHighScore()
    {
        // Check if we beat the high score
        if (_currentScore > _highScore)
        {
            _highScore = _currentScore;
            PlayerPrefs.SetInt("HighScore", _highScore);
            PlayerPrefs.Save();
        }
        return _highScore;
    }

    public bool AttemptPurchase(int cost)
    {
        if (_currentScore >= cost)
        {
            _currentScore -= cost;
            UpdateUI();
            return true; // Success
        }
        return false; // Not enough cash
    }
    // Optional: Carry score over to the shop scene
    // Ensure you check the "DontDestroyOnLoad" logic if ScoreManager isn't already persistent.
    // Since it's a SingletonPersistent, it should already survive scene loads.
    private void UpdateUI()
    {
        if (_scoreTextHUD != null)
        {
            _scoreTextHUD.text = _prefix + _currentScore.ToString("N0"); // "N0" adds commas (1,000)
        }
    }
}