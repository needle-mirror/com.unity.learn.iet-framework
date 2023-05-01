using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;

namespace Unity.Tutorials.Core.Editor
{
    enum TutorialConclusion
    {
        Completed,
        Quit,
        Reloaded
    }

    class TutorialAnalyticsEventData
    {
        public string tutorialName;
        public string version;
        public TutorialConclusion conclusion;
        public string lessonID;

        public TutorialAnalyticsEventData(string tutorialName, string version, TutorialConclusion conclusion, string lessonID)
        {
            this.tutorialName = tutorialName;
            this.version = version;
            this.conclusion = conclusion;
            this.lessonID = lessonID;
        }
    }

    enum TutorialPageConclusion
    {
        Completed,
        Reviewed
    }

    class TutorialPageAnalyticsEventData
    {
        public string tutorialName;
        public int pageIndex;
        public string guid;
        public TutorialPageConclusion conclusion;

        public TutorialPageAnalyticsEventData(string tutorialName, int pageIndex, string guid, TutorialPageConclusion conclusion)
        {
            this.tutorialName = tutorialName;
            this.pageIndex = pageIndex;
            this.guid = guid;
            this.conclusion = conclusion;
        }
    }

    enum TutorialParagraphConclusion
    {
        Completed,
        Regressed
    }

    class TutorialParagraphAnalyticsEventData
    {
        public string tutorialName;
        public int pageIndex;
        public int paragraphIndex;
        public TutorialParagraphConclusion conclusion;

        public TutorialParagraphAnalyticsEventData(string tutorialName, int pageIndex, int paragraphIndex, TutorialParagraphConclusion conclusion)
        {
            this.tutorialName = tutorialName;
            this.pageIndex = pageIndex;
            this.paragraphIndex = paragraphIndex;
            this.conclusion = conclusion;
        }
    }

    class AnalyticsHelper : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        Tutorial currentTutorial;

        [SerializeField]
        TutorialPage currentPage;

        [SerializeField]
        int currentPageIndex;

        [SerializeField]
        TutorialPage lastPage;

        [SerializeField]
        int lastPageIndex;

        [SerializeField]
        int currentParagraphIndex;

        DateTime currentTutorialStartTime;

        DateTime currentPageStartTime;

        DateTime lastPageStartTime;

        DateTime currentParagraphStartTime;

        [SerializeField]
        long currentTutorialStartTicks;

        [SerializeField]
        long currentPageStartTicks;

        [SerializeField]
        long lastPageStartTicks;

        [SerializeField]
        long currentParagraphStartTicks;

        public static AnalyticsHelper Instance
        {
            get
            {
                if (!s_Instance)
                {
                    var instance = Resources.FindObjectsOfTypeAll<AnalyticsHelper>();
                    if (instance.Length == 0)
                    {
                        s_Instance = CreateInstance<AnalyticsHelper>();
                        s_Instance.hideFlags = HideFlags.HideAndDontSave;
                    }
                    else
                    {
                        s_Instance = instance[0] as AnalyticsHelper;
                    }
                }
                return s_Instance;
            }
        }
        static AnalyticsHelper s_Instance;

        public void OnBeforeSerialize()
        {
            currentTutorialStartTicks = currentTutorialStartTime.Ticks;
            currentPageStartTicks = currentPageStartTime.Ticks;
            lastPageStartTicks = lastPageStartTime.Ticks;
            currentParagraphStartTicks = currentParagraphStartTime.Ticks;
        }

        public void OnAfterDeserialize()
        {
            currentTutorialStartTime = new DateTime(currentTutorialStartTicks, DateTimeKind.Utc);
            currentPageStartTime = new DateTime(currentPageStartTicks, DateTimeKind.Utc);
            lastPageStartTime = new DateTime(lastPageStartTicks, DateTimeKind.Utc);
            currentParagraphStartTime = new DateTime(currentParagraphStartTicks, DateTimeKind.Utc);
        }

        static void DebugWarning(string message, params object[] args)
        {
#if DEBUG_TUTORIALS
            Debug.LogWarningFormat(message, args);
#endif
        }

