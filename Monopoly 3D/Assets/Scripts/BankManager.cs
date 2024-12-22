using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class BankManager : MonoBehaviour
{
    private static BankManager instance;
    private Dictionary<int, ulong> propertyOwners = new Dictionary<int, ulong>();
    private void Awake()
    {
        instance = this;
    }
    public static BankManager GetInstance()
    {
        if (instance == null)
        {
            instance = new BankManager();
        }
        return instance;
    }
    public bool IsPropertyOwned(int index)
    {
        return propertyOwners.ContainsKey(index);
    }

    public bool IsSelled(int index)
    {
        return selledProperties.ContainsKey(index);
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
