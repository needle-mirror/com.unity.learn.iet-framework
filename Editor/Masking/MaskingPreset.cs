using System.Collections.Generic;
using Unity.Tutorials.Editor.Paragraphs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// A MaskingPreset is a ScriptableObject containing a preset for masking and revealing editor windows,
    /// in the form of a List of <see cref="UnmaskedView"/>.
    /// It is intended to be used within <see cref="TutorialPage"/> objects, so different pages can share
    /// the same masking.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMaskingPreset", menuName = "Tutorials/Masking Preset", order = 204)]
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class MaskingPreset : ScriptableObject
    {
        [SerializeField] internal List<UnmaskedView> m_unmaskedViews = new();

        private void OnValidate()
        {
            // Find all MaskingSettings referencing this MaskingPreset
            List<MaskingSettings> matchingMaskingSettings = new();
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(TutorialPage)}"))
            {
                TutorialPage page = AssetDatabase.LoadAssetAtPath<TutorialPage>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (ParagraphBase paragraph in page.Paragraphs)
                {
                    if (paragraph.MaskingSettings?.MaskPreset == this)
                        matchingMaskingSettings.Add(paragraph.MaskingSettings);
                }
            }

            // Update the MaskingSettings in it to match any modification that might have occurred
            foreach (MaskingSettings maskingSetting in matchingMaskingSettings)
            {
                maskingSetting.UnmaskedViews = new List<UnmaskedView>();

                foreach (UnmaskedView unmaskedView in m_unmaskedViews)
                    maskingSetting.UnmaskedViews.Add(unmaskedView);
            }
        }
    }
}
