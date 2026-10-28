using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Linq;
#if MELONLOADER
using Il2CppInterop.Runtime.Attributes;
using WeatherElectric.SecondaryMotion.Drawbridge;
#endif

// ReSharper disable UseCollectionExpression

namespace WeatherElectric.SecondaryMotion
{
#if UNITY_EDITOR
[AddComponentMenu("Secondary Motion/Secondary Motion")]
[HelpURL("https://git.weatherelectric.xyz/WeatherElectric/SecondaryMotion/wiki/SecondaryMotion")]
#endif
    public class SecondaryMotion : MonoBehaviour
    {
#if UNITY_EDITOR
        [Tooltip("The root bone.")]
#endif
        public Transform root;
#if UNITY_EDITOR
        [Tooltip("Bones to exclude in the simulation.")]
#endif
        public List<Transform> exclusions;
    
#if UNITY_EDITOR
        [Min(0f)]
        [Space(5f)]
        [Tooltip("Extra length at the end of the chain.")]
#endif
        public float endLength;
#if UNITY_EDITOR
        [Tooltip("Directional extra length at the end of the chain.")]
#endif
        public Vector3 endOffset = Vector3.zero;
        
#if UNITY_EDITOR
        [Tooltip("The update mode.")]
#endif
        public UpdateMode updateMode = UpdateMode.Normal;
        
#if UNITY_EDITOR
        [Tooltip("If enabled, Update Rate will be set to the application's fixedDeltaTime value in runtime.")]
#endif
        public bool syncUpdateRateToGameTime = true;
    
#if UNITY_EDITOR
        [Tooltip("The rate at which the simulation updates.")]
        [Min(1f)]
#endif
        public float updateRate = 60.0f;
    
#if UNITY_EDITOR
        [Space(5f)]
        [Range(0, 1)]
        [Tooltip("Blending between animation and simulation.")]
#endif
        public float blendWeight = 1.0f;
        
#if UNITY_EDITOR
        [Tooltip("Whether to stop simulation based on if the main meshes are visible or not.")]
#endif
        public bool enableCulling;
#if UNITY_EDITOR
        [Tooltip("Your primary meshes. If these aren't visible, the simulation stops. If you have LODs, these should be your highest quality LODs, usually LOD0 and LOD1. If those LODs get culled, then the simulation stops.")]
#endif
        public SkinnedMeshRenderer[] visibleRenderers;
    
#if UNITY_EDITOR
        [Range(0, 1)]
#endif
        public float dampening = 0.1f;
        public AnimationCurve dampeningCurve = new(new Keyframe(0, 1), new Keyframe(1, 1));

#if UNITY_EDITOR
        [Range(0, 1)]
#endif
        public float elasticity = 0.1f;
        public AnimationCurve elasticityCurve = new(new Keyframe(0, 1), new Keyframe(1, 1));

#if UNITY_EDITOR
        [Range(0, 1)]
#endif
        public float stiffness = 0.1f;
        public AnimationCurve stiffnessCurve = new(new Keyframe(0, 1), new Keyframe(1, 1));

#if UNITY_EDITOR
        [Range(0, 1)]
#endif
        public float inertia;
        public AnimationCurve inertiaCurve = new(new Keyframe(0, 1), new Keyframe(1, 1));
    
        public float friction;
        public AnimationCurve frictionCurve = new(new Keyframe(0, 1), new Keyframe(1, 1));
        
#if UNITY_EDITOR
        [Range(0f, 180f)]
        [Tooltip("The max angle a bone can rotate.")]
#endif
        public float maxAngle = 180f;
        public AnimationCurve maxAngleCurve = new(new Keyframe(0,1), new Keyframe(1,1));
    
#if UNITY_EDITOR
        [Space(5f)]
#endif
        public bool enableGravity;
#if UNITY_EDITOR
        [Tooltip("A good baseline is -9.81.")]
#endif
        public Vector3 gravity = new(0, -9.81f, 0);
    
