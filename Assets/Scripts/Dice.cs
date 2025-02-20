using Unity.Netcode;
using UnityEngine;

public class Dice : NetworkBehaviour
{
    private Rigidbody rb;
    private Vector3 diceVelocity;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        diceVelocity = rb.velocity;
    }
    public void RollDice()
    {
        float dirX = Random.Range(2000, 3000);
        float dirY = Random.Range(2000, 3000);
        float dirZ = Random.Range(2000, 3000);

        transform.rotation = Quaternion.identity;

        rb.AddForce(transform.up * 1000);
        rb.AddTorque(dirX, dirY, dirZ);
    }
    public bool IsStopped()
    {
        if (diceVelocity == Vector3.zero) return true;
        return false;
    }
    public int GetDiceValue()
    {
        if (!IsStopped()) return 0;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.up, out hit))
        {
            switch (hit.collider.name)
            {
                case "1": return 1;
                case "2": return 2;
                case "3": return 3;
                case "4": return 4;
                case "5": return 5;
                case "6": return 6;
            }
        }
        return 0;
    }
}
