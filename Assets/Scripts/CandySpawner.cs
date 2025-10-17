using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Unity.Netcode;

public class CandySpawner : NetworkBehaviour
{
    [Header("Candy Settings")]
    public GameObject candyPrefab;
    public Transform spawnPoint;
    public float respawnCooldown = 10f;
    public float launchForce = 3f;

    [Header("Interaction Settings")]
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Feedback")]
    public Text interactionText;
    public string interactionHint = "Press E to collect candy";
    public string inventoryFullHint = "Inventory full!";

    private NetworkVariable<bool> candyAvailable = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<ulong> currentCandyNetworkId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private GameObject currentCandy;
    private bool isOnCooldown = false;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (interactionText != null)
            interactionText.gameObject.SetActive(false);

        if (IsServer) {
            SpawnCandy();
        }

        if (!IsServer) {
            currentCandyNetworkId.OnValueChanged += OnCandyNetworkIdChanged;
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (!IsServer) {
            currentCandyNetworkId.OnValueChanged -= OnCandyNetworkIdChanged;
        }
    }

    private void OnCandyNetworkIdChanged(ulong oldId, ulong newId)
    {
        if (newId == 0) {
            currentCandy = null;
        } else {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(newId, out NetworkObject netObj)) {
                currentCandy = netObj.gameObject;
            }
        }
    }

    private void Update()
    {
        // Seul le joueur local vérifie l'interaction
        NetworkChildrenController localChild = GetLocalChild();

        if (localChild == null)
            return;

        // Si pas de candy disponible, on cache le texte
        if (!candyAvailable.Value || currentCandy == null) {
            HideInteractionText();
            return;
        }

        Camera playerCamera = Camera.main;
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance)) {
            if (hit.collider != null && hit.collider.gameObject == currentCandy) {
                ChildrenManager manager = localChild.GetComponent<ChildrenManager>();

                if (manager != null) {
                    if (manager.IsCandyFull()) {
                        ShowInteractionText(inventoryFullHint);
                    }
                    else {
                        ShowInteractionText(interactionHint);

                        if (Input.GetKeyDown(interactKey)) {
                            RequestCollectCandyServerRpc(localChild.NetworkObjectId);
                        }
                    }
                }
                return;
            }
        }

        HideInteractionText();
    }

    private NetworkChildrenController GetLocalChild() {
        NetworkChildrenController[] children = FindObjectsOfType<NetworkChildrenController>();

        foreach (var child in children) {
            if (child.IsOwner)
                return child;
        }
        return null;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCollectCandyServerRpc(ulong childNetworkId) {
        if (!candyAvailable.Value || currentCandy == null) {
            Debug.LogWarning("Candy not available or null on server");
            return;
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(childNetworkId, out NetworkObject networkObject)) {
            NetworkChildrenController child = networkObject.GetComponent<NetworkChildrenController>();
            if (child != null) {
                ChildrenManager manager = child.GetComponent<ChildrenManager>();

                if (manager != null && !manager.IsCandyFull()) {
                    if (manager.AddCandy()) {
                        // Despawn du candy
                        if (currentCandy != null) {
                            NetworkObject candyNetObj = currentCandy.GetComponent<NetworkObject>();
                            if (candyNetObj != null && candyNetObj.IsSpawned) {
                                candyNetObj.Despawn(true);
                            }
                        }

                        // Reset des variables réseau
                        currentCandy = null;
                        currentCandyNetworkId.Value = 0;
                        candyAvailable.Value = false;

                        Debug.Log($"🧍 {child.name} collected the candy!");

                        // Cache le texte pour tous les clients
                        HideInteractionTextClientRpc();

                        // Lance le respawn
                        if (!isOnCooldown) {
                            StartCoroutine(CandyRespawnCooldown());
                        }
                    }
                }
            }
        }
    }

    [ClientRpc]
    private void HideInteractionTextClientRpc() {
        HideInteractionText();
    }

    private void SpawnCandy() {
        if (!IsServer) {
            Debug.LogWarning("SpawnCandy called on client!");
            return;
        }

        if (candyPrefab == null || spawnPoint == null) {
            Debug.LogError("CandySpawner missing prefab or spawn point!");
            return;
        }

        // Instanciation du candy
        currentCandy = Instantiate(candyPrefab, spawnPoint.position, spawnPoint.rotation);

        // Setup du NetworkObject
        NetworkObject netObj = currentCandy.GetComponent<NetworkObject>();

        if (netObj == null) {
            Debug.LogError("Candy prefab is missing NetworkObject component!");
            Destroy(currentCandy);
            return;
        }

        // Spawn sur le réseau
        netObj.Spawn();

        // Setup de la physique
        Rigidbody rb = currentCandy.GetComponent<Rigidbody>();

        if (rb == null) {
            rb = currentCandy.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.isKinematic = false;

        // Lance le candy
        Vector3 randomDirection = Vector3.up + new Vector3(
            Random.Range(-0.2f, 0.2f),
            0,
            Random.Range(-0.2f, 0.2f)
        );
        rb.AddForce(randomDirection.normalized * launchForce, ForceMode.Impulse);

        // Met à jour les variables réseau
        currentCandyNetworkId.Value = netObj.NetworkObjectId;
        candyAvailable.Value = true;
        isOnCooldown = false;

        Debug.Log($"🍬 Candy spawned on network! NetworkObjectId: {netObj.NetworkObjectId}");
    }

    private IEnumerator CandyRespawnCooldown() {
        isOnCooldown = true;
        Debug.Log($"⏳ Waiting {respawnCooldown} seconds before next candy...");

        yield return new WaitForSeconds(respawnCooldown);

        isOnCooldown = false;

        if (IsServer) {
            SpawnCandy();
        }
    }

    private void ShowInteractionText(string text = null) {
        if (interactionText != null)
        {
            interactionText.text = text ?? interactionHint;
            interactionText.gameObject.SetActive(true);
        }
    }

    private void HideInteractionText() {
        if (interactionText != null && interactionText.gameObject.activeSelf)
        {
            interactionText.gameObject.SetActive(false);
        }
    }
}