        public Vector3 force = Vector3.zero;
        
        public float collisionRadius;
        public AnimationCurve collisionRadiusCurve = new(new Keyframe(0, 1), new Keyframe(1, 1));
        public List<SecondaryMotionCollider> colliders;
        

        private Vector3 _objectMove;
        private Vector3 _objectPrevPos;
        private float _objectScale;
        private float _weight = 1.0f;
        private readonly List<SecondaryMotionParticleTree> _particleTrees = new();
        private List<SecondaryMotionCollider> _effectiveCols;
        private float _deltaTime;
        private float _time;
        private int _preUpdCnt;
        private static int _updCnt;
        private static int _prepFrame;
        private bool _wasCulled;
        private bool _shouldSimulate;

        private void Awake()
        {
            if (root) return;
            root = transform;
        }

        private void Start()
        {
            ParticleSetup();
            SyncUpdateRate();
#if MELONLOADER
            FrustrumPlaneHelper.UpdateCamera(Camera.main);
            
            // this makes it seem like i hate dynamic bones but its just to prevent both scripts from running at once on the same object, and of course my mods gonna prioritize my own script
            // also has to be done through reflection or else melonloader doesnt like it
            if (DrawbridgeHelper.DynamicBonesAvailable) DynamicBoneCompat.StripFrom(gameObject);
            #if DEBUG
            Main.Logger.Msg($"FixedDeltaTime: {Time.fixedDeltaTime}");
            #endif
#endif
        }
        
        private void SyncUpdateRate()
        {
            if (!syncUpdateRateToGameTime && updateMode != UpdateMode.FixedUpdate) return;
            updateRate = 1f / Time.fixedDeltaTime;
        }

        private void FixedUpdate()
        {
            if (!_shouldSimulate) return;
            
            if (updateMode == UpdateMode.FixedUpdate)
            {
                SimUpdate();
            }
        }

        private void Update()
        {
            _shouldSimulate = CheckCulling();
            if (!_shouldSimulate) return;
            
            if (updateMode != UpdateMode.FixedUpdate)
            {
                SimUpdate();
            }
            
            ++_updCnt;
        }

        private void LateUpdate()
        {
            if (!_shouldSimulate) return;
            
            if (_preUpdCnt == 0)
                return;

            if (_updCnt > 0)
            {
                _updCnt = 0;
                ++_prepFrame;
            }

            SetWeight(blendWeight);
            
            if (UpdateNeeded())
            {
                Prepare();
                UpdateParticles();
                ApplyParticles();
            }

            _preUpdCnt = 0;
        }

#if MELONLOADER
        private bool? _isLocalPlayer;

        private bool IsLocalPlayerCached()
        {
            if (!DrawbridgeHelper.BoneLibAvailable) return true;
            
            if (_isLocalPlayer.HasValue) return _isLocalPlayer.Value;
            
            if (BoneLibCompat.TryIsLocalPlayer(this, out var result))
                _isLocalPlayer = result;

            return result;
        }
#endif
        
        private bool CheckCulling()
        {
#if MELONLOADER
            if (DrawbridgeHelper.BoneLibAvailable
                && Preferences.OnlySimulateLocalPlayer.Value 
                && !IsLocalPlayerCached())
            {
                if (!_wasCulled) InitTransforms();
                    _wasCulled = true;
                    return false;
            }
            
            if (!enableCulling
                || !Preferences.VisibilityCulling.Value
                || visibleRenderers.Length == 0)
            {
                _wasCulled = false;
                return true;
            }
            
            var culled = !ShouldSimulate();

            if (culled && !_wasCulled)
                InitTransforms();

            _wasCulled = culled;

            return !culled;

            bool ShouldSimulate()
            {
                foreach (var renderer in visibleRenderers)
                {
                    if (renderer && GeometryUtility.TestPlanesAABB(FrustrumPlaneHelper.FrustumPlanes, renderer.bounds))
                        return true;
                }

                return false;
            }
#endif
#if !MELONLOADER
            return true;
#endif
        }

