using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace WeatherElectric.SecondaryMotion
{
    internal static class SecondaryMotionHelpers
    {
        public static bool CollideOutsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
        {
            var radius = sphereRadius + particleRadius;
            var radius2 = radius * radius;
            var initialDist = particlePosition - sphereCenter;
            var squaredDistance = initialDist.sqrMagnitude;
        
            if (!(squaredDistance > 0) || !(squaredDistance < radius2)) return false;
            var distance = Mathf.Sqrt(squaredDistance);
            particlePosition = sphereCenter + initialDist * (radius / distance);
            return true;
        }

        public static bool CollideInsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
        {
            var radius = sphereRadius - particleRadius;
            var radius2 = radius * radius;
            var initialDist = particlePosition - sphereCenter;
            var squaredDistance = initialDist.sqrMagnitude;

            if (!(squaredDistance > radius2)) return false;
            var distance = Mathf.Sqrt(squaredDistance);
            particlePosition = sphereCenter + initialDist * (radius / distance);
            return true;
        }

        public static bool CollideOutsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius, float dirlen)
        {
            var radius = capsuleRadius + particleRadius;
            var radius2 = radius * radius;
            var dir = capsuleP1 - capsuleP0;
            var d = particlePosition - capsuleP0;
            var t = Vector3.Dot(d, dir);

            if (t <= 0)
            {
                var dlen2 = d.sqrMagnitude;
                if (!(dlen2 > 0) || !(dlen2 < radius2)) return false;
                var dlen = Mathf.Sqrt(dlen2);
                particlePosition = capsuleP0 + d * (radius / dlen);
                return true;
            }

            var dirlen2 = dirlen * dirlen;
            if (t >= dirlen2)
            {
                d = particlePosition - capsuleP1;
                var dlen2 = d.sqrMagnitude;
                if (!(dlen2 > 0) || !(dlen2 < radius2)) return false;
                var dlen = Mathf.Sqrt(dlen2);
                particlePosition = capsuleP1 + d * (radius / dlen);
                return true;
            }
        
            var q = d - dir * (t / dirlen2);
            var qlen2 = q.sqrMagnitude;
            if (!(qlen2 > 0) || !(qlen2 < radius2)) return false;
            var qlen = Mathf.Sqrt(qlen2);
            particlePosition += q * ((radius - qlen) / qlen);
            return true;
        }

        public static bool CollideInsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius, float dirlen)
        {
            var r = capsuleRadius - particleRadius;
            var r2 = r * r;
            var dir = capsuleP1 - capsuleP0;
            var d = particlePosition - capsuleP0;
            var t = Vector3.Dot(d, dir);

            if (t <= 0)
            {
                var dlen2 = d.sqrMagnitude;
                if (!(dlen2 > r2)) return false;
                var dlen = Mathf.Sqrt(dlen2);
                particlePosition = capsuleP0 + d * (r / dlen);
                return true;
            }

            var dirlen2 = dirlen * dirlen;
            if (t >= dirlen2)
            {
                d = particlePosition - capsuleP1;
                var dlen2 = d.sqrMagnitude;
                if (!(dlen2 > r2)) return false;
                var dlen = Mathf.Sqrt(dlen2);
                particlePosition = capsuleP1 + d * (r / dlen);
                return true;
            }
        
            var q = d - dir * (t / dirlen2);
            var qlen2 = q.sqrMagnitude;
            if (!(qlen2 > r2)) return false;
            var qlen = Mathf.Sqrt(qlen2);
            particlePosition += q * ((r - qlen) / qlen);
            return true;
        }

        public static bool CollideOutsideCapsule2(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius0, float capsuleRadius1, float dirlen)
        {
            var dir = capsuleP1 - capsuleP0;
            var d = particlePosition - capsuleP0;
            var t = Vector3.Dot(d, dir);

            if (t <= 0)
            {
                var r = capsuleRadius0 + particleRadius;
                var r2 = r * r;
                var dlen2 = d.sqrMagnitude;
                if (!(dlen2 > 0) || !(dlen2 < r2)) return false;
                var dlen = Mathf.Sqrt(dlen2);
                particlePosition = capsuleP0 + d * (r / dlen);
                return true;
            }

            var dirlen2 = dirlen * dirlen;
            if (t >= dirlen2)
            {
                var r = capsuleRadius1 + particleRadius;
                var r2 = r * r;
                d = particlePosition - capsuleP1;
                var dlen2 = d.sqrMagnitude;
                if (!(dlen2 > 0) || !(dlen2 < r2)) return false;
                var dlen = Mathf.Sqrt(dlen2);
                particlePosition = capsuleP1 + d * (r / dlen);
            }
            else
            {
                var q = d - dir * (t / dirlen2);
                var qlen2 = q.sqrMagnitude;

                var klen = Vector3.Dot(d, dir / dirlen);
                var r = Mathf.Lerp(capsuleRadius0, capsuleRadius1, klen / dirlen) + particleRadius;
                var r2 = r * r;

                if (!(qlen2 > 0) || !(qlen2 < r2)) return false;
                var qlen = Mathf.Sqrt(qlen2);
                particlePosition += q * ((r - qlen) / qlen);
            }

            return true;
        }

        public static bool CollideInsideCapsule2(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius0, float capsuleRadius1, float dirlen)
        {
            var dir = capsuleP1 - capsuleP0;
            var d = particlePosition - capsuleP0;
            var t = Vector3.Dot(d, dir);

            if (t <= 0)
            {
                var r = capsuleRadius0 - particleRadius;
                var r2 = r * r;
                var dlen2 = d.sqrMagnitude;
                if (!(dlen2 > r2)) return false;
                var dlen = Mathf.Sqrt(dlen2);
                particlePosition = capsuleP0 + d * (r / dlen);
                return true;
            }

            var dirlen2 = dirlen * dirlen;
            if (t >= dirlen2)
            {
                var r = capsuleRadius1 - particleRadius;
                var r2 = r * r;
                d = particlePosition - capsuleP1;
                var dlen2 = d.sqrMagnitude;
                if (!(dlen2 > r2)) return false;
                var dlen = Mathf.Sqrt(dlen2);
                particlePosition = capsuleP1 + d * (r / dlen);
            }
            else
            {
                var q = d - dir * (t / dirlen2);
                var qlen2 = q.sqrMagnitude;

                var klen = Vector3.Dot(d, dir / dirlen);
                var r = Mathf.Lerp(capsuleRadius0, capsuleRadius1, klen / dirlen) - particleRadius;
                var r2 = r * r;

                if (!(qlen2 > r2)) return false;
                var qlen = Mathf.Sqrt(qlen2);
                particlePosition += q * ((r - qlen) / qlen);
            }

            return true;
        }

        public static void ResetParticlePos(SecondaryMotionParticleTree particleTree)
        {
            foreach (var particle in particleTree.Particles)
            {
                if (particle.TransformIsntNull)
                {
                    particle.Position = particle.PrevPos = particle.Transform.position;
                }
                else
                {
                    var pb = particleTree.Particles[particle.ParentIndex].Transform;
                    particle.Position = particle.PrevPos = pb.TransformPoint(particle.EndOffset);
                }
                particle.IsCollider = false;
            }
        }

        public static void SetTransforms(SecondaryMotionParticleTree particleTree)
        {
            foreach (var particle in particleTree.Particles)
            {
                if (!particle.TransformIsntNull) continue;
                particle.Transform.localPosition = particle.InitLocalPos;
                particle.Transform.localRotation = particle.InitLocalRot;
            }
        }

#if MELONLOADER
        private static Assembly _dynamicBones;
        public static Assembly GetDynamicBonesAssembly()
        {
            if (_dynamicBones != null) return _dynamicBones;
            
            _dynamicBones = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(
                    a.GetName().Name,
                    "DynamicBonesBL",
                    StringComparison.OrdinalIgnoreCase));
#if DEBUG
            if (_dynamicBones != null)
            {
                Main.Logger.Msg($"Found {_dynamicBones.GetName().Name}, version {_dynamicBones?.GetName().Version}");
            }
#endif
            return _dynamicBones != null ? null : _dynamicBones;
        }
#endif
    }
}