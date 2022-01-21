using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player
{
    public class Trajectory : MonoBehaviour
    {
        private Scene _simScene;
        private PhysicsScene _phyScene;
        private readonly Dictionary<Transform, Transform> _spawnedObjects = new Dictionary<Transform, Transform>();
        private GameObject _currentObj;

        public Transform Marker;
        [SerializeField] private Transform environmentParent; // Empty object that contains all obstacles to be accounted for
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private int maxIterations;
        
        private void Start()
        {
            InitPhysicsScene();
        }
        
        private void Update() 
        {
            foreach (KeyValuePair<Transform, Transform> item in _spawnedObjects) 
            {
                item.Value.position = item.Key.position;
                item.Value.rotation = item.Key.rotation;
            }
        }
        
        void InitPhysicsScene()
        {
            _simScene = SceneManager.CreateScene("Simulation", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _phyScene = _simScene.GetPhysicsScene();

            foreach (Transform obj in environmentParent)
            {
                GameObject tempObj = InitSimObj(obj.gameObject, obj.transform.position, obj.rotation);
                if (!tempObj.isStatic) _spawnedObjects.Add(obj, tempObj.transform);
            }
        }

        public void CancelTrajectory()
        {
            lineRenderer.positionCount = 0;
        }
        
        public void SimulateTrajectory(PhysicsObject obj, Vector3 position, Vector3 velocity, quaternion rot)
        {
            /*
            if (_currentObj != obj || _currentObj == null)
            {
                Destroy(_currentObj);
                _currentObj = obj;
                _currentObj = InitSimObj(obj, position, quaternion.identity);
            }
            */

            var currentObj = Instantiate(obj, position, rot);
            SceneManager.MoveGameObjectToScene(currentObj.gameObject, _simScene); 
            
            //_currentObj.GetComponent<Rigidbody>().velocity = new Vector3(0f, 0f, 0f);
            //_currentObj.GetComponent<Rigidbody>().angularVelocity = new Vector3(0f, 0f, 0f);
            currentObj.AddForce(velocity, true);

            lineRenderer.positionCount = maxIterations;

            for (int i = 0; i < maxIterations; i++)
            {
                _phyScene.Simulate(Time.fixedDeltaTime);
                lineRenderer.SetPosition(i, currentObj.transform.position);
                if (i == 0) Marker.position = currentObj.transform.position;
            }
            
            Destroy(currentObj.gameObject);
        }

        private GameObject InitSimObj(GameObject obj, Vector3 position, Quaternion rotation)
        {
            GameObject simObj = Instantiate(obj, position, rotation);   // Instantiate duplicate obj's
            simObj.GetComponent<Renderer>().enabled = false;            // Stop obj's from being rendered
            SceneManager.MoveGameObjectToScene(simObj, _simScene);      // Move obj's to sim scene
            return simObj;
        }
    }
}
