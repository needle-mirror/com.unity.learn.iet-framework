using UnityEditor;
using UnityEngine;

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
                    var assetGUIDs = AssetDatabase.FindAssets($"t:{typeof(TutorialProjectSettings).FullName}");
                    if (assetGUIDs.Length == 0)
                        s_Instance = CreateInstance<TutorialProjectSettings>();
                    else
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[0]);

                        if (assetGUIDs.Length > 1)
                            Debug.LogWarningFormat("There is more than one TutorialProjectSetting asset in project.\n" +
                                "Using asset at path: {0}", assetPath);

                        s_Instance = AssetDatabase.LoadAssetAtPath<TutorialProjectSettings>(assetPath);
                    }
                }

                return s_Instance;
            }
        }
        static TutorialProjectSettings s_Instance;

        /// <summary>
        /// Resets the singleton instance.
        /// Use if you create a new project settings instance and want to update Instance to point to it.
        /// </summary>
        public static void ReloadInstance()
        {
            s_Instance = null;
        }

        /// <summary>
        /// The page shown in the welcome dialog when the project is started for the first time.
        /// </summary>
        public TutorialWelcomePage WelcomePage { get => m_WelcomePage; set => m_WelcomePage = value; }
        [Header("Initial Scene and Camera Settings")]
        [SerializeField]
        [Tooltip("If set, this page is shown in the welcome dialog when the project is started for the first time.")]
        TutorialWelcomePage m_WelcomePage = default;

        /// <summary>
        /// Initial scene that is loaded when the project is started for the first time.
        /// </summary>
        // TODO Setter?
        public SceneAsset InitialScene => m_InitialScene;
        [SerializeField]
        [Tooltip("Initial scene that is loaded when the project is started for the first time.")]
        SceneAsset m_InitialScene = null;

        /// <summary>
        /// Initial camera settings when the project is loaded for the first time.
        /// </summary>
        public SceneViewCameraSettings InitialCameraSettings => m_InitialCameraSettings;
        [SerializeField]
        SceneViewCameraSettings m_InitialCameraSettings = new SceneViewCameraSettings();

        /// <summary>
        /// If enabled, the original assets of the project are restored when a tutorial starts.
        /// </summary>
        // TODO setter?
        public bool RestoreDefaultAssetsOnTutorialReload => m_RestoreDefaultAssetsOnTutorialReload;
        [Header("Start-Up Settings")]
        [SerializeField]
        [Tooltip("If enabled, the original assets of the project are restored when a tutorial starts.")]
        bool m_RestoreDefaultAssetsOnTutorialReload = default;

        // TODO 2.0 remove
        [SerializeField]
        [Tooltip("If enabled, disregard startup tutorial and start the first tutorial found in the project.")]
        bool m_UseLegacyStartupBehavior = default;

        /// <summary>
        /// The tutorial to run at startup, from the Welcome page
        /// </summary>
        public Tutorial StartupTutorial
        {
            get
            {
                if (m_UseLegacyStartupBehavior)
                {
                    var guids = AssetDatabase.FindAssets($"t:{typeof(Tutorial).FullName}");
                    if (guids.Length > 0)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        return AssetDatabase.LoadAssetAtPath<Tutorial>(assetPath);
                    }

                    return null;
                }

                return m_StartupTutorial;
            }
            set { m_StartupTutorial = value; }
        }
        [SerializeField]
        [Tooltip("If set, this is the tutorial that can be started from the welcome dialog.")]
        Tutorial m_StartupTutorial = default;

        /// <summary>
        /// TutorialStyles settings for the project. If no settings exist, default settings will be created.
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
            // TODO setter?
        }
        [SerializeField]
        [Tooltip("Style settings for this project.")]
        TutorialStyles m_TutorialStyle;

        internal static readonly string k_DefaultStyleAsset =
            "Packages/com.unity.learn.iet-framework/Editor/UI/Tutorial Styles.asset";
    }
}