        private void Prepare()
        {
            _deltaTime = updateMode switch
            {
                UpdateMode.UnscaledTime => Time.unscaledDeltaTime,
                UpdateMode.FixedUpdate => Time.fixedDeltaTime * _preUpdCnt,
                _ => Time.deltaTime
            };

#if MELONLOADER
            // prevents jitter on systems that can't run the game well
            if (Preferences.MinSimFPS.Value > 0)
            {
                var clamp = 1f / Preferences.MinSimFPS.Value;
                _deltaTime = Mathf.Min(_deltaTime, clamp);
            }
#endif

            _objectScale = Mathf.Abs(transform.lossyScale.x);
            _objectMove = transform.position - _objectPrevPos;
            _objectPrevPos = transform.position;

            foreach (var particleTree in _particleTrees)
            {
                foreach (var particle in particleTree.Particles)
                {
                    if (!particle.TransformIsntNull) continue;
                    particle.TransformPos = particle.Transform.position;
                    particle.TransformLocalPos = particle.Transform.localPosition;
                    particle.TransformLocalToWorld = particle.Transform.localToWorldMatrix;
                }
            }

            _effectiveCols?.Clear();

            if (colliders == null) return;
            foreach (var collider in colliders)
            {
                if (!collider || !collider.enabled) continue;
                _effectiveCols ??= new List<SecondaryMotionCollider>();
                _effectiveCols.Add(collider);

                if (collider.PrepareFrame == _prepFrame) continue;
                collider.Prepare();
                collider.PrepareFrame = _prepFrame;
            }
        }

        private bool UpdateNeeded()
        {
            return _weight > 0;
        }

        private void SimUpdate()
        {
            if (UpdateNeeded())
            {
                InitTransforms();
            }
            ++_preUpdCnt;
        }

        private void OnEnable()
        {
            ResetParticlesPosition();
            SyncUpdateRate();
        }

        private void OnDisable()
        {
            InitTransforms();
        }

        private void OnDidApplyAnimationProperties()
        {
            UpdateParams();
        }

        public void SetWeight(float w)
        {
            if (Mathf.Approximately(_weight, w)) return;
            if (w == 0)
            {
                InitTransforms();
            }
            else if (_weight == 0)
            {
                ResetParticlesPosition();
            }
            _weight = blendWeight = w;
        }

        private void ParticleSetup()
        {
            _particleTrees.Clear();

            if (root != null)
            {
                BuildParticleTree(root);
            }

            _objectScale = Mathf.Abs(transform.lossyScale.x);
            _objectPrevPos = transform.position;
            _objectMove = Vector3.zero;

            foreach (var particleTree in _particleTrees)
            {
                CreateParticles(particleTree, particleTree.Root, -1, 0);
            }

            UpdateParams();
        }
        
