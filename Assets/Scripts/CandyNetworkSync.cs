using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class CandyNetworkSync : NetworkBehaviour
{
    private Rigidbody rb;

    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<Vector3> networkAngularVelocity = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Sync Settings")]
    [SerializeField] private float positionLerpSpeed = 10f;
    [SerializeField] private float rotationLerpSpeed = 10f;
    [SerializeField] private float syncInterval = 0.1f;

    private float syncTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) {
            networkPosition.OnValueChanged += OnPositionChanged;
            networkRotation.OnValueChanged += OnRotationChanged;
            networkVelocity.OnValueChanged += OnVelocityChanged;
            networkAngularVelocity.OnValueChanged += OnAngularVelocityChanged;

            rb.isKinematic = true;
        }
        else {
            rb.isKinematic = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!IsServer) {
            networkPosition.OnValueChanged -= OnPositionChanged;
            networkRotation.OnValueChanged -= OnRotationChanged;
            networkVelocity.OnValueChanged -= OnVelocityChanged;
            networkAngularVelocity.OnValueChanged -= OnAngularVelocityChanged;
        }
    }

    private void FixedUpdate()
    {
        if (IsServer) {
            syncTimer += Time.fixedDeltaTime;

            if (syncTimer >= syncInterval) {
                syncTimer = 0f;

                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;
                networkVelocity.Value = rb.linearVelocity;
                networkAngularVelocity.Value = rb.angularVelocity;
            }
        }
    }

    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        if (!IsServer) {
            transform.position = newValue;
        }
    }

    private void OnRotationChanged(Quaternion oldValue, Quaternion newValue)
    {
        if (!IsServer) {
            transform.rotation = newValue;
        }
    }

    private void OnVelocityChanged(Vector3 oldValue, Vector3 newValue)
    {
        // Info pour le débogage
    }

    private void OnAngularVelocityChanged(Vector3 oldValue, Vector3 newValue)
    {
        // Info pour le débogage
    }
}
