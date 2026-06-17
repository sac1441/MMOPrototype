using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class MMONetworkManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Server Settings")]
    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private ushort serverPort = 7777;

    private NetworkManager networkManager;
    private UnityTransport transport;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        transport = GetComponent<UnityTransport>();
        transport.SetConnectionData(serverAddress, serverPort);
    }

    private void OnGUI()
    {
        if (networkManager.IsClient || networkManager.IsServer)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 120));

        if (GUILayout.Button("Start Host (Server + Client)"))
            StartHost();

        if (GUILayout.Button("Start Server (Headless)"))
            StartServer();

        if (GUILayout.Button("Start Client"))
            StartClient();

        GUILayout.EndArea();
    }

    public void StartHost()
    {
        networkManager.StartHost();
        Debug.Log("[MMO] Started as Host");
    }

    public void StartServer()
    {
        networkManager.StartServer();
        Debug.Log("[MMO] Started as dedicated Server");
    }

    public void StartClient()
    {
        networkManager.StartClient();
        Debug.Log("[MMO] Started as Client");
    }
}