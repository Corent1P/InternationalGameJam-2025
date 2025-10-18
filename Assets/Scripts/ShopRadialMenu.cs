using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ShopRadialMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject radialMenuPanel;
    public Transform itemsContainer;
    public GameObject itemButtonPrefab;

    [Header("Shop Settings")]
    public KeyCode shopKey = KeyCode.B;
    public float selectionRadius = 150f;

    [Header("Items")]
    public List<ShopItem> shopItems = new List<ShopItem>();

    private bool isShopOpen = false;
    private readonly List<GameObject> itemButtons = new();

    private void Start()
    {
        if (radialMenuPanel == null)
        {
            Debug.LogError("[ShopRadialMenu] RadialMenuPanel is missing!");
            return;
        }

        radialMenuPanel.SetActive(false);
        CreateShopButtons();
    }

    private void Update()
    {
        if (Input.GetKeyDown(shopKey))
            ToggleShop();
    }

    private void ToggleShop()
    {
        isShopOpen = !isShopOpen;

        if (radialMenuPanel != null)
            radialMenuPanel.SetActive(isShopOpen);
        else
            Debug.LogWarning("[ShopRadialMenu] Tried to toggle shop but RadialMenuPanel is null!");

        Cursor.visible = isShopOpen;
        Cursor.lockState = isShopOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void CreateShopButtons()
    {
        if (itemsContainer == null)
        {
            Debug.LogError("[ShopRadialMenu] ItemsContainer is missing!");
            return;
        }

        if (itemButtonPrefab == null)
        {
            Debug.LogError("[ShopRadialMenu] ItemButtonPrefab is missing!");
            return;
        }

        if (shopItems.Count == 0)
        {
            Debug.LogWarning("[ShopRadialMenu] No items defined in shopItems.");
            return;
        }

        float angleStep = 360f / shopItems.Count;

        for (int i = 0; i < shopItems.Count; i++)
        {
            GameObject buttonObj = Instantiate(itemButtonPrefab, itemsContainer);
            RectTransform rect = buttonObj.GetComponent<RectTransform>();

            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 position = new(Mathf.Sin(angle) * selectionRadius, Mathf.Cos(angle) * selectionRadius);
            rect.anchoredPosition = position;

            ShopItemButton btn = buttonObj.GetComponent<ShopItemButton>();
            if (btn != null)
                btn.Setup(shopItems[i], i);
            else
                Debug.LogWarning($"[ShopRadialMenu] Missing ShopItemButton component on prefab {itemButtonPrefab.name}.");

            itemButtons.Add(buttonObj);
        }
    }
}

[System.Serializable]
public class ShopItem
{
    public string itemName;
    public int price;
}
