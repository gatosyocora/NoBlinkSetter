using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class GatoEditorUtility
{
    private const char BSLASH = '\\';

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

    public static bool CreateNoExistFolders(string path)
    {
        string directoryPath;
        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            directoryPath = path;
        }
        else
        {
            directoryPath = Path.GetDirectoryName(path);
        }

        if (!Directory.Exists(directoryPath))
        {
            var directories = directoryPath.Split(BSLASH);

            directoryPath = "Assets";
            for (int i = 1; i < directories.Length; i++)
            {
                if (!Directory.Exists(directoryPath +BSLASH+ directories[i]))
                {
                    AssetDatabase.CreateFolder(directoryPath, directories[i]);
                }

                directoryPath += BSLASH + directories[i];
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        return false;
    }
}
