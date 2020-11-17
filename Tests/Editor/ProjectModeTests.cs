using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace Unity.Tutorials.Core.Editor.InternalToolsTests
{
    public class ProjectModeTest
    {
        [Test]
        public void IsAuthoringMode_Passes()
        {
            ProjectMode.IsAuthoringMode();
        }
    }
}