        static void DebugLog(string message, params object[] args)
        {
#if DEBUG_TUTORIALS
            Debug.LogFormat(message, args);
#endif
        }

        static void DebugError(string message, params object[] args)
        {
#if DEBUG_TUTORIALS
            Debug.LogErrorFormat(message, args);
#endif
        }

        internal static void TutorialStarted(Tutorial tutorial)
        {
            if (Instance.currentTutorial != null)
            {
                DebugWarning("TutorialStarted Ignored because tutorial is already set: {0}", tutorial);
                return;
            }
            DebugLog("Tutorial Started: {0}", tutorial);
            Instance.currentTutorial = tutorial;
            Instance.currentTutorialStartTime = DateTime.UtcNow;
            Instance.currentPageIndex = Instance.lastPageIndex = Instance.currentParagraphIndex = -1;
            Instance.currentPage = Instance.lastPage = null;
        }

        internal static void TutorialEnded(TutorialConclusion conclusion)
        {
            if (Instance.currentTutorial == null)
            {
                DebugWarning("TutorialEnded Ignored because no tutorial is set");
                return;
            }
            if (conclusion == TutorialConclusion.Completed)
            {
                PageShown(Instance.lastPage, Instance.lastPageIndex + 1);  // "Show" a dummy page to get the last page to report
            }
            if (Instance.currentTutorial.ProgressTrackingEnabled)
            {
                SendTutorialEvent(
                    Instance.currentTutorial.name, Instance.currentTutorial.Version, conclusion,
                    Instance.currentTutorial.LessonId, Instance.currentTutorialStartTime,
                    DateTime.UtcNow - Instance.currentTutorialStartTime, false
                );
            }
            DebugLog("Tutorial Ended: " + conclusion);
            if (conclusion == TutorialConclusion.Quit)
                Instance.currentTutorial = null;
        }

        internal static void PageShown(TutorialPage page, int pageIndex)
        {
            if (Instance.currentTutorial == null)
            {
                DebugWarning("PageShown Ignored because no tutorial is set");
                return;
            }

            Instance.currentPageIndex = pageIndex;
            Instance.currentPage = page;
            Instance.currentPageStartTime = DateTime.UtcNow;

            if (Instance.lastPage != null)
            {
                if (Instance.currentPageIndex < Instance.lastPageIndex)
                {
                    SendTutorialPageEvent
                    (
                        Instance.currentTutorial.name, Instance.currentPageIndex, GetAssetGuid(Instance.currentPage),
                        TutorialPageConclusion.Reviewed, Instance.currentPageStartTime,
                        DateTime.UtcNow - Instance.currentPageStartTime, false
                    );

                    DebugLog("Page Reviewed: {0}", Instance.currentPageIndex);
                }
                else if (pageIndex > Instance.lastPageIndex)
                {
                    SendTutorialPageEvent
                    (
                        Instance.currentTutorial.name, Instance.lastPageIndex, GetAssetGuid(Instance.lastPage),
                        TutorialPageConclusion.Completed, Instance.lastPageStartTime,
                        DateTime.UtcNow - Instance.lastPageStartTime, false
                    );

                    DebugLog("Page Completed: {0}", Instance.lastPageIndex);
                }
            }

            if (Instance.currentPageIndex <= Instance.lastPageIndex)
                return;

            Instance.lastPageIndex = Instance.currentPageIndex;
            Instance.lastPage = Instance.currentPage;
            Instance.lastPageStartTime = DateTime.UtcNow;
            Instance.currentParagraphIndex = -1;
        }

        internal static void ParagraphStarted(int paragraphIndex)
        {
            if (Instance.currentTutorial == null)
            {
                DebugWarning("ParagraphStarted Ignored because no tutorial is set: {0}", paragraphIndex);
                return;
            }
            if (Instance.currentParagraphIndex >= 0)
            {
                ParagraphEnded(true);
            }
            DebugLog("Paragraph Started: {0}", paragraphIndex);
            Instance.currentParagraphStartTime = DateTime.UtcNow;
            Instance.currentParagraphIndex = paragraphIndex;
        }

        internal static void ParagraphEnded()
        {
            ParagraphEnded(false);
        }

