using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(AdultManager))]
[RequireComponent(typeof(Rigidbody))]
public class AdultCatchSystem : NetworkBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCooldown = 2f;
    [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Catch Settings")]
    [SerializeField] private float catchRadius = 1.5f;
    [SerializeField] private LayerMask childrenLayer;
    [SerializeField] private int coinsReward = 10;

    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem dashEffect;
    [SerializeField] private ParticleSystem catchEffect;
    [SerializeField] private NetworkSoundManager networkSoundManager;

    private AdultManager adultManager;
    private Rigidbody rb;
    private PlayerInputs playerInputs;
    
    private bool isDashing = false;
    private bool canDash = true;
    private float lastDashTime = -999f;

    private void Awake()
    {
        adultManager = GetComponent<AdultManager>();
        rb = GetComponent<Rigidbody>();
        networkSoundManager = FindAnyObjectByType<NetworkSoundManager>();
        
        // Initialiser les inputs
        playerInputs = new PlayerInputs();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            playerInputs.PlayerControls.Enable();
            playerInputs.PlayerControls.Dash.performed += ctx => TryDashCatch();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsOwner)
        {
            playerInputs.PlayerControls.Dash.performed -= ctx => TryDashCatch();
            playerInputs.PlayerControls.Disable();
        }
    }

    private void OnDestroy()
    {
        playerInputs?.Dispose();
    }

    /// <summary>
    /// Appelé quand le joueur appuie sur Dash
    /// </summary>
    private void TryDashCatch()
    {
        if (!IsOwner) return;
        if (isDashing) return;

        // Vérifier le cooldown
        if (Time.time - lastDashTime < dashCooldown)
        {
            Debug.Log($"Dash on cooldown! Wait {(dashCooldown - (Time.time - lastDashTime)):F1}s");
            return;
        }

        // Lancer le dash
        RequestDashServerRpc(transform.position, transform.forward);
    }

    /// <summary>
    /// Demande au serveur de lancer le dash
    /// </summary>
    [ServerRpc]
    private void RequestDashServerRpc(Vector3 startPos, Vector3 direction)
    {
        if (isDashing) return;
        
        // Lancer le dash pour tous les clients
        PerformDashClientRpc(startPos, direction);
    }

    /// <summary>
    /// Execute le dash visuellement sur tous les clients
    /// </summary>
    [ClientRpc]
    private void PerformDashClientRpc(Vector3 startPos, Vector3 direction)
    {
        if (isDashing) return;
        
        // Déclencher l'animation de catch au début du dash
        NetworkAdultController adultController = GetComponent<NetworkAdultController>();
        if (adultController != null)
        {
            adultController.TriggerCatchAnimation();
            Debug.Log($"[AdultCatchSystem] Catch animation triggered in PerformDashClientRpc");
        }
        else
        {
            Debug.LogError($"[AdultCatchSystem] NetworkAdultController NOT FOUND!");
        }
        
        StartCoroutine(DashCoroutine(startPos, direction));
    }

    /// <summary>
    /// Coroutine du dash avec détection de collision
    /// </summary>
    private IEnumerator DashCoroutine(Vector3 startPos, Vector3 dashDirection)
    {
        isDashing = true;
        canDash = false;
        lastDashTime = Time.time;

        // Effet visuel de départ
        if (dashEffect != null && IsOwner)
        {
            dashEffect.Play();
        }

        // Son du dash
        if (networkSoundManager != null)
        {
            networkSoundManager.PlayDashSound(transform.position);
        }

        Vector3 direction = dashDirection.normalized;
        Vector3 targetPos = startPos + direction * dashDistance;
        float elapsed = 0f;
        Vector3 previousPos = startPos;

        // Désactiver temporairement la gravité
        bool wasUseGravity = rb.useGravity;
        rb.useGravity = false;

        // Rayon pour la détection de collision (plus petit pour éviter le sol)
        float checkRadius = (GetComponent<CapsuleCollider>()?.radius ?? 0.5f) * 0.5f;
        // Hauteur pour faire le raycast au centre du personnage (pas au sol)
        float raycastHeight = GetComponent<CapsuleCollider>()?.height ?? 2f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;
            float curveValue = dashCurve.Evaluate(t);

            // Calculer la prochaine position
            Vector3 desiredPos = Vector3.Lerp(startPos, targetPos, curveValue);
            Vector3 moveDirection = desiredPos - previousPos;
            float moveDistance = moveDirection.magnitude;

            // Vérifier s'il y a un obstacle devant
            if (moveDistance > 0.001f)
            {
                RaycastHit hit;
                // Position de départ du raycast (au centre du personnage, pas au sol)
                Vector3 rayStart = previousPos + Vector3.up * (raycastHeight * 0.5f);
                
                // Raycast horizontal seulement pour détecter les murs
                Vector3 horizontalDirection = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
                float horizontalDistance = new Vector3(moveDirection.x, 0, moveDirection.z).magnitude;
                
                if (horizontalDistance > 0.001f && Physics.Raycast(rayStart, horizontalDirection, out hit, horizontalDistance + checkRadius))
                {
                    // Il y a un mur ! S'arrêter juste avant
                    if (!hit.collider.CompareTag("Child")) // Ne pas s'arrêter sur les enfants
                    {
                        // Calculer la position d'arrêt
                        float stopDistance = Mathf.Max(0, hit.distance - checkRadius);
                        desiredPos = previousPos + horizontalDirection * stopDistance;
                        rb.MovePosition(desiredPos);
                        Debug.Log("Dash stopped by wall!");
                        break; // Arrêter le dash
                    }
                }

                // Déplacer le personnage si pas de collision
                rb.MovePosition(desiredPos);
                previousPos = desiredPos;
            }

            // Sur le serveur uniquement, vérifier les collisions avec les enfants
            if (IsServer)
            {
                CheckForChildrenInRange();
            }

            yield return null;
        }

        // Restaurer la physique
        rb.useGravity = wasUseGravity;

        isDashing = false;

        // Le cooldown se gère automatiquement via lastDashTime
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    /// <summary>
    /// Vérifie si un enfant est à portée pendant le dash (Server only)
    /// </summary>
    private void CheckForChildrenInRange()
    {
        if (!IsServer) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, catchRadius, childrenLayer);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Child"))
            {
                ChildrenManager child = hit.GetComponent<ChildrenManager>();
                
                if (child != null && !child.IsCaught())
                {
                    // Attraper l'enfant !
                    CatchChild(child);
                    break; // Un seul enfant à la fois
                }
            }
        }
    }

    /// <summary>
    /// Attrape un enfant (Server only)
    /// </summary>
    private void CatchChild(ChildrenManager child)
    {
        if (!IsServer) return;

        // Marquer l'enfant comme attrapé
        child.SetCaught(true);

        // Faire tomber des bonbons si l'enfant en a
        int candyCount = child.GetCandyCount();
        for (int i = 0; i < candyCount; i++)
        {
            child.RemoveCandy();
            // TODO: Spawner les bonbons dans le monde
        }

        // Récompenser l'adulte
        adultManager.AddCoins(coinsReward);

        // Effet visuel de catch sur tous les clients
        PlayCatchEffectClientRpc(child.NetworkObjectId);

        //TODO: Envoyer le gosse en prison
        Debug.Log($"🎯 Adult caught child! Reward: {coinsReward} coins. Child had {candyCount} candies.");
    }

    /// <summary>
    /// Joue l'effet visuel de catch sur tous les clients
    /// </summary>
    [ClientRpc]
    private void PlayCatchEffectClientRpc(ulong childNetworkId)
    {
        if (catchEffect != null)
        {
            catchEffect.Play();
        }

        // Trouver l'enfant et jouer un effet sur lui aussi
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(childNetworkId, out NetworkObject childNetObj))
        {
            // TODO: Jouer une animation ou effet sur l'enfant
            Debug.Log($"Child {childNetObj.name} was caught!");
        }
    }

    /// <summary>
    /// Dessine les gizmos pour visualiser la portée
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Portée du catch
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchRadius);

        // Distance du dash
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * dashDistance);
    }

    /// <summary>
    /// Getters publics
    /// </summary>
    public bool IsDashing() => isDashing;
    public bool CanDash() => canDash && !isDashing;
    public float GetDashCooldown() => Mathf.Max(0, dashCooldown - (Time.time - lastDashTime));
}