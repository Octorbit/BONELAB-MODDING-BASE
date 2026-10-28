using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace WeatherElectric.SecondaryMotion
{
    [CustomEditor(typeof(SecondaryMotion))]
    [CanEditMultipleObjects]
    public class SecondaryMotionEditor : Editor
    {
        private SerializedProperty _root;
        private SerializedProperty _exclusions;
        private SerializedProperty _endLength;
        private SerializedProperty _endOffset;

		private SerializedProperty _syncUpdateRateToGameTime;
        private SerializedProperty _updateRate;
        private SerializedProperty _updateMode;
        private SerializedProperty _blendWeight;

        private SerializedProperty _enableCulling;
        private SerializedProperty _visibleRenderers;

        private SerializedProperty _dampening;
        private SerializedProperty _dampeningCurve;
        private SerializedProperty _elasticity;
        private SerializedProperty _elasticityCurve;
        private SerializedProperty _stiffness;
        private SerializedProperty _stiffnessCurve;
        private SerializedProperty _inertia;
        private SerializedProperty _inertiaCurve;
        private SerializedProperty _friction;
        private SerializedProperty _frictionCurve;
        private SerializedProperty _maxAngle;
        private SerializedProperty _maxAngleCurve;
        private SerializedProperty _enableGravity;
        private SerializedProperty _gravity;
        private SerializedProperty _force;

        private SerializedProperty _collisionRadius;
        private SerializedProperty _collisionRadiusCurve;
        private SerializedProperty _colliders;

        private static bool _showBones = true;
        private static bool _showSimulation = true;
        private static bool _showCulling;
        private static bool _showPhysics = true;
        private static bool _showCollision = true;

        private const string AvatarComponentTypeName = "SLZ.VRMK.Avatar";

        private static Type _avatarType;
        private static bool _avatarTypeSearched;

		// done via reflection so these scripts can exist in non-Marrow projects
        private static Type ResolveAvatarType()
        {
            if (_avatarTypeSearched) return _avatarType;
            _avatarTypeSearched = true;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type;
                try
                {
                    type = assembly.GetType(AvatarComponentTypeName);
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                if (type == null) continue;

                _avatarType = type;
                break;
            }

            return _avatarType;
        }

        private static Transform FindAvatarRoot(Transform from)
        {
            var avatarType = ResolveAvatarType();
            if (avatarType != null)
            {
                var current = from;
                while (current != null)
                {
                    if (current.GetComponent(avatarType) != null)
                        return current;
                    current = current.parent;
                }
            }

            return from.root;
        }

        private void OnEnable()
        {
            _root = serializedObject.FindProperty("root");
            _exclusions = serializedObject.FindProperty("exclusions");
            _endLength = serializedObject.FindProperty("endLength");
            _endOffset = serializedObject.FindProperty("endOffset");

			_syncUpdateRateToGameTime = serializedObject.FindProperty("syncUpdateRateToGameTime");
            _updateRate = serializedObject.FindProperty("updateRate");
            _updateMode = serializedObject.FindProperty("updateMode");
            _blendWeight = serializedObject.FindProperty("blendWeight");

            _enableCulling = serializedObject.FindProperty("enableCulling");
            _visibleRenderers = serializedObject.FindProperty("visibleRenderers");

            _dampening = serializedObject.FindProperty("dampening");
            _dampeningCurve = serializedObject.FindProperty("dampeningCurve");
            _elasticity = serializedObject.FindProperty("elasticity");
            _elasticityCurve = serializedObject.FindProperty("elasticityCurve");
            _stiffness = serializedObject.FindProperty("stiffness");
            _stiffnessCurve = serializedObject.FindProperty("stiffnessCurve");
            _inertia = serializedObject.FindProperty("inertia");
            _inertiaCurve = serializedObject.FindProperty("inertiaCurve");
            _friction = serializedObject.FindProperty("friction");
            _frictionCurve = serializedObject.FindProperty("frictionCurve");
            _maxAngle = serializedObject.FindProperty("maxAngle");
            _maxAngleCurve = serializedObject.FindProperty("maxAngleCurve");
            _enableGravity = serializedObject.FindProperty("enableGravity");
            _gravity = serializedObject.FindProperty("gravity");
            _force = serializedObject.FindProperty("force");

            _collisionRadius = serializedObject.FindProperty("collisionRadius");
            _collisionRadiusCurve = serializedObject.FindProperty("collisionRadiusCurve");
            _colliders = serializedObject.FindProperty("colliders");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBonesSection();
            EditorGUILayout.Space(4f);
            DrawSimulationSection();
            EditorGUILayout.Space(4f);
            DrawCullingSection();
            EditorGUILayout.Space(4f);
            DrawPhysicsSection();
            EditorGUILayout.Space(4f);
            DrawCollisionSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBonesSection()
        {
            _showBones = EditorGUILayout.Foldout(_showBones, "Bones", true, EditorStyles.foldoutHeader);
            if (!_showBones) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_root);
                if (_root.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Root is empty — it will default to this GameObject's transform at runtime (via Awake/Reset).", MessageType.Info);

                EditorGUILayout.PropertyField(_exclusions, true);
                EditorGUILayout.Space(4f);
                EditorGUILayout.PropertyField(_endLength);
                EditorGUILayout.PropertyField(_endOffset);
            }
        }

        private void DrawSimulationSection()
		{
			_showSimulation = EditorGUILayout.Foldout(_showSimulation, "Simulation", true, EditorStyles.foldoutHeader);
			if (!_showSimulation) return;

			using (new EditorGUI.IndentLevelScope())
			{
				EditorGUILayout.PropertyField(_updateMode);

				var isFixedUpdate = _updateMode.enumValueIndex == (int)UpdateMode.FixedUpdate;

				using (new EditorGUI.DisabledScope(isFixedUpdate))
				{
					EditorGUILayout.PropertyField(_syncUpdateRateToGameTime);

					using (new EditorGUI.DisabledScope(_syncUpdateRateToGameTime.boolValue))
					{
						EditorGUILayout.PropertyField(_updateRate);
					}
				}

				EditorGUILayout.Slider(_blendWeight, 0f, 1f);
			}
		}

        private void DrawCullingSection()
        {
            _showCulling = EditorGUILayout.Foldout(_showCulling, "Visibility Culling [EXPERIMENTAL]", true, EditorStyles.foldoutHeader);
            if (!_showCulling) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_enableCulling);

                if (!_enableCulling.boolValue) return;

                EditorGUILayout.PropertyField(_visibleRenderers, true);

                if (_visibleRenderers.arraySize == 0)
                {
                    EditorGUILayout.HelpBox(
                        "Culling is enabled but no renderers are assigned — simulation will never be culled. Assign your highest-quality LOD(s) above.",
                        MessageType.Warning);
                }
            }
        }

        private void DrawPhysicsSection()
        {
            _showPhysics = EditorGUILayout.Foldout(_showPhysics, "Physics", true, EditorStyles.foldoutHeader);
            if (!_showPhysics) return;

            using (new EditorGUI.IndentLevelScope())
            {
                DrawCurvePair("Dampening", _dampening, _dampeningCurve, 0f, 1f);
                DrawCurvePair("Elasticity", _elasticity, _elasticityCurve, 0f, 1f);
                DrawCurvePair("Stiffness", _stiffness, _stiffnessCurve, 0f, 1f);
                DrawCurvePair("Inertia", _inertia, _inertiaCurve, 0f, 1f);
                DrawCurvePair("Friction", _friction, _frictionCurve, 0f, 1f);
                DrawCurvePair("Max Angle", _maxAngle, _maxAngleCurve, 0f, 180f);

                EditorGUILayout.Space(6f);
                EditorGUILayout.PropertyField(_enableGravity);
                using (new EditorGUI.DisabledScope(!_enableGravity.boolValue))
                {
                    EditorGUILayout.PropertyField(_gravity);
                }
                EditorGUILayout.PropertyField(_force);
            }
        }

        private static void DrawCurvePair(string label, SerializedProperty scalar, SerializedProperty curve, float min, float max)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            var rect = EditorGUILayout.GetControlRect();
            var scalarWidth = rect.width * 0.55f;

            var scalarRect = new Rect(rect.x, rect.y, scalarWidth - 4f, rect.height);
            var curveRect = new Rect(rect.x + scalarWidth, rect.y, rect.width - scalarWidth, rect.height);

            EditorGUI.BeginProperty(scalarRect, GUIContent.none, scalar);
            scalar.floatValue = EditorGUI.Slider(scalarRect, scalar.floatValue, min, max);
            EditorGUI.EndProperty();

            EditorGUI.BeginProperty(curveRect, GUIContent.none, curve);
            curve.animationCurveValue = EditorGUI.CurveField(curveRect, curve.animationCurveValue);
            EditorGUI.EndProperty();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCollisionSection()
        {
            _showCollision = EditorGUILayout.Foldout(_showCollision, "Collision", true, EditorStyles.foldoutHeader);
            if (!_showCollision) return;

            using (new EditorGUI.IndentLevelScope())
            {
                DrawCurvePair("Collision Radius", _collisionRadius, _collisionRadiusCurve, 0f, 1f);
                EditorGUILayout.Space(4f);
                EditorGUILayout.PropertyField(_colliders, true);

                EditorGUILayout.Space(4f);
                if (GUILayout.Button("Find Colliders In Children"))
                {
                    PopulateColliders();
                }
            }
        }

        private void PopulateColliders()
        {
            var behaviour = (Component)target;
            var searchRoot = FindAvatarRoot(behaviour.transform);
            var found = searchRoot.GetComponentsInChildren<SecondaryMotionCollider>(true)
                .Where(c => c != null)
                .ToArray();

            Undo.RecordObject(target, "Find Colliders In Children");
            _colliders.arraySize = found.Length;
            for (var i = 0; i < found.Length; i++)
            {
                _colliders.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }

            serializedObject.ApplyModifiedProperties();

            if (found.Length == 0)
            {
                Debug.LogWarning($"No SecondaryMotionCollider components found under '{searchRoot.name}'.", behaviour);
            }
        }
    }
}