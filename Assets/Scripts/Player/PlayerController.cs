using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private float verticalVelocity = 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Only rotate if there is horizontal input
        if (Mathf.Abs(h) > 0.01f)
            transform.Rotate(0f, h * rotateSpeed * Time.deltaTime, 0f);

        // Gravity
        if (characterController.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        // Only move forward/back if there is vertical input
        Vector3 move = Vector3.zero;
        if (Mathf.Abs(v) > 0.01f)
            move = transform.forward * v * moveSpeed * Time.deltaTime;

        move.y = verticalVelocity * Time.deltaTime;

        characterController.Move(move);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            GetComponent<Renderer>().material.color = Color.green;
            Debug.Log("[MMO] Local player spawned");
        }
        else
        {
            GetComponent<Renderer>().material.color = Color.red;
        }
    }
}