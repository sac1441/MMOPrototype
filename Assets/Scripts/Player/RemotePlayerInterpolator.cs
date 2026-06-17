using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class RemotePlayerInterpolator : NetworkBehaviour
{
    [Header("Interpolation Settings")]
    [SerializeField] private float smoothSpeed = 15f;
    [SerializeField] private float snapDistance = 5f;

    public NetworkVariable<Vector3> SyncPosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<Quaternion> SyncRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        targetPosition = transform.position;
        targetRotation = transform.rotation;

        if (!IsOwner)
        {
            SyncPosition.OnValueChanged += OnPositionChanged;
            SyncRotation.OnValueChanged += OnRotationChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        SyncPosition.OnValueChanged -= OnPositionChanged;
        SyncRotation.OnValueChanged -= OnRotationChanged;
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (IsOwner)
        {
            // Push our position to server every frame
            if (IsServer)
            {
                SyncPosition.Value = transform.position;
                SyncRotation.Value = transform.rotation;
            }
            else
            {
                UpdateServerRpc(transform.position, transform.rotation);
            }
        }
        else
        {
            // Smoothly move remote player toward target
            float distance = Vector3.Distance(transform.position, targetPosition);

            // Snap if too far (respawn, teleport)
            if (distance > snapDistance)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                return;
            }

            // Smooth interpolation
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                smoothSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                smoothSpeed * Time.deltaTime
            );
        }
    }

    [ServerRpc]
    private void UpdateServerRpc(Vector3 position, Quaternion rotation)
    {
        SyncPosition.Value = position;
        SyncRotation.Value = rotation;
    }

    private void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        targetPosition = newPos;
    }

    private void OnRotationChanged(Quaternion oldRot, Quaternion newRot)
    {
        targetRotation = newRot;
    }
}