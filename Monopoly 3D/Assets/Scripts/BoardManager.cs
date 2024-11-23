using System;
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
[CreateAssetMenu(fileName = "BoardData", menuName = "MonopolyData/Create New BoardData")]
public class BoardData:ScriptableObject
{
    public CellData[] data;
}

public class BoardManager : MonoBehaviour
{
    private static BoardManager instance;
    [SerializeField]
    private BoardData boardData;

    private void Awake()
    {
        instance = this;
    }
    public static BoardManager GetInstance()
    {
        if (instance == null)
        {
            instance = new BoardManager();
        }
        return instance;
    }
    public int GetCellDataLength()
    {
        return boardData.data.Length;
    }
    public CellData GetCellData(int index)
    {
        return (boardData.data[index]);
    }
    public int GetIndexOfCellType(CellType type)
    {
        for(int i = 0; i < boardData.data.Length; i++)
        {
            if (boardData.data[i].type == type)
                return i;
        }
        return -1;
    }

}
