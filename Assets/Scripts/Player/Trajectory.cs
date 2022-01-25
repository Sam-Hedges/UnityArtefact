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
        private PhysicsObject _currentObj;
        private int _currentObjID;

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

            if (_currentObj == null)
            {
                _currentObjID = obj.GetInstanceID();
                _currentObj = InitSimObj(obj.gameObject, position, quaternion.identity).GetComponent<PhysicsObject>();
            }

            if (_currentObjID != obj.GetInstanceID())
            {
                _currentObjID = obj.GetInstanceID();
                Destroy(_currentObj.gameObject); 
                Debug.Log("Deleted");

                _currentObj = InitSimObj(obj.gameObject, position, quaternion.identity).GetComponent<PhysicsObject>();
            }
            

            //var currentObj = Instantiate(obj, position, rot);
            //SceneManager.MoveGameObjectToScene(_currentObj.gameObject, _simScene);

            _currentObj.transform.position = obj.transform.position;
            _currentObj.GetComponent<Rigidbody>().velocity = obj.GetComponent<Rigidbody>().velocity;
            _currentObj.GetComponent<Rigidbody>().angularDrag = obj.GetComponent<Rigidbody>().angularDrag;
            _currentObj.GetComponent<Rigidbody>().angularVelocity = obj.GetComponent<Rigidbody>().angularVelocity;
            _currentObj.AddForce(velocity, true);

            lineRenderer.positionCount = maxIterations;

            for (int i = 0; i < maxIterations; i++)
            {
                _phyScene.Simulate(Time.fixedDeltaTime);
                lineRenderer.SetPosition(i, _currentObj.transform.position);
                if (i == 0) Marker.position = _currentObj.transform.position;
            }
        }

        private GameObject InitSimObj(GameObject obj, Vector3 position, Quaternion rotation)
        {
            GameObject simObj = Instantiate(obj, position, rotation);   // Instantiate duplicate obj's
            ChangeRenderState(simObj, false);                       // Stop obj's from being rendered
            SceneManager.MoveGameObjectToScene(simObj, _simScene);      // Move obj's to sim scene
            return simObj;
        }

        // Changes the render state of the current object and recursively
        // all of its children
        private void ChangeRenderState(GameObject obj, bool state, bool changeChildren = true)
        {
            SetRenderState(obj, state);
            
            if (obj.transform.childCount > 0 && changeChildren)
                foreach (Transform child in obj.transform)
                {
                    SetRenderState(child.gameObject, state);
                    ChangeRenderState(child.gameObject, state);
                }
        }

        // Sets the renderers enabled value of obj GameObject to the state Boolean
        private void SetRenderState(GameObject obj, bool state)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null) r.enabled = state;
        }
        
    }
}
