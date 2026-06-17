using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using Unity.Tutorials.Editor.Paragraphs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// The window used to display tutorials and their content
    /// </summary>
#pragma warning disable 0618 //suppress obsolete warning for us, keep them active for users
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public sealed class TutorialWindow : EditorWindow
#pragma warning restore 0618
    {
        private static readonly Vector2 k_MinWindowSize = new(400, 600f);
        private static readonly Vector2 k_MaxWindowSize = new(600, 1200f);

        /// <summary>
        /// Are we currently (during this frame) transitioning from one tutorial to another?
        /// </summary>
        /// <remarks>
        /// This transition typically happens when using a Switch Tutorial button on a tutorial page.
        /// </remarks>
        public bool IsTransitioningBetweenTutorials => Model.Tutorial.IsTransitioningBetweenTutorials;

        /// <summary>
        /// The currently active tutorial, if any.
        /// </summary>
        public Tutorial CurrentTutorial => Model.Tutorial.CurrentTutorial;

        /// <summary>
        /// The category of which tutorial are being displayed. Null if the project and its packages contains no categories, meaning all tutorials are being displayed.
        /// </summary>
        public TutorialContainer CurrentContainer => Model.TableOfContent.CurrentContainer;

        /// <summary>
        /// Are we currently loading a window layout.
        /// </summary>
        /// <remarks>
        /// A window layout load typically happens when the project is started for the first time
        /// and the project's startup settings specify a window layout for the project, or when entering
        /// or exiting a tutorial with a window layout specified.
        /// </remarks>
        internal static bool s_IsLoadingLayout = false;

        /// <summary>
        /// Should the UI be rebuilt even if the layout has been reloaded?
        /// Usually needed when opening a closed tutorial window from the menu item.
        /// </summary>
        internal static bool s_RebuildFrontendEvenIfIsLoadingLayout;

        internal EventManager EventManager = new();

        /// <summary>
        /// The active instance of this window
        /// </summary>
        public static TutorialWindow Instance
        {
            get
            {
                if (_instance == null) _instance = GetOrCreateWindowNextToInspector();
                return _instance;
            }
            set => _instance = value;
        }

        private static TutorialWindow _instance;

        private static TutorialWindow FindInstance() => Resources.FindObjectsOfTypeAll<TutorialWindow>().FirstOrDefault();

        /// <summary>
        /// Checks if the instance of the window is available, without creating one. Made mostly for tests.
        /// </summary>
        internal static bool IsAvailable => _instance != null;

        /// <summary>
        /// Returns true or false depending if localization is still initializing or not
        /// </summary>
        internal bool IsWaitingForLocalizationToBeReady
        {
            get => _isWaitingForLocalizationToBeReady;
            private set => _isWaitingForLocalizationToBeReady = value;
        }

        /// <summary>
        /// True if the basic frontend data of the Window is set, meaning that specific Views can be loaded
        /// </summary>
        internal bool FrontendIsReadyToBeInitialized
        {
            get => _frontendIsReadyToBeInitialized;
            private set => _frontendIsReadyToBeInitialized = value;
        }

        internal string CurrentView
        {
            get => m_Model.CurrentView;
            private set
            {
                m_Model.PreviousView = CurrentView;
                m_Model.CurrentView = value;
            }
        }

        internal TutorialFrameworkModel Model => m_Model;

        private TutorialFrameworkController m_Controller;
        private TutorialFrameworkModel m_Model;
        private HashSet<IModel> m_Models;
        private HashSet<Controller> m_Controllers;
        private TableOfContentModel m_TableOfContentModel;
        private TutorialModel m_TutorialModel;

        private VisualElement m_Root;
        private HashSet<View> m_Views;

        internal TableOfContentView TableOfContentView
        {
            get => _tableOfContentView;
            private set => _tableOfContentView = value;
        }

        internal TutorialView TutorialView
        {
            get => _tutorialView;
            private set => _tutorialView = value;
        }

        private StyleSheet m_LastCommonStyleSheet; // Dark/Light theme

        /// <summary>
        /// Holds all the Frontend setup methods of the available tabs
        /// </summary>
        private Dictionary<string, Action> m_ViewFrontendSetupMethods;

        private bool _isWaitingForLocalizationToBeReady = true;
        private bool _frontendIsReadyToBeInitialized;
        private TableOfContentView _tableOfContentView;
        private TutorialView _tutorialView;

        // TODO: Remove this next major upgrade (i.e. 7.0.0)
        /// <summary>
        /// Shows Tutorials window using the currently specified behaviour.
        /// </summary>
        /// <remarks>
        /// Different behaviors:
        /// 1. If a single root tutorial container (TutorialContainer.ParentContainer is null) that has Project Layout specified exists,
        ///    the window is loaded and shown using the specified project window layout (old behaviour).
        ///    If the project layout does not contain Tutorials window, the window is shown an as a free-floating window.
        /// 2. If no root tutorial containers exist, or a root container's Project Layout is not specified, the window is shown
        ///     by anchoring and docking it next to the Inspector (new behaviour). If the Inspector is not available,
        ///     the window is shown an as a free-floating window.
        /// 3. If there is more than one root tutorial container with different Project Layout setting in the project,
        ///    one asset is chosen randomly to specify the behavior.
        /// 4. If Tutorials window is already created, it is simply brought to the foreground and focused.
        /// </remarks>
        /// <returns>The the created, or already existing, window instance.</returns>
        public static TutorialWindow ShowWindow()
        {
            return ShowWindow(true);
        }

        /// <summary>
        /// Main logic for ShowWindow()
        /// </summary>
        /// <param name="shouldRefreshLayout">Whether or not we should reset the layout to the basic tutorial layout.
        /// Should be false when loading a tutorial step and true when first initializing the tutorial window.</param>
        /// <returns>The TutorialWindow, created or found open.</returns>
        public static TutorialWindow ShowWindow(bool shouldRefreshLayout)
        {
            List<TutorialContainer> rootContainers = TutorialEditorUtils.FindAssets<TutorialContainer>()
                                                    .Where(category => category.ParentContainer is null).ToList();

            TutorialContainer defaultCategory = rootContainers.FirstOrDefault();
            Object projectLayout = defaultCategory?.ProjectLayout;
            if (rootContainers.Any(category => category.ProjectLayout != projectLayout))
            {
                Debug.LogWarningFormat(
                    "There is more than one TutorialContainers asset with different Project Layout setting in the project. " +
                    "Using asset at path {0} for the window behavior settings.",
                    AssetDatabase.GetAssetPath(defaultCategory)
                );
            }

            TutorialWindow window = null;
            if (!rootContainers.Any() || defaultCategory!.ProjectLayout == null)
            {
                window = GetOrCreateWindowNextToInspector();
            }
            else if (defaultCategory.ProjectLayout != null)
            {
                window = GetOrCreateWindowAndLoadLayout(defaultCategory, shouldRefreshLayout);
            }

            return window;
        }

        /// <summary>
        /// Creates the window if it does not exist, anchoring it as a tab next to the first found Inspector.
        /// If the window exists already, it's simply brought to the foreground and focused without any other actions.
        /// If any Inspector is not visible currently, Tutorials window is will be shown as a free-floating window.
        /// </summary>
        /// <remarks>
        /// This is the new and preferred way to show the Tutorials window.
        /// </remarks>
        /// <returns>The created, or already existing, window instance.</returns>
        internal static TutorialWindow GetOrCreateWindowNextToInspector()
        {
            EditorWindow inspector = Resources.FindObjectsOfTypeAll<EditorWindow>()
                                           .FirstOrDefault(w => w.GetType().Name == "InspectorWindow");

            Type windowToAnchorTo = inspector != null ? inspector.GetType() : null;
            bool wasWindowPresent = _instance is not null;
            _instance = GetOrCreateWindow(windowToAnchorTo); // create & anchor or simply focus
            float inspectorWidth = inspector != null ? inspector.rootVisualElement.layout.width : 0f;

            float minimumInspectorWidth = 450f;
            // If Inspector not visible/opened, Tutorials window will be created as a free-floating window
            if (!wasWindowPresent && inspector && inspectorWidth > minimumInspectorWidth)
            {
                // Tutorial Window is docked side-by-side to the Inspector only if Inspector is wider than a certain amount,
                // to avoid the effect of a narrow tutorial window. If not, it's docked *with* it, and brought to the front.
                inspector.DockWindow(_instance, EditorWindowUtils.DockPosition.Right);
            }

            return _instance;
        }

        /// <summary>
        /// Brings up the Tutorials window, and highlights it (mask).
        /// Exposed as a public method in both <see cref="TutorialContainer"/> and
        /// <see cref="TutorialWelcomePage"/> so that it can be invoked by UnityEvents.
        /// </summary>
        internal static void BringUpAndHighlight()
        {
            TutorialWindow tutorialWindow = Instance;
            ShowWindow(false); // This call seems redundant,
            // but it ensure the window is brought to the front if it's in the background
            TutorialModel Model = tutorialWindow.Model.Tutorial;

            MaskingManager.Unmask();
            UnmaskedView.MaskData unmaskedViews = UnmaskedView.GetViewsAndRects(new[] { UnmaskedView.CreateInstanceForEditorWindow(typeof(TutorialWindow))}, out bool _);
            UnmaskedView.MaskData highlightedViews;

            if (unmaskedViews.Count > 0) // Unmasked views should be highlighted
            {
                highlightedViews = (UnmaskedView.MaskData)unmaskedViews.Clone();
            }
            else
            {
                highlightedViews = new UnmaskedView.MaskData();
            }

            unmaskedViews.AddTooltipViews();
            // Also ensure the Media Popout window (used to enlarge video and image) is unmasked
            unmaskedViews.AddPopoutWindow();

            MaskingManager.Mask(
                unmaskedViews,
                Model.Styles == null ? Color.magenta * new Color(1f, 1f, 1f, 0.8f) : Model.Styles.MaskingColor,
                highlightedViews,
                Model.Styles == null ? Color.cyan * new Color(1f, 1f, 1f, 0.8f) : Model.Styles.HighlightColor,
                Model.Styles == null ? new Color(1, 1, 1, 0.5f) : Model.Styles.BlockedInteractionColor,
                Model.Styles == null ? 3f : Model.Styles.HighlightThickness
            );
        }

        /// <summary>
        /// Creates the window if it does not exist, and positions it using a window layout
        /// specified either by the TutorialContainer.ProjectLayout or Tutorial Framework's default layout.
        /// If the window exists already, it's simply brought to the foreground and focused without any other actions.
        /// If the project layout does not contain Tutorials window, it will be shown as a free-floating window.
        /// </summary>
        /// <remarks>
        /// This is the old way to show the Tutorials window and should be preferred only in situations where
        /// a special window layout is preferred when starting a tutorial project for the first time.
        /// </remarks>
        /// <param name="container">The container used for the project layout setting.</param>
        /// <returns></returns>
        internal static TutorialWindow GetOrCreateWindowAndLoadLayout(TutorialContainer container, bool shouldRefreshLayout)
        {
            if (container != null && shouldRefreshLayout)
            {
                container.LoadTutorialProjectLayout();
            }

            // Try to find the window in the newly-loaded layout.
            _instance = EditorWindowUtils.FindOpenInstance<TutorialWindow>();

            // It might be null at this point but it's ok because next time the Instance property is accessed,
            // it will create a new window.

            return _instance;
        }

        /// <summary>
        /// Creates a window and positions it as a tab of another window, if wanted.
        /// If the window exists already, it's brought to the foreground and focused.
        /// </summary>
        /// <param name="windowToAnchorTo"></param>
        /// <returns></returns>
        internal static TutorialWindow GetOrCreateWindow(Type windowToAnchorTo = null)
        {
            _instance = GetWindow<TutorialWindow>(Localization.Tr(LocalizationKeys.k_WindowTitle), windowToAnchorTo);
            _instance.minSize = k_MinWindowSize; // NOTE minSize has no effect on docked windows on 2021.2 and newer
            _instance.maxSize = k_MaxWindowSize;
            _instance.titleContent.image = UIUtils.LoadIcon("TutorialsWindowIcon.png", true, true);

            return _instance;
        }

        private void OnEnable()
        {
            FrontendIsReadyToBeInitialized = false;
            IsWaitingForLocalizationToBeReady = true;
            SetupBackend();
            SetupFrontend();
            IsWaitingForLocalizationToBeReady = false;
        }

        private void OnDisable()
        {
            if (TutorialFrameworkModel.s_ShowTutorialsWindowClosedDialog
            && !m_Model.Tutorial.IsLoadingLayout
            && !m_Model.Tutorial.PlayModeChanging)
            {
                // Delay call prevents us getting the dialog upon assembly reload.
                EditorApplication.delayCall += delegate
                {
                    TutorialFrameworkModel.s_ShowTutorialsWindowClosedDialog.SetValue(false);

                    string m_PromptOk = Localization.Tr(LocalizationKeys.k_WindowClosedDialogButtonOk);
                    string m_TabClosedDialogTitle = Localization.Tr(LocalizationKeys.k_WindowClosedDialogTitle);
                    string m_MenuPathGuide = Localization.Tr(MenuItems.Menu) + " > " + Localization.Tr(MenuItems.ShowTutorials);
                    string m_TabClosedDialogText = string.Format(Localization.Tr(LocalizationKeys.k_WindowClosedDialogMessage), m_MenuPathGuide);

                    EditorUtility.DisplayDialog(m_TabClosedDialogTitle, m_TabClosedDialogText, m_PromptOk);
                };
            }
            TeardownBackend();
        }

        private void SetupBackend()
        {
            _instance = FindInstance();

            TableOfContentView = new TableOfContentView();
            TutorialView = new TutorialView();
            RegisterView(TableOfContentView, SetupTableOfContentView);
            RegisterView(TutorialView, SetupTutorialView);

            m_Model = new TutorialFrameworkModel();
            m_TableOfContentModel = new TableOfContentModel();
            m_TutorialModel = new TutorialModel();
            m_Models = new HashSet<IModel> { m_Model, m_TableOfContentModel, m_TutorialModel };

            LoadModelsState();

            m_Controller = new TutorialFrameworkController(m_Model);
            TableOfContentController tableOfContentController = new();
            TutorialController tutorialController = new();
            m_Controllers = new HashSet<Controller> { m_Controller, tableOfContentController, tutorialController };

            SubscribeEvents();
            m_Model.IsOpen = true;
        }

        internal void RegisterView(View view, Action frontendSetupMethod)
        {
            if (m_Views == null)
            {
                m_Views = new HashSet<View>();
            }
            if (m_ViewFrontendSetupMethods == null)
            {
                m_ViewFrontendSetupMethods = new Dictionary<string, Action>();
            }
            m_Views.Add(view);
            m_ViewFrontendSetupMethods.Add(view.Name, frontendSetupMethod);
        }

        internal void UnregisterView(View view)
        {
            if (m_Views == null)
            {
                return;
            }
            m_Views.Remove(view);
            m_ViewFrontendSetupMethods.Remove(view.Name);
        }


        private void SetupFrontend()
        {
            //The root is not the root of the window, as we want an always present bottom bar to report problem at the
            //bottom
            m_Root = new VisualElement();
            m_Root.style.flexGrow = 1.0f;
            rootVisualElement.Add(m_Root);

            titleContent = new GUIContent(Localization.Tr(LocalizationKeys.k_WindowTitle));
            minSize = k_MinWindowSize;
            maxSize = k_MaxWindowSize;
            FrontendIsReadyToBeInitialized = true;

            RebuildFrontend();
        }

        private void RebuildFrontend()
        {
            if (s_IsLoadingLayout && !s_RebuildFrontendEvenIfIsLoadingLayout)
            {
                if (!s_RebuildFrontendEvenIfIsLoadingLayout)
                {
                    return;
                }
                s_RebuildFrontendEvenIfIsLoadingLayout = false;
            }

            if (string.IsNullOrEmpty(CurrentView))
            {
                LoadView(TableOfContentView.k_Name);
            }
            else
            {
                LoadView(CurrentView);
            }
        }

        internal void LoadView(string viewName)
        {
            if (!CanSwitchToView(viewName)) { return; }

            CurrentView = viewName;
            m_Root.Clear();

            if (TutorialModel.s_AuthoringModeEnabled)
            {
                m_Root.Add(DrawAuthoringToolbar());
            }

            VisualTreeAsset windowContent = UIUtils.LoadUXML(viewName);
            windowContent.CloneTree(m_Root);

            //preserve the base style, remove all styles defined in UXML and apply new skin
            for (int i = m_Root.styleSheets.count - 1; i > 0; i--)
            {
                m_Root.styleSheets.Remove(m_Root.styleSheets[i]);
            }

            UIUtils.LoadCommonStyleSheet(m_Root);
            UpdateWindowSkin();

            m_ViewFrontendSetupMethods[viewName].Invoke();
        }

        private void UpdateWindowSkin()
        {
            UIUtils.RemoveStyleSheet(m_LastCommonStyleSheet, m_Root);
            UIUtils.LoadEditorThemeStyleSheet(out m_LastCommonStyleSheet, m_Root);

            if (TutorialProjectSettings.Instance != null && TutorialProjectSettings.Instance.TutorialStyle != null)
            {
                TutorialProjectSettings.Instance.TutorialStyle.AddCustomStyleSheet(m_Root);
            }
        }

        private void SubscribeEvents()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private void UnsubscribeEvents()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            if (m_Views != null)
            {
                foreach (View view in m_Views)
                {
                    view.UnsubscribeEvents();
                }
            }
        }

        private void OnDestroy()
        {
            if (!m_Model.IsOpen)
            {
                if (!s_IsLoadingLayout)
                {
                    CurrentView = string.Empty; // We ensure that something is loaded when the window is reopened
                }
                SaveModelsState();
            }
            _instance = null;
        }

        private void TeardownBackend()
        {
            m_Model.IsOpen = false;
            if (m_Models != null)
            {
                foreach (IModel model in m_Models)
                {
                    model.OnStop();
                }
            }

            if (!m_Model.DomainReloadOccured) // If it occurred we don't want to save the models as their "correct"state is already being managed in the Assembly reload-related callbacks
            {
                SaveModelsState();
            }

            if (m_Controllers != null)
            {
                foreach (Controller controller in m_Controllers)
                {
                    controller.RemoveListeners();
                }
            }
            UnsubscribeEvents();
        }

        /// <summary>
        /// Restore window state after assembly reload.
        /// </summary>
        private void LoadModelsState()
        {
            foreach (IModel model in m_Models)
            {
                model.RestoreState(WindowCache.Instance);
            }

            foreach (IModel model in m_Models)
            {
                model.OnStart();
            }
        }

        /// <summary>
        /// Save state before assembly reload.
        /// </summary>
        internal void SaveModelsState()
        {
            foreach (IModel model in m_Models)
            {
                model.SaveState(WindowCache.Instance);
            }
            WindowCache.Instance.Serialize();
        }

        internal void OnBeforeAssemblyReload()
        {
            m_Model.DomainReloadOccured = true;
            SaveModelsState();
        }

        internal void OnAfterAssemblyReload()
        {
            LoadModelsState();
            if (Model.DomainReloadOccured)
            {
                Broadcast(new DomainReloadOccurredEvent());
            }
        }

        private bool CanSwitchToView(string viewName)
        {
            if (m_Model.DomainReloadOccured
            && viewName != TutorialView.Name) //TutorialView triggers domain reload frequently and its initialization flow is strictly managed by the controller, so we don't want to accidentally re-reload it
            {
                m_Model.DomainReloadOccured = false;
                return true;
            }
            return (string.IsNullOrEmpty(CurrentView)
                && !string.IsNullOrEmpty(viewName))
                || viewName != CurrentView;
        }

        private void SetupTableOfContentView() { TableOfContentView.Initialize(m_Root); }
        private void SetupTutorialView() { TutorialView.Initialize(m_Root); }

        /// <summary>
        /// Notifies an event to the component's of the app
        /// </summary>
        /// <param name="evt"></param>
        internal void Broadcast(AppEvent evt)
        {
            EventManager.Broadcast(evt);
        }

        /// <summary>same as <see cref="Broadcast"/>, but static</summary>
        internal static void BroadcastEvent(AppEvent evt)
        {
            _instance?.Broadcast(evt);
        }

        internal IModel GetModel(Type modelType)
        {
            return m_Models.Where(m => m.GetType() == modelType).FirstOrDefault();
        }

        private VisualElement DrawAuthoringToolbar()
        {
            VisualTreeAsset toolbarUxml = UIUtils.LoadUXML("AuthoringToolbar");
            VisualElement toolbar = toolbarUxml.CloneTree();

            toolbar.style.flexShrink = 0; // Prevents vertical squashing at low resolutions and/or when there are many Containers

            // Select Category button
            ToolbarButton selectContainerBtn = toolbar.Q<ToolbarButton>("SelectContainerButton");
            selectContainerBtn.clicked += () => Selection.activeObject = Model.TableOfContent.CurrentContainer;
            selectContainerBtn.tooltip = Localization.Tr(LocalizationKeys.k_AuthoringButtonSelectCategory);
            selectContainerBtn.enabledSelf = Model.TableOfContent.CurrentContainer != null;

            // Select Tutorial button
            ToolbarButton selectTutorialBtn = toolbar.Q<ToolbarButton>("SelectTutorialButton");
            selectTutorialBtn.clicked += () => Selection.activeObject = Model.Tutorial.CurrentTutorial;
            selectTutorialBtn.tooltip = Localization.Tr(LocalizationKeys.k_AuthoringButtonSelectTutorial);
            selectTutorialBtn.enabledSelf = Model.Tutorial.CurrentTutorial != null;

            // Select Tutorial Page button
            ToolbarButton selectTutorialPageBtn = toolbar.Q<ToolbarButton>("SelectTutorialPageButton");
            selectTutorialPageBtn.clicked += () => Selection.activeObject = Model.Tutorial.CurrentTutorial?.CurrentPage;
            selectTutorialPageBtn.tooltip = Localization.Tr(LocalizationKeys.k_AuthoringButtonSelectPage);
            selectTutorialPageBtn.enabledSelf = Model.Tutorial.CurrentTutorial != null;

            // Skip to End button
            ToolbarButton skipToEndBtn = toolbar.Q<ToolbarButton>("SkipToEndButton");
            skipToEndBtn.clicked += () =>
            {
                Model.Tutorial.CurrentTutorial.SkipToLastPage();
                Model.Tutorial.CurrentTutorial.TryGoToNextPage(); // Needed to trigger completion event
            };
            skipToEndBtn.tooltip = Localization.Tr(LocalizationKeys.k_AuthoringButtonSkipToEnd);
            skipToEndBtn.enabledSelf = Model.Tutorial.CurrentTutorial != null;

            // Autocomplete button
            ToolbarButton autocompleteBtn = toolbar.Q<ToolbarButton>("AutocompleteButton");
            autocompleteBtn.clicked += () =>
            {
                IEnumerable<ParagraphBase> paragraphsToComplete =
                    Model.Tutorial.CurrentTutorial.CurrentPage.Paragraphs.Where(p => !p.IsCompleted());
                foreach (ParagraphBase instructiveParagraph in paragraphsToComplete)
                {
                    // TODO: UPDATE
                    // foreach (var criterion in instructiveParagraph.Criteria)
                    // {
                    //     criterion.Criterion.AutoComplete();
                    //     criterion.Criterion.UpdateCompletion();
                    // }
                }

                if (!Model.Tutorial.CurrentTutorial.CurrentPage.AutoAdvanceOnComplete)
                {
                    Model.Tutorial.CurrentTutorial.TryGoToNextPage();
                }
            };
            autocompleteBtn.tooltip = Localization.Tr(LocalizationKeys.k_AuthoringButtonAutocompletePage);
            autocompleteBtn.enabledSelf = Model.Tutorial.CurrentTutorial != null;

            // Masking button
            ToolbarToggle maskPreviewToggle = toolbar.Q<ToolbarToggle>("MaskingToggle");
            maskPreviewToggle.RegisterValueChangedCallback(evt =>
            {
                Model.Tutorial.MaskingEnabled = evt.newValue;
                TutorialView.ApplyMaskingSettings(true);
            });
            maskPreviewToggle.value = Model.Tutorial.MaskingEnabled;
            maskPreviewToggle.tooltip = Localization.Tr(LocalizationKeys.k_IconPreviewMaskingTooltip);

            // Run Startup code button
            ToolbarButton runStartupCodeBtn = toolbar.Q<ToolbarButton>("RunStartupCodeButton");
            runStartupCodeBtn.clicked += () =>
            {
                Broadcast(new TutorialQuitEvent());
                UserStartupCode.RunStartupCode(TutorialProjectSettings.Instance);
            };
            runStartupCodeBtn.tooltip = Localization.Tr(LocalizationKeys.k_ButtonRunStartupCode);

            return toolbar;
        }

        /// <summary>
        /// Stops and restars an editor coroutine
        /// </summary>
        /// <param name="routine"></param>
        /// <param name="method"></param>
        internal void RestartEditorCoroutine(ref EditorCoroutine routine, IEnumerator method)
        {
            StopAndNullifyEditorCoroutine(ref routine);
            routine = EditorCoroutineUtility.StartCoroutine(method, this);
        }

        internal void StopAndNullifyEditorCoroutine(ref EditorCoroutine routine)
        {
            if (routine != null)
            {
                EditorCoroutineUtility.StopCoroutine(routine);
                routine = null;
            }
        }

        internal const float k_AuthoringButtonWidth = 30f;
        internal static bool Button(string iconName, string tooltip) =>
            GUILayout.Button(IconContent(iconName, tooltip), EditorStyles.toolbarButton, GUILayout.Width(k_AuthoringButtonWidth));

        internal static GUIContent IconContent(string iconName, string tooltip)
        {
            //you can find more suitable icons name in: https://github.com/halak/unity-editor-icons
            return EditorGUIUtility.IconContent(iconName, "|" + tooltip); // "|" needed for text to appear as tooltip
        }

        /// <summary>
        /// Marks all known tutorials as uncompleted
        /// </summary>
        internal void MarkAllTutorialsUncompleted()
        {
            IEnumerable<Tutorial> allTutorials = TutorialEditorUtils.FindAssets<Tutorial>()
                                                  .Where(t => t.ProgressTrackingEnabled);

            foreach (Tutorial tutorial in allTutorials)
            {
                tutorial.CompletedByUser = false;
            }

            Broadcast(new TutorialsCompletionStatusUpdatedEvent());
        }

        /// <summary>
        /// Starts a tutorial as if it was clicked in the Table of content.
        /// </summary>
        /// <param name="tutorial">The tutorial to start</param>
        public static void StartTutorial(Tutorial tutorial)
        {
            if (!_instance)
            {
                GetOrCreateWindowNextToInspector();
                EditorCoroutineUtility.StartCoroutineOwnerless(StartTutorialWhenFrontendIsReady(tutorial));
                return;
            }
            _instance.Broadcast(new TutorialStartRequestedEvent(tutorial, null));
        }

        private static IEnumerator StartTutorialWhenFrontendIsReady(Tutorial tutorial)
        {
            while (!_instance.Model.IsOpen || !_instance.FrontendIsReadyToBeInitialized)
            {
                yield return null;
            }
            _instance.Broadcast(new TutorialStartRequestedEvent(tutorial, null));
        }

        /// <summary>
        /// Exits the current tutorial
        /// </summary>
        public static void ExitTutorial()
        {
            if (!_instance)
            {
                return;
            }
            _instance.Broadcast(new TutorialQuitEvent());
        }

        /// <summary>
        /// Clear localization table cache
        /// </summary>
        public static void ClearLocalizationCache()
        {
            LocalizationDatabaseProxy.ClearLocalizationCache();
        }
    }
}
