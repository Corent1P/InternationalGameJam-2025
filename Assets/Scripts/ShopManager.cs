using UnityEngine;
using Unity.Netcode;

public class ShopManager : NetworkBehaviour
{
    public NetworkAdultController adultController;

    public void SetAdultController(NetworkAdultController controller)
    {
        adultController = controller;
    }

    public void TryPurchaseItem(int itemIndex)
    {
        if (adultController == null)
            return;

        if (!adultController.IsOwner)
            return;

        PurchaseItemServerRpc(itemIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PurchaseItemServerRpc(int itemIndex)
    {
        Debug.Log($"[ShopManager] PurchaseItemServerRpc called for item index {itemIndex} by player");
        if (adultController == null)
            return;

        AdultManager adultManager = adultController.GetAdultManager();

        if (adultManager == null)
            return;

        var shop = adultController.GetShopMenu();
        if (shop == null || itemIndex < 0 || itemIndex >= shop.shopItems.Count) return;

        Debug.Log($"[ShopManager] Attempting to purchase item at index {itemIndex}");

        ShopItem item = shop.shopItems[itemIndex];

        Debug.Log($"[ShopManager] Player {adultController.OwnerClientId} is attempting to purchase {item.itemName} for {item.price} coins.");

        if (adultManager.GetCoins() < item.price)
            return;
        if (adultManager.IsInventoryFull())
            return;
        if (item.itemPrefab == null)
            return;

        adultManager.RemoveCoins(item.price);

        bool success = adultManager.AddItemToInventory(item.itemPrefab);

        if (success)
        {
            NotifyPurchaseSuccessClientRpc(item.itemName);
        }
        else
        {
            // Remboursement si échec
            adultManager.AddCoins(item.price);
        }
    }

    [ClientRpc]
    private void NotifyPurchaseSuccessClientRpc(string itemName)
    {
        if (adultController != null && adultController.IsOwner)
        {
            Debug.Log($"✅ Purchased item: {itemName}");
        }
    }
}
