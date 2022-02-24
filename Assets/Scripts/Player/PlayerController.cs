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
        private static CinemachineVirtualCamera _currentCamera;
        private static Vector3 CameraTransformForward => ScaleCameraTransform(_currentCamera.transform.forward);
        private static Vector3 CameraTransformRight => ScaleCameraTransform(_currentCamera.transform.right);
        private static Vector3 ScaleCameraTransform(Vector3 cameraTransform) {
            cameraTransform.y = 0.0f;
            cameraTransform.Normalize();
            return cameraTransform;
        }
        private float xRot;
        private float yRot;
        
        // Input
        private PlayerInput _input;
        private struct IMovement {
            
            public Vector2 MovementInputVector;
            public Vector3 MovementOutputVector => ScaleCameraTransform(_currentCamera.transform.forward) * MovementInputVector.y + CameraTransformRight * MovementInputVector.x;
            public bool IsPressed => MovementInputVector != Vector2.zero;
        }
        private struct ILook {
            
            public Vector2 LookInputVector;
            public Vector3 LookOutputVector;
            public bool IsPressed => LookInputVector != Vector2.zero;
        }
        
        private IMovement _iMovement;
        private ILook _iLook;

        private bool IsGrounded => Physics.CheckSphere(groundCheckOrigin.position, groundCheckRad, groundMask);
        private Vector3 _velocity;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckRad = 0.5f;
        [SerializeField] private Transform groundCheckOrigin;
        [SerializeField] private float playerSpeed = 2.0f;
        [SerializeField] private float playerJumpHeight = 1.0f;
        [SerializeField] private float gravityStrength = -9.81f;

        [Header("Interaction")]
        [SerializeField] private Transform holdPoint;
        [SerializeField] private float sphereCastRadius = 0.5f;
        [SerializeField] private int interactLayerIndex = 3;
        [SerializeField] private Trajectory trajectory;
        [SerializeField] private float interactDist = 5f;
        private Vector3 _rayPos;
        private GameObject _lookObj;
        private PhysicsObject _physicsObject;
        private GameObject _heldObj;
        private Rigidbody _pickupRb;

        [Header("ObjectMovement")] 
        [SerializeField] private float shootForce = 1000f;
        [SerializeField] private float minSpeed;
        [SerializeField] private float maxSpeed = 300f;
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
            _currentCamera = _playerCamera;
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
        private void FixedUpdate()
        {
            if (_heldObj != null) {
                _objDist = Vector3.Distance(holdPoint.position, _pickupRb.position);
                _objVelocity = Mathf.SmoothStep(minSpeed, maxSpeed, _objDist / interactDist);
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
            
            Rotate();

            Movement(playerSpeed);

            Jump();
            
            Gravity();
            
            Interact();
            
            /*
            if (_heldObj != null) {
                trajectory.SimulateTrajectory(_physicsObject, _heldObj.transform.localPosition, _playerCamera.transform.forward * shootForce + _charController.velocity, _heldObj.transform.rotation);
            }
            else { trajectory.CancelTrajectory(); }
            */
            Throw();
            
            if (_heldObj != null && _currentCamera == _groupCamera) {
                trajectory.SimulateTrajectory(_physicsObject, _heldObj.transform.localPosition, _playerCamera.transform.forward * shootForce + _charController.velocity, _heldObj.transform.rotation);
            }
            
            if (_input.Player.Aim.GetButtonDown()) {
                _playerCamera.Priority = 0;
                _groupCamera.Priority = 10;
                _currentCamera = _groupCamera;
            }

            if (_input.Player.Aim.GetButtonUp()) {
                _playerCamera.Priority = 10;
                _groupCamera.Priority = 0;
                _currentCamera = _playerCamera;
                
                trajectory.CancelTrajectory();
            }
            
            Throw();
        }
        
        #endregion
        
        #region Movement Methods
        
        private void Rotate() {
            
            xRot -= _iLook.LookInputVector.y * 100f * Time.deltaTime;
            xRot = Mathf.Clamp(xRot,-90f, 90f);
            yRot += _iLook.LookInputVector.x * 100f * Time.deltaTime;

            _playerCamera.transform.localRotation = Quaternion.Euler(xRot, 0, 0);

            transform.rotation = Quaternion.Euler(0, yRot, 0);
        }

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
            
            obj.AddForce(_playerCamera.transform.forward * shootForce + _charController.velocity, false);
        }
        
        private void Interact() {
            
            if (_heldObj == null) { _lookObj = CastForObject(); }

            if (_input.Player.Interact.GetButtonDown()) {
                if (_heldObj == null) {
                    if (_lookObj != null) {
                        PickUpObj();
                    }
                }
                else {
                    BreakConnection();
                }
            }
            
            if (_heldObj != null && _objDist > interactDist) { BreakConnection(); }
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

            if (Physics.SphereCast(_rayPos, sphereCastRadius, mainCamera.transform.forward, out RaycastHit hit, interactDist, 1 << interactLayerIndex)) {
                
                GameObject castObj = hit.collider.transform.root.gameObject;
                
                if (castObj.GetComponent<Interactable>()) {
                    castObj.GetComponent<Interactable>().ToggleOutline(false);
                }
                
                return castObj;
            }
            
            if (_lookObj != null) { _lookObj.GetComponent<Interactable>().ToggleOutline(true); }
            
            return null;
        }
        
        #endregion
    }
}
