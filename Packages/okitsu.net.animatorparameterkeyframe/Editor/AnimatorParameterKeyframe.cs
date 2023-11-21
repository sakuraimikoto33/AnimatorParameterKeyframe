using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class AnimatorParameterKeyframe : EditorWindow
{
    private AnimationClip selectedClip;
    private string parameterName;
    private float parameterValue;
    private AnimatorController animatorController;
    private int selectedParameterIndex = 0;
    private AnimatorControllerParameterType[] parameterTypes; // パラメータの種類

    [MenuItem("Window/Animator Parameter Keyframe")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AnimatorParameterKeyframe));
    }

    private void OnGUI()
    {
        GUILayout.Label("Animator Parameter Keyframe", EditorStyles.boldLabel);

        selectedClip = EditorGUILayout.ObjectField("Animation Clip", selectedClip, typeof(AnimationClip), true) as AnimationClip;

        // Animation Clipが未指定の場合、新しいAnimation Clipを作成するオプションを表示
        if (selectedClip == null)
        {
            EditorGUILayout.HelpBox("No Animation Clip selected. Create a new Animation Clip?", MessageType.Info);
            if (GUILayout.Button("Create New Animation Clip"))
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Animation Clip", "New Animation", "anim", "Select the destination to save the new Animation Clip.");
                if (!string.IsNullOrEmpty(path))
                {
                    selectedClip = new AnimationClip();
                    AssetDatabase.CreateAsset(selectedClip, path);
                    AssetDatabase.Refresh();
                }
            }
        }

        // Animator Controllerの選択
        animatorController = EditorGUILayout.ObjectField("Animator Controller (Opt.)", animatorController, typeof(AnimatorController), true) as AnimatorController;

        // Animator Controllerが選択されている場合、その中のパラメータを取得し、パラメータの種類を設定
        if (animatorController != null)
        {
            var parameters = animatorController.parameters;
            string[] parameterNames = new string[parameters.Length];
            parameterTypes = new AnimatorControllerParameterType[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterNames[i] = parameters[i].name;
                parameterTypes[i] = parameters[i].type;
            }

            selectedParameterIndex = EditorGUILayout.Popup("Select Parameter", selectedParameterIndex, parameterNames);
            parameterName = parameters[selectedParameterIndex].name;
        }
        else
        {
            parameterName = EditorGUILayout.TextField("Parameter Name", parameterName);
        }

        // パラメータの種類に合わせて入力フィールドを表示
        if (animatorController != null)
        {
            switch (parameterTypes[selectedParameterIndex])
            {
                case AnimatorControllerParameterType.Float:
                    parameterValue = EditorGUILayout.FloatField("Parameter Value", parameterValue);
                    break;
                case AnimatorControllerParameterType.Int:
                    parameterValue = EditorGUILayout.IntField("Parameter Value", Mathf.RoundToInt(parameterValue));
                    break;
                case AnimatorControllerParameterType.Bool:
                    parameterValue = EditorGUILayout.Toggle("Parameter Value", parameterValue == 1) ? 1 : 0;
                    break;
            }
        }
        else
        {
            parameterValue = EditorGUILayout.FloatField("Parameter Value", parameterValue);
        }

        if (GUILayout.Button("Add Keyframe"))
        {
            if (selectedClip == null)
            {
                Debug.LogError("Please select an Animation Clip or create a new one.");
                return;
            }

            if (string.IsNullOrEmpty(parameterName))
            {
                Debug.LogError("Please enter a valid parameter name.");
                return;
            }

            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), parameterName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(selectedClip, binding);

            if (curve == null)
            {
                curve = new AnimationCurve();
                AnimationUtility.SetEditorCurve(selectedClip, binding, curve);
            }
            else
            {
                // 同一parameterNameのキーフレームが存在し、かつparameterValueが異なる場合、上書きするかどうかの確認ダイアログを表示
                if (curve.keys.Length == 1 && curve.keys[0].value != parameterValue)
                {
                    bool overwrite = EditorUtility.DisplayDialog(
                        "Overwrite Keyframe",
                        "A keyframe for parameter '" + parameterName + "' already exists with a different value. Do you want to overwrite it?",
                        "Overwrite",
                        "Cancel");

                    if (!overwrite)
                    {
                        // キャンセルされた場合は追加を中止
                        return;
                    }
                    else
                    {
                        // キーフレームを削除してから新しいキーフレームを追加
                        curve.RemoveKey(0);
                    }
                }
            }

            curve.AddKey(new Keyframe(0, parameterValue));
            AnimationUtility.SetEditorCurve(selectedClip, binding, curve);
            EditorUtility.SetDirty(selectedClip);

            // アセットを保存
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Added keyframe for parameter '" + parameterName + "' with value " + parameterValue + " to the Animation Clip. Asset has been saved.");
        }
    }
}
