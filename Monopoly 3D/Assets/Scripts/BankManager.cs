using System.Collections.Generic;
using UnityEngine;

public class BankManager : MonoBehaviour
{
    private static BankManager instance;
    private Dictionary<int, PlayerManager> selledProperties = new Dictionary<int, PlayerManager>();
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

    public bool IsSelled(int index)
    {
        return selledProperties.ContainsKey(index);
    }
    public void SetOwner(int index,PlayerManager owner)
    {
        selledProperties[index]=owner;
    }
    public PlayerManager GetOwner(int index)
    {
        if (selledProperties.TryGetValue(index, out PlayerManager owner))
        {
            return owner;
        }
        return null;
    }
}
