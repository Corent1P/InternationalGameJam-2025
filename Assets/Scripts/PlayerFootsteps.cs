using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class PlayerFootsteps : NetworkBehaviour
{
    [Header("Footstep Settings")]
    [SerializeField] private float stepInterval = 0.5f; // Temps entre chaque pas
    [SerializeField] private float minSpeedForFootsteps = 0.1f; // Vitesse minimum pour faire du bruit
    [SerializeField] private bool playOnlyWhenGrounded = true;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.3f;

    [SerializeField] private NetworkSoundManager networkSoundManager;

    private Rigidbody rb;
    private float stepTimer = 0f;
    private bool isGrounded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkSoundManager = FindAnyObjectByType<NetworkSoundManager>();
    }

    private void Update()
    {
        if (!IsOwner) return; // Seul le propriétaire gère ses propres pas

        CheckGroundStatus();
        HandleFootsteps();
    }

    /// <summary>
    /// Vérifie si le joueur est au sol
    /// </summary>
    private void CheckGroundStatus()
    {
        if (!playOnlyWhenGrounded)
        {
            isGrounded = true;
            return;
        }

        // Raycast vers le bas pour vérifier le sol
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance, groundLayer);
    }

    /// <summary>
    /// Gère les sons de pas
    /// </summary>
    private void HandleFootsteps()
    {
        // Obtenir la vitesse horizontale du joueur
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float speed = horizontalVelocity.magnitude;

        // Le joueur marche-t-il assez vite ?
        bool isMoving = speed > minSpeedForFootsteps;

        if (isMoving && isGrounded)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                // Jouer le son de pas
                PlayFootstep();

                // Adapter l'intervalle en fonction de la vitesse
                float speedFactor = Mathf.Clamp(speed / 5f, 0.7f, 1.3f);
                stepTimer = stepInterval / speedFactor;
            }
        }
        else
        {
            // Réinitialiser le timer quand le joueur s'arrête
            stepTimer = 0f;
        }
    }

    /// <summary>
    /// Joue un son de pas à la position du joueur
    /// </summary>
    private void PlayFootstep()
    {
        Debug.Log("Playing footstep sound");
        if (networkSoundManager != null)
        {
            // Position légèrement en dessous du joueur (au niveau des pieds)
            Vector3 footPosition = transform.position + Vector3.down * 0.5f;
            networkSoundManager.PlayFootstepSound(footPosition);
        }
    }

    /// <summary>
    /// Gizmos pour debug
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (playOnlyWhenGrounded)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawLine(rayStart, rayStart + Vector3.down * groundCheckDistance);
        }
    }

    /// <summary>
    /// Méthode publique pour forcer un son de pas (utile pour le dash par exemple)
    /// </summary>
    public void ForceFootstep()
    {
        if (IsOwner)
        {
            PlayFootstep();
        }
    }
}