        private void BuildParticleTree(Transform rootTransform)
        {
            if (rootTransform == null)
                return;

            var particleTree = new SecondaryMotionParticleTree
            {
                Root = rootTransform
            };
            _particleTrees.Add(particleTree);
        }

#if MELONLOADER
        [HideFromIl2Cpp]
#endif
        private void CreateParticles(SecondaryMotionParticleTree particleTree, Transform bone, int parentIndex, float boneLength)
        {
            while (true)
            {
                var particle = new SecondaryMotionParticle { Transform = bone, TransformIsntNull = bone != null, ParentIndex = parentIndex };

                if (bone != null)
                {
                    particle.Position = particle.PrevPos = bone.position;
                    particle.InitLocalPos = bone.localPosition;
                    particle.InitLocalRot = bone.localRotation;
                }
                else
                {
                    var parentBone = particleTree.Particles[parentIndex].Transform;
                    if (endLength > 0)
                    {
                        var parentParentBone = parentBone.parent;
                        if (parentParentBone != null)
                        {
                            particle.EndOffset = parentBone.InverseTransformPoint(parentBone.position * 2 - parentParentBone.position) * endLength;
                        }
                        else
                        {
                            particle.EndOffset = new Vector3(endLength, 0, 0);
                        }
                    }
                    else
                    {
                        particle.EndOffset = parentBone.InverseTransformPoint(transform.TransformDirection(endOffset) + parentBone.position);
                    }

                    particle.Position = particle.PrevPos = parentBone.TransformPoint(particle.EndOffset);
                    particle.InitLocalPos = Vector3.zero;
                    particle.InitLocalRot = Quaternion.identity;
                }

                if (parentIndex >= 0)
                {
                    boneLength += (particleTree.Particles[parentIndex].Transform.position - particle.Position).magnitude;
                    particle.BoneLength = boneLength;
                    particleTree.BoneTotalLength = Mathf.Max(particleTree.BoneTotalLength, boneLength);
                    ++particleTree.Particles[parentIndex].ChildCount;
                }

                var index = particleTree.Particles.Count;
                particleTree.Particles.Add(particle);

                if (bone == null) return;
                for (var i = 0; i < bone.childCount; ++i)
                {
                    var child = bone.GetChild(i);
                    var exclude = false;
                    if (exclusions != null)
                    {
                        exclude = exclusions.Contains(child);
                    }

                    if (!exclude)
                    {
                        CreateParticles(particleTree, child, index, boneLength);
                    }
                    else if (endLength > 0 || endOffset != Vector3.zero)
                    {
                        CreateParticles(particleTree, null, index, boneLength);
                    }
                }

                if (bone.childCount == 0 && (endLength > 0 || endOffset != Vector3.zero))
                {
                    bone = null;
                    parentIndex = index;
                    continue;
                }

                break;
            }
        }

        private void UpdateParams()
        {
            SetWeight(blendWeight);

            foreach (var particleTree in _particleTrees)
            {
                foreach (var particle in particleTree.Particles)
                {
                    particle.Dampening = dampening;
                    particle.Elasticity = elasticity;
                    particle.Stiffness = stiffness;
                    particle.Inertia = inertia;
                    particle.Friction = friction;
                    particle.MaxAngle = maxAngle;
                    particle.CollisionRadius = collisionRadius;

                    if (particleTree.BoneTotalLength > 0)
                    {
                        var a = particle.BoneLength / particleTree.BoneTotalLength;
                        if (dampeningCurve != null && dampeningCurve.keys.Length > 0)
                            particle.Dampening *= dampeningCurve.Evaluate(a);
                        if (elasticityCurve != null && elasticityCurve.keys.Length > 0)
                            particle.Elasticity *= elasticityCurve.Evaluate(a);
                        if (stiffnessCurve != null && stiffnessCurve.keys.Length > 0)
                            particle.Stiffness *= stiffnessCurve.Evaluate(a);
                        if (inertiaCurve != null && inertiaCurve.keys.Length > 0)
                            particle.Inertia *= inertiaCurve.Evaluate(a);
                        if (frictionCurve != null && frictionCurve.keys.Length > 0)
                            particle.Friction *= frictionCurve.Evaluate(a);
                        if (collisionRadiusCurve != null && collisionRadiusCurve.keys.Length > 0)
                            particle.CollisionRadius *= collisionRadiusCurve.Evaluate(a);
                        if (maxAngleCurve != null && maxAngleCurve.keys.Length > 0)
                            particle.MaxAngle *= maxAngleCurve.Evaluate(a);
                    }

                    particle.Dampening = Mathf.Clamp01(particle.Dampening);
                    particle.Elasticity = Mathf.Clamp01(particle.Elasticity);
                    particle.Stiffness = Mathf.Clamp01(particle.Stiffness);
                    particle.Inertia = Mathf.Clamp01(particle.Inertia);
                    particle.Friction = Mathf.Clamp01(particle.Friction);
                    particle.MaxAngle = Mathf.Clamp(particle.MaxAngle, 0, 180);
                    particle.CollisionRadius = Mathf.Max(particle.CollisionRadius, 0);
                }
            }
        }

