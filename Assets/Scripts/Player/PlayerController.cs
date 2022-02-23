using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        // Reference Variables
        private CharacterController _charController;

        [Header("Camera")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private GameObject playerCameraHelper;
        [SerializeField] private GameObject groupCameraHelper;
        private static CinemachineVirtualCamera _playerCamera;
        private static CinemachineVirtualCamera _groupCamera;
        private static Vector3 CameraTransformForward => ScaleCameraTransform(_playerCamera.transform.forward);
        private static Vector3 CameraTransformRight => ScaleCameraTransform(_playerCamera.transform.right);
        private static Vector3 ScaleCameraTransform(Vector3 cameraTransform)
        {
            return Vector3.Scale(cameraTransform.normalized, new Vector3(1, 0, 1));
        }
        
        // Input
        private PlayerInput _input;
        private struct IMovement
        {
            public Vector2 MovementInputVector;
            public Vector3 MovementOutputVector => CameraTransformForward * MovementInputVector.y + CameraTransformRight * MovementInputVector.x;
            public bool IsMovementPressed => MovementInputVector != Vector2.zero;
        }
        private struct Look
        {
            public Vector2 LookInputVector;
            public Vector3 LookOutputVector;
            public bool IsLookPressed => LookInputVector != Vector2.zero;
        }
        
        private IMovement _iMovement;
        private Look _iLook;
        
        
        private bool IsGrounded => Physics.CheckSphere(groundCheckOrigin.position, groundCheckRad, groundMask);
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
        [SerializeField] private Trajectory trajectory;
        private Vector3 _rayPos;
        private GameObject _lookObj;
        private PhysicsObject _physicsObject;

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

        private void InitializeInput() {
            
            _input = new PlayerInput();

            _input.Player.Movement.started += OnMovementInput;
            _input.Player.Movement.canceled += OnMovementInput;
            _input.Player.Movement.performed += OnMovementInput;

            _input.Player.Look.started += OnLookInput;
            _input.Player.Look.canceled += OnLookInput;
            _input.Player.Look.performed += OnLookInput;
        }
        private void OnMovementInput(InputAction.CallbackContext context) { _iMovement.MovementInputVector = context.ReadValue<Vector2>(); }
        private void OnLookInput(InputAction.CallbackContext context) { _iLook.LookInputVector = context.ReadValue<Vector2>(); }
        private void InitializeCameras() {
            _playerCamera = playerCameraHelper.GetComponent<CinemachineVirtualCamera>();
            _groupCamera = groupCameraHelper.GetComponent<CinemachineVirtualCamera>();
        }

        #region Unity Event Methods
        
        private void Awake() {
            
            if(mainCamera == null) { mainCamera = Camera.main; }

            _charController = GetComponent<CharacterController>();
            
            InitializeInput();
            
            InitializeCameras();
        }
        private void Start() {
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        private void OnEnable() {
            _input.Player.Enable();
        }
        private void OnDisable() {
            _input.Player.Disable();
        }
        private void FixedUpdate() {
            
            if (_heldObj != null)
            {
                _objDist = Vector3.Distance(holdPoint.position, _pickupRb.position);
                _objVelocity = Mathf.SmoothStep(minSpeed, maxSpeed, _objDist / maxDist);
                _objVelocity *= Time.fixedDeltaTime;
                
                Vector3 direction = holdPoint.position - _pickupRb.position;
                _pickupRb.velocity = direction.normalized * _objVelocity;
                
                // Rotation
                lookRot = Quaternion.LookRotation(mainCamera.transform.position - _pickupRb.position);
                lookRot = Quaternion.Slerp(mainCamera.transform.rotation, lookRot, rotSpeed * Time.fixedDeltaTime);
                _pickupRb.MoveRotation(lookRot);
            }
        }
        private void Update() {
            
            Movement(playerSpeed);

            Jump();
            
            Gravity();
            
            Interact();

            if (_heldObj != null) { trajectory.SimulateTrajectory(_physicsObject, _heldObj.transform.localPosition, mainCamera.transform.forward * shootForce + _charController.velocity, _heldObj.transform.rotation); }
            else { trajectory.CancelTrajectory(); }
            
            Throw();

            if (_input.Player.Aim.GetButtonDown())
            {
                _playerCamera.Priority = 0;
                _groupCamera.Priority = 10;
            }

            if (_input.Player.Aim.GetButtonUp())
            {
                _playerCamera.Priority = 10;
                _groupCamera.Priority = 0;
            }
        }
        
        #endregion
        
        #region Movement Methods
        
        // Applies movement to the player character based on the players input
        private void Movement(float speed) {
            // Calculate final movement Vector
            Vector3 moveDirection = Vector3.ClampMagnitude(_iMovement.MovementOutputVector, 1f) * speed * Time.deltaTime;
            
            // Inputs the final movement vector to the character controller component
            _charController.Move(moveDirection);
        }
        
        // Adds the force of the gravity over time to the vertical axis of the player so they get pulled down
        private void Gravity() {
            
            if (!IsGrounded) { _velocity.y += gravityStrength * Time.deltaTime; }   // if the player is not grounded increase the vertical velocity
            else if (IsGrounded && _velocity.y < 0f) { _velocity.y = -2f; }         // if the player is grounded reset the velocity

            // applies the new velocity vector to the character controller
            _charController.Move(_velocity * Time.deltaTime);
        }

        private void Jump() {
            // Changes the height position of the player..
            if (_input.Player.Jump.GetButtonDown() && IsGrounded)
            {
                _velocity.y += Mathf.Sqrt(playerJumpHeight * -3.0f * gravityStrength);
            }
        }
        
        #endregion
        
        #region Interaction Methods
        
        private void Throw() {
            
            if (_heldObj == null ) return; 
            
            if (!_input.Player.Fire.GetButtonDown()) return; 
            
            PhysicsObject obj = _heldObj.GetComponent<PhysicsObject>();
            
            BreakConnection();
            
            obj.AddForce(mainCamera.transform.forward * shootForce + _charController.velocity, false);
        }
        
        private void Interact() {
            
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

        private void PickUpObj() {
            
            _physicsObject = _lookObj.GetComponentInChildren<PhysicsObject>();
            _heldObj = _lookObj;
            _pickupRb = _heldObj.GetComponent<Rigidbody>();
            _pickupRb.constraints = RigidbodyConstraints.FreezeRotation;
            _physicsObject.playerInteractions = this;
            StartCoroutine(_physicsObject.PickUp());
        }

        public void BreakConnection() {
            
            _pickupRb.constraints = RigidbodyConstraints.None;
            _objDist = 0;
            _physicsObject.pickedUp = false;
            _heldObj = null;
        }
        
        private GameObject CastForObject() {
            
            _rayPos = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

            if (Physics.SphereCast(_rayPos, sphereCastRadius, mainCamera.transform.forward, out RaycastHit hit, maxDist,
                    1 << interactLayerIndex))
            {
                return hit.collider.transform.root.gameObject;
            }

            return null;
        }
        
        #endregion
    }
}
