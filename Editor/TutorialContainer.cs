using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Events;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// A generic event for signaling changes in a tutorial container.
    /// Parameters: sender.
    /// </summary>
    [Serializable]
    public class TutorialContainerEvent : UnityEvent<TutorialContainer>
    {
    }

    /// <summary>
    /// An index for the tutorials in the project.
    /// </summary>
    public class TutorialContainer : ScriptableObject
    {
        /// <summary>
        /// Raised when any TutorialContainer is modified.
        /// </summary>
        /// <remarks>
        /// Raised before Modified event.
        /// </remarks>
        public static TutorialContainerEvent TutorialContainerModified = new TutorialContainerEvent();

        /// <summary>
        /// Raised when any field of this container is modified.
        /// </summary>
        public TutorialContainerEvent Modified;

        /// <summary>
        /// Background texture for the header area that is used to display Title and Subtitle.
        /// </summary>
        [FormerlySerializedAs("HeaderBackground")]
        public Texture2D BackgroundImage;

        /// <summary>
        /// Title shown in the container card and header area.
        /// </summary>
        [Tooltip("Title shown in the container card and header area.")]
        public LocalizableString Title;

        /// <summary>
        /// Subtitle shown in the container card and header area.
        /// </summary>
        [Tooltip("Subtitle shown in the container card and header area.")]
        public LocalizableString Subtitle;

        /// <summary>
        /// Used as the tooltip for the container card.
        /// </summary>
        [Tooltip("Used as the tooltip for the container card.")]
        public LocalizableString Description;

        /// <summary>
        /// Can be used to override or disable (the default behavior) the default project layout specified by the Tutorial Framework.
        /// </summary>
        [Tooltip("Can be used to override or disable (the default behavior) the default project layout specified by the Tutorial Framework.")]
        public UnityEngine.Object ProjectLayout;

        /// <summary>
        /// Sections/cards of this container.
        /// </summary>
        public Section[] Sections = {};

        /// <summary>
        /// Returns the path for the ProjectLayout, relative to the project folder,
        /// or a default tutorial layout path if ProjectLayout not specified.
        /// </summary>
        public string ProjectLayoutPath =>
            ProjectLayout != null ? AssetDatabase.GetAssetPath(ProjectLayout) : k_DefaultLayoutPath;

        // The default layout used when a project is started for the first time, if project layout is used.
        internal static readonly string k_DefaultLayoutPath =
            "Packages/com.unity.learn.iet-framework/Editor/Layouts/DefaultLayout.wlt";

        /// <summary>
        /// A section/card for starting a tutorial or opening a web page.
        /// </summary>
        [Serializable]
        public class Section
        {
            /// <summary>
            /// Order the the view. Use 0, 2, 4, and so on.
            /// </summary>
            public int OrderInView; // used to reorder Sections as it's not currently implement as ReorderableList.

            /// <summary>
            /// Title of the card.
            /// </summary>
            public LocalizableString Heading;

            /// <summary>
            /// Description of the card.
            /// </summary>
            public LocalizableString Text;

            /// <summary>
            /// Used as content type metadata for external references/URLs
            /// </summary>
            [Tooltip("Used as content type metadata for external references/URLs"), FormerlySerializedAs("LinkText")]
            public string Metadata;

            /// <summary>
            /// The URL of this section.
            /// Setting the URL will take precedence and make the card act as a link card instead of a tutorial card
            /// </summary>
            [Tooltip("Setting the URL will take precedence and make the card act as a link card instead of a tutorial card")]
            public string Url;

            /// <summary>
            /// Image for the card.
            /// </summary>
            public Texture2D Image;

            /// <summary>
            /// The tutorial this container contains
            /// </summary>
            public Tutorial Tutorial;

            /// <summary>
            /// Has the tutorial been already completed?
            /// </summary>
            public bool TutorialCompleted { get; set; }

            /// <summary>
            /// Does this represent a tutorial?
            /// </summary>
            public bool IsTutorial => Url.IsNullOrEmpty();

            /// <summary>
            /// The ID of the represented tutorial, if any
            /// </summary>
            public string TutorialId => Tutorial?.LessonId.AsEmptyIfNull();

            internal string SessionStateKey => $"Unity.Tutorials.Core.Editor.lesson{TutorialId}";

            /// <summary>
            /// Starts the tutorial of the section
            /// </summary>
            public void StartTutorial()
            {
                TutorialManager.Instance.StartTutorial(Tutorial);
            }

            /// <summary>
            /// Opens the URL Of the section, if any
            /// </summary>
            public void OpenUrl()
            {
                TutorialEditorUtils.OpenUrl(Url);
                AnalyticsHelper.SendExternalReferenceEvent(Url, Heading.Untranslated, Metadata, Tutorial?.LessonId);
            }

            /// <summary>
            /// Loads the state of the section from SessionState.
            /// </summary>
            /// <returns>returns true if the state was found from EditorPrefs</returns>
            public bool LoadState()
            {
                const string nonexisting = "NONEXISTING";
                var state = SessionState.GetString(SessionStateKey, nonexisting);
                if (state == "")
                {
                    TutorialCompleted = false;
                }
                else if (state == "Finished")
                {
                    TutorialCompleted = true;
                }
                return state != nonexisting;
            }

            /// <summary>
            /// Saves the state of the section from SessionState.
            /// </summary>
            public void SaveState()
            {
                SessionState.SetString(SessionStateKey, TutorialCompleted ? "Finished" : "");
            }
        }

        void OnValidate()
        {
            SortSections();
            for (int i = 0; i < Sections.Length; ++i)
            {
                Sections[i].OrderInView = i * 2;
            }
        }

        void SortSections()
        {
            Array.Sort(Sections, (x, y) => x.OrderInView.CompareTo(y.OrderInView));
        }

        /// <summary>
        /// Loads the tutorial project layout
        /// </summary>
        public void LoadTutorialProjectLayout()
        {
            TutorialManager.LoadWindowLayoutWorkingCopy(ProjectLayoutPath);
        }

        /// <summary>
        /// Raises the Modified events for this asset.
        /// </summary>
        public void RaiseModified()
        {
            TutorialContainerModified?.Invoke(this);
            Modified?.Invoke(this);
        }
    }
}
