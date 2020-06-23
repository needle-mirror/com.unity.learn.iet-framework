using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    [InitializeOnLoad]
    public static class UserStartupCode
    {
        internal static void RunStartupCode()
        {
            var projectSettings = TutorialProjectSettings.instance;
            if (projectSettings.initialScene != null)
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(projectSettings.initialScene));

            TutorialManager.WriteAssetsToTutorialDefaultsFolder();

            // Ensure Editor is in predictable state
            EditorPrefs.SetString("ComponentSearchString", string.Empty);
            Tools.current = Tool.Move;


            // Replace LastProjectPaths in window layouts used in tutorials so that e.g.
            // pre-saved Project window states work correctly.
            var readme = TutorialWindow.FindReadme();
            if (readme)
            {
                TutorialManager.PrepareWindowLayout(readme.ProjectLayoutPath);
            }

            AssetDatabase.FindAssets($"t:{typeof(Tutorial).FullName}")
                .Select(guid =>
                    AssetDatabase.LoadAssetAtPath<Tutorial>(AssetDatabase.GUIDToAssetPath(guid)).windowLayoutPath
                )
                .Where(StringExt.IsNotNullOrEmpty)
                .Distinct()
                .ToList()
                .ForEach(layoutPath =>
                {
                    TutorialManager.PrepareWindowLayout(layoutPath);
                });


            if (readme)
                readme.LoadTutorialProjectLayout();

            // NOTE camera settings can be applied successfully only after potential layout changes
            if (projectSettings.InitialCameraSettings != null && projectSettings.InitialCameraSettings.enabled)
                projectSettings.InitialCameraSettings.Apply();

            if (projectSettings.WelcomePage)
                TutorialModalWindow.TryToShow(projectSettings.WelcomePage, () => {});

            var wnd = TutorialManager.GetTutorialWindow();
            if (wnd)
                wnd.showStartHereMarker = true;
        }

        internal static readonly string initFileMarkerPath = "InitCodeMarker";
        // Folder so that user can easily create this from the Editor's Project view.
        internal static readonly string dontRunInitCodeMarker = "Assets/DontRunInitCodeMarker";

        static UserStartupCode()
        {
            // Language change trigger an assembly reload.
            if (LoadPreviousEditorLanguage() != LocalizationDatabaseProxy.currentEditorLanguage)
            {
                SaveCurrentEditorLanguage();

                var title = Localization.Tr("Editor Language Change Detected");
                var msg = Localization.Tr("It's recommended to restart the Editor for the language change to be applied fully.");
                var ok = Localization.Tr("Restart");
                var cancel = Localization.Tr("Continue without restarting");
                if (EditorUtility.DisplayDialog(title, msg, ok, cancel))
                    RestartEditor();
            }

            if (IsDontRunInitCodeMarkerSet())
                return;
            if (IsInitialized())
                return;

            EditorApplication.update += InitRunStartupCode;
        }

        static void InitRunStartupCode()
        {
            SetInitialized();
            EditorApplication.update -= InitRunStartupCode;
            RunStartupCode();
        }

        public static bool IsInitialized()
        {
            return File.Exists(initFileMarkerPath);
        }

        static bool IsDontRunInitCodeMarkerSet()
        {
            return Directory.Exists(dontRunInitCodeMarker);
        }

        public static void SetInitialized()
        {
            File.CreateText(initFileMarkerPath).Close();
        }

        static SystemLanguage LoadPreviousEditorLanguage() =>
            (SystemLanguage)EditorPrefs.GetInt("EditorLanguage", (int)SystemLanguage.English);

        static void SaveCurrentEditorLanguage() =>
            EditorPrefs.SetInt("EditorLanguage", (int)LocalizationDatabaseProxy.currentEditorLanguage);

        // TODO as a menu item temporarily for dev/testing purposes, will be removed
        [MenuItem(TutorialWindowMenuItem.MenuPath + "Restart Editor")]
        static void RestartEditor()
        {
            // In older versions, calling EditorApplication.OpenProject() while having unsaved modifications
            // can cause us to get stuck in a dialog loop. This seems to be fixed in 2020.1 (and newer?).
            // As a workaround, ask for saving before starting to restart the Editor for real. However,
            // we get the dialog twice and it can cause issues if user chooses first "Don't save" and then tries
            // to "Cancel" in the second dialog.
#if !UNITY_2020_1_OR_NEWER
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
#endif
            {
                EditorApplication.OpenProject(Application.dataPath + "/..");
            }
        }
    }
}
