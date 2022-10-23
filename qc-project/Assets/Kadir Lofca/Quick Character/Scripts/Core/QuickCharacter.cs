// Author: Kadir Lofca
// github.com/kadirlofca

using UnityEngine;
using static QUICK.QuickMath;

namespace QUICK
{
    /// <summary>
    /// QuickCharacter is the base class which you can derive your character scripts from.
    /// QuickCharacter contains many "plug and play" functions that you can use to customize your character.
    /// <see href="http://stackoverflow.com">Documentation/QuickCharacter</see>
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
    public class QuickCharacter : MonoBehaviour
    {
        // References
        protected Rigidbody rb { get; private set; }
        protected CapsuleCollider cap { get; private set; }

        // Character information
        protected MoveMedium medium { get; private set; }
        protected Surface floor { get; private set; }
        protected Vector3 worldInput { get; private set; }
        protected float timeSinceWalkedOffEdge { get; private set; }
        protected int numberOfJumps;

        // Constants
        private const float MAX_FLOOR_DIST = 0.04f;

        // Private
        private int forcedMediumFrames;

        #region Properties
        public Vector3 pos { get { return transform.position; } }
        public Vector3 topCenter { get { return pos + transform.up * (cap.height - cap.radius); } }
        public Vector3 center { get { return pos + transform.up * cap.height * 0.5f; } }
        public Vector3 bottomCenter { get { return pos + transform.up * cap.radius; } }
        public bool isValid { get { return rb && cap; } }
        #endregion

        #region Character Controls
        public void AddMovementInput(Vector3 wishDir, float scale)
        {
            wishDir.Normalize();

            worldInput = wishDir * Mathf.Clamp01(scale);
        }

        public bool HasMovementInput()
        {
            return worldInput != Vector3.zero;
        }

        public void ChangeStance(Stance stance)
        {
            cap.radius = stance.radius;
            cap.height = stance.height;

            // The capsule component must stay above the parent transform position.
            // This is very important because all calculations are done assuming the capsule is above the character position.
            // We offset the capsule center so that the bottom of the capsule touches the parent position.
            cap.center = Vector3.up * cap.height * 0.5f;
        }

        public void Jump(float force, int maxNumberOfJumps, bool resetVerticalVelocity)
        {
            // Walking off the floor edge consumes 1 jump.
            if (!floor.isValid && timeSinceWalkedOffEdge >= 0)
            {
                maxNumberOfJumps--;
            }

            if (numberOfJumps < maxNumberOfJumps)
            {
                // By forcing the medium to air, we are stopping forces such as floor friction.
                // Frame duration is arbitrarily set to 2, change if needed.
                ForceMediumForDuration(2, MoveMedium.air);

                if (resetVerticalVelocity && rb.velocity.y < 0)
                {
                    ResetVerticalVelocity();
                }

                rb.velocity += Vector3.up * force;
                numberOfJumps++;
            }
        }

        public void Jump(float force, int maxNumberOfJumps, bool resetVerticalVelocity, float cayoteTime)
        {
            bool canCayoteJump = timeSinceWalkedOffEdge >= 0 && timeSinceWalkedOffEdge < cayoteTime;

            // Walking off the floor edge consumes 1 jump.
            // This version is a bit different than Jump() without the cayoteTime because walking off an edge does not account for cayote time.
            // So instead of decrementing maxNumberOfJumps, we increment numberOfJumps to consume a jump. 
            if (!floor.isValid && !canCayoteJump && numberOfJumps <= 0)
            {
                numberOfJumps++;
            }

            if (numberOfJumps < maxNumberOfJumps)
            {
                // By forcing the medium to air, we are stopping forces such as floor friction.
                // Frame duration is arbitrarily set to 2, change if needed.
                ForceMediumForDuration(2, MoveMedium.air);

                if (resetVerticalVelocity && rb.velocity.y < 0)
                {
                    ResetVerticalVelocity();
                }

                rb.velocity += Vector3.up * force;
                numberOfJumps++;
            }
        }

        public void SurfaceJump(float upwardsForce, float perpendicularForce, bool resetVerticalVelocity, Surface surface)
        {
            // By forcing the medium to air, we are stopping the character from going back into the mode they started from.
            // Frame duration is arbitrarily set to 6, change if needed.
            ForceMediumForDuration(2, MoveMedium.air);

            if (resetVerticalVelocity && rb.velocity.y < 0)
            {
                ResetVerticalVelocity();
            }

            rb.velocity += (Vector3.up * upwardsForce) + (surface.normal * perpendicularForce);
            numberOfJumps++;
        }
        #endregion

