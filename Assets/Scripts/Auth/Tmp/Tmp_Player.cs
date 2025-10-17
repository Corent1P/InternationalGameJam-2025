using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Collections;

public class Tmp_Player : NetworkBehaviour
{
    // private InputSystem_Actions action;
    public Animator animator;
    public Rigidbody rb;
    public LayerMask groundLayer;
    public GameObject projectilePrefab;
    public Transform firePoint;

    public float jumpForce;
    public float speed;
    public bool OnMove = false;
    public Vector2 moveInput = Vector2.zero;

    // !----- PLAYER 5 -------------------------------------
    public NetworkVariable<FixedString32Bytes> accountID = new();
    public NetworkVariable<int> health = new(100);
    public NetworkVariable<int> attack = new(0);
    // !-----------------------------------------------------

    // public void OnEnable()
    // {
    //     action.Enable();
    //     action.Player.Move.performed += OnMovePerformed;
    //     action.Player.Move.canceled += OnMoveCanceled;
    //     action.Player.Jump.performed += OnJump;
    //     action.Player.Attack.performed += OnAttack;
    // }

    // public void OnDisable()
    // {

    //     action.Player.Move.performed -= OnMovePerformed;
    //     action.Player.Move.canceled -= OnMoveCanceled;
    //     action.Player.Jump.performed -= OnJump;
    //     action.Disable();
    // }
    // private void Awake()
    // {
    //     action = new InputSystem_Actions();
    // }
    void Start()
    {

    }
    void Update()
    {
        if (!IsOwner) return;

        Movement();
        GroundCheckRpc();
    }

    public void Movement()
    {
        if (OnMove) MovementRpc(moveInput);

    }

    [Rpc(SendTo.Server)]
    public void MovementRpc(Vector2 moveInput)
    {
        Vector3 move = new Vector3(moveInput.x, rb.linearVelocity.y, moveInput.y);
        rb.linearVelocity = move * speed;

    }
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        OnMove = true;
    }
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        OnMove = false;
    }
    private void OnJump(InputAction.CallbackContext context)
    {
        PerformJumpRpc();
    }
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        ShootRpc();
    }
    [Rpc(SendTo.Server)]
    public void PerformJumpRpc()
    {
        transform.GetComponent<Rigidbody>().AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        animator.SetTrigger("Jump");
    }
    [Rpc(SendTo.Server)]
    public void GroundCheckRpc()
    {
        if (Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer))
        {
            animator.SetBool("Grounded", true);
            animator.SetBool("FreeFall", false);
        }
        else
        {
            animator.SetBool("Grounded", false);
            animator.SetBool("FreeFall", true);
        }
    }

    [Rpc(SendTo.Server)]
    public void ShootRpc()
    {
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        proj.GetComponent<NetworkObject>().Spawn(true);

        proj.GetComponent<Rigidbody>().AddForce(firePoint.rotation * Vector3.forward * 5, ForceMode.Impulse);
    }

    // !----- PLAYER 5 -------------------------------------
    public override void OnNetworkSpawn()
    {
        // if (IsOwner)
        // {
        //     Debug.Log("Player5: I am the owner of this object." + NetworkManager.Singleton.LocalClientId);
        // }
        // else
        // {
        //     Debug.Log("Player5: I am not the owner of this object." + NetworkManager.Singleton.LocalClientId);
        // }
    }

    public override void OnNetworkDespawn()
    {
        GameManager5.Instance.playerStatesByID[accountID.Value.ToString()] = new PlayerData_tmp(NetworkManager.Singleton.LocalClientId, transform.position, health.Value, attack.Value);
        Debug.Log($"Player5: Despawning player {accountID.Value} and saving state.");
        Debug.Log($"Player5: Saved state - Position: {transform.position}, Health: {health.Value}, Attack: {attack.Value}");
    }

    public void SetData(PlayerData_tmp data)
    {
        accountID.Value = data.ClientId.ToString();
        health.Value = data.Health;
        attack.Value = data.Attack;
        transform.position = data.Position;
    }
    // !-----------------------------------------------------

}
