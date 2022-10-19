// Author: Kadir Lofca
// github.com/kadirlofca

using UnityEngine;

namespace QUICK
{
    public enum MoveMedium
    {
        air,
        ground,
        wall
    }

    [System.Serializable]
    public struct Gait
    {
        public float speed;
        public float accel;
        public float decel;
    }

    [System.Serializable]
    public struct Stance
    {
        public float height;
        public float radius;

        public bool Compare(Stance compared)
        {
            return this.height == compared.height && this.radius == compared.radius;
        }
    }

    public struct Surface
    {
        public Vector3 point;
        public Vector3 normal;
        public Collider collider;

        private const float FALLBACK_FRICTION = 1f;
        private const float MIN_FRICTION = 0.004f;

        public bool isValid { get { return this.normal != Vector3.zero; } }
        public Vector3 velocity { get { return collider?.attachedRigidbody ? collider.attachedRigidbody.GetPointVelocity(point) : Vector3.zero; } }
        public float friction { get { return GetSafeFriction(); } }
        public static Surface invalidSurface { get { return new Surface(Vector3.zero, Vector3.zero, null); } }        

        public Surface(Vector3 point, Vector3 normal, Collider collider)
        {
            this.point = point;
            this.normal = normal;
            this.collider = collider;
        }

        public Surface(RaycastHit raycastHit)
        {
            this.point = raycastHit.point;
            this.normal = raycastHit.normal;
            this.collider = raycastHit.collider;
        }

        private float GetSafeFriction()
        {
            bool validPhysicMaterial = isValid && collider && collider.material;
            return Mathf.Clamp(validPhysicMaterial ? collider.material.staticFriction : FALLBACK_FRICTION, MIN_FRICTION, 1);
        }

        public override string ToString()
        {
            return isValid ? "Valid surface." : "Invalid surface.";
        }
    }

    public static class QuickMath
    {
        public const float SMALL_NUMBER = 0.1f;
        public const float VERY_SMALL_NUMBER = 0.01f;
        public const float SUPER_SMALL_NUMBER = 0.001f;

        public static float HemisphereX2Y(float x, float radius)
        {
            // Hemisphere equation: y = sqrt(r^2-x^2).
            float xSquare = x * x;
            float rSquare = radius * radius;
            return Mathf.Sqrt(Mathf.Abs(rSquare - xSquare));
        }

        public static float Map(this float from, float fromMin, float fromMax, float toMin, float toMax)
        {
            var fromAbs = from - fromMin;
            var fromMaxAbs = fromMax - fromMin;

            var normal = fromAbs / fromMaxAbs;

            var toMaxAbs = toMax - toMin;
            var toAbs = toMaxAbs * normal;

            var to = Mathf.Clamp(toAbs + toMin, toMin, toMax);

            return to;
        }
    }

    public static class QuickExtensions
    {
        public static Vector3 XZ(this Vector3 v)
        {
            return new Vector3(v.x, 0, v.z);
        }

        public static Vector3 RotateToNormal(this Vector3 v, Vector3 normal)
        {
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
            return rotation * v;
        }
    } 
}

namespace QUICK.EXAMPLE
{
    [System.Serializable]
    public struct StanceGait
    {
        public Gait gait;
        public Stance stance;
    }
}