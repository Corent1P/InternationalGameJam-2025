using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopItemButton : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button button;

    private int itemIndex;
    private ShopRadialMenu shopMenu;

    public void Setup(ShopItem item, int index, ShopRadialMenu menu)
    {
        itemIndex = index;
        shopMenu = menu;

        if (nameText != null)
            nameText.text = item.itemName;

        if (priceText != null)
            priceText.text = $"{item.price} $";

        if (button == null)
            button = GetComponent<Button>();

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
        else {
            Debug.LogWarning($"[ShopItemButton] No Button component found on {gameObject.name}");
        }
    }

    private void OnButtonClicked()
    {
        if (shopMenu != null) {
            shopMenu.TryPurchaseItem(itemIndex);
        }
        else {
            Debug.LogError("[ShopItemButton] ShopRadialMenu reference is null!");
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }
}