        internal static void ParagraphEnded(bool regressed)
        {
            if (Instance.currentTutorial == null)
            {
                DebugWarning("ParagraphEnded Ignored because no tutorial is set");
                return;
            }
            if (Instance.currentParagraphIndex == -1)
            {
                DebugWarning("ParagraphEnded Ignored because no paragraph is active");
                return;
            }
            DebugLog("Paragraph Ended: regression = {0}", regressed);
            var conclusion = regressed ? TutorialParagraphConclusion.Regressed : TutorialParagraphConclusion.Completed;

            SendTutorialParagraphEvent
            (
                Instance.currentTutorial.name, Instance.currentPageIndex, Instance.currentParagraphIndex, conclusion,
                Instance.currentParagraphStartTime, DateTime.UtcNow - Instance.currentParagraphStartTime, false
            );

            Instance.currentParagraphIndex = -1;
        }

        static string GetAssetGuid(ScriptableObject asset) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));

        /// <summary>
        /// Use for external references/links, documentation, assets, etc.
        /// https://docs.google.com/spreadsheets/d/1vftlkO4yps3qUoPgM2wnbJu4YwRO3fZ4cS7IELk4Gww/edit#gid=1343103808
        /// </summary>
#if UNITY_6000
        public struct ExternalReferenceEventData : IAnalytic.IData
#else
        public struct ExternalReferenceEventData
#endif
        {
            public int ts; // timestamp
            public string id;
            public string title;
            public string type; // e.g. Asset Store or Mods
            public string path; // URL
        }

#if UNITY_6000
        public struct ExternalReferenceImpressionEventData : IAnalytic.IData
#else
        public struct ExternalReferenceImpressionEventData
#endif
        {
            public int ts; // timestamp
            public string id;
            public string title;
            public string type; // e.g. Asset Store or Mods
            public string path; // URL
        }

#if UNITY_6000
        public struct TutorialEventData : IAnalytic.IData
#else
        public struct TutorialEventData
#endif
        {
            public int ts; // timestamp
            public string tutorialName;
            public string version;
            public int conclusion;
            public string lessonID;
            public int startTime;
            public int duration;
            public bool isBlocking;
        }

#if UNITY_6000
        public struct TutorialPageEventData : IAnalytic.IData
#else
        public struct TutorialPageEventData
#endif
        {
            public int ts; // timestamp
            public string tutorialName;
            public int pageIndex;
            public string guid;
            public int conclusion;
            public int startTime;
            public int duration;
            public bool isBlocking;
        }

#if UNITY_6000
        public struct TutorialParagraphEventData : IAnalytic.IData
#else
        public struct TutorialParagraphEventData
#endif
        {
            public int ts; // timestamp
            public string tutorialName;
            public int pageIndex;
            public int paragraphIndex;
            public int conclusion;
            public int startTime;
            public int duration;
            public bool isBlocking;
        }

        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.iet"; // the format needs to be "unity.xxx"

        const string k_EventExternalReference = "iet_externalReference";
        const string k_EventExternalReferenceImpression = "iet_externalReferenceImpression";
        const string k_EventTutorial = "iet_tutorial";
        const string k_EventTutorialPage = "iet_tutorialPage";
        const string k_EventTutorialParagraph = "iet_tutorialParagraph";

