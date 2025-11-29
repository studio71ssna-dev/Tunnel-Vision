using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Singletons;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour
{
    [System.Serializable]
    public struct ShopItem
    {
        public BulletData weaponData;
        public int price;
        public Button buyButton;
        public TextMeshProUGUI priceText;
    }

    [Header("Shop Config")]
    [SerializeField] private ShopItem[] _itemsForSale;
    [SerializeField] private string _nextLevelName = "Level2";

    [Header("Forecast UI")]
    [SerializeField] private TextMeshProUGUI _forecastText;
    [SerializeField] private TextMeshProUGUI _walletText;

    private void Start()
    {
        // 1. Display Forecast (Strategy Phase)
        UpdateForecastDisplay();

        // 2. Setup Shop Buttons
        foreach (var item in _itemsForSale)
        {
            // Setup Text
            if (item.priceText != null)
                item.priceText.text = $"${item.price}";

            // Setup Button Click
            if (item.buyButton != null)
            {
                // Remove old listeners to be safe
                item.buyButton.onClick.RemoveAllListeners();
                item.buyButton.onClick.AddListener(() => TryBuyItem(item));
            }
        }

        UpdateWalletUI();

        // Optional: Clear previous loadout so player must rebuy or keep previous?
        // usually in roguelikes you keep previous, but if you want "Reset per level", call:
        // PlayerLoadout.Instance.ClearLoadout();
    }

    private void UpdateForecastDisplay()
    {
        if (_forecastText == null) return;

        string report = "<b>NEXT AREA FORECAST:</b>\n";

        // Read from the static dictionary in GameFlowManager
        foreach (var kvp in GameFlowManager.NextLevelForecast)
        {
            // Convert 0.7f to "70%"
            report += $"{kvp.Key}: {kvp.Value * 100:0}%\n";
        }

        if (GameFlowManager.NextLevelForecast.Count == 0)
            report += "No Data Available.";

        _forecastText.text = report;
    }

    private void TryBuyItem(ShopItem item)
    {
        if (ScoreManager.Instance == null || PlayerLoadout.Instance == null) return;

        // 1. Check Money
        if (ScoreManager.Instance.AttemptPurchase(item.price))
        {
            // 2. Add to Inventory
            PlayerLoadout.Instance.AddWeapon(item.weaponData);

            // 3. UI Feedback (Disable button or show "Purchased")
            item.buyButton.interactable = false;
            if (item.priceText != null) item.priceText.text = "OWNED";

            UpdateWalletUI();
        }
        else
        {
            Debug.Log("Not enough Loot!");

        }
    }

    private void UpdateWalletUI()
    {
        if (_walletText != null && ScoreManager.Instance != null)
        {
            _walletText.text = $"LOOT: {ScoreManager.Instance.GetCurrentScore()}";
        }
    }

    public void OnStartLevelClicked()
    {
        SceneManager.LoadScene(_nextLevelName);
    }
}