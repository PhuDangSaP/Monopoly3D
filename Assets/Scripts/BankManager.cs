using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BankManager : NetworkBehaviour
{
    public static BankManager Instance { get; private set; }

    private Dictionary<int, ulong> propertyOwners = new Dictionary<int, ulong>();
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsPropertyOwned(int index)
    {
        return propertyOwners.ContainsKey(index);
    }
    public void SetPropertyOwner(int propertyIndex, ulong clientId)
    {
        if (propertyOwners.ContainsKey(propertyIndex))
        {
            Debug.Log("Property already has owner");
        }
        else
        {
            propertyOwners[propertyIndex] = clientId;
        }
    }
    public ulong GetPropertyOwner(int propertyIndex)
    {
        if (propertyOwners.TryGetValue(propertyIndex, out ulong clientId))
        {
            return clientId;
        }
        return ulong.MaxValue; 
    }
    public void RemovePropertyOwner(int propertyIndex)
    {
        if (propertyOwners.ContainsKey(propertyIndex))
        {
            propertyOwners.Remove(propertyIndex);
        }
    }
}
