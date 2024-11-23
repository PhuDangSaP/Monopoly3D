using System;
using System.Collections;
using System.Collections.Generic;
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
[CreateAssetMenu(fileName = "ChanceData", menuName = "MonopolyData/Create New ChanceData")]
public class ChanceData: ScriptableObject
{
    public ChanceCard[] data;
}

public class ChanceHandler : MonoBehaviour
{
    private static ChanceHandler instance;
    [SerializeField]
    private ChanceData chaneData;
    private void Awake()
    {
        instance = this;
    }
    public ChanceCard DrawChaneCard()
    {
        int i = UnityEngine.Random.Range(0, chaneData.data.Length-1);
        return chaneData.data[i];
    }
    public static ChanceHandler GetInstance()
    {
        if (instance == null)
        {
            instance = new ChanceHandler();
        }
        return instance;
    }
}
