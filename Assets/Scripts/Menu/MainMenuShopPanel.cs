using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuShopPanel : MonoBehaviour
{
    [Serializable]
    public class DiamondPurchasePack
    {
        public string productId = "diamonds.starter";
        public string displayName = "STARTER PACK";
        public string displayPrice = "$0.99";
        [Min(0)] public int diamondAmount = 100;
        public Sprite icon;
    }

    [Header("Navigation")]
    public Button backButton;

    [Header("Balance")]
    public TMP_Text diamondBalanceText;

    [Header("Diamond Drop Upgrade")]
    [Min(0)] public int firstUpgradeCost = 100;
    [Min(0)] public int costIncreasePerStack = 50;
    public TMP_Text upgradeLevelText;
    public TMP_Text upgradeDescriptionText;
    public TMP_Text upgradeCostText;
    public Button upgradeButton;
    public TMP_Text statusText;

    [Header("In-App Purchase Catalog")]
    [Tooltip("Add, remove or reorder packs here. One card is generated for every entry when the Shop opens.")]
    public List<DiamondPurchasePack> purchasePacks = new List<DiamondPurchasePack>
    {
        new DiamondPurchasePack()
    };
    public RectTransform purchasePackContent;
    public RectTransform purchasePackTemplate;

    [Header("Purchase Integration")]
    [Tooltip("Editor-only test mode. It never grants free Diamonds in a player build.")]
    public bool simulatePurchasesInEditor = true;
    [Tooltip("Connect a store/IAP bridge here. Passes the selected productId.")]
    public UnityEvent<string> onPurchaseRequested = new UnityEvent<string>();

    private readonly List<GameObject> generatedPackCards = new List<GameObject>();

    private void OnEnable()
    {
        PlayerProfileManager.OnDiamondsChanged += HandleDiamondsChanged;
        PlayerProfileManager.OnProfileLoaded += Refresh;
        PlayerProfileManager.OnProfileReset += Refresh;
        PlayerProfileManager.OnProfileStatsChanged += Refresh;

        if (backButton != null) backButton.onClick.AddListener(CloseShop);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(BuyDiamondDropUpgrade);

        RebuildPurchasePackUI();
        Refresh();
    }

    private void OnDisable()
    {
        PlayerProfileManager.OnDiamondsChanged -= HandleDiamondsChanged;
        PlayerProfileManager.OnProfileLoaded -= Refresh;
        PlayerProfileManager.OnProfileReset -= Refresh;
        PlayerProfileManager.OnProfileStatsChanged -= Refresh;

        if (backButton != null) backButton.onClick.RemoveListener(CloseShop);
        if (upgradeButton != null) upgradeButton.onClick.RemoveListener(BuyDiamondDropUpgrade);
    }

    public void Refresh()
    {
        PlayerProfileManager profile = PlayerProfileManager.Instance;
        int diamonds = profile != null ? profile.CurrentDiamonds : 0;
        int level = profile != null ? profile.DiamondDropChanceUpgradeLevel : 0;
        int maxLevel = PlayerProfileManager.MaxDiamondDropChanceUpgradeLevel;
        int cost = GetUpgradeCost(level);
        bool maxed = level >= maxLevel;

        if (diamondBalanceText != null) diamondBalanceText.text = $"DIAMONDS  {diamonds}";
        if (upgradeLevelText != null) upgradeLevelText.text = $"DIAMOND DROP CHANCE  {level}/{maxLevel}";
        if (upgradeDescriptionText != null)
            upgradeDescriptionText.text = $"Permanent +1% drop chance per stack. Current bonus: +{level}%";
        if (upgradeCostText != null) upgradeCostText.text = maxed ? "MAX" : cost.ToString();
        if (upgradeButton != null) upgradeButton.interactable = profile != null && !maxed && diamonds >= cost;
    }

    public void BuyDiamondDropUpgrade()
    {
        PlayerProfileManager profile = PlayerProfileManager.Instance;
        if (profile == null)
        {
            SetStatus("PROFILE NOT READY");
            return;
        }

        int level = profile.DiamondDropChanceUpgradeLevel;
        if (level >= PlayerProfileManager.MaxDiamondDropChanceUpgradeLevel)
        {
            SetStatus("UPGRADE ALREADY MAXED");
            return;
        }

        int cost = GetUpgradeCost(level);
        if (!profile.TryUpgradeDiamondDropChance(cost))
        {
            SetStatus("NOT ENOUGH DIAMONDS");
            return;
        }

        SetStatus($"DROP CHANCE UPGRADED TO +{profile.DiamondDropChanceUpgradeLevel}%");
        Refresh();
    }

    public void RequestPurchase(string productId)
    {
        DiamondPurchasePack pack = FindPack(productId);
        if (pack == null)
        {
            SetStatus("PACK CONFIGURATION INVALID");
            return;
        }

#if UNITY_EDITOR
        if (simulatePurchasesInEditor)
        {
            CompletePurchase(productId);
            return;
        }
#endif

        if (onPurchaseRequested == null || onPurchaseRequested.GetPersistentEventCount() == 0)
        {
            SetStatus("STORE CONNECTION NOT CONFIGURED");
            Debug.LogWarning($"Shop purchase requested for '{productId}', but no IAP bridge is connected.", this);
            return;
        }

        onPurchaseRequested.Invoke(productId);
        SetStatus("CONTACTING STORE...");
    }

    public bool CompletePurchase(string productId)
    {
        DiamondPurchasePack pack = FindPack(productId);
        PlayerProfileManager profile = PlayerProfileManager.Instance;
        if (pack == null || profile == null || pack.diamondAmount <= 0)
            return false;

        int granted = profile.AddDiamonds(pack.diamondAmount, false, true);
        SetStatus(granted > 0 ? $"RECEIVED {granted} DIAMONDS" : "DIAMOND WALLET IS FULL");
        Refresh();
        return granted > 0;
    }

    public void RebuildPurchasePackUI()
    {
        for (int i = 0; i < generatedPackCards.Count; i++)
        {
            if (generatedPackCards[i] != null)
            {
                if (Application.isPlaying)
                {
                    generatedPackCards[i].SetActive(false);
                    Destroy(generatedPackCards[i]);
                }
                else
                    DestroyImmediate(generatedPackCards[i]);
            }
        }
        generatedPackCards.Clear();

        if (purchasePackContent == null || purchasePackTemplate == null)
            return;

        purchasePackTemplate.gameObject.SetActive(false);
        if (purchasePacks == null)
            return;

        HashSet<string> configuredProductIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < purchasePacks.Count; i++)
        {
            DiamondPurchasePack pack = purchasePacks[i];
            if (pack == null)
                continue;

            RectTransform card = Instantiate(purchasePackTemplate, purchasePackContent);
            card.gameObject.name = $"PurchasePack_{i + 1}";
            card.gameObject.SetActive(true);
            generatedPackCards.Add(card.gameObject);

            SetText(card, "PackName", string.IsNullOrWhiteSpace(pack.displayName) ? $"PACK {i + 1}" : pack.displayName);
            SetText(card, "DiamondAmount", $"+{Mathf.Max(0, pack.diamondAmount)} DIAMONDS");
            SetText(card, "Price", string.IsNullOrWhiteSpace(pack.displayPrice) ? "--" : pack.displayPrice);

            Transform iconTransform = FindDeepChild(card, "Icon");
            Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (icon != null)
            {
                icon.sprite = pack.icon;
                icon.enabled = pack.icon != null;
            }

            Transform buttonTransform = FindDeepChild(card, "BuyButton");
            Button button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
            if (button != null)
            {
                string capturedProductId = pack.productId;
                bool validProductId = !string.IsNullOrWhiteSpace(capturedProductId) &&
                                      configuredProductIds.Add(capturedProductId);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => RequestPurchase(capturedProductId));
                button.interactable = validProductId && pack.diamondAmount > 0;
                if (!validProductId && !string.IsNullOrWhiteSpace(capturedProductId))
                    Debug.LogWarning($"Duplicate Shop productId '{capturedProductId}'. Only the first matching pack can be purchased.", this);
            }
        }
    }

    private int GetUpgradeCost(int currentLevel)
    {
        long cost = (long)Mathf.Max(0, firstUpgradeCost) +
                    (long)Mathf.Max(0, costIncreasePerStack) * Mathf.Max(0, currentLevel);
        return (int)Math.Min(cost, int.MaxValue);
    }

    private DiamondPurchasePack FindPack(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId) || purchasePacks == null)
            return null;

        for (int i = 0; i < purchasePacks.Count; i++)
        {
            DiamondPurchasePack pack = purchasePacks[i];
            if (pack != null && string.Equals(pack.productId, productId, StringComparison.Ordinal))
                return pack;
        }
        return null;
    }

    private void CloseShop()
    {
        if (MainMenuController.Instance != null)
            MainMenuController.Instance.CloseShop();
    }

    private void HandleDiamondsChanged(int _) => Refresh();

    private void SetStatus(string value)
    {
        if (statusText != null)
            statusText.text = value;
    }

    private static void SetText(Transform root, string childName, string value)
    {
        Transform child = FindDeepChild(root, childName);
        TMP_Text text = child != null ? child.GetComponent<TMP_Text>() : null;
        if (text != null)
            text.text = value;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
            return null;
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);
            if (found != null)
                return found;
        }
        return null;
    }
}
