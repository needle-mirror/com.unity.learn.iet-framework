using System;
using Unity.Tutorials.Editor.Paragraphs;
using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Editor
{
    [Serializable]
    internal class AutoCompletion
    {
        private Tutorial m_Tutorial;

        private bool m_Running;
        private bool m_Continue;

        private const double k_TimeoutDuration = 10f;
        private double m_CompletionDeadline;

        public AutoCompletion(Tutorial tutorial)
        {
            m_Tutorial = tutorial;
        }

        public bool running => m_Running;

        public void OnEnable()
        {
            if (m_Running)
            {
                if (m_Tutorial.IsCompleted)
                    Stop();
                else
                {
                    EditorApplication.update += OnUpdate;
                    Criterion.CriterionCompleted.AddListener(OnCriterionCompleted);
                }
            }
        }

        public void OnDisable()
        {
            if (m_Running)
            {
                EditorApplication.update -= OnUpdate;
                Criterion.CriterionCompleted.RemoveListener(OnCriterionCompleted);
            }
        }

        public void Start()
        {
            if (m_Running || m_Tutorial.IsCompleted)
                return;

            m_Running = true;
            m_Continue = true;
            m_CompletionDeadline = 0f;

            EditorApplication.update += OnUpdate;
            Criterion.CriterionCompleted.AddListener(OnCriterionCompleted);
        }

        public void Stop()
        {
            if (!m_Running)
                return;

            m_Running = false;

            EditorApplication.update -= OnUpdate;
            Criterion.CriterionCompleted.RemoveListener(OnCriterionCompleted);

            if (m_Tutorial.IsCompleted)
                Debug.Log("Tutorial was completed automatically.");
            else
                Debug.Log("Tutorial was did not complete automatically.");
        }

        private void OnCriterionCompleted(Criterion completedCriterion)
        {
            m_Continue = true;
        }

        private void OnUpdate()
        {
            if (m_CompletionDeadline != 0f && EditorApplication.timeSinceStartup > m_CompletionDeadline)
            {
                Debug.LogWarning("Auto completion timed out.");
                Stop();
            }

            if (m_Continue)
                AutoCompleteNextIncompleteCriterion();
        }

        private void AutoCompleteNextIncompleteCriterion()
        {
            m_Continue = false;

            while (m_Tutorial.TryGoToNextPage())
            {
            }

            Criterion nextIncompleteCriterion = GetNextIncompleteCriterion();
            if (nextIncompleteCriterion == null)
            {
                Stop();
                return;
            }

            if (!nextIncompleteCriterion.AutoComplete())
            {
                nextIncompleteCriterion.UpdateCompletion();
                Debug.LogWarning("Unable to complete criterion automatically");
                Stop();
            }

            m_CompletionDeadline = EditorApplication.timeSinceStartup + k_TimeoutDuration;
        }

        private Criterion GetNextIncompleteCriterion()
        {
            foreach (TutorialPage page in m_Tutorial.PagesCollection)
            {
                foreach (ParagraphBase paragraph in page.Paragraphs)
                {
                    if (paragraph.HasCriteria())
                    {
                        foreach (TypedCriterion typedCriterion in paragraph.Criterias())
                        {
                            Criterion criterion = typedCriterion.Criterion;
                            if (!criterion.IsCompleted)
                                return criterion;
                        }
                    }
                }
            }

            return null;
        }
    }
}