#if UNITY_6000
        //Unity 6 editor analytics changed, you now define 1 class per type of analytics, so we need to create a class for each even above

        [AnalyticInfo(eventName: k_EventTutorial, vendorKey: k_VendorKey)]
        internal class TutorialAnalytic : IAnalytic
        {
            private TutorialEventData parameters;

            public TutorialAnalytic(string tutorialName, string version, TutorialConclusion conclusion, string lessonID, DateTime startTime, TimeSpan duration, bool isBlocking)
            {
                this.parameters = new TutorialEventData
                {
                    ts = DateTime.UtcNow.Millisecond,
                    tutorialName = tutorialName,
                    version = version,
                    conclusion = (int)conclusion,
                    lessonID = lessonID,
                    duration = duration.Milliseconds,
                    startTime = startTime.Millisecond,
                    isBlocking = isBlocking
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data =  parameters;
                return data != null;
            }
        }

        [AnalyticInfo(eventName: k_EventTutorialPage, vendorKey: k_VendorKey)]
        internal class TutorialPageAnalytic : IAnalytic
        {
            private TutorialPageEventData parameters;

            public TutorialPageAnalytic(string tutorialName, int pageIndex, string guid, TutorialPageConclusion conclusion, DateTime startTime, TimeSpan duration, bool isBlocking)
            {
                parameters = new TutorialPageEventData
                {
                    ts = DateTime.UtcNow.Millisecond,
                    tutorialName = tutorialName,
                    pageIndex = pageIndex,
                    guid = guid,
                    conclusion = (int)conclusion,
                    duration = duration.Milliseconds,
                    startTime = startTime.Millisecond,
                    isBlocking = isBlocking
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data =  parameters;
                return data != null;
            }
        }

        [AnalyticInfo(eventName: k_EventTutorialParagraph, vendorKey: k_VendorKey)]
        internal class TutorialParagraphAnalytic : IAnalytic
        {
            private TutorialParagraphEventData parameters;

            public TutorialParagraphAnalytic(string tutorialName, int pageIndex, int paragraphIndex, TutorialParagraphConclusion conclusion, DateTime startTime, TimeSpan duration, bool isBlocking)
            {
                parameters = new TutorialParagraphEventData
                {
                    ts = DateTime.UtcNow.Millisecond,
                    tutorialName = tutorialName,
                    pageIndex = pageIndex,
                    paragraphIndex = paragraphIndex,
                    conclusion = (int)conclusion,
                    duration = duration.Milliseconds,
                    startTime = startTime.Millisecond,
                    isBlocking = isBlocking
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data =  parameters;
                return data != null;
            }
        }

        [AnalyticInfo(eventName: k_EventExternalReference, vendorKey: k_VendorKey)]
        internal class ExternalReferenceAnalytic : IAnalytic
        {
            private ExternalReferenceEventData parameters;

            public ExternalReferenceAnalytic(string url, string title, string contentType, string id = null)
            {
                parameters =  new ExternalReferenceEventData
                {
                    ts = DateTime.UtcNow.Millisecond,
                    id = id,
                    title = title,
                    type = contentType,
                    path = url
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data =  parameters;
                return data != null;
            }
        }

        [AnalyticInfo(eventName: k_EventExternalReferenceImpression, vendorKey: k_VendorKey)]
        internal class ExternalReferenceImpressionAnalytic : IAnalytic
        {
            private ExternalReferenceImpressionEventData parameters;

            public ExternalReferenceImpressionAnalytic(string url, string title, string contentType, string id = null)
            {
                parameters = new ExternalReferenceImpressionEventData
                {
                    ts = DateTime.UtcNow.Millisecond,
                    id = id,
                    title = title,
                    type = contentType,
                    path = url
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data =  parameters;
                return data != null;
            }
        }
#else
        static bool RegisterEvent(string name)
        {
            AnalyticsResult result = EditorAnalytics.RegisterEventWithLimit(name, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);
            if (result != AnalyticsResult.Ok)
            {
                DebugError("Error in RegisterEvent: {0}", result);
                return false;
            }
            return true;
        }
#endif

        public static AnalyticsResult SendTutorialEvent(string tutorialName, string version, TutorialConclusion conclusion, string lessonID, DateTime startTime, TimeSpan duration, bool isBlocking)
        {

            if (!EditorAnalytics.enabled
#if !UNITY_6000
                || !RegisterEvent(k_EventTutorial)
#endif
                ) { return AnalyticsResult.AnalyticsDisabled; }

#if UNITY_6000
            return EditorAnalytics.SendAnalytic(new TutorialAnalytic(tutorialName, version, conclusion, lessonID, startTime, duration, isBlocking));
#else
            var data = new TutorialEventData
            {
                ts = DateTime.UtcNow.Millisecond,
                tutorialName = tutorialName,
                version = version,
                conclusion = (int)conclusion,
                lessonID = lessonID,
                duration = duration.Milliseconds,
                startTime = startTime.Millisecond,
                isBlocking = isBlocking
            };

            return SendEvent(k_EventTutorial, data);
#endif
        }

        public static AnalyticsResult SendTutorialPageEvent(string tutorialName, int pageIndex, string guid, TutorialPageConclusion conclusion, DateTime startTime, TimeSpan duration, bool isBlocking)
        {
            if (!EditorAnalytics.enabled
#if !UNITY_6000
                || !RegisterEvent(k_EventTutorialPage)
#endif
                ) { return AnalyticsResult.AnalyticsDisabled; }

#if UNITY_6000
            return EditorAnalytics.SendAnalytic(new TutorialPageAnalytic(tutorialName, pageIndex, guid, conclusion, startTime, duration, isBlocking));
#else
            var data = new TutorialPageEventData
            {
                ts = DateTime.UtcNow.Millisecond,
                tutorialName = tutorialName,
                pageIndex = pageIndex,
                guid = guid,
                conclusion = (int)conclusion,
                duration = duration.Milliseconds,
                startTime = startTime.Millisecond,
                isBlocking = isBlocking
            };

            return SendEvent(k_EventTutorialPage, data);
#endif
        }

        public static AnalyticsResult SendTutorialParagraphEvent(string tutorialName, int pageIndex, int paragraphIndex, TutorialParagraphConclusion conclusion, DateTime startTime, TimeSpan duration, bool isBlocking)
        {
            if (!EditorAnalytics.enabled
#if !UNITY_6000
                || !RegisterEvent(k_EventTutorialParagraph)
#endif
                ) { return AnalyticsResult.AnalyticsDisabled; }

#if UNITY_6000
            return EditorAnalytics.SendAnalytic(new TutorialParagraphAnalytic(tutorialName, pageIndex, paragraphIndex, conclusion, startTime, duration, isBlocking));
#else
            var data = new TutorialParagraphEventData
            {
                ts = DateTime.UtcNow.Millisecond,
                tutorialName = tutorialName,
                pageIndex = pageIndex,
                paragraphIndex = paragraphIndex,
                conclusion = (int)conclusion,
                duration = duration.Milliseconds,
                startTime = startTime.Millisecond,
                isBlocking = isBlocking
            };
            return SendEvent(k_EventTutorialParagraph, data);
#endif
        }

        public static AnalyticsResult SendExternalReferenceEvent(string url, string title, string contentType, string id = null)
        {
            if (!EditorAnalytics.enabled
#if !UNITY_6000
                || !RegisterEvent(k_EventExternalReference)
#endif
                ) { return AnalyticsResult.AnalyticsDisabled; }

#if UNITY_6000
            return EditorAnalytics.SendAnalytic(new ExternalReferenceAnalytic(url, title, contentType, id));
#else
            var data = new ExternalReferenceEventData
            {
                ts = DateTime.UtcNow.Millisecond,
                id = id,
                title = title,
                type = contentType,
                path = url
            };
            return SendEvent(k_EventExternalReference, data);
#endif
        }

        public static AnalyticsResult SendExternalReferenceImpressionEvent(string url, string title, string contentType, string id = null)
        {
            if (!EditorAnalytics.enabled
#if !UNITY_6000
                || !RegisterEvent(k_EventExternalReferenceImpression)
#endif
                ) { return AnalyticsResult.AnalyticsDisabled; }

#if UNITY_6000
            return EditorAnalytics.SendAnalytic(new ExternalReferenceImpressionAnalytic(url, title, contentType, id));
#else
            var data = new ExternalReferenceImpressionEventData
            {
                ts = DateTime.UtcNow.Millisecond,
                id = id,
                title = title,
                type = contentType,
                path = url
            };
            return SendEvent(k_EventExternalReferenceImpression, data);
#endif
        }

#if !UNITY_6000
        static AnalyticsResult SendEvent(string eventName, object parameters)
        {
            AnalyticsResult result = EditorAnalytics.SendEventWithLimit(eventName, parameters);
            if (result != AnalyticsResult.Ok)
            {
                DebugError("Error in {0}: {1}", eventName, result);
            }
            return result;
        }
#endif
    }
}
