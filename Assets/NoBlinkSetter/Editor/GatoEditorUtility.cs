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

    /// <summary>
    /// 任意のアセットを複製する
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="newAssetName"></param>
    /// <param name="saveFolderPath"></param>
    /// <returns></returns>
    public static T DuplicateAsset<T>(T source, string newAssetPath) where T : UnityEngine.Object
    {
        var sourcePath = AssetDatabase.GetAssetPath(source);
        return DuplicateAsset<T>(sourcePath, newAssetPath);
    }

    public static T DuplicateAsset<T>(string sourcePath, string newAssetPath) where T : UnityEngine.Object
    {
        var newFolderPath = Path.GetDirectoryName(newAssetPath);
        CreateNoExistFolders(newFolderPath);
        var newPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);
        AssetDatabase.CopyAsset(sourcePath, newPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var newAsset = AssetDatabase.LoadAssetAtPath(newPath, typeof(T)) as T;

        return newAsset;
    }
}
