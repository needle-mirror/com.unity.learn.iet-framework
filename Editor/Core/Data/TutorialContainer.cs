using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Unity.Tutorials.Editor
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
    /// A tutorial container is a collection of tutorial content, and is used to access the actual tutorials in the project.
    /// </summary>
    /// <remarks>
    /// A tutorial container can be two things:
    /// 1. Tutorial project (null Parent): a root container which is the entry point for tutorial content in the project.
    /// 2. Tutorial category (non-null Parent): a set of tutorials that are a part of some other container
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class TutorialContainer : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Raised when any TutorialContainer is modified.
        /// </summary>
        /// <remarks>
        /// Raised before Modified event.
        /// </remarks>
        public static TutorialContainerEvent TutorialContainerModified = new();

        /// <summary>
        /// Raised when any field of this container is modified.
        /// </summary>
        /// <remarks>
        /// If 'this' container is parented, we consider modifications to 'this' container also to be modifications of the parent.
        /// </remarks>
        public TutorialContainerEvent Modified;

        /// <summary>
        /// By setting another container as a parent, this container becomes a tutorial category in the parent container.
        /// </summary>
        [Header("Structure")]
        [Tooltip("By setting another container as a parent, this container becomes a tutorial category in the parent container.")]
        public TutorialContainer ParentContainer;

        /// <summary>
        /// Determines the position that this container will be shown at in its parent container (if it has one).
        /// </summary>
        [FormerlySerializedAs("OrderInView")]
        [Tooltip("Determines the position that this container will be shown at in its parent container (if it has one).")]
        public int OrderInParent;

        /// <summary>
        /// Sections (tutorial or link card) of this container.
        /// </summary>
        [Tooltip("Cards displayed on this container, either linking to a tutorial or an external URL.")]
        public Section[] Sections = { };

        /// <summary>
        /// Title shown in the card/header.
        /// </summary>
        [Header("Properties")]
        [Tooltip("Title shown in the card/header.")]
        public LocalizableString Title = new();

        /// <summary>
        /// Subtitle shown in the container card and header area.
        /// </summary>
        [Tooltip("Subtitle shown in the card/header.")]
        public LocalizableString Subtitle = new();

        /// <summary>
        /// Background texture for the card/header.
        /// </summary>
        [Tooltip("Background texture for the container's card and header.")]
        [FormerlySerializedAs("HeaderBackground")]
        public Texture2D BackgroundImage;

        /// <summary>
        /// Can be used to override or disable (the default behavior) the default project layout specified by the Tutorial Framework.
        /// </summary>
        [Header("Settings")]
        [Tooltip("Can be used to override or disable (the default behavior) the default project layout specified by the Tutorial Framework.")]
        public Object ProjectLayout;

        /// <summary>
        /// A list of questions that can be applied to that whole container
        /// </summary>
        [Tooltip("FAQ entries shown for this whole container.")]
        [SerializeField]
        public FaqEntry[] FaqEntries = Array.Empty<FaqEntry>();

        /// <summary>
        /// Returns the path for the ProjectLayout, relative to the project folder,
        /// or a default tutorial layout path if ProjectLayout not specified.
        /// </summary>
        public string ProjectLayoutPath =>
            ProjectLayout != null ? AssetDatabase.GetAssetPath(ProjectLayout) : k_DefaultLayoutPath;

        // The default layout used when a project is started for the first time, if project layout is used.
        internal static readonly string k_DefaultLayoutPath =
#if UNITY_2020
            "Packages/com.unity.learn.iet-framework/DefaultAssets/Layouts/DefaultLayout2020.wlt";
#else
            "Packages/com.unity.learn.iet-framework/DefaultAssets/Layouts/DefaultLayout2021.wlt";
