using System.Collections;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 10f;
    private int currentTileIndex = 0;
    private bool isMoving = false;
    private int money = 0;
    private void Awake()
    {
        currentTileIndex = 0;
        money = 200;
    }

    public void MovePlayer(int steps)
    {
        int targetTileIndex = (currentTileIndex + steps) % BoardManager.GetInstance().GetCellDataLength();
        isMoving = true;
        StartCoroutine(MoveToTile(targetTileIndex));
    }
    private IEnumerator MoveToTile(int targetCellIndex)
    {
        while (currentTileIndex != targetCellIndex)
        {
            currentTileIndex = (currentTileIndex + 1) % BoardManager.GetInstance().GetCellDataLength();
            Vector2 cellPos = BoardManager.GetInstance().GetCellData(currentTileIndex).position;
            Vector3 nextCellPosition = new Vector3(cellPos.x, transform.position.y, cellPos.y);
            while (Vector3.Distance(transform.position, nextCellPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, nextCellPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
        }
        isMoving = false;
        DiceManager.GetInstace().ResetAction();
        HandleEvent();
    }
    public bool GetIsMoving()
    {
        return isMoving;
    }
    public void HandleEvent()
    {
        CellData data = BoardManager.GetInstance().GetCellData(currentTileIndex);
        switch (data.type)
        {
            case CellType.GO:
                break;
            case CellType.GOTOJAIL:
                break;
            case CellType.JAIL:
                break;
            case CellType.FREEPARKING:
                break;
            case CellType.CHANCE:
                CellType target;
                switch (ChanceHandler.GetInstance().DrawChaneCard().type)
                {
                    case ChanceType.GOTOJAIL:
                        target = CellType.JAIL; break;
                    case ChanceType.GOTOSTART:
                        target = CellType.GO; break;
                    case ChanceType.GOTOTAX:
                        target = CellType.TAX; break;
                    default
                        :
                        target = CellType.GO; break;
                }
                Debug.Log("ChaneCard: " + target);
                int index = BoardManager.GetInstance().GetIndexOfCellType(target);
                isMoving = true;
                StartCoroutine(MoveToTile(index));
                break;
            case CellType.TAX:
                break;
            case CellType.CHEST:
                break;
            case CellType.PROPERTY:
                BankManager bankManager = BankManager.GetInstance();
                if (bankManager.IsSelled(currentTileIndex))
                {
                    if (bankManager.GetOwner(currentTileIndex) == this)
                    {
                        // nâng cấp
                    }
                    else
                    {
                        // trả tiền thuê 
                    }
                }
                else
                {
                    // mua
                    // bật ui mua property
                }
                break;
        }
        Debug.Log(data.type);
    }
    public void BuyProperty()
    {
        money -= BoardManager.GetInstance().GetCellData(currentTileIndex).price;
        BankManager.GetInstance().SetOwner(currentTileIndex, this);
    }
}
