using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFDCollider : MonoBehaviour
{
    public bool IsOn;
    public void Initialize(PhysicsCollisionController collisionController, int x, int y, int z)
    {
        Controller = collisionController;
        X = x; Y = y; Z = z;
    }

    PhysicsCollisionController Controller;
    public int X, Y, Z;

    private void OnTriggerEnter(Collider other)
    {
        Controller.OnEnter(X, Y, Z);
        IsOn = true;
    }
    private void OnTriggerStay(Collider other)
    {
        Controller.OnStay(X, Y, Z);
    }
    private void OnTriggerExit(Collider other)
    {
        Controller.OnExit(X, Y, Z);
        IsOn = false;
    }
}
