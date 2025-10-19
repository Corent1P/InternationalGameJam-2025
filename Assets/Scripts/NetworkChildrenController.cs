using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(ChildrenManager))]
public class NetworkChildrenController : NetworkPlayerController
{
    private ChildrenManager childrenManager;
    private Animator animator;
    
    // Variable réseau pour synchroniser la vitesse de l'animation
    private NetworkVariable<float> networkAnimSpeed = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        base.Awake();
        childrenManager = GetComponent<ChildrenManager>();
        animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // S'abonner aux changements de la vitesse d'animation pour tous les clients
        networkAnimSpeed.OnValueChanged += OnAnimSpeedChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        networkAnimSpeed.OnValueChanged -= OnAnimSpeedChanged;
    }

    private void OnAnimSpeedChanged(float oldValue, float newValue)
    {
        // Mettre à jour l'animator quand la valeur réseau change
        if (animator != null)
        {
            animator.SetFloat("Speed", newValue);
        }
    }

    protected override void HandleMovement() {
        if (!childrenManager.CanMove()) {
            ResetVelocity();
            // Mettre à jour la vitesse réseau uniquement si c'est le propriétaire
            if (IsOwner)
            {
                networkAnimSpeed.Value = 0f;
            }
            return;
        }
        
        // Mettre à jour la vitesse réseau uniquement si c'est le propriétaire
        if (IsOwner)
        {
            if (moveInput == Vector2.zero)
                networkAnimSpeed.Value = 0f;
            else
                networkAnimSpeed.Value = 1f;
        }

        float multiplier = childrenManager.GetSpeedMultiplier();

        Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        Vector3 targetVelocity = moveDirection * (moveSpeed * multiplier);
        Vector3 currentVelocity = rb.linearVelocity;

        targetVelocity.y = currentVelocity.y;
        rb.linearVelocity = targetVelocity;
    }

    protected override void Jump() {
        if (!childrenManager.CanMove())
            return;

        if (isGrounded) {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    private void ResetVelocity() {
        Vector3 velocity = rb.linearVelocity;

        velocity.x = 0f;
        velocity.z = 0f;
        rb.linearVelocity = velocity;
    }

    public void UseAbility() => childrenManager?.UseAbility();
}
