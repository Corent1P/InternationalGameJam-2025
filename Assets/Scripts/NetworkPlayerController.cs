using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public abstract class NetworkPlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Mouse")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;

    [Header("Network")]
    public bool disableNonLocalCamera = true;

    protected Rigidbody rb;
    protected Vector2 moveInput;
    protected bool isGrounded = true;

    private PlayerInputs inputActions;
    private Vector2 lookInput;
    private float xRotation = 0f;

    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
    private NetworkVariable<float> networkCameraRotation = new NetworkVariable<float>();

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new PlayerInputs();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Si c'est le joueur local
        if (IsOwner)
        {
            // Activer les inputs
            inputActions.PlayerControls.Enable();
            inputActions.PlayerControls.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            inputActions.PlayerControls.Move.canceled += ctx => moveInput = Vector2.zero;

            inputActions.PlayerControls.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
            inputActions.PlayerControls.Look.canceled += ctx => lookInput = Vector2.zero;

            inputActions.PlayerControls.Jump.performed += ctx => Jump();

            // Verrouiller et cacher le curseur
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Activer la caméra locale
            if (cameraTransform != null)
            {
                Camera cam = cameraTransform.GetComponent<Camera>();
                if (cam != null) cam.enabled = true;

                AudioListener listener = cameraTransform.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }
        }
        else
        {
            // Désactiver la caméra des autres joueurs
            if (disableNonLocalCamera && cameraTransform != null)
            {
                Camera cam = cameraTransform.GetComponent<Camera>();
                if (cam != null) cam.enabled = false;

                AudioListener listener = cameraTransform.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }

        // S'abonner aux changements de variables réseau (pour les clients non-propriétaires)
        if (!IsOwner)
        {
            networkPosition.OnValueChanged += OnPositionChanged;
            networkRotation.OnValueChanged += OnRotationChanged;
            networkCameraRotation.OnValueChanged += OnCameraRotationChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (inputActions != null)
        {
            inputActions.PlayerControls.Disable();
        }

        if (!IsOwner)
        {
            networkPosition.OnValueChanged -= OnPositionChanged;
            networkRotation.OnValueChanged -= OnRotationChanged;
            networkCameraRotation.OnValueChanged -= OnCameraRotationChanged;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleLook();

        // Envoyer la position au serveur à chaque frame
        UpdateNetworkStateServerRpc(transform.position, transform.rotation, xRotation);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        HandleMovement();
    }

    protected virtual void HandleMovement()
    {
        Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        Vector3 targetVelocity = moveDirection * moveSpeed;
        Vector3 currentVelocity = rb.linearVelocity;

        // Conserve la vitesse verticale (gravité / saut)
        targetVelocity.y = currentVelocity.y;
        rb.linearVelocity = targetVelocity;
    }

    protected virtual void Jump()
    {
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

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

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

    #region Network Synchronization

    [ServerRpc]
    private void UpdateNetworkStateServerRpc(Vector3 position, Quaternion rotation, float camRotation)
    {
        // Le serveur met à jour les NetworkVariables
        networkPosition.Value = position;
        networkRotation.Value = rotation;
        networkCameraRotation.Value = camRotation;
    }

    // Callbacks appelés quand les NetworkVariables changent (pour les clients non-propriétaires)
    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        // Interpolation smooth vers la nouvelle position
        transform.position = Vector3.Lerp(transform.position, newValue, Time.deltaTime * 10f);
    }

    private void OnRotationChanged(Quaternion oldValue, Quaternion newValue)
    {
        // Interpolation smooth vers la nouvelle rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, newValue, Time.deltaTime * 10f);
    }

    private void OnCameraRotationChanged(float oldValue, float newValue)
    {
        if (cameraTransform != null && !IsOwner)
        {
            cameraTransform.localRotation = Quaternion.Euler(newValue, 0f, 0f);
        }
    }

    #endregion
}
