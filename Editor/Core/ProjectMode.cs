using UnityEngine.Scripting.APIUpdating;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Can be used to query the mode of the Tutorial Project.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public static class ProjectMode
    {
        /// <summary>
        /// Returns true if Tutorial Authoring Tools are present and we are in authoring mode.
        /// </summary>
        /// <returns>True if the Framework is in Authoring mode, false otherwise</returns>
        public static bool IsAuthoringMode()
        {
#if TUTORIAL_AUTHORING
            return true;
#else
            return false;
#endif
        }
    }
}
