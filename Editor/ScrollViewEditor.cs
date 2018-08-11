/*
 * Unity Module By Garson(https://github.com/garsonlab)
 * -------------------------------------------------------------------
 * FileName: ScrollViewEditor
 * Date    : 2018/08/11
 * Version : v1.0
 * Describe: 
 */
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(ScrollView), true)]
[CanEditMultipleObjects]
public class ScrollViewEditor : Editor
{
    SerializedProperty m_ViewPort;
    SerializedProperty m_Content;
    SerializedProperty m_Prefab;

    SerializedProperty m_MotionType;
    SerializedProperty m_MovementType;
    SerializedProperty m_Elasticity;
    SerializedProperty m_Inertia;
    SerializedProperty m_DecelerationRate;
    SerializedProperty m_ScrollSensitivity;

    SerializedProperty m_Margin;
    SerializedProperty m_ItemSize;
    SerializedProperty m_Space;
    SerializedProperty m_FixedCount;
    SerializedProperty m_NumItems;
    SerializedProperty m_Loop;
    SerializedProperty m_AutoAttach;
    SerializedProperty m_AttachSnap;
    SerializedProperty m_ScaleCurve;
    SerializedProperty m_ScaleAnimation;

    SerializedProperty m_HorizontalScrollbar;
    SerializedProperty m_VerticalScrollbar;
    SerializedProperty m_HorizontalScrollbarVisibility;
    SerializedProperty m_VerticalScrollbarVisibility;
    SerializedProperty m_HorizontalScrollbarSpacing;
    SerializedProperty m_VerticalScrollbarSpacing;
    SerializedProperty m_OnValueChanged;
    SerializedProperty m_OnRenderItem;
    AnimBool m_ShowElasticity;
    AnimBool m_ShowDecelerationRate;
    AnimBool m_ShowAutoAttach;
    AnimBool m_ShowScaleCurve;
    bool m_ViewportIsNotChild, m_HScrollbarIsNotChild, m_VScrollbarIsNotChild;
    static string s_HError = "For this visibility mode, the Viewport property and the Horizontal Scrollbar property both needs to be set to a Rect Transform that is a child to the Scroll View.";
    static string s_VError = "For this visibility mode, the Viewport property and the Vertical Scrollbar property both needs to be set to a Rect Transform that is a child to the Scroll View.";

    bool m_ScrollSetting;
    bool m_ScrollbarSetting;
    bool m_EventSetting;

    static string s_ScrollSettingLabel = "Scroll Setting";
    static string s_ScrollbarSettingLabel = "Scrollbar Setting";
    static string s_EventSettingLabel = "Event Setting";
    static string s_WelcomInfo = "Welcom to use  Infinite and Loop List";
    static string s_ViewportNull = "Infinite and Loop List Error:\n  Viewport property is Null";
    static string s_ContentNull = "Infinite and Loop List Error:\n  Content property is Null";
    static string s_PrefabtNull = "Infinite and Loop List Warning:\n  Prefab property is Null";

