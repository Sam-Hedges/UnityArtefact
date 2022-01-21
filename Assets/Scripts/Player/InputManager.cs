using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class InputManager : MonoBehaviour
    {
        #region Singleton Variables
        
        private static InputManager _instance;

        public static InputManager Instance => _instance;
        
        #endregion
        
        private PlayerInput _playerControls;

        #region Standard Methods

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
            }
            
            _playerControls = new PlayerInput();
        }

        private void OnEnable()
        {
            _playerControls.Enable();
        }

        private void OnDisable()
        {
            _playerControls.Disable();
        }
        
        #endregion
        
        #region Input Methods
        
        public Vector2 GetPlayerMovement()
        {
            return _playerControls.Player.Movement.ReadValue<Vector2>();
        }
    
        public Vector2 GetMouseDelta()
        {
            return _playerControls.Player.Look.ReadValue<Vector2>();
        }

        public bool JumpedOnFrame()
        {
            return _playerControls.Player.Jump.triggered;
        }
        
        
        public bool InteractedOnFrame()
        {
            return _playerControls.Player.Interact.triggered;
        }
        
        public bool FiredOnFrame()
        {
            return _playerControls.Player.Fire.triggered;
        }
        
        public bool AimedOnFrame()
        {
            return _playerControls.Player.Aim.triggered;
        }
        
        public bool SprintedOnFrame()
        {
            return _playerControls.Player.Sprint.triggered;
        }
        
        #endregion
    }
}
