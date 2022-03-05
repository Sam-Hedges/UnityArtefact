using UnityEngine;

namespace Gravity
{
    public class GravityVolume : MonoBehaviour
    {
        private GravityManager gravityManager;
    
        public void SetParentManager(GravityManager gm) {
            gravityManager = gm;
        }

        private void OnTriggerEnter(Collider col) {
            if (col.CompareTag("Player")) {
                gravityManager.ChangeGravityDirection(-transform.up);
            }
        }

        private void OnTriggerExit(Collider col) {
            if (col.CompareTag("Player")) {
                gravityManager.ResetGravityDirection(-transform.up);
            }
        }
    }
}
