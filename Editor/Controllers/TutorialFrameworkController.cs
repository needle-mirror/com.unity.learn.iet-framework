namespace Unity.Tutorials.Editor
{
    internal class TutorialFrameworkController : Controller
    {
        private TutorialFrameworkModel m_Model;

        internal TutorialFrameworkController(TutorialFrameworkModel model)
        {
            m_Model = model;
        }

        internal override void RemoveListeners()
        {
        }
        internal void OnSignInClicked()
        {
            UnityConnectSession.instance.ShowLogin();
        }
    }
}
