using System.Collections.Generic;
using Player;
using UnityEngine;

namespace Gravity
{
    public class GravityManager : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        private Rigidbody playerRigidbody;
        [SerializeField] private float gravityStrength = 9.81f;
        [SerializeField] private List<GravityVolume> gravityVolumes;
        private Vector3 lastGravityVector;
        public Vector3 debugVector;

        void Awake()
        {
            Physics.gravity = Vector3.down * gravityStrength;

            foreach (GravityVolume gravityVolume in gravityVolumes) {
                gravityVolume.SetParentManager(this);
            }

            lastGravityVector = Physics.gravity / gravityStrength;
            
            playerRigidbody = player.gameObject.GetComponent<Rigidbody>();
        }

        private void Update() {
            debugVector = Physics.gravity;
        }

        public void ChangeGravityDirection(Vector3 newTransformUp) {
        
            newTransformUp.Normalize();
            lastGravityVector = Physics.gravity / gravityStrength;
            Physics.gravity = newTransformUp * gravityStrength;
            
            playerRigidbody.rotation = Quaternion.FromToRotation(player.transform.up, newTransformUp);
            player.transform.up = newTransformUp;
            player.upTransform = -newTransformUp;
            
        }

        public void ResetGravityDirection(Vector3 newTransformUp) {

            newTransformUp.Normalize();
            
            lastGravityVector = newTransformUp;
        
            if (Physics.gravity / gravityStrength != lastGravityVector) { return; }
        
            Physics.gravity = Vector3.down * gravityStrength;
            
            playerRigidbody.rotation = Quaternion.FromToRotation(player.transform.up, Vector3.down);
            player.transform.up = Vector3.down;
            player.upTransform = Vector3.up;

        }
    }
}
