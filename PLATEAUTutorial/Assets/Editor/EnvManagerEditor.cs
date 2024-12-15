using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnvManager))]
public class EnvManagerEditor : Editor
{
    SerializedProperty simulateModeProperty;
    SerializedProperty timeScaleProperty;

    private void OnEnable()
    {
        // プロパティを取得
        simulateModeProperty = serializedObject.FindProperty("Mode");
        timeScaleProperty = serializedObject.FindProperty("TimeScale");
    }

    public override void OnInspectorGUI()
    {
        // 必須: serializedObjectの更新
        serializedObject.Update();

        // SimulateModeの表示
        EditorGUILayout.PropertyField(simulateModeProperty, new GUIContent("Simulate Mode"));

        // TimeScaleの表示/非表示
        EnvManager.SimulateMode simulateMode = (EnvManager.SimulateMode)simulateModeProperty.enumValueIndex;
        if (simulateMode == EnvManager.SimulateMode.Inference)
        {
            EditorGUILayout.PropertyField(timeScaleProperty, new GUIContent("Time Scale"));
        }

        // 他のすべてのプロパティを表示
        DrawPropertiesExcluding(serializedObject, "simulateMode", "TimeScale");

        // 必須: プロパティの変更を適用
        serializedObject.ApplyModifiedProperties();
    }
}
