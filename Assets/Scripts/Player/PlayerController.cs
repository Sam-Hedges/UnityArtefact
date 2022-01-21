using System;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // Reference Variables
        private PlayerInput _input;
        private CharacterController CharController => GetComponent<CharacterController>();
        private Camera MainCamera => Camera.main;
        
        
        //Camera Directions
        private Vector3 CameraTransformForward => ScaleCameraTransform(MainCamera.transform.forward);
        private Vector3 CameraTransformRight => ScaleCameraTransform(MainCamera.transform.right);
        private Vector3 ScaleCameraTransform(Vector3 cameraTransform)
        {
            return Vector3.Scale(cameraTransform.normalized, new Vector3(1, 0, 1));
        }

        [SerializeField] private CinemachineVirtualCamera playerCamera;
        [SerializeField] private CinemachineVirtualCamera groupCamera;
        
        // Player input related variables
        private Vector2 _movementInputVector;
        private Vector3 _movementOutputVector;
        private bool _isMovementPressed;
        
        private bool _isGrounded;
        private Vector3 _velocity;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckRad = 0.5f;
        [SerializeField] private Transform groundCheckOrigin;
        [SerializeField] private float playerSpeed = 2.0f;
        [SerializeField] private float playerJumpHeight = 1.0f;
        [SerializeField] private float gravityStrength = -9.81f;

        [Header("Interaction")]
        [SerializeField] private float sphereCastRadius = 0.5f;
        [SerializeField] private int interactLayerIndex = 3;
        private Vector3 _rayPos;
        private GameObject _lookObj;
        private PhysicsObject _physicsObject;
        private Trajectory _trajectory;

        [Header("Pickup")] 
        [SerializeField] private Transform holdPoint;
        private GameObject _heldObj;
        private Rigidbody _pickupRb;

        [Header("ObjectMovement")] 
        [SerializeField] private float shootForce = 1000f;
        [SerializeField] private float minSpeed;
        [SerializeField] private float maxSpeed = 300f;
        [SerializeField] private float maxDist = 10f;
        [SerializeField] private float rotSpeed = 100f;
        private float _objVelocity;
        private float _objDist;
        private Quaternion lookRot;
        
        //        public Transform Marker;
        private void Awake()
        {
            // Initialize needed components
            _input = new PlayerInput();
            _trajectory = GetComponent<Trajectory>();
            
            _input.Player.Movement.started += OnMovementInput;
            _input.Player.Movement.canceled += OnMovementInput;
            _input.Player.Movement.performed += OnMovementInput;
        }
        
        void OnMovementInput(InputAction.CallbackContext context)
        {
            _movementInputVector = context.ReadValue<Vector2>();
            _movementOutputVector = CameraTransformForward * _movementInputVector.y + CameraTransformRight * _movementInputVector.x;
            _isMovementPressed = _movementInputVector != Vector2.zero;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            _input.Player.Enable();
        }

        private void OnDisable()
        {
            _input.Player.Disable();
        }
        
        //Velocity movement toward pickup parent and rotation
        private void FixedUpdate()
        {
            if (_heldObj != null)
            {
                _objDist = Vector3.Distance(holdPoint.position, _pickupRb.position);
                _objVelocity = Mathf.SmoothStep(minSpeed, maxSpeed, _objDist / maxDist);
                _objVelocity *= Time.fixedDeltaTime;
                
                Vector3 direction = holdPoint.position - _pickupRb.position;
                _pickupRb.velocity = direction.normalized * _objVelocity;
                
                // Rotation
                lookRot = Quaternion.LookRotation(MainCamera.transform.position - _pickupRb.position);
                lookRot = Quaternion.Slerp(MainCamera.transform.rotation, lookRot, rotSpeed * Time.fixedDeltaTime);
                _pickupRb.MoveRotation(lookRot);
            }
 
        }
        
        void Update()
        {
            _isGrounded = Physics.CheckSphere(groundCheckOrigin.position, groundCheckRad, groundMask);
            
            Movement(playerSpeed);
            
            //Rotate player to move
            //gameObject.transform.forward = Vector3.Scale(CameraTransformForward, new Vector3(1, 0, 1));
            
            Debug.Log(_isGrounded);
            // Changes the height position of the player..
            if (_input.Player.Jump.GetButtonDown() && _isGrounded)
            {
                Debug.Log("Jump");
                _velocity.y += Mathf.Sqrt(playerJumpHeight * -3.0f * gravityStrength);
            }
            

            Gravity();
            
            Interact();

            //if (_heldObj != null) Marker.transform.position = _heldObj.transform.position;
            
            if (_heldObj != null) { _trajectory.SimulateTrajectory(_physicsObject, _heldObj.transform.localPosition, MainCamera.transform.forward * shootForce, _heldObj.transform.rotation); }
            else { _trajectory.CancelTrajectory(); }
            
            Throw();

            if (_input.Player.Aim.GetButtonDown())
            {
                playerCamera.Priority = 0;
                groupCamera.Priority = 10;
            }
            else if (_input.Player.Aim.GetButtonUp())
            {
                playerCamera.Priority = 10;
                groupCamera.Priority = 0;
            }
        }
        
        // Applies movement to the player character based on the players input
        private void Movement(float speed)
        {
            // Adjust movement for camera
            _movementOutputVector = CameraTransformForward * _movementInputVector.y + CameraTransformRight * _movementInputVector.x;
            
            // Calculate final movement Vector
            Vector3 moveDirection = Vector3.ClampMagnitude(_movementOutputVector, 1f) * speed * Time.deltaTime;
            
            // Inputs the final movement vector to the character controller component
            CharController.Move(moveDirection);
        }
        
        // Adds the force of the gravity over time to the vertical axis of the player so they get pulled down
        private void Gravity()
        {
            if (!_isGrounded) { _velocity.y += gravityStrength * Time.deltaTime; }   // if the player is not grounded increase the vertical velocity
            else if (_isGrounded && _velocity.y < 0f) { _velocity.y = -2f; }         // if the player is grounded reset the velocity

            // applies the new velocity vector to the character controller
            CharController.Move(_velocity * Time.deltaTime);
        }

        private void Throw()
        {
            if (_heldObj == null ) return; 
            
            if (!_input.Player.Fire.GetButtonDown()) return; 
            
            PhysicsObject obj = _heldObj.GetComponent<PhysicsObject>();
            
            BreakConnection();
            
            obj.AddForce(MainCamera.transform.forward * shootForce, false);
        }
        
        private void Interact()
        {
            _lookObj = CastForObject();

            if (_input.Player.Interact.GetButtonDown())
            {
                if (_heldObj == null)
                {
                    if (_lookObj != null)
                    {
                        PickUpObj();
                    }
                }
                else
                {
                    BreakConnection();
                }
            }
            
            if (_heldObj != null && _objDist > maxDist) { BreakConnection(); }
        }

        private void PickUpObj()
        {
            _physicsObject = _lookObj.GetComponentInChildren<PhysicsObject>();
            _heldObj = _lookObj;
            _pickupRb = _heldObj.GetComponent<Rigidbody>();
            _pickupRb.constraints = RigidbodyConstraints.FreezeRotation;
            _physicsObject.playerInteractions = this;
            StartCoroutine(_physicsObject.PickUp());
        }

        public void BreakConnection()
        {
            _pickupRb.constraints = RigidbodyConstraints.None;
            _objDist = 0;
            _physicsObject.pickedUp = false;
            _heldObj = null;
        }
        
        private GameObject CastForObject()
        {
            _rayPos = MainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            RaycastHit hit;
            if (Physics.SphereCast(_rayPos, sphereCastRadius, MainCamera.transform.forward, out hit, maxDist,
                    1 << interactLayerIndex))
            {
                return hit.collider.transform.root.gameObject;
            }

            return null;
        }
    }
}
