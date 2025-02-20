using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum ChanceType
{
    GOTOJAIL,
    GOTOTAX,
    GOTOSTART,
}
[Serializable]
public class ChanceCard
{
    public ChanceType type;
    public string description;
}

public class ChanceHandler : MonoBehaviour
{
    public static ChanceHandler Instance { get; private set; }
    [SerializeField]
    private ChanceData chanceData;
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
    public ChanceCard DrawChaneCard()
    {
        int i = UnityEngine.Random.Range(0, chanceData.data.Length-1);
        return chanceData.data[i];
    }
    
}