        #region Character Physics Helpers
        protected void FindWall(Vector3 checkDirection, float checkLength, float checkHeight, float verticalOffsetFromCenter, Surface oldWallSurface, out Surface wallSurface)
        {
            checkDirection.Normalize();

            if (checkDirection == Vector3.zero)
            {
                wallSurface = Surface.invalidSurface;
                return;
            }

            // Radius * sqrt(2) = side length of the largest square that can fit in a circle with that radius.
            const float SQUAREROOT_TWO = 1.4f;
            float halfSideLength = cap.radius * SQUAREROOT_TWO * 0.5f;

            Vector3 halfExtents = new Vector3(halfSideLength, checkHeight * 0.5f, halfSideLength);
            Vector3 origin = center + Vector3.up * verticalOffsetFromCenter;
            float maxDistance = checkLength + cap.radius - halfSideLength;

            if (Physics.BoxCast(origin, halfExtents, checkDirection, out RaycastHit hit, Quaternion.LookRotation(checkDirection), maxDistance))
            {
                wallSurface = new Surface(hit);
                return;
            }

            wallSurface = Surface.invalidSurface;
        }

        private RaycastHit FindActualFloor(RaycastHit hitInfo)
        {
            Ray ray = new Ray();
            ray.origin = hitInfo.point + Vector3.up * SUPER_SMALL_NUMBER;
            ray.direction = Vector3.down;

            RaycastHit actualHit = new RaycastHit();
            hitInfo.collider.Raycast(ray, out actualHit, SUPER_SMALL_NUMBER * 2);

            return actualHit;
        }

        private void FindFloor()
        {
            // We add a small amount of offset because a SphereCast() does not detect objects that it initially overlaps.
            Vector3 origin = bottomCenter + transform.up * SUPER_SMALL_NUMBER;
            float maxDistance = MAX_FLOOR_DIST + SUPER_SMALL_NUMBER;
            float radius = cap.radius;

            if (Physics.SphereCast(origin, radius, -transform.up, out RaycastHit hit, maxDistance))
            {
                hit = FindActualFloor(hit);
                floor = new Surface(hit);
                return;
            }

            floor = Surface.invalidSurface;
        }

        private void CheckFloorEdge()
        {
            // timeOfWalkOffEdge < 0 means character is not standing on a floor or standing on floor but not at the edge.
            // timeOfWalkOffEdge = 0 means character is standing on the floor edge.
            // timeOfWalkOffEdge > 0 is the time passed since character walked off the edge of the floor (jumping will not trigger this).

            if (floor.isValid)
            {
                Ray ray = new Ray();
                ray.origin = pos + Vector3.up * SUPER_SMALL_NUMBER;
                ray.direction = Vector3.down;

                if (!Physics.Raycast(ray, SUPER_SMALL_NUMBER + MAX_FLOOR_DIST))
                {
                    timeSinceWalkedOffEdge = 0;
                    return;
                }
            }
            else if (timeSinceWalkedOffEdge >= 0)
            {
                // Here, character has walked off an edge and the timer is counting.

                timeSinceWalkedOffEdge += Time.fixedDeltaTime;
                return;
            }

            timeSinceWalkedOffEdge = -1;
        }

        private void ResetVerticalVelocity()
        {
            rb.velocity = rb.velocity.XZ();
        }
        #endregion

        #region Character Physics
        protected void ApplyWallClimbMovement(Gait gait, Surface wallSurface)
        {
            Vector3 wishDir = HasMovementInput() ? worldInput.RotateToNormal(wallSurface.normal) : Vector3.zero;
            Move(wishDir, gait, wallSurface);
        }

        protected void ApplyWallRunMovement(Gait gait, Surface wallSurface, Vector3 wishDir)
        {
            wishDir = HasMovementInput() ? Vector3.ProjectOnPlane(wishDir, wallSurface.normal) : Vector3.zero;
            Move(wishDir, gait, wallSurface);
        }

        protected void ApplyFloorMovement(Gait gait)
        {
            Vector3 wishDir = HasMovementInput() ? Vector3.ProjectOnPlane(worldInput, floor.normal) : Vector3.zero;
            Move(wishDir, gait, floor);
        }

        protected void ApplyAirMovement(float acceleration)
        {
            if (HasMovementInput())
            {
                Vector3 deltaVel = worldInput * acceleration * Time.fixedDeltaTime;

                rb.velocity += deltaVel;
            }
        }

