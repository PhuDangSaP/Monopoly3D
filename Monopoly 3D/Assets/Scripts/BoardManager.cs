using System;
using Unity.VisualScripting;
using UnityEngine;

public enum CellType
{
    GO,
    GOTOJAIL,
    JAIL,
    FREEPARKING,
    CHANCE,
    TAX,
    CHEST,
    PROPERTY
}

[Serializable]
public class CellData
{
    public CellType type;
    public Vector2 position;
    public int price;
}


public class BoardManager : MonoBehaviour
{
    public CellData[] cellData;

    private void Start()
    {
     
    }

}
