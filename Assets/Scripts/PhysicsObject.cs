using System;
using System.Collections;
using Player;
using UnityEngine;

public class PhysicsObject : MonoBehaviour
{
    public GameObject ballPrefab;
    public float waitOnPickup = 0.2f;
    public float breakForce = 35f;
    [HideInInspector] public bool pickedUp;
    [HideInInspector] public PlayerController playerInteractions;
    private bool _simulated;
    private Rigidbody _rb;
    [SerializeField] private Trajectory trajectory;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void AddForce(Vector3 velocity, bool simulated)
    {
        _simulated = simulated;
        _rb.AddForce(velocity, ForceMode.Impulse);
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (_simulated)
        {
            trajectory.CollisionDetected(collision.gameObject.CompareTag("Target"));
            return;
        } 
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_simulated)
        {
            trajectory.CollisionDetected(false);
            return;
        }

        if(pickedUp)
        {
            if(collision.relativeVelocity.magnitude > breakForce)
            {
                playerInteractions.BreakConnection();
            }
        }
    }

    // to prevent the connection from breaking when you've just picked up
    public IEnumerator PickUp()
    {
        yield return new WaitForSecondsRealtime(waitOnPickup);
        pickedUp = true;

    }
}