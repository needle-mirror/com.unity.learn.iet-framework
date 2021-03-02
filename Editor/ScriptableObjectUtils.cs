using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Utility function for creating ScriptableObjects.
    /// </summary>
    public class ScriptableObjectUtils
    {
        /// <summary>
        /// This makes it easy to create, name and place unique new ScriptableObject asset files.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static T CreateAsset<T>(string fileName, string path = "") where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            SaveAsset<T>(asset, fileName, path);
            return asset;
        }

        /// <summary>
        /// Creates and saves an asset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <param name="fileName"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string SaveAsset<T>(T asset, string fileName, string path) where T : ScriptableObject
        {
            if (path == "")
            {
                path = TutorialEditorUtils.GetActiveFolderPath();
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "New " + typeof(T).ToString();
            }

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(string.Format("{0}/{1}.asset", path, fileName));

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            return assetPathAndName;
        }

        /// <summary>
        /// Saves an asset if it doesn't exists already
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <param name="fileName"></param>
        /// <returns>The newly created asset or its existing instance (and its path)</returns>
        // TODO Unused, remove?
        public static (T, string) GetOrSaveUniqueAsset<T>(T asset, string fileName) where T : ScriptableObject
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = TutorialEditorUtils.GetActiveFolderPath();
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "New " + typeof(T).ToString();
            }

            string assetPathAndName = string.Format("{0}/{1}.asset", path, fileName);

            T existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPathAndName);
            if (existingAsset) { return (existingAsset, assetPathAndName); }
            assetPathAndName = SaveAsset<T>(asset, fileName, path);
            return (asset, assetPathAndName);
        }
    }
}
