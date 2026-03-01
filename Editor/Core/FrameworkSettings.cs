using System;
using UnityEditor;
using UnityEditor.SettingsManagement;

namespace Unity.Tutorials.Editor
{
    using static Localization;

    internal static class FrameworkSettings
    {
        internal const string k_PackageName = "com.unity.learn.iet-framework";
        private static readonly float k_OriginalLabelWidth = EditorGUIUtility.labelWidth;
        private static readonly string k_Category = Tr(LocalizationKeys.k_SettingsCategory);

        private static Settings s_Instance;
        internal static Settings Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new Settings(k_PackageName);
                }
                return s_Instance;
            }
        }

        [SettingsProviderGroup]
        private static SettingsProvider[] CreateSettingsProviders()
        {
            /* We need to add the name of the each setting on our own as keywords as we don't use the default
            UserSettingsProvider because it doesn't support localization. Add also "iet" shortcut, allowing "iet some setting" searches. */
            string[] keywords = {
                "iet",
                MaskingManager.MaskingEnabled.Name,
                SerializedTypeDrawer.ShowSimplifiedTypeNames.Name,
                TutorialFrameworkModel.s_ShowTutorialsWindowClosedDialog.Name,
            };
            SettingsProvider userSettings = new("Preferences/" + k_Category, SettingsScope.User, keywords)
            {
                guiHandler = searchContext => DrawSettings(searchContext, DrawUserSettings)
            };

            string[] projectSettingsKeywords = {
                TutorialFrameworkModel.s_DisplayWelcomeDialogOnStartup.Name,
                TutorialFrameworkModel.s_DataMigrationToV6.Name
            };

            SettingsProvider projectSettings = new("Project/" + k_Category, SettingsScope.Project, projectSettingsKeywords)
            {
                guiHandler = searchContext => DrawSettings(searchContext, DrawProjectSettings)
            };
            return new[] { userSettings, projectSettings };
        }

        private static void SetLabelWidth(float width) { EditorGUIUtility.labelWidth = width; }
        private static void RestoreOriginalLabelWidth() { EditorGUIUtility.labelWidth = k_OriginalLabelWidth; }

        private static bool DrawToggle(BaseSetting<bool> value, string searchContext)
        {
            return SettingsGUILayout.SettingsToggle(value.GetGuiContent(), value, searchContext);
        }

        private static void DrawUserSettings(string searchContext)
        {
            MaskingManager.MaskingEnabled.value = DrawToggle(MaskingManager.MaskingEnabled, searchContext);
            SerializedTypeDrawer.ShowSimplifiedTypeNames.value = DrawToggle(SerializedTypeDrawer.ShowSimplifiedTypeNames, searchContext);
            TutorialFrameworkModel.s_ShowTutorialsWindowClosedDialog.value = DrawToggle(TutorialFrameworkModel.s_ShowTutorialsWindowClosedDialog, searchContext);
        }

        private static void DrawProjectSettings(string searchContext)
        {
            TutorialFrameworkModel.s_DisplayWelcomeDialogOnStartup.value = DrawToggle(TutorialFrameworkModel.s_DisplayWelcomeDialogOnStartup, searchContext);

            // TODO: Add a button? Or menu item (currently there is one, but it's behind Authoring >)
            //TutorialFrameworkModel.s_DataMigrationToV6.value = DrawToggle(TutorialFrameworkModel.s_DataMigrationToV6, searchContext);
        }

        private static void DrawSettings(string searchContext, Action<string> drawIndentGroupContent)
        {
            SetLabelWidth(300);
            // Space and indentation to mimic the default settings GUI layout as closely as possible.
            EditorGUILayout.Space();

            using (new SettingsGUILayout.IndentedGroup())
            {
                drawIndentGroupContent.Invoke(searchContext);
            }
            RestoreOriginalLabelWidth();
        }
    }
}
