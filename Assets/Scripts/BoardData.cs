using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoardData", menuName = "MonopolyData/Create New BoardData")]
public class BoardData : ScriptableObject
{
    public CellData[] data;
}
