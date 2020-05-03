using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class GatoEditorUtility
{
    public static void NonIndentHelpBox(string message, MessageType messageType)
    {
        var currentIndentLevel = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        EditorGUILayout.HelpBox(message, messageType);
        EditorGUI.indentLevel = currentIndentLevel;
    }

    public static void NonIndentButton(string text, Action action)
    {
        var currentIndentLevel = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        if (GUILayout.Button(text))
        {
            action.Invoke();
        }
        EditorGUI.indentLevel = currentIndentLevel;
    }
}
