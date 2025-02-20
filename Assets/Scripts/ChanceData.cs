using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChanceData", menuName = "MonopolyData/Create New ChanceData")]
public class ChanceData : ScriptableObject
{
    public ChanceCard[] data;
}

