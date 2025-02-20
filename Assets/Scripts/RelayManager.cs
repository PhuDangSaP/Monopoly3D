using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    public async Task<string> StartHostWithRelay(int maxConnections = 3)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return NetworkManager.Singleton.StartHost() ? joinCode : null;
        }
        catch(RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }
    public async Task<bool> StartClientWithRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
            return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return false;
        }
    }
}
