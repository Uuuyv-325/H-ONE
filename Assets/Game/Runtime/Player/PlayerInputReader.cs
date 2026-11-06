using UnityEngine;
using UnityEngine.InputSystem;

namespace HOne.Game
{
    public struct PlayerInputFrame
    {
        public float Move;
        public bool Jump, ChargePressed, ChargeReleased, Cancel;
    }

    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        private InputActionAsset instance;
        private InputActionMap gameplay;
        private InputAction move, aim;
        private PlayerInputFrame pending;
        private bool pauseRequested;
        private bool requireChargeRelease;
        public Vector2 PointerPosition => aim != null ? aim.ReadValue<Vector2>() : Vector2.zero;
        public InputActionAsset Actions => instance;
        public void Configure(InputActionAsset asset) => actions = asset;

        private void Awake()
        {
            if (actions == null) { enabled = false; return; }
            instance = Instantiate(actions);
            gameplay = instance.FindActionMap("Gameplay", true);
            move = gameplay.FindAction("Move", true);
            aim = gameplay.FindAction("Aim", true);
            gameplay.FindAction("Jump", true).performed += _ => pending.Jump = true;
            var charge = gameplay.FindAction("Charge", true);
            // A held button must enter the action's tracking state after re-enable,
            // so its physical release still sends canceled and clears the guard.
            charge.wantsInitialStateCheck = true;
            charge.performed += _ => { if (!requireChargeRelease) pending.ChargePressed = true; };
            charge.canceled += _ =>
            {
                if (!requireChargeRelease) pending.ChargeReleased = true;
                requireChargeRelease = false;
            };
            gameplay.FindAction("Cancel", true).performed += _ => pending.Cancel = true;
            instance.FindAction("System/Pause", true).performed += _ => pauseRequested = true;
        }

        private void OnEnable() { if (instance != null) instance.Enable(); }
        private void OnDisable()
        {
            if (instance != null) instance.Disable();
            pending = default;
        }
        private void OnDestroy() { if (instance != null) Destroy(instance); }

        public PlayerInputFrame Consume()
        {
            var frame = pending;
            frame.Move = move != null ? move.ReadValue<float>() : 0f;
            pending = default;
            return frame;
        }

        public bool ConsumePause()
        {
            var result = pauseRequested;
            pauseRequested = false;
            return result;
        }

        public void DiscardUntilRelease()
        {
            pending = default;
            requireChargeRelease = instance != null && instance.FindAction("Gameplay/Charge").IsPressed();
        }

        public void SetGameplayEnabled(bool value)
        {
            if (gameplay == null) return;
            if (value)
            {
                requireChargeRelease = false;
                foreach (var control in gameplay.FindAction("Charge").controls)
                    if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed)
                        requireChargeRelease = true;
                gameplay.Enable();
            }
            else
            {
                gameplay.Disable();
                requireChargeRelease = true;
            }
            pending = default;
        }
    }
}
