using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceEdge
{
    public class InputPollingSystem : MonoBehaviour
    {
        private PlayerInput _playerInput;
        public static Vector2 MoveInput { get; private set; }

        public static Vector2 RotateInput { get; private set; }

        public static Vector2 AimInput { get; private set; }
        public static bool FireInput { get; private set; }

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        private void Update()
        {
            PollInput();
        }

        private void PollInput()
        {
            MoveInput = _playerInput.actions["Move"].ReadValue<Vector2>();
            RotateInput = _playerInput.actions["Rotate"].ReadValue<Vector2>();
            AimInput = _playerInput.actions["Aim"].ReadValue<Vector2>();
            FireInput = _playerInput.actions["Fire"].WasPressedThisFrame();
        }
    }
}