        private void InitTransforms()
        {
            foreach (var particleTree in _particleTrees)
            {
                SecondaryMotionHelpers.SetTransforms(particleTree);
            }
        }

        private void UpdateParticles()
        {
            if (_particleTrees.Count <= 0)
                return;

            var loop = 1;
            float timeVar = 1;
            var deltaTime = _deltaTime;
            var stepTime = deltaTime;

            if (updateMode == UpdateMode.Normal)
            {
                if (updateRate > 0)
                {
                    timeVar = deltaTime * updateRate;
                }
            }
            else
            {
                if (updateRate > 0)
                {
                    var frameTime = 1.0f / updateRate;
                    stepTime = frameTime;
                    _time += deltaTime;
                    loop = 0;

                    while (_time >= frameTime)
                    {
                        _time -= frameTime;
                        if (++loop < 3) continue;
                        _time = 0;
                        break;
                    }
                }
            }

            if (loop > 0)
            {
                for (var i = 0; i < loop; ++i)
                {
                    UpdateParticlesLoop(stepTime, i);
                    UpdateParticlesLoop(timeVar);
                }
            }
            else
            {
                SkipParticleUpdate();
            }
        }
        
        private void UpdateParticlesLoop(float stepTime, int loopIndex)
        {
            foreach (var particleTree in _particleTrees)
            {
                var objForce = enableGravity
                    ? (gravity + force) * (_objectScale * stepTime * stepTime)
                    : force * (_objectScale * stepTime * stepTime);

                var objectMove = loopIndex == 0 ? _objectMove : Vector3.zero;     

                foreach (var particle in particleTree.Particles)
                {
                    if (particle.ParentIndex >= 0)
                    {
                        var pos = particle.Position - particle.PrevPos;
                        var rmove = objectMove * particle.Inertia;
                        particle.PrevPos = particle.Position + rmove;
                        var damp = particle.Dampening;
                        if (particle.IsCollider)
                        {
                            damp += particle.Friction;
                            if (damp > 1)
                            {
                                damp = 1;
                            }
                            particle.IsCollider = false;
                        }
                        particle.Position += pos * (1 - damp) + objForce + rmove;
                    }
                    else
                    {
                        particle.PrevPos = particle.Position;
                        particle.Position = particle.TransformPos;
                    }
                }
            }
        }

