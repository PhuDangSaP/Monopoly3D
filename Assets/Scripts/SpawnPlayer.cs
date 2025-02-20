using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnPlayer : MonoBehaviour
{
    //private void Start()
    //{
    //    NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
    //}

    //private void HandleSceneEvent(SceneEvent sceneEvent)
    //{
    //    if (sceneEvent.SceneEventType == SceneEventType.LoadComplete && NetworkManager.Singleton.IsHost)
    //    {
    //        SpawnPlayers(sceneEvent.ClientId);
    //    }
    //}

    //private void SpawnPlayers(ulong clientId)
    //{
    //    Vector3 spawnPosition = GetSpawnPosition(clientId);
    //    Quaternion spawnRotation = Quaternion.identity;

    //    // Spawn nhân vật
    //    var playerObject = Instantiate(playerPrefab, spawnPosition, spawnRotation);
    //    playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    //}

    //private Vector3 GetSpawnPosition(ulong clientId)
    //{
    //    // Xác định vị trí spawn dựa trên clientId
    //    return new Vector3(clientId * 2, 0, 0); // Ví dụ đơn giản
    //}
}
