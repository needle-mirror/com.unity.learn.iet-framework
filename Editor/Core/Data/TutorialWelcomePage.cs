using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// A generic event for signaling changes in a tutorial welcome page.
    /// Parameters: sender.
    /// </summary>
    [Serializable]
    public class TutorialWelcomePageEvent : UnityEvent<TutorialWelcomePage>
    {
    }

    /// <summary>
    /// Welcome page/dialog for a project shown using TutorialModalWindow.
    /// </summary>
    /// <remarks>
    /// In addition of window title, header image, title, and description,
    /// a welcome page/dialog contains a fully customizable button row.
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class TutorialWelcomePage : ScriptableObject
    {
        /// <summary>
        /// Data for a customizable button.
        /// </summary>
        [Serializable]
        public class ButtonData
        {
            /// <summary>
            /// Text of the button.
            /// </summary>
            public LocalizableString Text = new();
            /// <summary>
            /// Tooltip of the button.
            /// </summary>
            public LocalizableString Tooltip = new();
            /// <summary>
            /// Callback for the button click.
            /// </summary>
            public UnityEvent OnClick = new();
        }

        /// <summary>
        /// The Video Media settings used by the header if the media type is set to Video
        /// </summary>
        public MediaContent HeaderContent { get => m_HeaderContent; set => m_HeaderContent = value; }
        [Header("Header")]
        [SerializeField]
        private MediaContent m_HeaderContent;

        /// <summary>
        /// Window title of the welcome dialog.
        /// </summary>
        public LocalizableString WindowTitle { get => m_WindowTitle; set => m_WindowTitle = value; }
        [Header("Properties")]
        [SerializeField]
        internal LocalizableString m_WindowTitle;

        /// <summary>
        /// Title of the welcome dialog.
        /// </summary>
        public LocalizableString Title { get => m_Title; set => m_Title = value; }
        [SerializeField]
        internal LocalizableString m_Title;

        /// <summary>
        /// Description of the welcome dialog.
        /// </summary>
        public LocalizableString Description { get => m_Description; set => m_Description = value; }
        [SerializeField, LocalizableTextArea(1, 10)]
        internal LocalizableString m_Description;

        /// <summary>
        /// Does this Welcome Dialog mask the rest of the editor when displayed
        /// </summary>
        public bool MaskEditor { get => m_MaskEditor; set => m_MaskEditor = value; }

        [SerializeField]
        [Header("Settings")]
        [Tooltip("Is the editor masked when the Welcome Dialog is opened")]
        internal bool m_MaskEditor;

        /// <summary>
        /// Buttons specified for the welcome page.
        /// </summary>
        public ButtonData[] Buttons { get => m_Buttons; set => m_Buttons = value; }
        [SerializeField]
        internal ButtonData[] m_Buttons;

        /// <summary>
        /// Raised when any welcome page is modified.
        /// </summary>
        /// <remarks>
        /// Raised before Modified event.
        /// </remarks>
        public static TutorialWelcomePageEvent TutorialWelcomePageModified = new();

        /// <summary>
        /// Raised when any field of the welcome page is modified.
        /// </summary>
        internal TutorialWelcomePageEvent Modified = new();

        /// <summary>
        /// Raises the Modified events for this asset.
        /// </summary>
        public void RaiseModified()
        {
            TutorialWelcomePageModified?.Invoke(this);
            Modified?.Invoke(this);
        }

        private void OnValidate()
        {
            Title = POFileUtils.SanitizeString(Title);
            WindowTitle = POFileUtils.SanitizeString(WindowTitle);
            Description = POFileUtils.SanitizeString(Description);
        }

        /// <summary>
        /// Creates a default Close button.
        /// </summary>
        /// <param name="page">Page for which the buttons is created.</param>
        /// <returns>Data structure for the button.</returns>
        public static ButtonData CreateCloseButton(TutorialWelcomePage page)
        {
            ButtonData data = new() { Text = "Close", OnClick = new UnityEvent() };
            UnityEventTools.AddVoidPersistentListener(data.OnClick, page.CloseCurrentModalDialog);
            data.OnClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            return data;
        }

        // Providing functionality for some default behaviours of the welcome dialog.

        /// <summary>
        /// Closes the an open instance of TutorialModalWindow.
        /// </summary>
        public void CloseCurrentModalDialog()
        {
            TutorialModalWindow window = EditorWindowUtils.FindOpenInstance<TutorialModalWindow>();
            if (window) window.Close();
        }

        /// <summary>
        /// This highlights the Tutorial Window. Just invokes <see cref="TutorialWindow.BringUpAndHighlight"/>.
        /// </summary>
        internal void HighlightTutorialWindow() => TutorialWindow.BringUpAndHighlight();

        /// <summary>
        /// Exits the Editor.
        /// </summary>
        public void ExitEditor()
        {
            EditorApplication.Exit(0);
        }
    }
}
