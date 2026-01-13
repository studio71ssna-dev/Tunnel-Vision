using UnityEngine;
using Singletons;
using System.Collections.Generic;

public class ToolController : MonoBehaviour
{
    [System.Serializable]
    public struct ToolEntry
    {
        public ToolType toolType;
        public GameObject toolObject;
    }

    [Header("Tools in Left Hand")]
    [SerializeField] private List<ToolEntry> tools = new();

    private int _currentIndex = 0;

    public ToolType CurrentTool => tools[_currentIndex].toolType;

    private void Start()
    {
        // Disable all tools
        foreach (var t in tools)
            t.toolObject.SetActive(false);

        // Enable first tool
        if (tools.Count > 0)
            tools[_currentIndex].toolObject.SetActive(true);
    }

    public void CycleTool()
    {
        if (InputManager.Instance.IsAiming) return;
        if (tools.Count <= 1) return;

        tools[_currentIndex].toolObject.SetActive(false);

        _currentIndex++;
        if (_currentIndex >= tools.Count)
            _currentIndex = 0;

        tools[_currentIndex].toolObject.SetActive(true);
    }
}
