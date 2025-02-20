using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnPlayers : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
    }

    private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (IsHost && sceneName.Equals("GameScene"))
        {
            int slotIndex = 0;
            foreach (ulong clientId in clientsCompleted)
            {
                Debug.Log($"Spawning player for client {clientId}");
                GameObject player = Instantiate(playerPrefab);
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
                //player.GetComponent<NetworkObject>().Spawn(true);
                slotIndex++;
            }
        }
    }
}
