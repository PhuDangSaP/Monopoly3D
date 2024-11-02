using UnityEngine;

public class DiceManager : MonoBehaviour
{
    private GameObject[] dices;

    private void Awake()
    {
        dices = GameObject.FindGameObjectsWithTag("Dice");
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
}
