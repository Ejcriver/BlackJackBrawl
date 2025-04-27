using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkSceneLoader : MonoBehaviour
{
    [Header("Scene Names")] 
    public string mainGameSceneName = "MainGameScene";
    public string titleSceneName = "TitleScene";

    private void Awake()
    {
        // Ensure this persists if needed
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        else
        {
            // Try again in Start if NetworkManager isn't ready yet
            StartCoroutine(WaitForNetworkManager());
        }
    }

    private System.Collections.IEnumerator WaitForNetworkManager()
    {
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Only the host should trigger scene loading
        if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count == 1)
        {
            // First connection: host loads the main game scene
            NetworkManager.Singleton.SceneManager.LoadScene(mainGameSceneName, LoadSceneMode.Single);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // If this is the local player being disconnected (including kicked, left, or lost connection)
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Always return to TitleScene locally
            SceneManager.LoadScene(titleSceneName, LoadSceneMode.Single);
        }
        // If host and all clients are gone, host returns to TitleScene
        if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count == 1)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(titleSceneName, LoadSceneMode.Single);
        }
    }
}
