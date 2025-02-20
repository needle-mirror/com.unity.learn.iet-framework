using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Controls start-up and initial settings and behavior of the tutorial project.
    /// </summary>
    public class TutorialProjectSettings : ScriptableObject
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static TutorialProjectSettings Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var settings = TutorialEditorUtils.FindAssets<TutorialProjectSettings>();
                    if (!settings.Any())
                    {
                        s_Instance = CreateInstance<TutorialProjectSettings>();
                    }
                    else
                    {
                        var setting = settings.First();
                        if (settings.Count() > 1)
                        {
                            Debug.LogWarningFormat(
                                "There is more than one TutorialProjectSetting asset in project. Using asset at path '{0}'. " +
                                "Set TutorialProjectSettings.Instance programmatically if you wish to use another asset.",
                                AssetDatabase.GetAssetPath(setting)
                            );
                        }

                        s_Instance = setting;
                    }
                }

                return s_Instance;
            }
            set
            {
                s_Instance = value;
            }
        }
        static TutorialProjectSettings s_Instance;

        /// <summary>
        /// The page shown in the welcome dialog when the project is started for the first time.
        /// </summary>
        public TutorialWelcomePage WelcomePage { get => m_WelcomePage; set => m_WelcomePage = value; }
        [Header("Start-Up Settings")]
        [SerializeField, Tooltip("If set, this page is shown in the welcome dialog when the project is started for the first time.")]
        TutorialWelcomePage m_WelcomePage;

        /// <summary>
        /// Initial scene that is loaded when the project is started for the first time.
        /// </summary>
        public SceneAsset InitialScene { get => m_InitialScene; set => m_InitialScene = value; }
        [SerializeField]
        [Tooltip("Initial scene that is loaded when the project is started for the first time.")]
        SceneAsset m_InitialScene;

        /// <summary>
        /// Initial camera settings when the project is loaded for the first time.
        /// </summary>
        public SceneViewCameraSettings InitialCameraSettings { get => m_InitialCameraSettings; set => m_InitialCameraSettings = value; }
        [SerializeField]
        SceneViewCameraSettings m_InitialCameraSettings;

        /// <summary>
        /// Style settings asset for the project. If no asset exists, a default asset will be used.
        /// </summary>
        public TutorialStyles TutorialStyle
        {
            get
            {
                if (!m_TutorialStyle)
                {
                    m_TutorialStyle = AssetDatabase.LoadAssetAtPath<TutorialStyles>(k_DefaultStyleAsset);
                }
                return m_TutorialStyle;
            }
            set { m_TutorialStyle = value; }
        }
        [Header("Other")]
        [SerializeField, Tooltip("Style settings for this project.")]
        TutorialStyles m_TutorialStyle;

        internal static readonly string k_DefaultStyleAsset = "Packages/com.unity.learn.iet-framework/Editor/DefaultAssets/Tutorial Styles.asset";

        /// <summary>
        /// If enabled, the original assets of the project are restored when a tutorial starts.
        /// </summary>
        [System.Obsolete("Will be removed in v4. Use RestoreAssetsBackupOnTutorialReload instead")]
        public bool RestoreDefaultAssetsOnTutorialReload { get => RestoreAssetsBackupOnTutorialReload; set => RestoreAssetsBackupOnTutorialReload = value; }

        /// <summary>
        /// If enabled, the original assets of the project are restored when a tutorial starts.
        /// </summary>
        public bool RestoreAssetsBackupOnTutorialReload { get => m_RestoreAssetsBackupOnTutorialReload; set => m_RestoreAssetsBackupOnTutorialReload = value; }
        [SerializeField, Tooltip("If enabled, the original assets of the project are restored when a tutorial starts."), FormerlySerializedAs("m_RestoreDefaultAssetsOnTutorialReload")]
        bool m_RestoreAssetsBackupOnTutorialReload;

        /// <summary>
        /// The address the link at the bottom of the Tutorial Window used to report problem with the Tutorials
        /// </summary>
        public string ReportUrl { get => m_ReportUrl; set => m_ReportUrl = value; }
        [SerializeField, Tooltip("The address called when clicking on the report link at the bottom of the TutorialWindow ")]
        string m_ReportUrl = "https://discussions.unity.com/c/learn/50";

        /// <summary>
        /// If this is set to true data about the current tutorial will be appended as an URL variable to the report url
        /// The target website can then process that URL to handle it.
        /// </summary>
        public bool AppendDataToReport { get => m_AppendDataToReport; set => m_AppendDataToReport = value; }
        [SerializeField, Tooltip("If true, this append a json object with ContainerTitle, TutorialTitle, PageTile to the link as a TutorialData url variable")]
        bool m_AppendDataToReport = false;

        /// <summary>
        /// Call this in the beginning of tutorial asset editors.
        /// </summary>
        internal static void DrawDefaultAssetRestoreWarning()
        {
            // Check the effective TutorialProjectSettings instance, not 'this' instance.
            if (Instance.RestoreAssetsBackupOnTutorialReload && !ProjectMode.IsAuthoringMode())
            {
                EditorGUILayout.HelpBox(Localization.Tr(LocalizationKeys.k_SettingsHelpboxRestoreDefaultAssetsWarning), MessageType.Warning);
                EditorGUILayout.Space(10);
            }
        }

        /// <summary>
        /// VisualElement version of DrawDefaultAssetRestoreWarning, call at the beginning of tutorial assets editors
        /// </summary>
        /// <param name="root"></param>
        internal static void DrawDefaultAssetRestoreWarningElement(VisualElement root)
        {
            if (Instance.RestoreAssetsBackupOnTutorialReload && !ProjectMode.IsAuthoringMode())
            {
                var helpBox = new HelpBox();
                helpBox.text = Localization.Tr(LocalizationKeys.k_SettingsHelpboxRestoreDefaultAssetsWarning);
                helpBox.messageType = HelpBoxMessageType.Warning;
                helpBox.style.marginBottom = 10;

                root.Add(helpBox);
            }
        }
    }
}
