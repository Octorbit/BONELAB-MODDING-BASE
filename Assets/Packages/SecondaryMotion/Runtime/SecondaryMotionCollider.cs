using UnityEngine;
using System.Collections.Generic;
using System;
#if MELONLOADER
using WeatherElectric.SecondaryMotion.Drawbridge;
#endif

namespace WeatherElectric.SecondaryMotion
{
#if UNITY_EDITOR
[AddComponentMenu("Secondary Motion/Secondary Motion Collider")]
[HelpURL("https://git.weatherelectric.xyz/WeatherElectric/SecondaryMotion/wiki/SecondaryMotionCollider")]
#endif
    public class SecondaryMotionCollider : MonoBehaviour
    {
        // ReSharper disable once UnusedMember.Global
        public ColliderType type = ColliderType.Sphere;
        public ColliderDirection direction = ColliderDirection.Y;
        public ColliderBounds bounds = ColliderBounds.Outside;

        public int PrepareFrame { get; set; }
        public Vector3 center = Vector3.zero;
        public float radius = 0.5f;
        public float height;
        public float radius2;

        private float _scaledRadius;
        private float _scaledRadius2;
        private Vector3 _col1;
        private Vector3 _col2;
        private float _col1Col2Distance;
        private int _collideType;
        
#if MELONLOADER
        public void Start()
        {
            // this makes it seem like i hate dynamic bones but its just to prevent both scripts from running at once on the same object, and of course my mods gonna prioritize my own script
            // also has to be done through reflection or else melonloader doesnt like it
            if (DrawbridgeHelper.DynamicBonesAvailable) DynamicBoneCompat.StripFrom(gameObject, true);
        }
#endif

        public void Prepare()
        {
            var scale = Mathf.Abs(transform.lossyScale.x);
            var halfHeight = height * 0.5f;

            if (radius2 <= 0 || Mathf.Abs(radius - radius2) < 0.01f)
            {
                _scaledRadius = radius * scale;

                var newHeight = halfHeight - radius;
                if (newHeight <= 0)
                {
                    _col1 = transform.TransformPoint(center);

                    _collideType = bounds == ColliderBounds.Outside ? 0 : 1;
                }
                else
                {
                    var center1 = center;
                    var center2 = center;

                    switch (direction)
                    {
                        case ColliderDirection.X:
                            center1.x += newHeight;
                            center2.x -= newHeight;
                            break;
                        case ColliderDirection.Y:
                            center1.y += newHeight;
                            center2.y -= newHeight;
                            break;
                        case ColliderDirection.Z:
                            center1.z += newHeight;
                            center2.z -= newHeight;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    _col1 = transform.TransformPoint(center1);
                    _col2 = transform.TransformPoint(center2);
                    _col1Col2Distance = (_col2 - _col1).magnitude;

                    _collideType = bounds == ColliderBounds.Outside ? 2 : 3;
                }
            }
            else
            {
                var r = Mathf.Max(radius, radius2);
                if (halfHeight - r <= 0)
                {
                    _scaledRadius = r * scale;
                    _col1 = transform.TransformPoint(center);

                    _collideType = bounds == ColliderBounds.Outside ? 0 : 1;
                }
                else
                {
                    _scaledRadius = radius * scale;
                    _scaledRadius2 = radius2 * scale;

                    var height1 = halfHeight - radius;
                    var height2 = halfHeight - radius2;
                    var center1 = center;
                    var center2 = center;

                    switch (direction)
                    {
                        case ColliderDirection.X:
                            center1.x += height1;
                            center2.x -= height2;
                            break;
                        case ColliderDirection.Y:
                            center1.y += height1;
                            center2.y -= height2;
                            break;
                        case ColliderDirection.Z:
                            center1.z += height1;
                            center2.z -= height2;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    _col1 = transform.TransformPoint(center1);
                    _col2 = transform.TransformPoint(center2);
                    _col1Col2Distance = (_col2 - _col1).magnitude;

                    _collideType = bounds == ColliderBounds.Outside ? 4 : 5;
                }
            }
        }

        public bool Collide(ref Vector3 particlePosition, float particleRadius)
        {
            return _collideType switch
            {
                0 => SecondaryMotionHelpers.CollideOutsideSphere(ref particlePosition, particleRadius, _col1, _scaledRadius),
                1 => SecondaryMotionHelpers.CollideInsideSphere(ref particlePosition, particleRadius, _col1, _scaledRadius),
                2 => SecondaryMotionHelpers.CollideOutsideCapsule(ref particlePosition, particleRadius, _col1, _col2, _scaledRadius, _col1Col2Distance),
                3 => SecondaryMotionHelpers.CollideInsideCapsule(ref particlePosition, particleRadius, _col1, _col2, _scaledRadius, _col1Col2Distance),
                4 => SecondaryMotionHelpers.CollideOutsideCapsule2(ref particlePosition, particleRadius, _col1, _col2, _scaledRadius, _scaledRadius2,
                    _col1Col2Distance),
                5 => SecondaryMotionHelpers.CollideInsideCapsule2(ref particlePosition, particleRadius, _col1, _col2, _scaledRadius, _scaledRadius2,
                    _col1Col2Distance),
                _ => false
            };
        }
    
#if MELONLOADER
        // ReSharper disable once ConvertToPrimaryConstructor
        public SecondaryMotionCollider(IntPtr ptr) : base(ptr) { }
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            radius = Mathf.Max(radius, 0);
            height = Mathf.Max(height, 0);
            radius2 = Mathf.Max(radius2, 0);
        }

        private void OnDrawGizmosSelected()
        {
            if (!enabled)
                return;

            Prepare();

            Gizmos.color = bounds == ColliderBounds.Outside ? Color.green : Color.magenta;

            switch (_collideType)
            {
                case 0:
                case 1:
                    Gizmos.DrawWireSphere(_col1, _scaledRadius);
                    break;
                case 2:
                case 3:
                    DrawCapsule(_col1, _col2, _scaledRadius, _scaledRadius);
                    break;
                case 4:
                case 5:
                    DrawCapsule(_col1, _col2, _scaledRadius, _scaledRadius2);
                    break;
            }
        }

        private static void DrawCapsule(Vector3 c0, Vector3 c1, float radius0, float radius1)
        {
            Gizmos.DrawLine(c0, c1);
            Gizmos.DrawWireSphere(c0, radius0);
            Gizmos.DrawWireSphere(c1, radius1);
        }
#endif
    }
}
