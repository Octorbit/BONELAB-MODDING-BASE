using UnityEngine;

namespace WeatherElectric.SecondaryMotion
{
    public class SecondaryMotionParticle
    {
        public Transform Transform;
        public int ParentIndex;
        public int ChildCount;
        public float Dampening;
        public float Elasticity;
        public float Stiffness;
        public float Inertia;
        public float Friction;
        public float MaxAngle;
        public float CollisionRadius;
        public float BoneLength;
        public bool IsCollider;
        public bool TransformIsntNull;

        public Vector3 Position;
        public Vector3 PrevPos;
        public Vector3 EndOffset;
        public Vector3 InitLocalPos;
        public Quaternion InitLocalRot;
    
        public Vector3 TransformPos;
        public Vector3 TransformLocalPos;
        public Matrix4x4 TransformLocalToWorld;
    }
}