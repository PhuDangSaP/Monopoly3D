using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dice : MonoBehaviour
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

        rb.AddForce(transform.up*1000);
        rb.AddTorque(dirX, dirY, dirZ);
    }
}
