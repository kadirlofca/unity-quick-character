// Author: Kadir Lofca
// github.com/kadirlofca

using UnityEngine;
using UnityEngine.InputSystem;

namespace QUICK.EXAMPLE
{
    /// <summary>
    /// AdvancedCharacter is an example class that derives from QuickCharacter and contains logic for various types of movement
    /// that QuickCharacter has to offer.
    /// <see href="http://stackoverflow.com">Documentation/AdvancedCharacter</see>
    /// </summary>
    public class AdvancedCharacter : QuickCharacter
    {
        [Header("Ground Movement")]
        public float friction = 7.8f;
        public StanceGait walkGait;
        public StanceGait crouchGait;

        [Header("Jumping")]
        public float jumpForce = 3.6f;
        public int maxJumps = 2;
        public float cayoteTime = 0.36f;

        [Header("Air Movement")]
        public float drag = 0.06f;
        public float airControl = 1.4f;
        public float airAcceleration = 8f;
        public float airAccelBoostThreshold = 4f;
        public float ascendingGravity = 9.8f;
        public float descendingGravity = 14.2f;

        [Header("Wall Movement")]
        public float wallCheckLength = 0.1f;
        public float wallJumpUpwardsForce = 3.6f;
        public float wallJumpPerpendicularForce = 2f;
        public Gait wallGait;

        [Header("References")]
        public Transform controlTransform;

        private StanceGait currentGroundGait;
        private Surface wallSurface;

        public void OnJump()
        {
            if (medium == MoveMedium.wall)
            {
                SurfaceJump(wallJumpUpwardsForce, wallJumpPerpendicularForce, true, wallSurface);
                return;
            }

            Jump(jumpForce, maxJumps, true, cayoteTime);


            GetComponentInChildren<Animator>().SetTrigger("jump");
        }

        public void OnCrouch()
        {
            if (currentGroundGait.stance.Compare(crouchGait.stance))
            {
                ChangeStance(walkGait.stance);
                currentGroundGait.stance = walkGait.stance;
                currentGroundGait.gait = walkGait.gait;
            }
            else
            {
                ChangeStance(crouchGait.stance);
                currentGroundGait.stance = crouchGait.stance;
                currentGroundGait.gait = crouchGait.gait;
            }
        }

        private void WallUpdate()
        {
            ApplyWallRunMovement(wallGait, wallSurface, controlTransform.forward.XZ());
            ApplyDrag(drag);
        }

        private void GroundUpdate()
        {
            // If velocity is greater than walk speed, the player has lost control over their movement.
            if (rb.velocity.magnitude > currentGroundGait.gait.speed)
            {
                ApplySurfaceDrag(friction, floor);
            }
            else
            {
                ApplyFloorMovement(currentGroundGait.gait);
            }
        }

        private void AirUpdate()
        {
            ApplyGravity(descendingGravity, ascendingGravity);
            ApplyAirControlMovement(airControl);
            ApplyAirMovement(rb.velocity.XZ().magnitude.Map(airAccelBoostThreshold, 0, 0, airAcceleration));
            ApplyDrag(drag);
        }

        private MoveMedium FindNextMedium()
        {
            FindWall(worldInput, wallCheckLength, cap.height - (cap.radius * 2), 0, wallSurface, out wallSurface);

            if (floor.isValid)
            {
                return MoveMedium.ground;
            }
            else if (wallSurface.isValid)
            {
                return MoveMedium.wall;
            }
            else
            {
                return MoveMedium.air;
            }
        }

        protected override MoveMedium PhysicsUpdate()
        {
            switch (medium)
            {
                case MoveMedium.ground:
                    GroundUpdate();
                    break;

                case MoveMedium.air:
                    AirUpdate();
                    break;

                case MoveMedium.wall:
                    WallUpdate();
                    break;

                default:
                    AirUpdate();
                    break;
            }

            return FindNextMedium();
        }

        protected override void OnMediumChange(MoveMedium oldMedium)
        {
            if(DidMediumChangeTo(oldMedium, MoveMedium.ground))
            {
                GetComponentInChildren<Animator>().SetTrigger("land");
            }
        }

        protected override void Awake()
        {
            base.Awake();

            currentGroundGait.gait = walkGait.gait;
            currentGroundGait.stance = walkGait.stance;
        }
    }
}