        private void UpdateParticlesLoop(float timeVar)
        {
            foreach (var particleTree in _particleTrees)
            {
                for (var i = 1; i < particleTree.Particles.Count; ++i)
                {
                    var particle = particleTree.Particles[i];
                    var parent = particleTree.Particles[particle.ParentIndex];

                    var restLength = particle.TransformIsntNull ? (parent.TransformPos - particle.TransformPos).magnitude : parent.TransformLocalToWorld.MultiplyVector(particle.EndOffset).magnitude;
                
                    
                    var objStiffness = Mathf.Lerp(1.0f, particle.Stiffness, _weight);
                    if (objStiffness > 0 || particle.Elasticity > 0)
                    {
                        var matrix = parent.TransformLocalToWorld;
                        matrix.SetColumn(3, parent.Position);
                        var restPos = matrix.MultiplyPoint3x4(particle.TransformIsntNull ? particle.TransformLocalPos : particle.EndOffset);
                        
                        var restDir = (restPos - parent.Position).normalized; 
                        var currentDir = (particle.Position - parent.Position).normalized;
                        var angle = Vector3.Angle(restDir, currentDir);
                        if (angle > particle.MaxAngle)
                        {
                            var clampedDir = Vector3.RotateTowards(restDir, currentDir, particle.MaxAngle * Mathf.Deg2Rad, 0f);
                            particle.Position = parent.Position + clampedDir * restLength;
                        }

                        var restDifference = restPos - particle.Position;
                        particle.Position += restDifference * (particle.Elasticity * timeVar);

                        if (objStiffness > 0)
                        {
                            restDifference = restPos - particle.Position;
                            var len = restDifference.magnitude;
                            var maxlen = restLength * (1f + (1f - objStiffness) * 2);
                            if (len > maxlen)
                            {
                                particle.Position += restDifference * ((len - maxlen) / len);
                            }
                        }
                    }
                
                    if (_effectiveCols != null)
                    {
                        var particleRadius = particle.CollisionRadius * _objectScale;
                        foreach (var c in _effectiveCols)
                        {
                            particle.IsCollider |= c.Collide(ref particle.Position, particleRadius);
                        }
                    }
                
                    var parentDifference = parent.Position - particle.Position;
                    var parentLength = parentDifference.magnitude;
                    if (parentLength > 0)
                    {
                        particle.Position += parentDifference * ((parentLength - restLength) / parentLength);
                    }
                }
            }
        }
        
        private void SkipParticleUpdate()
        {
            foreach (var particleTree in _particleTrees)
            {
                var particles = particleTree.Particles;

                foreach (var particle in particles)
                {
                    if (particle.ParentIndex < 0)
                    {
                        particle.PrevPos = particle.Position;
                        particle.Position = particle.TransformPos;
                        continue;
                    }

                    particle.PrevPos += _objectMove;
                    particle.Position += _objectMove;

                    var parent = particles[particle.ParentIndex];

                    var restLength = particle.TransformIsntNull
                        ? Vector3.Distance(parent.TransformPos, particle.TransformPos)
                        : parent.TransformLocalToWorld.MultiplyVector(particle.EndOffset).magnitude;
                    
                    var objStiffness = Mathf.Lerp(1f, particle.Stiffness, _weight);

                    if (objStiffness > 0f)
                    {
                        var matrix = parent.TransformLocalToWorld;
                        matrix.SetColumn(3, parent.Position);

                        var restPos = matrix.MultiplyPoint3x4(
                            particle.TransformIsntNull
                                ? particle.TransformLocalPos
                                : particle.EndOffset
                        );

                        var difference = restPos - particle.Position;
                        var distance = difference.magnitude;

                        var maxDistance = restLength * (1f + (1f - stiffness) * 2f);

                        if (distance > maxDistance)
                        {
                            particle.Position += difference / distance * (distance - maxDistance);
                        }
                    }
                    
                    var direction = parent.Position - particle.Position;
                    var distanceToParent = direction.magnitude;

                    if (distanceToParent > 0f)
                    {
                        particle.Position +=
                            direction / distanceToParent *
                            (distanceToParent - restLength);
                    }
                }
            }
        }

        private void ResetParticlesPosition()
        {
            foreach (var particleTree in _particleTrees)
            {
                SecondaryMotionHelpers.ResetParticlePos(particleTree);
            }

            _objectPrevPos = transform.position;
        }

        private void ApplyParticles()
        {
            foreach (var particleTree in _particleTrees)
            {
                for (var i = 1; i < particleTree.Particles.Count; ++i)
                {
                    var particle = particleTree.Particles[i];
                    var parent = particleTree.Particles[particle.ParentIndex];

                    if (parent.ChildCount <= 1)
                    {
                        var localPos = particle.TransformIsntNull ? particle.Transform.localPosition : particle.EndOffset;
                        var origDirection = parent.Transform.TransformDirection(localPos);
                        var currDirection = particle.Position - parent.Position;

                        var rot = Quaternion.FromToRotation(origDirection, currDirection);
                        parent.Transform.rotation = rot * parent.Transform.rotation;
                    }

                    if (particle.TransformIsntNull)
                    {
                        particle.Transform.position = particle.Position;
                    }
                }
            }
        }
        
#if MELONLOADER
        public SecondaryMotion(IntPtr ptr)  : base(ptr) { }
#endif
    
#if UNITY_EDITOR
        private void Reset()
        {
            if (root == null) root = transform;
        }