    protected virtual void OnEnable()
    {
        m_Content = serializedObject.FindProperty("m_Content");
        m_ViewPort = serializedObject.FindProperty("m_ViewPort");
        m_Prefab = serializedObject.FindProperty("m_Prefab");
        m_MotionType = serializedObject.FindProperty("m_MotionType");
        
        m_MovementType = serializedObject.FindProperty("m_MovementType");
        m_Elasticity = serializedObject.FindProperty("m_Elasticity");
        m_Inertia = serializedObject.FindProperty("m_Inertia");
        m_DecelerationRate = serializedObject.FindProperty("m_DecelerationRate");
        m_ScrollSensitivity = serializedObject.FindProperty("m_ScrollSensitivity");

        m_Margin = serializedObject.FindProperty("m_Margin");
        m_FixedCount = serializedObject.FindProperty("m_FixedCount");
        m_ItemSize = serializedObject.FindProperty("m_ItemSize");
        m_Space = serializedObject.FindProperty("m_Space");
        m_NumItems = serializedObject.FindProperty("m_NumItems");
        m_Loop = serializedObject.FindProperty("m_Loop");
        m_AutoAttach = serializedObject.FindProperty("m_AutoAttach");
        m_AttachSnap = serializedObject.FindProperty("m_AttachSnap");
        m_ScaleCurve = serializedObject.FindProperty("m_ScaleCurve");
        m_ScaleAnimation = serializedObject.FindProperty("m_ScaleAnimation");

        m_HorizontalScrollbar = serializedObject.FindProperty("m_HorizontalScrollbar");
        m_VerticalScrollbar = serializedObject.FindProperty("m_VerticalScrollbar");
        m_HorizontalScrollbarVisibility = serializedObject.FindProperty("m_HorizontalScrollbarVisibility");
        m_VerticalScrollbarVisibility = serializedObject.FindProperty("m_VerticalScrollbarVisibility");
        m_HorizontalScrollbarSpacing = serializedObject.FindProperty("m_HorizontalScrollbarSpacing");
        m_VerticalScrollbarSpacing = serializedObject.FindProperty("m_VerticalScrollbarSpacing");
        m_OnValueChanged = serializedObject.FindProperty("m_OnValueChanged");
        m_OnRenderItem = serializedObject.FindProperty("m_OnRenderItem");

        m_ShowElasticity = new AnimBool(Repaint);
        m_ShowDecelerationRate = new AnimBool(Repaint);
        m_ShowAutoAttach = new AnimBool(Repaint);
        m_ShowScaleCurve = new AnimBool(Repaint);
        SetAnimBools(true);

        m_ScrollSetting = true;
        m_ScrollbarSetting = true;
        m_EventSetting = true;
    }

    protected virtual void OnDisable()
    {
        m_ShowElasticity.valueChanged.RemoveListener(Repaint);
        m_ShowDecelerationRate.valueChanged.RemoveListener(Repaint);
        m_ShowAutoAttach.valueChanged.RemoveListener(Repaint);
        m_ShowScaleCurve.valueChanged.RemoveListener(Repaint);
    }

    void SetAnimBools(bool instant)
    {
        SetAnimBool(m_ShowElasticity, !m_MovementType.hasMultipleDifferentValues && m_MovementType.enumValueIndex == (int)ScrollView.MovementType.Elastic, instant);
        SetAnimBool(m_ShowDecelerationRate, !m_Inertia.hasMultipleDifferentValues && m_Inertia.boolValue == true, instant);

        SetAnimBool(m_ShowAutoAttach, m_AutoAttach.boolValue, instant);
        SetAnimBool(m_ShowScaleCurve, m_ScaleCurve.boolValue, instant);
    }

    void SetAnimBool(AnimBool a, bool value, bool instant)
    {
        if (instant)
            a.value = value;
        else
            a.target = value;
    }

    void CalculateCachedValues()
    {
        m_ViewportIsNotChild = false;
        m_HScrollbarIsNotChild = false;
        m_VScrollbarIsNotChild = false;
        if (targets.Length == 1)
        {
            Transform transform = ((ScrollView)target).transform;
            if (m_ViewPort.objectReferenceValue == null || ((RectTransform)m_ViewPort.objectReferenceValue).transform.parent != transform)
                m_ViewportIsNotChild = true;
            if (m_HorizontalScrollbar.objectReferenceValue == null || ((Scrollbar)m_HorizontalScrollbar.objectReferenceValue).transform.parent != transform)
                m_HScrollbarIsNotChild = true;
            if (m_VerticalScrollbar.objectReferenceValue == null || ((Scrollbar)m_VerticalScrollbar.objectReferenceValue).transform.parent != transform)
                m_VScrollbarIsNotChild = true;
        }
    }

