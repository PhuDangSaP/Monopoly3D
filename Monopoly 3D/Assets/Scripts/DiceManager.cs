using UnityEngine;

public class DiceManager : MonoBehaviour
{
    private static DiceManager instance;
    private GameObject[] dices;
    private bool inAction = false;

    private void Awake()
    {
        instance = this;
        dices = GameObject.FindGameObjectsWithTag("Dice");
    }
    private void Update()
    {
        if (IsAllStopped() && inAction)
        {
            PlayerManager playerManager = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerManager>();
            if (playerManager.GetIsMoving()) return;
            playerManager.MovePlayer(GetDicesValue());
        }
        if (!IsAllStopped())
        {
            inAction = true;
        }
    }
    public void RollDices()
    {
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
    }
    public static DiceManager GetInstace()
    {
        if(instance == null)
        {
            instance = new DiceManager();
        }
        return instance;
    }
}
