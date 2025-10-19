using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class NetworkDoor : NetworkBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private float openRotationY = 90f; // Rotation à appliquer quand ouverte
    [SerializeField] private float openSpeed = 2f; // Vitesse d'ouverture
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 2f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private NetworkSoundManager networkSoundManager;

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private GameObject interactPrompt; // UI "Press E to open"
    [SerializeField] private Material highlightMaterial;

    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);
    private bool isAnimating = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private MeshRenderer meshRenderer;
    private Material originalMaterial;

    private Transform playerInRange;
    private PlayerInputs playerInputs;

    private void Awake()
    {
        // Sauvegarder la rotation fermée initiale
        closedRotation = transform.rotation;
        
        // Calculer la rotation ouverte
        openRotation = closedRotation * Quaternion.Euler(0, openRotationY, 0);

        // Récupérer le renderer pour le highlight
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
        }

        // Cacher le prompt au départ
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
        playerInputs = new PlayerInputs();

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // S'abonner aux changements d'état de la porte
        isOpen.OnValueChanged += OnDoorStateChanged;

        // Appliquer l'état initial
        if (isOpen.Value)
        {
            transform.rotation = openRotation;
        }
        else
        {
            transform.rotation = closedRotation;
        }

        if (IsOwner)
        {
            playerInputs.PlayerControls.Enable();
            playerInputs.PlayerControls.Interact.performed += ctx => ToggleDoor();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isOpen.OnValueChanged -= OnDoorStateChanged;

        if (IsOwner)
        {
            playerInputs.PlayerControls.Interact.performed -= ctx => ToggleDoor();
            playerInputs.PlayerControls.Disable();
        }
    }

    private void Update()
    {
        if (isAnimating) return;

        CheckForNearbyPlayer();
        HandlePlayerInput();
    }

    /// <summary>
    /// Vérifie si un joueur est à portée
    /// </summary>
    private void CheckForNearbyPlayer()
    {
        // Chercher le joueur local le plus proche
        GameObject localPlayer = FindLocalPlayer();

        if (localPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, localPlayer.transform.position);

            if (distance <= interactionDistance)
            {
                playerInRange = localPlayer.transform;
                ShowInteractionPrompt(true);
                HighlightDoor(true);
            }
            else
            {
                if (playerInRange != null)
                {
                    playerInRange = null;
                    ShowInteractionPrompt(false);
                    HighlightDoor(false);
                }
            }
        }
    }

    /// <summary>
    /// Trouve le joueur local (celui contrôlé par ce client)
    /// </summary>
    private GameObject FindLocalPlayer()
    {
        // Chercher parmi tous les NetworkObjects celui qui appartient au joueur local
        foreach (var netObj in FindObjectsOfType<NetworkObject>())
        {
            if (netObj.IsOwner && (netObj.CompareTag("Adult") || netObj.CompareTag("Child")))
            {
                return netObj.gameObject;
            }
        }
        return null;
    }

    /// <summary>
    /// Gère l'input du joueur
    /// </summary>
    private void HandlePlayerInput()
    {
        // if (playerInRange == null) return;

        // bool interactPressed = false;

        // // Vérifier l'input (supporte à la fois l'ancien et le nouveau système)
        // if (Input.GetKeyDown(interactKey))
        // {
        //     interactPressed = true;
        // }

        // // Si vous utilisez le nouveau Input System
        // try
        // {
        //     if (Input.GetButtonDown(interactButtonName))
        //     {
        //         interactPressed = true;
        //     }
        // }
        // catch
        // {
        //     // Ignore si le bouton n'existe pas
        // }

        // if (interactPressed)
        // {
        //     ToggleDoor();
        // }
    }

    /// <summary>
    /// Toggle l'état de la porte
    /// </summary>
    private void ToggleDoor()
    {
        Debug.Log("ToggleDoor called");
        if (playerInRange == null) return;
        Debug.Log("Player is in range");
        if (isAnimating) return;

        // Demander au serveur de changer l'état
        ToggleDoorServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleDoorServerRpc()
    {
        if (playerInRange == null) return;
        if (isAnimating) return;

        // Inverser l'état
        isOpen.Value = !isOpen.Value;
    }

    /// <summary>
    /// Appelé quand l'état de la porte change (sur tous les clients)
    /// </summary>
    private void OnDoorStateChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            StartCoroutine(AnimateDoor(openRotation, openSound));
        }
        else
        {
            StartCoroutine(AnimateDoor(closedRotation, closeSound));
        }
    }

    /// <summary>
    /// Anime l'ouverture/fermeture de la porte
    /// </summary>
    private IEnumerator AnimateDoor(Quaternion targetRotation, AudioClip sound)
    {
        isAnimating = true;

        Quaternion startRotation = transform.rotation;
        float duration = 1f / openSpeed;
        float elapsed = 0f;

        // Jouer le son
        if (networkSoundManager != null && sound != null)
        {
            networkSoundManager.PlaySoundAtPosition(sound, transform.position);
        }

        // Animation fluide
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = openCurve.Evaluate(t);

            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);

            yield return null;
        }

        // S'assurer d'atteindre exactement la rotation cible
        transform.rotation = targetRotation;

        isAnimating = false;
    }

    /// <summary>
    /// Affiche/cache le prompt d'interaction
    /// </summary>
    private void ShowInteractionPrompt(bool show)
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(show);

            // Mettre à jour le texte si nécessaire
            var promptText = interactPrompt.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (promptText != null)
            {
                promptText.text = isOpen.Value ? $"Press Left Click to Close" : $"Press Left Click to Open";
            }
        }
    }

    /// <summary>
    /// Highlight la porte quand le joueur est proche
    /// </summary>
    private void HighlightDoor(bool highlight)
    {
        if (meshRenderer == null || highlightMaterial == null) return;

        if (highlight)
        {
            meshRenderer.material = highlightMaterial;
        }
        else
        {
            meshRenderer.material = originalMaterial;
        }
    }

    /// <summary>
    /// Méthodes publiques pour contrôle externe
    /// </summary>
    public bool IsOpen() => isOpen.Value;
    public bool IsAnimating() => isAnimating;

    public void ForceOpen()
    {
        if (IsServer)
        {
            isOpen.Value = true;
        }
    }

    public void ForceClose()
    {
        if (IsServer)
        {
            isOpen.Value = false;
        }
    }

    /// <summary>
    /// Gizmos pour visualiser la distance d'interaction
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        // Visualiser la rotation ouverte
        Gizmos.color = Color.green;
        Quaternion openRot = transform.rotation * Quaternion.Euler(0, openRotationY, 0);
        Vector3 openDirection = openRot * Vector3.forward;
        Gizmos.DrawRay(transform.position, openDirection * 2f);
    }
}