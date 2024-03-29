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
    [SerializeField] private ScoreManager scoreManager;

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
            trajectory.CollisionDetected(collision.gameObject.CompareTag("Home") || collision.gameObject.CompareTag("Away"), _rb.velocity);
            return;
        }

        if (!_simulated) {
            scoreManager.AddScore(collision.gameObject.tag, _rb.velocity);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (_simulated)
        {
            trajectory.CollisionDetected(false, Vector3.zero);
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