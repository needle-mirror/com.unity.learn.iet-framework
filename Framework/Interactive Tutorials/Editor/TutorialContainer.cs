using System;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// An index for the tutorials in the project.
    /// </summary>
    public class TutorialContainer : ScriptableObject
    {
        public event Action Modified;

        public Texture2D HeaderBackground;

        public LocalizableString Title;

        public LocalizableString ProjectName;

        public LocalizableString Description;

        [Tooltip("Can be used to override the default layout specified by the Tutorial Framework.")]
        public UnityEngine.Object ProjectLayout;

        public Section[] Sections = {};

        public string ProjectLayoutPath =>
            ProjectLayout != null ? AssetDatabase.GetAssetPath(ProjectLayout) : k_DefaultLayoutPath;

        // The default layout used when a project is started for the first time.
        internal static readonly string k_DefaultLayoutPath =
            "Packages/com.unity.learn.iet-framework/Framework/Interactive Tutorials/Layouts/DefaultLayout.wlt";

        [Serializable]
        public class Section
        {
            public int OrderInView;

            public LocalizableString Heading;

            public LocalizableString Text;

            // TODO Rename
            /// <summary>
            /// Used as content type metadata for external references/URLs
            /// </summary>
            [Tooltip("Used as content type metadata for external references/URLs")]
            public string LinkText;

            /// <summary>
            /// The URL of this section.
            /// Setting the URL will take precedence and make the card act as a link card instead of a tutorial card
            /// </summary>
            [Tooltip("Setting the URL will take precedence and make the card act as a link card instead of a tutorial card")]
            public string Url;

            /// <summary>
            /// Use for Unity Connect auto-login, shortened URLs do not work
            /// </summary>
            [Tooltip("Use for Unity Connect auto-login, shortened URLs do not work")]
            public bool AuthorizedUrl;

            public Texture2D Image;

            /// <summary>
            /// The tutorial this container contains
            /// </summary>
            public Tutorial Tutorial = null;

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
            public string TutorialId => Tutorial?.lessonId.AsEmptyIfNull();

            public string SessionStateKey => $"Unity.InteractiveTutorials.lesson{TutorialId}";

            /// <summary>
            /// Starts the tutorial of the section
            /// </summary>
            public void StartTutorial()
            {
                TutorialManager.instance.StartTutorial(Tutorial);
            }

            /// <summary>
            /// Opens the URL Of the section, if any
            /// </summary>
            public void OpenUrl()
            {
                if (string.IsNullOrEmpty(Url))
                    return;

                if (AuthorizedUrl && UnityConnectProxy.loggedIn)
                    UnityConnectProxy.OpenAuthorizedURLInWebBrowser(Url);
                else
                    Application.OpenURL(Url);

                AnalyticsHelper.SendExternalReferenceEvent(Url, Heading.Untranslated, LinkText, Tutorial?.lessonId);
            }

            /// <summary>
            /// Loads the state of a section
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
        /// Raises theModified event
        /// </summary>
        public void RaiseModifiedEvent()
        {
            Modified?.Invoke();
        }
    }
}