        protected void ApplyAirControlMovement(float airControl)
        {
            if (HasMovementInput())
            {
                // We don't want to touch the vertical velocity.
                // So we use XZ() to remove the Y axis.
                // Later we replace the removed Y with rb.velocity.y.
                Vector3 currentVel = rb.velocity.XZ();
                Vector3 targetVel = (worldInput * rb.velocity.magnitude).XZ();
                Vector3 newVel = Vector3.RotateTowards(currentVel, targetVel, airControl * Time.fixedDeltaTime, 0);
                newVel.y = rb.velocity.y;

                rb.velocity = newVel;
            }
        }

        protected void ApplyDrag(float drag)
        {
            drag = Mathf.Max(drag, 0);

            Vector3 dragDir = -rb.velocity.normalized;

            rb.velocity += Vector3.ClampMagnitude(dragDir * rb.velocity.sqrMagnitude * drag * 0.5f * Time.fixedDeltaTime, rb.velocity.magnitude);
        }

        protected void ApplySurfaceDrag(float drag, Surface surface)
        {
            ApplyDrag(drag * surface.friction);
        }

        protected void ApplyGravity(float gravity)
        {
            gravity = Mathf.Max(gravity);
            rb.velocity += -Vector3.up * gravity * Time.fixedDeltaTime;
        }   

        protected void ApplyGravity(float descendingGravity, float ascendingGravity)
        {
            float gravity = (rb.velocity.y > 0) ? ascendingGravity : descendingGravity;

            ApplyGravity(gravity);
        }

        private void Move(Vector3 wishDir, Gait gait, Surface surface)
        {
            wishDir.Normalize();

            Vector3 targetVel = surface.velocity;
            float deltaDist = surface.friction * Time.fixedDeltaTime;

            if (wishDir == Vector3.zero)
            {
                deltaDist *= gait.decel;
                rb.velocity = Vector3.MoveTowards(rb.velocity, Vector3.zero, deltaDist);
            }
            else
            {
                targetVel += wishDir * gait.speed;
                deltaDist *= gait.accel;
                rb.velocity = Vector3.MoveTowards(rb.velocity, targetVel, deltaDist);
            }
        }
        #endregion

        #region Framework
        protected virtual MoveMedium PhysicsUpdate()
        {
            return floor.isValid ? MoveMedium.ground : MoveMedium.air;
        }

        private void PrePhysicsUpdate()
        {
            FindFloor();
            CheckFloorEdge();
        }

        protected bool DidMediumChangeTo(MoveMedium oldMedium, MoveMedium comparedMedium)
        {
            return oldMedium != comparedMedium && medium == comparedMedium;
        }

        protected virtual void OnMediumChange(MoveMedium oldMedium)
        {
        }

        private void OnMediumChangeInternal(MoveMedium oldMedium)
        {
            // Reset jump when landed on ground or wall.
            if (DidMediumChangeTo(oldMedium, MoveMedium.ground) || DidMediumChangeTo(oldMedium, MoveMedium.wall))
            {
                numberOfJumps = 0;
            }
        }

        private void ForceMediumForDuration(int nFramesDuration, MoveMedium forcedMedium)
        {
            forcedMediumFrames = Mathf.Max(nFramesDuration, 0);
            SetMedium(forcedMedium);
        }

        private void SetMedium(MoveMedium newMedium)
        {
            MoveMedium oldMedium = medium;
            medium = newMedium;
            OnMediumChangeInternal(oldMedium);
            OnMediumChange(oldMedium);
        }

        private void PostPhysicsUpdate(MoveMedium newMedium)
        {
            // Using the ForceMediumForDuration() method, we can set forcedMediumFrames. 
            // This ensures that during that amount of frames, the medium can not be changed.
            if (forcedMediumFrames > 0)
            {
                forcedMediumFrames--;
            }
            else
            {
                SetMedium(newMedium);
            }
        }

        protected virtual void FixedUpdate()
        {
            PrePhysicsUpdate();
            MoveMedium nextMedium = PhysicsUpdate();
            PostPhysicsUpdate(nextMedium);
        }

        private void InitializeComponents()
        {
            rb = GetComponent<Rigidbody>();
            cap = GetComponent<CapsuleCollider>();

            // Force some of the rigidbody variables for best results.
            rb.freezeRotation = true;
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.inertiaTensor = Vector3.zero;
            rb.drag = rb.angularDrag = 0;

            // Force some of the capsule variables for best results.
            cap.isTrigger = false;
            cap.direction = 1;
            cap.center = Vector3.up * cap.height * 0.5f;
            cap.material.dynamicFriction = cap.material.staticFriction = 0;
            cap.material.bounciness = 0;
        }

        protected virtual void Awake()
        {
            InitializeComponents();
        }
        #endregion
    }
}