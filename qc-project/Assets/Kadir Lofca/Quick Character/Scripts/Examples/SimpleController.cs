// Author: Kadir Lofca
// github.com/kadirlofca

using UnityEngine;

namespace QUICK.EXAMPLE
{
    /// <summary>
    /// SimpleController is an example controller that reads player input and communicates with QuickCharacter to convert the input into movement.
    /// <see href="http://stackoverflow.com">Documentation/SimpleController</see>
    /// </summary>
    public class SimpleController : MonoBehaviour
    {
        public QuickCharacter quickCharacter;

        [Header("Camera")]
        public Transform cameraTransform;
        public float cameraSensitivity = 0.5f;

        private float cameraPitch = 0;
        private const float MAX_CAMERA_PITCH = 89.99f;

        private QuickInputActions.SimpleActions actions;

        private void Awake()
        {
            if (!quickCharacter)
            {
                quickCharacter = GetComponent<QuickCharacter>();
            }

            actions = new QuickInputActions().Simple;
        }

        private void OnEnable()
        {
            actions.Enable();
        }

        private void OnDisable()
        {
            actions.Disable();
        }

        private void UpdateCameraPosition()
        {
            if (!cameraTransform)
            {
                return;
            }

            // This will handle changes in location when character moves and when the capsule height changes.
            cameraTransform.position = quickCharacter.topCenter;
        }

        private void UpdateLookInput()
        {
            Vector2 input = actions.Look.ReadValue<Vector2>();
            input *= cameraSensitivity;

            cameraPitch = Mathf.Clamp(cameraPitch - input.y, -MAX_CAMERA_PITCH, MAX_CAMERA_PITCH);
            float cameraYaw = cameraTransform.rotation.eulerAngles.y + input.x;

            cameraTransform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);
        }

        private void UpdateMovementInput()
        {
            Vector2 input = actions.Move.ReadValue<Vector2>();

            Vector3 wishDir = Vector3.zero;
            wishDir += Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized * input.y;
            wishDir += cameraTransform.right.normalized * input.x;

            quickCharacter.AddMovementInput(wishDir, input.magnitude);
        }

        private void Update()
        {
            UpdateCameraPosition();
            UpdateLookInput();
            UpdateMovementInput();
        }
    }
}