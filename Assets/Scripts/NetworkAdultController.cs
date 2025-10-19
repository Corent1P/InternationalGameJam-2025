using UnityEngine;

[RequireComponent(typeof(AdultManager))]
public class NetworkAdultController : NetworkPlayerController
{
    [Header("UI Prefabs")]
    public GameObject uiPrefabShop;
    public GameObject uiPrefabInventory;
    private ShopManager shopManager;

    private GameObject uiShopInstance;
    private GameObject uiInventoryInstance;

    private ShopRadialMenu shopMenu;
    private InventoryUI inventoryUI;

    private AdultManager adultManager;

    protected override void Awake()
    {
        base.Awake();
        adultManager = GetComponent<AdultManager>();
        shopManager = GetComponent<ShopManager>();
    }

    public ShopRadialMenu GetShopMenu() => shopMenu;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            Debug.Log($"[NetworkAdultController] Not owner, skipping UI initialization for {gameObject.name}.");
            return;
        }

        if (adultManager == null)
        {
            Debug.LogWarning("[NetworkAdultController] No AdultManager found — skipping UI initialization.");
            return;
        }

        if (uiPrefabShop != null)
        {
            uiShopInstance = Instantiate(uiPrefabShop);
            uiShopInstance.name = $"{gameObject.name}_ShopUI";

            shopMenu = uiShopInstance.GetComponentInChildren<ShopRadialMenu>(true);
            if (shopMenu != null)
            {
                shopMenu.gameObject.SetActive(true);
                shopMenu.SetAdultController(this);
                shopMenu.SetShopManager(shopManager);
                Debug.Log("[NetworkAdultController] Shop menu instantiated and linked successfully.");
            }
            else
            {
                Debug.LogError("[NetworkAdultController] ShopRadialMenu component not found in Shop UI prefab!");
            }
        }
        else
        {
            Debug.LogError("[NetworkAdultController] No Shop UI prefab assigned!");
        }

        if (uiPrefabInventory != null)
        {
            uiInventoryInstance = Instantiate(uiPrefabInventory);
            uiInventoryInstance.name = $"{gameObject.name}_InventoryUI";

            inventoryUI = uiInventoryInstance.GetComponentInChildren<InventoryUI>(true);
            if (inventoryUI != null)
            {
                inventoryUI.gameObject.SetActive(true);
                inventoryUI.Initialize(this);
                Debug.Log("[NetworkAdultController] Inventory UI instantiated and linked successfully.");
            }
            else
            {
                Debug.LogError("[NetworkAdultController] InventoryUI component not found in Inventory UI prefab!");
            }
        }
        else
        {
            Debug.LogError("[NetworkAdultController] No Inventory UI prefab assigned!");
        }
    }

    public AdultManager GetAdultManager() => adultManager;
}
