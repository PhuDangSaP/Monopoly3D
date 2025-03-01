using Unity.Netcode;
using UnityEngine;

public class DiceManager : NetworkBehaviour
{
    private static DiceManager instance;
    private GameObject[] dices;
    private bool inAction = false;
    private bool hasRolled = false;

    private void Awake()
    {
        instance = this;
        dices = GameObject.FindGameObjectsWithTag("Dice");
    }
    private void Update()
    {
        if (IsServer && hasRolled && IsAllStopped() && !inAction && TurnManager.Instance.isStarted)
        {
            ulong currentPlayerId = TurnManager.Instance.GetCurrentPlayerId();
            PlayerController currentPlayer = NetworkManager.Singleton.ConnectedClients[currentPlayerId].PlayerObject.GetComponent<PlayerController>();
            if (!currentPlayer.GetIsMoving())
            {
                currentPlayer.MovePlayer(GetDicesValue());
            }
        }
        if (!IsAllStopped())
        {
            inAction = false;
        }
    }
    public void RollDices()
    {
        Debug.Log("Roll");
        Debug.Log("Is my turn: " + TurnManager.Instance.IsMyTurn());
        if (TurnManager.Instance.IsMyTurn())
        {
            RollDicesServerRpc();
        }

    }
    [ServerRpc(RequireOwnership = false)]
    private void RollDicesServerRpc()
    {
        hasRolled = true;
        inAction = true;
        foreach (GameObject dice in dices)
        {
            Dice x = dice.GetComponent<Dice>();
            if (x != null)
            {
                x.RollDice();
            }
        }
    }
    private bool IsAllStopped()
    {
        foreach (GameObject dice in dices)
        {
            if (!dice.GetComponent<Dice>().IsStopped()) return false;
        }

        return true;
    }
    public int GetDicesValue()
    {
        int value = 0;
        foreach (GameObject dice in dices)
        {
            value += dice.GetComponent<Dice>().GetDiceValue();
        }
        return value;
    }
    public void ResetAction()
    {
        inAction = false;
        hasRolled = false;
    }
    public static DiceManager GetInstace()
    {

        return instance;
    }
}
