using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tutorials.Editor
{
    internal class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T s_Instance;
        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                    CreateAndLoad();
                return s_Instance;
            }
        }

        protected ScriptableObjectSingleton()
        {
            if (s_Instance != null)
            {
                Debug.LogError("Singleton already exists!");
            }
            else
            {
                s_Instance = this as T;
                Assert.IsFalse(s_Instance == null);
            }
        }

        private static void CreateAndLoad()
        {
            Assert.IsTrue(s_Instance == null);

            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                InternalEditorUtility.LoadSerializedFileAndForget(filePath);
            }

            if (s_Instance == null)
            {
                ScriptableObjectSingleton<T> inst = CreateInstance<T>() as ScriptableObjectSingleton<T>;
                Assert.IsFalse(inst == null);
                inst.hideFlags = HideFlags.HideAndDontSave;
                inst.Save();
            }

            Assert.IsFalse(s_Instance == null);
        }

        protected void Save()
        {
            if (s_Instance == null)
            {
                Debug.LogError("Cannot save singleton, no instance!");
                return;
            }

            string locationFilePath = GetFilePath();
            string directoryName = Path.GetDirectoryName(locationFilePath);
            if (directoryName == null)
            {
                Debug.LogError("Could not save cache because target directory for the save file is empty");
                return;
            }
            Directory.CreateDirectory(directoryName);
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { s_Instance }, locationFilePath, true);
        }

        [CanBeNull]
        private static string GetFilePath()
        {
            LocationAttribute attr = typeof(T).GetCustomAttributes(true)
                                .OfType<LocationAttribute>()
                                .FirstOrDefault();
            return attr?.FilePath;
        }
    }
}