#endif

        internal IEnumerable<TutorialContainer> FindSubCategories() =>
            TutorialEditorUtils.FindAssets<TutorialContainer>().Where(c => c.ParentContainer == this);

        /// <summary>
        /// The type of a section, which determines how it appears
        /// within the <see cref="TutorialContainer"/> that hosts it.
        /// </summary>
        public enum SectionType
        {
            /// <summary>A section that will start a <see cref="Tutorial"/> when clicked.</summary>
            Tutorial,
            /// <summary>A section that will open a link in the browser when clicked.</summary>
            ExternalLink,
        }

        /// <summary>
        /// A section/card for starting a Tutorial, or opening a web page.
        /// </summary>
        [Serializable]
        public class Section
        {
            /// <summary>
            /// Title of the section.
            /// </summary>
            [Tooltip("Title shown on the section card.")]
            public LocalizableString Heading;

            /// <summary>
            /// Description of the section.
            /// </summary>
            [Tooltip("Description shown on the section card.")]
            public LocalizableString Text;

            /// <summary>
            /// The type of section.
            /// </summary>
            [Tooltip("Whether this card starts a tutorial or opens an external URL.")]
            public SectionType Type;

            /// <summary>
            /// The URL of this section.
            /// Setting the URL will take precedence and make the card act as a link card instead of a tutorial card
            /// </summary>
            [Tooltip("The URL to go to when the section type is set to be an external link.")]
            public string Url;

            /// <summary>
            /// Used as content type metadata for external URLs.
            /// </summary>
            [Tooltip("Used as content type metadata for external URLs"), FormerlySerializedAs("LinkText")]
            public string Metadata;

            /// <summary>
            /// Image for the card.
            /// </summary>
            [Tooltip("Image displayed on the section card.")]
            public Texture2D Image;

            /// <summary>
            /// The tutorial this container contains
            /// </summary>
            [Tooltip("The Tutorial to start when the section type is set to be Tutorial.")]
            public Tutorial Tutorial;

            /// <summary>
            /// Does this represent a tutorial?
            /// </summary>
            public bool IsTutorial => Type == SectionType.Tutorial;

            // Is this a tutorial section with an actual Tutorial assigned?
            internal bool ContainsTutorial => IsTutorial && Tutorial != null;

            /// <summary>
            /// Is this section set up properly? Does it have all the data needed to fulfill its purpose?
            /// </summary>
            public bool IsConfiguredCorrectly
            {
                get
                {
                    if (IsTutorial)
                    {
                        return Tutorial != null;
                    }
                    return !Url.IsNullOrEmpty();
                }
            }
            /// <summary>
            /// The ID of the represented tutorial, if any
            /// </summary>
            public string TutorialId => Tutorial?.LessonId.AsEmptyIfNull();

            /// <summary>
            /// Opens the URL Of the section, if any
            /// </summary>
            public void OpenUrl()
            {
                // TODO by making a static OpenUrl(string url) utility function we can easily track rich text hyperlink clicks also
                TutorialEditorUtils.OpenUrl(Url);
                AnalyticsHelper.SendExternalReferenceEvent(Url, Heading.Untranslated, Metadata, Tutorial?.LessonId);
            }

            /// <summary>
            /// Loads the state of the section from SessionState.
            /// </summary>
            /// <returns>returns true if the state was found from EditorPrefs</returns>
            internal bool LoadState()
            {
                return IsTutorial
                    && (IsConfiguredCorrectly && Tutorial.LoadLocalCompletionState());
            }
        }

        /// <summary>
        /// UnityEngine.ISerializationCallbackReceiver override, do not call.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// UnityEngine.ISerializationCallbackReceiver override, do not call.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // This is for supporting assets made with < 6.0 versions of IET.
            // v5 sections had no Type field (a section acted as a link whenever Url was set),
            // so v5 assets deserialize with the default Type == Tutorial. Promote them to
            // ExternalLink when the Url is the only configuration present. The Tutorial == null
            // guard keeps intact v6 sections deliberately set to Tutorial type that still carry
            // a stale Url (the Inspector hides but doesn't clear it).
            foreach (Section section in Sections)
            {
                if (section.Type == SectionType.Tutorial
                    && section.Tutorial == null
                    && section.Url.IsNotNullOrEmpty())
                {
                    section.Type = SectionType.ExternalLink;
                }
            }
        }

        private void OnValidate()
        {
            Title = POFileUtils.SanitizeString(Title);
            Subtitle = POFileUtils.SanitizeString(Subtitle);
            foreach (Section section in Sections)
            {
                section.Heading = POFileUtils.SanitizeString(section.Heading);
                section.Text = POFileUtils.SanitizeString(section.Text);
            }
        }

        /// <summary>
        /// Loads the tutorial project layout
        /// </summary>
        public void LoadTutorialProjectLayout()
        {
            TutorialModel.LoadWindowLayoutWorkingCopy(ProjectLayoutPath);
        }

        /// <summary>
        /// Raises the Modified events for this asset.
        /// </summary>
        public void RaiseModified()
        {
            TutorialContainerModified?.Invoke(this);
            Modified?.Invoke(this);
        }

        /// <summary>
        /// This highlights the Tutorial Window. Just invokes <see cref="TutorialWindow.BringUpAndHighlight"/>.
        /// </summary>
        public void HighlightTutorialWindow() => TutorialWindow.BringUpAndHighlight();

        /// <summary>
        /// Return a number between 0.0 and 1.0 that is the number of tutorials in that containers that are completed.
        /// Tutorials that don't have tracking enable always count as completed
        /// </summary>
        /// <returns>The percentage of tutorials completed, as a number between 0.0 and 1.0</returns>
        public float GetCompletionRate()
        {
            int tutorialsCount = 0;
            int completedTutorialsCount = 0;
            foreach (Section section in Sections)
            {
                if (section.ContainsTutorial && section.Tutorial.ProgressTrackingEnabled)
                {
                    tutorialsCount += 1;
                    if (section.Tutorial.CompletedByUser)
                    {
                        completedTutorialsCount += 1;
                    }
                }
            }

            //no tracked tutorial mean the container is "completed"
            return tutorialsCount == 0 ? 1.0f : completedTutorialsCount / (float)tutorialsCount;
        }

        internal void DecideSectionTypeV6()
        {
            foreach (Section section in Sections)
            {
                section.Type = section.Url.IsNotNullOrEmpty() ? SectionType.ExternalLink : SectionType.Tutorial;
            }
            EditorUtility.SetDirty(this); // Ensures the subsequent AssetDatabase.SaveAssets() persists the new Types
        }
    }
}
