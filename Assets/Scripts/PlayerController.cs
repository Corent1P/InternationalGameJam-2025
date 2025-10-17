using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Mouse")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;

    private Coroutine slowCoroutine;
    private float originalSpeed;
    private bool canMove = true;
    private Rigidbody rb;
    private PlayerInputs inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;
    private bool isGrounded = true;

    public bool CanMove
    {
        get { return canMove; }
        set { canMove = value; }
    }

    public void ApplySlow(float slowMultiplier, float duration)
    {
        if (slowCoroutine != null)
            StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowDownRoutine(slowMultiplier, duration));
    }

    private IEnumerator SlowDownRoutine(float slowMultiplier, float duration)
    {
        moveSpeed *= slowMultiplier;
        yield return new WaitForSeconds(duration);
        moveSpeed = originalSpeed;
        slowCoroutine = null;
    }

    private void Start()
    {
        originalSpeed = moveSpeed;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new PlayerInputs();
    }

    private void OnEnable()
    {
        inputActions.PlayerControls.Enable();
        inputActions.PlayerControls.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.PlayerControls.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.PlayerControls.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.PlayerControls.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.PlayerControls.Jump.performed += ctx => Jump();
    }

    private void OnDisable()
    {
        inputActions.PlayerControls.Disable();
    }

    private void Update()
    {
        HandleLook();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void resetVelociyty()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        rb.linearVelocity = velocity;
    }

    private void HandleMovement()
    {
        if (!canMove) {
            resetVelociyty();
            return;
        }

        Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        Vector3 targetVelocity = moveDirection * moveSpeed;
        Vector3 currentVelocity = rb.linearVelocity;

        // Conserve la vitesse verticale (gravité / saut)
        targetVelocity.y = currentVelocity.y;
        rb.linearVelocity = targetVelocity;
    }

    private void Jump()
    {
        if (!canMove)
            return;

        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    private void HandleLook()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Détection sol simplifiée (par contact)
        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f)
            {
                isGrounded = true;
            }
        }
    }
}
