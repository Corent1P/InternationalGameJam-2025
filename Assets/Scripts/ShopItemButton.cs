using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopItemButton : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;

    private int itemIndex;

    public void Setup(ShopItem item, int index)
    {
        itemIndex = index;
        nameText.text = item.itemName;
        priceText.text = $"{item.price} $";
    }
}
