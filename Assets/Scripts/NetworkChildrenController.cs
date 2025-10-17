using UnityEngine;

[RequireComponent(typeof(ChildrenManager))]
public class NetworkChildrenController : NetworkPlayerController
{
    private ChildrenManager childrenManager;

    private void Awake() {
        base.Awake();
        childrenManager = GetComponent<ChildrenManager>();
    }

    protected override void HandleMovement() {
        if (!childrenManager.CanMove()) {
            ResetVelocity();
            return;
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
