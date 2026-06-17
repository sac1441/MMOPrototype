using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float spawnRadius = 5f;

    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();

        // Auto-find prefab if not assigned in Inspector
        if (playerPrefab == null)
        {
            // Try to get it from NetworkManager's registered prefab list
            if (networkManager.NetworkConfig.PlayerPrefab != null)
            {
                playerPrefab = networkManager.NetworkConfig.PlayerPrefab;
                Debug.Log("[MMO] PlayerSpawner: Auto-assigned prefab from NetworkManager");
            }
            else
            {
                Debug.LogError("[MMO] PlayerSpawner: No player prefab assigned!");
            }
        }
    }

    private void OnEnable()
    {
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDisable()
    {
        if (networkManager == null) return;
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!networkManager.IsServer) return;
        if (playerPrefab == null) return;

        Vector3 spawnPos = new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            1f,
            Random.Range(-spawnRadius, spawnRadius)
        );

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        Debug.Log($"[MMO] Spawned player for client {clientId} at {spawnPos}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!networkManager.IsServer) return;
        Debug.Log($"[MMO] Client {clientId} disconnected");
    }
}