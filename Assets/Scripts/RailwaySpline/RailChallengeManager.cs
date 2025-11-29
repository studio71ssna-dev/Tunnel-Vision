using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RailChallengeManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SplineEventController _splineController;
    [SerializeField] private GameObject _playerCartVisuals; // The thing that explodes
    [SerializeField] private TextMeshProUGUI _timerText;

    [Header("Challenge Settings")]
    [SerializeField] private float _timeLimit = 60f;
    [SerializeField] private bool _isActive = false;

    private float _timer;
    private bool _failed = false;

    private void Start()
    {
        _timer = _timeLimit;

        // Hide timer until activated
        if (_timerText != null) _timerText.gameObject.SetActive(false);
    }

    // Call this via a Trigger or Event to start the rail section
    public void BeginChallenge()
    {
        _isActive = true;
        if (_timerText != null) _timerText.gameObject.SetActive(true);

        // Start the cart
        if (_splineController != null) _splineController.ResumeMovement();
    }

    private void Update()
    {
        if (!_isActive || _failed) return;

        _timer -= Time.deltaTime;

        // Update UI
        if (_timerText != null)
            _timerText.text = $"{_timer:00.00}";

        // Fail Check
        if (_timer <= 0)
        {
            FailChallenge();
        }
    }

    public void CompleteChallenge()
    {
        _isActive = false;
        if (_timerText != null) _timerText.color = Color.green;
        Debug.Log("Rail Challenge Survived!");
    }

    private void FailChallenge()
    {
        _failed = true;
        _isActive = false;

        Debug.Log("TIME OVER! CART EXPLODING...");

        // 1. Stop Movement
        if (_splineController != null) _splineController.PauseMovement();

        // 2. Visuals (Explosion)
        if (_playerCartVisuals != null)
        {
            // Spawn explosion prefab here if you have one
            Destroy(_playerCartVisuals);
        }

        // 3. Restart Level or Game Over
        Invoke(nameof(RestartLevel), 2f);
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}