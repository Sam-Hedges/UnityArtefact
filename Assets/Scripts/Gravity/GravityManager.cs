using System;
using System.Collections.Generic;
using Player;
using UnityEngine;

namespace Gravity
{
    public class GravityManager : MonoBehaviour
    {
        private Rigidbody playerRigidbody;
        [SerializeField] private float gravityStrength = 9.81f;
        private Vector3 lastGravityVector;
        private PlayerController player;
        public Vector3 debugVector;

        private void Awake()
        {
            Physics.gravity = Vector3.down * gravityStrength;

            player = GetComponent<PlayerController>();
            
            lastGravityVector = Physics.gravity / gravityStrength;
            
            playerRigidbody = player.gameObject.GetComponent<Rigidbody>();
        }

        private void Update() {
            debugVector = transform.up;
        }

        private void OnTriggerEnter(Collider coll) {
            if (coll.CompareTag("GravityZone")) { ChangeGravityDirection(coll.transform); }
        }

        private void OnTriggerExit(Collider coll) {
            if (coll.CompareTag("GravityZone")) { ResetGravityDirection(coll.transform); }
        }

        private void ChangeGravityDirection(Transform newTransform) {
            
            lastGravityVector = Physics.gravity / gravityStrength;
            Physics.gravity = -newTransform.up * gravityStrength;
            
            playerRigidbody.rotation = Quaternion.LookRotation(Quaternion.Euler(90, 0, 0) * newTransform.up, newTransform.up);
            transform.up = newTransform.up;
            transform.forward = Quaternion.Euler(90, 0, 0) * newTransform.up;
        }

        private void ResetGravityDirection(Transform newTransform) {

            lastGravityVector = -newTransform.up;
        
            if (Physics.gravity / gravityStrength != lastGravityVector) { return; }
        
            Physics.gravity = Vector3.down * gravityStrength;
            
            playerRigidbody.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);;
            transform.up = Vector3.up;
            transform.forward = Vector3.forward;

        }
    }
}