        private void OnValidate()
        {
            SyncUpdateRate();
            updateRate = Mathf.Max(updateRate, 0);
            dampening = Mathf.Clamp01(dampening);
            elasticity = Mathf.Clamp01(elasticity);
            stiffness = Mathf.Clamp01(stiffness);
            inertia = Mathf.Clamp01(inertia);
            friction = Mathf.Clamp01(friction);
            collisionRadius = Mathf.Max(collisionRadius, 0);

            if (!Application.isEditor || !Application.isPlaying) return;
            if (_particleTrees.Any(particleTree => root != particleTree.Root))
            {
                InitTransforms();
                ParticleSetup();
            }
            else
            {
                UpdateParams();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!enabled)
                return;

            if (Application.isEditor && !Application.isPlaying && transform.hasChanged)
            {
                ParticleSetup();
            }
            
            foreach (var particleTree in _particleTrees)
            {
                DrawGizmos(particleTree);
            }
        }

        private void DrawGizmos(SecondaryMotionParticleTree particleTree)
        {
            foreach (var particle in particleTree.Particles)
            {
                if (particle.ParentIndex >= 0)
                {
                    var parentParticle = particleTree.Particles[particle.ParentIndex];
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(
                        parentParticle.Position,
                        particle.Position);
                    
                    if (particle.MaxAngle > 0f)
                    {
                        DrawMaxAngleGizmo(particle, parentParticle);
                    }
                }

                if (!(particle.CollisionRadius > 0))
                    continue;

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(
                    particle.Position,
                    particle.CollisionRadius * _objectScale);
            }
        }
        
        private static void DrawMaxAngleGizmo(SecondaryMotionParticle particle, SecondaryMotionParticle parent)
        {
            if (particle.Transform == null || parent.Transform == null || particle.MaxAngle <= 0f) return;

            var origin = parent.Transform.position;
            var restVector = particle.Transform.position - origin;
            var restLength = restVector.magnitude;
            if (restLength <= 0.0001f) return;
            var restDir = restVector / restLength;

            Gizmos.color = Color.cyan;

            DrawAngleCone(origin, restDir, particle.MaxAngle, restLength);
        }
        
        private static void DrawAngleCone(Vector3 origin, Vector3 direction, float angle, float length)
        {
            const int segments = 32;

            angle = Mathf.Clamp(angle, 0f, 180f);

            if (angle <= 0.001f)
            {
                Gizmos.DrawLine(origin,origin + direction * length);

                return;
            }

            var axis = Vector3.Cross(direction, Vector3.up);

            if (axis.sqrMagnitude < 0.001f)axis = Vector3.Cross(direction, Vector3.right);

            axis.Normalize();

            var secondAxis = Vector3.Cross(direction, axis).normalized;
            var angleRad = angle * Mathf.Deg2Rad;
            var ringRadius = Mathf.Sin(angleRad) * length;
            var ringDistance = Mathf.Cos(angleRad) * length;

            var ringCenter = origin + direction * ringDistance;

            var previous = ringCenter + axis * ringRadius;

            for (var i = 1; i <= segments; i++)
            {
                var theta = i / (float)segments * Mathf.PI * 2f;

                var offset = axis * Mathf.Cos(theta) * ringRadius + secondAxis * Mathf.Sin(theta) * ringRadius;

                var current = ringCenter + offset;

                Gizmos.DrawLine(previous, current);

                if (i % 4 == 0) Gizmos.DrawLine(origin, current);

                previous = current;
            }
        }
#endif
    }
}