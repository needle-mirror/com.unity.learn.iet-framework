using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.Tutorials.Editor
{
    [InitializeOnLoad]
    internal static class GenesisHelper
    {
        private static readonly string prodHost = "https://api.unity.com";
        private static readonly string stagingHost = "https://api-staging.unity.com";

        private static List<KeyValuePair<UnityWebRequestAsyncOperation, Action<UnityWebRequest>>> m_Requests = new();

        private static HttpClient s_HttpClientInstance;

        private static HttpClient s_HttpClient
        {
            get
            {
                if (s_HttpClientInstance == null)
                {
                    s_HttpClientInstance = new HttpClient();
                    s_HttpClientInstance.BaseAddress = new Uri(HostAddress);
                }
                return s_HttpClientInstance;
            }
        }

        public static bool HasWarnedAboutLogin { get; set; }

        private static string HostAddress => (IsStagingEnv() ? stagingHost : prodHost);

        private static bool IsStagingEnv()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                if (commandLineArgs[i] == "-cloudEnvironment")
                {
                    if (i + 1 < commandLineArgs.Length && commandLineArgs[i + 1] == "staging")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static string GetVersion()
        {
            return PackageInfo.FindForAssembly(Assembly.GetExecutingAssembly()).version;
        }

        static GenesisHelper()
        {
            EditorApplication.update += WebRequestProcessor;
        }

        public static void LogTutorialStarted(string lessonId)
        {
            if (!IsLessonIdValid(lessonId))
            {
                return;
            }
            GetTutorial(lessonId, list =>
            {
                TutorialProgressStatus lesson = list.FirstOrDefault(s => s.lessonId == lessonId);
                if (lesson == null || string.IsNullOrEmpty(lesson.status.Trim()))
                {
                    // The fact no entries exist means the user never opened (started) this tutorial
                    LogTutorialStatusUpdate(lessonId, nameof(TutorialProgressStatus.Status.Started));
                }
            });
        }

        public static void LogTutorialEnded(string lessonId)
        {
            if (!IsLessonIdValid(lessonId))
            {
                return;
            }
            // Always set the status to Finished: if we just began a tutorial and completed it very fast
            // we might not have received the up-to-date (Started) state from the backend if querying it here.
            LogTutorialStatusUpdate(lessonId, nameof(TutorialProgressStatus.Status.Finished));
        }

        private static bool IsLessonIdValid(string lessonId)
        {
            if (string.IsNullOrEmpty(lessonId.Trim()))
            {
                LogWarningOnlyInAuthoringMode("LessonId is not set. You can set LessonId on the tutorial asset");
                return false;
            }
            return true;
        }

        private static void LogWarningOnlyInAuthoringMode(string message)
        {
            // We don't want to spam users with warning messages
            // but we want to catch them while creating tutorials
            if (ProjectMode.IsAuthoringMode())
                Debug.LogWarning(message);
        }

        public static void PrintAllTutorials()
        {
            GetAllTutorials(tutorials =>
            {
                string result = "";
                foreach (TutorialProgressStatus tutorial in tutorials)
                {
                    result += tutorial.lessonId + ": " + tutorial.status + "\n";
                }
                Debug.Log(result);
            });
        }

        public static void GetAllTutorials(Action<List<TutorialProgressStatus>> action)
        {
            GetTutorial(null, action);
        }

        private static bool IsRequestSuccess(UnityWebRequest unityWebRequest)
        {
#if UNITY_2020_1_OR_NEWER
            if ((unityWebRequest.result == UnityWebRequest.Result.ConnectionError)
                || (unityWebRequest.result == UnityWebRequest.Result.ProtocolError))
#else
            if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
#endif
            {
                LogWarningOnlyInAuthoringMode(unityWebRequest.error);
                return false;
            }
            return true;
        }

        private static void GetTutorial(string lessonId, Action<List<TutorialProgressStatus>> action)
        {
            string userId = UnityConnectSession.instance.GetUserId();
            if (userId.IsNullOrEmpty() || userId == UnityConnectSession.k_NotSignedInUserUsername)
            {
                if (!HasWarnedAboutLogin)
                {
                    Debug.LogWarning("Error: No user ID. Are you logged in?");
                    HasWarnedAboutLogin = true;
                }
                return;
            }
            string getLink = "/v1/users/" + userId + "/lessons";
            string url = HostAddress + getLink;
            UnityWebRequest req = MakeGetLessonsRequest(url, lessonId);
            SendWebRequest(req, r =>
            {
                if (!IsRequestSuccess(r))
                {
                    return;
                }
                List<TutorialProgressStatus> lessonResponses = TutorialProgressStatus.ParseResponses(r.downloadHandler.text);
                action(lessonResponses);
            });
        }

        public static async void LogTutorialStatusUpdate(string lessonId, string lessonStatus)
        {
            string userId = UnityConnectSession.instance.GetUserId();
            if (userId.IsNullOrEmpty()) return;
            string getLink = "/v1/users/" + userId + "/lessons";

            string jsonData = RegisterLessonRequest.GetJSONString(lessonStatus, userId, lessonId);

            // UnityWebRequests were causing memory leaks here, so they were replaced with HttpClient
            using (HttpRequestMessage request = new(HttpMethod.Post, getLink))
            {
                StringContent data = new(jsonData, Encoding.UTF8, "application/json");

                request.Content = data;

                request.Headers.Add("X-IET-Version", GetVersion());
                request.Headers.Add("Authorization", "Bearer " + UnityConnectSession.instance.GetAccessToken());
                HttpResponseMessage response = await s_HttpClient.SendAsync(request);
            }
        }

        private static void SendWebRequest(UnityWebRequest request, Action<UnityWebRequest> onFinished)
        {
            KeyValuePair<UnityWebRequestAsyncOperation, Action<UnityWebRequest>> pair = new(request.SendWebRequest(), onFinished);
            m_Requests.Add(pair);
        }

        private static void WebRequestProcessor()
        {
            if (!m_Requests.Any())
                return;

            for (int i = 0; i < m_Requests.Count; i++)
            {
                UnityWebRequestAsyncOperation request = m_Requests[i].Key;
                if (!request.isDone)
                    continue;
                Action<UnityWebRequest> callback = m_Requests[i].Value;
                m_Requests.RemoveAt(i);
                callback(request.webRequest);
                break;
            }
        }

        private static UnityWebRequest MakeGetLessonsRequest(string url, string lessonId)
        {
            if (!string.IsNullOrEmpty(lessonId))
            {
                url += "?lessonId=" + lessonId;
            }

            UnityWebRequest request = UnityWebRequest.Post(url, new WWWForm());
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-IET-Version", GetVersion());
            request.SetRequestHeader("Authorization", "Bearer " + UnityConnectSession.instance.GetAccessToken());
            request.method = "GET";
            return request;
        }

        [Serializable]
        public class TutorialProgressStatus
        {
            public string lessonId;
            public string status; // TODO make an enum instead

            public enum Status
            {
                Started,
                Finished
            }

            [Serializable]
            private class Wrapper
            {
                public List<TutorialProgressStatus> statuses;
            }

            public static List<TutorialProgressStatus> ParseResponses(string respText)
            {
                StringBuilder builder = new(12 + respText.Length + 1);
                builder.Append("{\"statuses\":");
                builder.Append(respText);
                builder.Append("}");
                Wrapper wrapper = JsonUtility.FromJson<Wrapper>(builder.ToString());
                return wrapper.statuses;
            }
        }

        private class RegisterLessonRequest
        {
            public string status;
            public string userId;
            public string lessonId;

            public static string GetJSONString(string status, string userId, string lessonId)
            {
                RegisterLessonRequest r = new();
                r.status = status;
                r.userId = userId;
                r.lessonId = lessonId;
                return JsonUtility.ToJson(r);
            }
        }
    }
}
