using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    [Header("Level Settings")]
    [SerializeField] private GeneratorCore[] _levelCores;
    [SerializeField] private string _shopSceneName = "ShopMenu";
    [SerializeField] private string _nextLevelName = "Level2";

    private int _activeCoresCount;

    // Data to pass to the Shop Scene
    public static Dictionary<ElementType, float> NextLevelForecast = new Dictionary<ElementType, float>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // Register Cores
        _activeCoresCount = 0;
        foreach (var core in _levelCores)
        {
            if (core != null) _activeCoresCount++;
        }

        // Listen for destruction
        GeneratorCore.OnCoreDestroyed += HandleCoreDestroyed;
    }

    private void HandleCoreDestroyed()
    {
        _activeCoresCount--;
        Debug.Log($"Core Destroyed! Remaining: {_activeCoresCount}");

        if (_activeCoresCount <= 0)
        {
            LevelComplete();
        }
    }

    private void LevelComplete()
    {
        Debug.Log("LEVEL COMPLETE!");

        // 1. Generate Forecast for NEXT level (Mock data for now)
        // In a real game, you'd load the next level data asset to check this.
        GenerateForecast();

        // 2. Load Shop
        SceneManager.LoadScene(_shopSceneName);
    }

    private void GenerateForecast()
    {
        NextLevelForecast.Clear();
        // Example: logic to determine what the next level holds
        // For prototype, we hardcode: "Next level is 70% Fire, 30% Poison"
        NextLevelForecast.Add(ElementType.Fire, 0.7f);
        NextLevelForecast.Add(ElementType.Poison, 0.3f);
    }

    private void OnDestroy()
    {
        GeneratorCore.OnCoreDestroyed -= HandleCoreDestroyed;
    }
}