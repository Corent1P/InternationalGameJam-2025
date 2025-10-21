using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform itemsContainer;
    public GameObject itemSlotPrefab;

    [Header("Settings")]
    public KeyCode inventoryKey = KeyCode.I;
    public float placeDistance = 3f;
    public LayerMask groundLayer;

    [Header("Placement Preview")]
    public Material previewMaterial;
    private GameObject previewInstance;
    private int previewSlot = -1;

    private NetworkAdultController adultController;
    private AdultManager adultManager;
    private bool isInventoryOpen = false;
    private int selectedSlot = -1;
    private List<GameObject> itemSlots = new List<GameObject>();

    private void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    public void Initialize(NetworkAdultController controller)
    {
        if (controller == null)
        {
            Debug.LogError("[InventoryUI] Cannot initialize: controller is null!");
            return;
        }
        adultController = controller;
        adultManager = controller.GetAdultManager();
        Debug.Log("[InventoryUI] Initialized!");
    }

    private void Update()
    {
        if (adultController == null || !adultController.IsOwner) return;

        // Toggle inventory
        if (Input.GetKeyDown(inventoryKey))
        {
            ToggleInventory();
        }

        // Placer un item avec clic gauche (quand inventaire ouvert)
        if (isInventoryOpen && selectedSlot >= 0 && Input.GetMouseButtonDown(0))
        {
            TryPlaceItem();
        }

        // --- Gestion des touches rapides et preview ---
        if (!isInventoryOpen)
        {
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || (i == 9 && Input.GetKeyDown(KeyCode.Alpha0)))
                {
                    int slot = (i == 9) ? 9 : i;
                    StartPlacementPreview(slot);
                }
            }
        }

        // Mise à jour de la preview si elle est active
        if (previewInstance != null)
        {
            UpdatePreviewPosition();

            if (Input.GetKeyDown(KeyCode.E))
            {
                ConfirmPlacement();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelPreview();
            }
        }
    }

    private void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(isInventoryOpen);

        Cursor.visible = isInventoryOpen;
        Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;

        if (isInventoryOpen)
        {
            RefreshInventoryUI();
        }
    }

    // 🔥 MODIFIÉ : Rendu public pour pouvoir être appelé depuis AdultManager
    public void RefreshInventoryUI()
    {
        if (adultManager == null || itemsContainer == null || itemSlotPrefab == null) return;

        // Nettoyer les slots existants
        foreach (var slot in itemSlots)
        {
            if (slot != null) Destroy(slot);
        }
        itemSlots.Clear();

        // Créer les nouveaux slots
        List<GameObject> inventory = adultManager.GetInventory();
        for (int i = 0; i < inventory.Count; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, itemsContainer);

            // Récupérer les composants
            TextMeshProUGUI nameText = slotObj.GetComponentInChildren<TextMeshProUGUI>();
            Button button = slotObj.GetComponent<Button>();

            int index = i; // Capture pour le lambda
            if (button != null)
            {
                button.onClick.AddListener(() => OnSlotClicked(index));
            }

            itemSlots.Add(slotObj);
        }
    }

    private void OnSlotClicked(int index)
    {
        selectedSlot = index;

        // Highlight visuel
        for (int i = 0; i < itemSlots.Count; i++)
        {
            Image img = itemSlots[i].GetComponent<Image>();
            if (img != null)
            {
                img.color = (i == index) ? Color.yellow : Color.white;
            }
        }
    }

    private void TryPlaceItem()
    {
        if (adultManager == null || selectedSlot < 0) return;

        // Raycast depuis la caméra pour trouver où placer
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Vector3 placePosition = hit.point;
            Quaternion placeRotation = Quaternion.identity;

            adultManager.PlaceTrap(selectedSlot, placePosition, placeRotation);

            selectedSlot = -1;
            RefreshInventoryUI();
        }
        else
        {
            Debug.LogWarning("[InventoryUI] No valid ground found!");
        }
    }

    private void StartPlacementPreview(int slot)
    {
        if (adultManager == null) return;

        if (slot >= adultManager.GetInventoryCount())
        {
            return;
        }

        // Détruire l'ancienne preview si elle existe
        if (previewInstance != null)
            Destroy(previewInstance);

        GameObject prefab = adultManager.GetItemAtIndex(slot);
        if (prefab == null)
        {
            Debug.LogWarning("[InventoryUI] Invalid item prefab for preview!");
            return;
        }

        // Créer l'objet fantôme
        previewInstance = Instantiate(prefab);
        previewInstance.transform.position = new Vector3(previewInstance.transform.position.x, previewInstance.transform.position.y - 0.9f, previewInstance.transform.position.z);
        previewSlot = slot;

        // Désactiver les collisions et la logique réseau
        foreach (Collider c in previewInstance.GetComponentsInChildren<Collider>())
            c.enabled = false;

        foreach (var netObj in previewInstance.GetComponentsInChildren<Unity.Netcode.NetworkObject>())
            netObj.enabled = false;

        // Rendre semi-transparent
        SetPreviewMaterial(previewInstance, previewMaterial);
    }

    private void UpdatePreviewPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPosition = adultController.transform.position + adultController.transform.forward * placeDistance;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            targetPosition = hit.point;

        targetPosition.y -= 0.9f;

        previewInstance.transform.position = targetPosition;
        previewInstance.transform.rotation = Quaternion.identity;
    }

    private void ConfirmPlacement()
    {
        if (previewInstance == null || previewSlot < 0) return;

        Vector3 pos = previewInstance.transform.position;
        Quaternion rot = previewInstance.transform.rotation;

        Destroy(previewInstance);
        previewInstance = null;

        adultManager.PlaceTrap(previewSlot, pos, rot);
    }

    private void CancelPreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
            previewSlot = -1;
            Debug.Log("[InventoryUI] Placement preview canceled");
        }
    }

    private void SetPreviewMaterial(GameObject obj, Material mat)
    {
        if (mat == null) return;

        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            r.material = mat;
        }
    }
}
