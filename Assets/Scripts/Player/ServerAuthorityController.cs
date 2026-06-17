using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Phase 3 - Server Authority with Client Prediction
/// 
/// How it works:
/// 1. Client captures input every frame and sends to server
/// 2. Client also predicts movement locally (feels instant)
/// 3. Server validates and simulates movement authoritatively  
/// 4. Server broadcasts authoritative position to all clients
/// 5. Client reconciles - if server position differs from
///    prediction, snap/correct back to server position
/// 
/// This is exactly how WoW, FFXIV, Knight Online work.
/// </summary>

public struct InputPayload : INetworkSerializable
{
    public int tick;
    public Vector3 inputDirection;
    public float rotation;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref tick);
        serializer.SerializeValue(ref inputDirection);
        serializer.SerializeValue(ref rotation);
    }
}

public struct StatePayload : INetworkSerializable
{
    public int tick;
    public Vector3 position;
    public Quaternion rotation;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref tick);
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
    }
}

[RequireComponent(typeof(CharacterController))]
public class ServerAuthorityController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float gravity = -20f;

    [Header("Reconciliation")]
    [SerializeField] private float reconcileThreshold = 0.5f;

    private CharacterController characterController;
    private float verticalVelocity = 0f;

    // Tick system - like a heartbeat for the game
    private int currentTick = 0;
    private float tickTimer = 0f;
    private float tickRate = 0.05f; // 20 ticks per second

    // Client-side prediction buffer
    // Stores recent inputs so we can replay them during reconciliation
    private const int BUFFER_SIZE = 64;
    private InputPayload[] inputBuffer = new InputPayload[BUFFER_SIZE];
    private StatePayload[] stateBuffer = new StatePayload[BUFFER_SIZE];

    // Latest state from server
    private StatePayload latestServerState;
    private bool hasServerState = false;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            GetComponent<Renderer>().material.color = Color.green;
            Debug.Log("[MMO] ServerAuthority: Local player spawned");
        }
        else
        {
            GetComponent<Renderer>().material.color = Color.red;
        }
    }

    private void Update()
    {
        // Only the owning client runs prediction
        if (!IsOwner) return;

        tickTimer += Time.deltaTime;

        // Run one tick per tickRate interval (20 times/sec)
        while (tickTimer >= tickRate)
        {
            tickTimer -= tickRate;
            HandleClientTick();
            currentTick++;
        }
    }

    // ─── CLIENT SIDE ──────────────────────────────────────────

    private void HandleClientTick()
    {
        // Step 1: Check if server sent a correction
        if (hasServerState)
            HandleServerReconciliation();

        // Step 2: Gather this frame's input
        InputPayload input = GatherInput(currentTick);

        // Step 3: Store input in buffer (for reconciliation replay)
        int bufferIndex = currentTick % BUFFER_SIZE;
        inputBuffer[bufferIndex] = input;

        // Step 4: Predict movement locally (instant feel)
        StatePayload predictedState = SimulateMovement(input, transform.position, transform.rotation);
        stateBuffer[bufferIndex] = predictedState;

        // Step 5: Send input to server for authoritative simulation
        SubmitInputServerRpc(input);
    }

    private InputPayload GatherInput(int tick)
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        return new InputPayload
        {
            tick = tick,
            inputDirection = new Vector3(0, 0, v),
            rotation = h
        };
    }

    private void HandleServerReconciliation()
    {
        hasServerState = false;

        int bufferIndex = latestServerState.tick % BUFFER_SIZE;
        StatePayload predicted = stateBuffer[bufferIndex];

        // Compare server position vs our prediction
        float positionError = Vector3.Distance(
            latestServerState.position,
            predicted.position
        );

        // If difference is too large — correct it
        if (positionError > reconcileThreshold)
        {
            Debug.Log($"[MMO] Reconciling! Error: {positionError:F2}m");

            // Snap to server position
            characterController.enabled = false;
            transform.position = latestServerState.position;
            transform.rotation = latestServerState.rotation;
            characterController.enabled = true;

            // Replay all inputs from server tick to now
            int replayTick = latestServerState.tick;
            while (replayTick < currentTick)
            {
                int replayIndex = replayTick % BUFFER_SIZE;
                StatePayload replayState = SimulateMovement(
                    inputBuffer[replayIndex],
                    transform.position,
                    transform.rotation
                );
                stateBuffer[replayIndex] = replayState;
                replayTick++;
            }
        }
    }

    // ─── SERVER SIDE ──────────────────────────────────────────

    [ServerRpc]
    private void SubmitInputServerRpc(InputPayload input)
    {
        // Server simulates the movement authoritatively
        StatePayload serverState = SimulateMovement(
            input,
            transform.position,
            transform.rotation
        );

        // Send authoritative state back to the owning client
        SendStateClientRpc(serverState);
    }

    [ClientRpc]
    private void SendStateClientRpc(StatePayload state)
    {
        // Only the owner needs to reconcile
        if (IsOwner)
        {
            latestServerState = state;
            hasServerState = true;
        }
    }

    // ─── SHARED SIMULATION ────────────────────────────────────
    // This runs on BOTH client (prediction) and server (authority)
    // Using the exact same logic = consistent results

    private StatePayload SimulateMovement(
        InputPayload input,
        Vector3 currentPosition,
        Quaternion currentRotation)
    {
        // Temporarily move to the state we're simulating from
        characterController.enabled = false;
        transform.position = currentPosition;
        transform.rotation = currentRotation;
        characterController.enabled = true;

        // Rotate
        if (Mathf.Abs(input.rotation) > 0.01f)
            transform.Rotate(0f, input.rotation * rotateSpeed * tickRate, 0f);

        // Gravity
        if (characterController.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * tickRate;

        // Move
        Vector3 move = Vector3.zero;
        if (Mathf.Abs(input.inputDirection.z) > 0.01f)
            move = transform.forward * input.inputDirection.z * moveSpeed * tickRate;

        move.y = verticalVelocity * tickRate;
        characterController.Move(move);

        return new StatePayload
        {
            tick = input.tick,
            position = transform.position,
            rotation = transform.rotation
        };
    }
}