    public override void OnInspectorGUI()
    {
        SetAnimBools(false);

        serializedObject.Update();
        // Once we have a reliable way to know if the object changed, only re-cache in that case.
        CalculateCachedValues();

        if(m_ViewPort.objectReferenceValue == null )
            EditorGUILayout.HelpBox(s_ViewportNull, MessageType.Error);
        else if (m_Content.objectReferenceValue == null)
            EditorGUILayout.HelpBox(s_ContentNull, MessageType.Error);
        else if (m_Prefab.objectReferenceValue == null)
            EditorGUILayout.HelpBox(s_PrefabtNull, MessageType.Warning);
        else
            EditorGUILayout.HelpBox(s_WelcomInfo, MessageType.Info);


        EditorGUILayout.PropertyField(m_ViewPort);
        EditorGUILayout.PropertyField(m_Content);
        EditorGUILayout.PropertyField(m_Prefab);
        EditorGUILayout.Space();

        #region ScrollSetting
        m_ScrollSetting = EditorGUILayout.Foldout(m_ScrollSetting, s_ScrollSettingLabel);
        if (m_ScrollSetting)
        {
            EditorGUILayout.PropertyField(m_MotionType);
            EditorGUILayout.PropertyField(m_MovementType);
            if (EditorGUILayout.BeginFadeGroup(m_ShowElasticity.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Elasticity);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.PropertyField(m_Inertia);
            if (EditorGUILayout.BeginFadeGroup(m_ShowDecelerationRate.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_DecelerationRate);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.PropertyField(m_ScrollSensitivity);
        }
        #endregion

        EditorGUILayout.Space();
        
        EditorGUILayout.PropertyField(m_Margin, true);
        EditorGUILayout.PropertyField(m_ItemSize, true);
        EditorGUILayout.PropertyField(m_Space, true);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(m_FixedCount);
        EditorGUILayout.PropertyField(m_NumItems);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(m_Loop);
        EditorGUILayout.PropertyField(m_AutoAttach);
        if (EditorGUILayout.BeginFadeGroup(m_ShowAutoAttach.faded))
        {
            EditorGUILayout.PropertyField(m_AttachSnap, true);
        }
        EditorGUILayout.EndFadeGroup();


        EditorGUILayout.PropertyField(m_ScaleCurve);
        if (EditorGUILayout.BeginFadeGroup(m_ShowScaleCurve.faded))
        {
            EditorGUILayout.PropertyField(m_ScaleAnimation, true);
        }
        EditorGUILayout.EndFadeGroup();

        #region ScrollBar
        EditorGUILayout.Space();
        m_ScrollbarSetting = EditorGUILayout.Foldout(m_ScrollbarSetting, s_ScrollbarSettingLabel);
        if (m_ScrollbarSetting)
        {
            EditorGUILayout.PropertyField(m_HorizontalScrollbar);
            if (m_HorizontalScrollbar.objectReferenceValue && !m_HorizontalScrollbar.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_HorizontalScrollbarVisibility, new GUIContent("Visibility"));

                if ((ScrollView.ScrollbarVisibility) m_HorizontalScrollbarVisibility.enumValueIndex ==
                    ScrollView.ScrollbarVisibility.AutoHideAndExpandViewport
                    && !m_HorizontalScrollbarVisibility.hasMultipleDifferentValues)
                {
                    if (m_ViewportIsNotChild || m_HScrollbarIsNotChild)
                        EditorGUILayout.HelpBox(s_HError, MessageType.Error);
                    EditorGUILayout.PropertyField(m_HorizontalScrollbarSpacing, new GUIContent("Spacing"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(m_VerticalScrollbar);
            if (m_VerticalScrollbar.objectReferenceValue && !m_VerticalScrollbar.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_VerticalScrollbarVisibility, new GUIContent("Visibility"));

                if ((ScrollView.ScrollbarVisibility) m_VerticalScrollbarVisibility.enumValueIndex ==
                    ScrollView.ScrollbarVisibility.AutoHideAndExpandViewport
                    && !m_VerticalScrollbarVisibility.hasMultipleDifferentValues)
                {
                    if (m_ViewportIsNotChild || m_VScrollbarIsNotChild)
                        EditorGUILayout.HelpBox(s_VError, MessageType.Error);
                    EditorGUILayout.PropertyField(m_VerticalScrollbarSpacing, new GUIContent("Spacing"));
                }

                EditorGUI.indentLevel--;
            }
        }

        #endregion
        
        #region Event
        EditorGUILayout.Space();
        m_EventSetting = EditorGUILayout.Foldout(m_EventSetting, s_EventSettingLabel);
        if (m_EventSetting)
        {
            EditorGUILayout.PropertyField(m_OnValueChanged, true);
            EditorGUILayout.PropertyField(m_OnRenderItem, true);
        }

        #endregion
        
        serializedObject.ApplyModifiedProperties();
    }
}
