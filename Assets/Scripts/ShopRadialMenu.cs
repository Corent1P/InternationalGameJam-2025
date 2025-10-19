using UnityEngine;
using System.Collections.Generic;

public class ShopRadialMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject radialMenuPanel;
    public Transform itemsContainer;
    public GameObject itemButtonPrefab;

    [Header("Settings")]
    public KeyCode shopKey = KeyCode.B;
    public float selectionRadius = 150f;

    [Header("Items")]
    public List<ShopItem> shopItems = new List<ShopItem>();

    private bool isShopOpen = false;
    private readonly List<GameObject> itemButtons = new();
    private NetworkAdultController adultController;
    private ShopManager shopManager;

    public void SetAdultController(NetworkAdultController controller)
    {
        adultController = controller;
    }

    public void SetShopManager(ShopManager manager)
    {
        shopManager = manager;
    }

    private void Start()
    {
        if (radialMenuPanel != null)
            radialMenuPanel.SetActive(false);

        CreateShopButtons();
    }

    private void Update()
    {
        if (!adultController || !adultController.IsOwner) return;

        if (Input.GetKeyDown(shopKey))
            ToggleShop();
    }

    private void ToggleShop()
    {
        isShopOpen = !isShopOpen;
        if (radialMenuPanel != null)
            radialMenuPanel.SetActive(isShopOpen);

        Cursor.visible = isShopOpen;
        Cursor.lockState = isShopOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void CreateShopButtons()
    {
        if (itemsContainer == null || itemButtonPrefab == null || shopItems.Count == 0) return;

        float angleStep = 360f / shopItems.Count;

        for (int i = 0; i < shopItems.Count; i++)
        {
            GameObject btnObj = Instantiate(itemButtonPrefab, itemsContainer);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            float angle = i * angleStep * Mathf.Deg2Rad;
            rect.anchoredPosition = new(Mathf.Sin(angle) * selectionRadius, Mathf.Cos(angle) * selectionRadius);

            ShopItemButton btn = btnObj.GetComponent<ShopItemButton>();
            if (btn != null)
                btn.Setup(shopItems[i], i, this);

            itemButtons.Add(btnObj);
        }
    }

    // Appelé par le bouton UI
    public void TryPurchaseItem(int index)
    {
        if (shopManager != null)
            shopManager.TryPurchaseItem(index);
    }
}

[System.Serializable]
public class ShopItem
{
    public string itemName;
    public int price;
    public GameObject itemPrefab; // Le prefab du trap/item à ajouter à l'inventaire
}
