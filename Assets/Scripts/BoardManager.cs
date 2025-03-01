using System;
using Unity.Netcode;
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
    public Vector2 houseOffset;
    public GameObject houseObject;
    public string name;
    public int price;
}

public class BoardManager : NetworkBehaviour
{
    private static BoardManager instance;
    [SerializeField]
    private BoardData boardData;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public static BoardManager GetInstance()
    {
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
        for (int i = 0; i < boardData.data.Length; i++)
        {
            if (boardData.data[i].type == type)
                return i;
        }
        return -1;
    }
    public int GetIndexOfClosetCellType(CellType type,int currentIndex)
    {
        for (int i = currentIndex; i < boardData.data.Length; i++)
        {
            if (boardData.data[i].type == type)
                return i;
        }
        for (int i = 0; i < boardData.data.Length; i++)
        {
            if (boardData.data[i].type == type)
                return i;
        }
        return -1;
    }

}
