using UnityEditor;
using UnityEngine;

namespace WeatherElectric.SecondaryMotion
{
	[CustomEditor(typeof(SecondaryMotionCollider))]
	public class SecondaryMotionColliderEditor : Editor
	{
		private SerializedProperty _type;
		private SerializedProperty _height;
		private SerializedProperty _radius2;

		private void OnEnable()
		{
			_type = serializedObject.FindProperty("type");
			_height = serializedObject.FindProperty("height");
			_radius2 = serializedObject.FindProperty("radius2");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(_type);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("direction"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("bounds"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("center"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"));

			var type = (ColliderType)_type.enumValueIndex;

			if (type == ColliderType.Capsule)
			{
				EditorGUILayout.PropertyField(_height);
				EditorGUILayout.PropertyField(_radius2);
			}
			else
			{
				_height.floatValue = 0f;
				_radius2.floatValue = 0f;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}