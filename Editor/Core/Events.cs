using System;
using System.Collections.Generic;
using static Unity.Tutorials.Editor.TutorialContainer;

namespace Unity.Tutorials.Editor
{
    internal class AppEvent { }
    internal class CategoriesRefreshRequestedEvent : AppEvent { }
    internal class CategoryClickedEvent : AppEvent
    {
        public TutorialContainer Category { get; private set; }

        public CategoryClickedEvent(TutorialContainer category)
        {
            Category = category;
        }
    }

    internal class BackButtonClickedEvent : AppEvent { }

    internal class SectionClickedEvent : AppEvent
    {
        public Section Section { get; private set; }

        public SectionClickedEvent(Section section)
        {
            Section = section;
        }
    }

    internal class TutorialStartRequestedEvent : AppEvent
    {
        public Tutorial Tutorial { get; private set; }
        public Tutorial PreviousTutorial { get; private set; }

        public TutorialStartRequestedEvent(Tutorial tutorial, Tutorial previousTutorial)
        {
            Tutorial = tutorial;
            PreviousTutorial = previousTutorial;
        }
    }

    internal class TutorialQuitEvent : AppEvent { }
    internal class TutorialNavigationEvent : AppEvent
    {
        public bool MoveToNextPage { get; private set; }

        public TutorialNavigationEvent(bool moveToNextPage)
        {
            MoveToNextPage = moveToNextPage;
        }
    }
    internal class DomainReloadOccurredEvent : AppEvent { }
    internal class TutorialsCompletionStatusUpdatedEvent : AppEvent { }

    /// <summary>
    /// A simple Event System that can be used for remote systems communication
    /// </summary>
    internal class EventManager
    {
        private readonly Dictionary<Type, Action<AppEvent>> s_Events = new();
        private readonly Dictionary<Delegate, Action<AppEvent>> s_EventLookups = new();

        public void AddListener<T>(Action<T> evt) where T : AppEvent
        {
            if (s_EventLookups.ContainsKey(evt)) { return; }

            Action<AppEvent> newAction = e => evt((T)e);
            s_EventLookups[evt] = newAction;

            if (s_Events.TryGetValue(typeof(T), out Action<AppEvent> internalAction))
            {
                s_Events[typeof(T)] = internalAction += newAction;
            }
            else
            {
                s_Events[typeof(T)] = newAction;
            }
        }

        public void RemoveListener<T>(Action<T> evt) where T : AppEvent
        {
            if (!s_EventLookups.TryGetValue(evt, out Action<AppEvent> action)) { return; }

            if (s_Events.TryGetValue(typeof(T), out Action<AppEvent> tempAction))
            {
                tempAction -= action;
                if (tempAction == null)
                    s_Events.Remove(typeof(T));
                else
                    s_Events[typeof(T)] = tempAction;
            }

            s_EventLookups.Remove(evt);
        }

        public void Broadcast(AppEvent evt)
        {
            if (s_Events.TryGetValue(evt.GetType(), out Action<AppEvent> action))
                action.Invoke(evt);
        }

        public void Clear()
        {
            s_Events.Clear();
            s_EventLookups.Clear();
        }
    }
}
