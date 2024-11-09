using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public BoardManager boardManager;
    public DiceManager diceManager;
    public float moveSpeed = 10f; 
    private int currentTileIndex = 0; 
    private bool isMoving=false;
    
    public void MovePlayer(int steps)
    {
        int targetTileIndex = (currentTileIndex + steps) % boardManager.cellData.Length;
        isMoving = true;
        StartCoroutine(MoveToTile(targetTileIndex));
    }
    private IEnumerator MoveToTile(int targetCellIndex)
    {
        Debug.Log("move to " + targetCellIndex);
        while (currentTileIndex != targetCellIndex)
        { 
            currentTileIndex = (currentTileIndex + 1) % boardManager.cellData.Length;
            Vector2 cellPos = boardManager.cellData[currentTileIndex].position;
            Vector3 nextCellPosition = new Vector3(cellPos.x,transform.position.y,cellPos.y);
            while (Vector3.Distance(transform.position, nextCellPosition) > 0.1f)
            {
               transform.position = Vector3.MoveTowards(transform.position, nextCellPosition, moveSpeed * Time.deltaTime);
                yield return null; 
            }
  
            yield return new WaitForSeconds(0.1f);
        }    
        isMoving = false;
        diceManager.ResetAction();
    }
    public bool GetIsMoving()
    {
        return isMoving;
    }
}
