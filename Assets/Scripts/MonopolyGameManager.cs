using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonopolyGameManager : NetworkBehaviour
{
    private static MonopolyGameManager instance;
    [SerializeField] private GameObject playerPrefab;

    [SerializeField] private List<Transform> slots; 
    private void Awake()
    {
        instance = this;
       
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
             NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform parent=null;
            foreach(Transform slot in slots)
            {
                if(slot.childCount == 0)
                {
                    parent = slot;
                    break;
                }
            }
            GameObject playerTransform = Instantiate(playerPrefab,parent);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    public static MonopolyGameManager Instance
    {
        get
        { 
            if (instance == null)
            {
                instance = new MonopolyGameManager();
            }
            return instance;
        }
    }
}
