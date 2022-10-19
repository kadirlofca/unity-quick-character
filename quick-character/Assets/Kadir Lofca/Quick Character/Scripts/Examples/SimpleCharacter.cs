// Author: Kadir Lofca
// github.com/kadirlofca

using UnityEngine;
using UnityEngine.InputSystem;

namespace QUICK.EXAMPLE
{
    /// <summary>
    /// SimpleCharacter is an example class that derives from QuickCharacter and contains logic for walking and falling.
    /// <see href="http://stackoverflow.com">Documentation/SimpleCharacter</see>
    /// </summary>
    public class SimpleCharacter : QuickCharacter
    {
        public Gait walkGait;
        public float jumpPower = 2.4f;
        public float airControl = 0.8f;
        public float gravity = 9.8f;

        public void OnJump()
        {
            Jump(jumpPower, 1, true);
        }

        protected override MoveMedium PhysicsUpdate()
        {
            if (medium == MoveMedium.ground)
            {
                ApplyFloorMovement(walkGait);
            }
            else
            {
                ApplyGravity(gravity);
                ApplyAirControlMovement(airControl);
            }

            return floor.isValid ? MoveMedium.ground : MoveMedium.air;
        }
    }
}

