using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Runs IET project initialization logic.
    /// </summary>
    [InitializeOnLoad]
    public static class UserStartupCode
    {
        private const string k_DefaultsFolder = "Tutorial Defaults";
        private const string k_EditorLanguageInitializedState = "EditorLanguageInitialized";
        private const string k_EditorLanguagePreference = "EditorLanguage";

        private static bool DisplayWelcomeDialogOnStartup
        {
            get => TutorialFrameworkModel.s_DisplayWelcomeDialogOnStartup;
            set => TutorialFrameworkModel.s_DisplayWelcomeDialogOnStartup.SetValue(value, true);
        }

        private static bool DataMigrationV6
        {
            get => TutorialFrameworkModel.s_DataMigrationToV6;
            set => TutorialFrameworkModel.s_DataMigrationToV6.SetValue(value, true);
        }

        private static bool IsLanguageInitialized() => SessionState.GetBool(k_EditorLanguageInitializedState, false);
        private static void SetLanguageInitialized() => SessionState.SetBool(k_EditorLanguageInitializedState, true);
        private static SystemLanguage LoadPreviousEditorLanguage() => (SystemLanguage)EditorPrefs.GetInt(k_EditorLanguagePreference, (int)SystemLanguage.English);
        private static void SaveCurrentEditorLanguage() => EditorPrefs.SetInt(k_EditorLanguagePreference, (int)LocalizationDatabaseProxy.currentEditorLanguage);

        static UserStartupCode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
            || TutorialWindow.s_IsLoadingLayout)
            {
                return;
            }

            // Language change triggers an assembly reload.
            if (LoadPreviousEditorLanguage() != LocalizationDatabaseProxy.currentEditorLanguage)
            {
                SaveCurrentEditorLanguage();
                // There are several smaller and bigger localization issues with if we don't restart
                // the Editor so let's query the user to do so.
                string title = Localization.Tr(LocalizationKeys.k_TOCLabelTitle);
                string message = Localization.Tr(LocalizationKeys.k_LanguageDialogMessage);
                string ok = Localization.Tr(LocalizationKeys.k_LanguageDialogButtonOk);
                string cancel = Localization.Tr(LocalizationKeys.k_LanguageDialogButtonCancel);
                if (EditorUtility.DisplayDialog(title, message, ok, cancel))
                {
                    RestartEditor();
                }
            }

            EditorApplication.update += InitRunStartupCode;
        }

        internal static void RunStartupCode(TutorialProjectSettings projectSettings)
        {
            if (projectSettings.InitialScene != null)
            {
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(projectSettings.InitialScene));
            }

            BackupProjectAssets();

            // Ensure Editor is in predictable state
            EditorPrefs.SetString("ComponentSearchString", string.Empty);
            Tools.current = Tool.Move;

            if (TutorialEditorUtils.FindAssets<TutorialContainer>().Any())
            {
                if (TutorialWindow.Instance != null) TutorialWindow.Instance.Close();
                TutorialWindow.ShowWindow(true);
            }

            // NOTE camera settings can be applied successfully only after potential layout changes
            if (projectSettings.InitialCameraSettings is { Enabled: true })
            {
                projectSettings.InitialCameraSettings.Apply();
            }

            if (projectSettings.WelcomePage)
            {
                TutorialModalWindow.Show(projectSettings.WelcomePage);
            }
        }

        private static void InitRunStartupCode()
        {
            if (LocalizationDatabaseProxy.enableEditorLocalization && !IsLanguageInitialized())
            {
                /* Need to Request a script reload in order overcome Editor Localization issues
                with static initialization when opening the project for the first time. */
                SetLanguageInitialized();
                EditorUtility.RequestScriptReload();
                return;
            }

            /* Prepare the layout always. For example, the user might have moved the project around,
            so we need to ensure the file paths in the layouts are correct. */
            TutorialController.PrepareWindowLayouts();
            EditorApplication.update -= InitRunStartupCode;

            if (DataMigrationV6 &&
                TutorialEditorUtils.CheckIfV6UpgradeRequired())
            {
                DataMigrationV6 = false; // Will prevent the popup to show each time the project is started
                TutorialEditorUtils.StartV6Upgrade();
            }

            if (!DisplayWelcomeDialogOnStartup) return;

            // TODO: Find a better solution for the below?
            // We turn the option off automatically only for a tutorial user.
            // When authoring we don't want this to be turned off continuously, because often the author wants to ship the tutorial with the option on.
            // (especially critical for when authoring templates)
#if !TUTORIAL_AUTHORING
            DisplayWelcomeDialogOnStartup = false;
#endif

            RunStartupCode(TutorialProjectSettings.Instance);
        }

        /// <summary>
        /// Restart the Editor.
        /// </summary>
        internal static void RestartEditor()
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
                EditorApplication.OpenProject(".");
            }
        }

        internal static void BackupProjectAssets()
        {
            if (!TutorialProjectSettings.Instance.RestoreAssetsBackupOnTutorialReload)
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Defaults cannot be written during play mode");
                return;
            }

            string defaultsPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, k_DefaultsFolder);
            DirectoryInfo defaultsDirectory = new(defaultsPath);
            if (defaultsDirectory.Exists)
            {
                foreach (FileInfo file in defaultsDirectory.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo directory in defaultsDirectory.GetDirectories())
                {
                    directory.Delete(true);
                }
            }
            DirectoryCopy(Application.dataPath, defaultsPath);
        }

        internal static void DirectoryCopy(string sourceDirectory, string destinationDirectory, HashSet<string> dirtyMetaFiles = default)
        {
            DirectoryInfo sourceDir = new(sourceDirectory);
            if (!sourceDir.Exists)
            {
                return;
            }

            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            foreach (FileInfo file in sourceDir.GetFiles())
            {
                string tempPath = Path.Combine(destinationDirectory, file.Name);
                if (dirtyMetaFiles != null && string.Equals(Path.GetExtension(tempPath), ".meta", StringComparison.OrdinalIgnoreCase))
                {
                    if (!File.Exists(tempPath)
                    || !File.ReadAllBytes(tempPath).SequenceEqual(File.ReadAllBytes(file.FullName)))
                    {
                        dirtyMetaFiles.Add(tempPath);
                    }
                }
                file.CopyTo(tempPath, true);
            }

            foreach (DirectoryInfo subdir in sourceDir.GetDirectories())
            {
                string tempPath = Path.Combine(destinationDirectory, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, dirtyMetaFiles);
            }
        }
    }
}
