using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// A MaskingPreset is a ScriptableObject containing a preset for masking and revealing editor windows,
    /// in the form of a List of <see cref="UnmaskedView"/>.
    /// It is intended to be used within <see cref="TutorialPage"/> objects, so different pages can share
    /// the same masking.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMaskingPreset", menuName = "Tutorials/Masking Preset", order = 204)]
    public class MaskingPreset : ScriptableObject
    {
        [SerializeField] internal List<UnmaskedView> m_unmaskedViews = new();

        private void OnValidate()
        {
            TutorialPage[] allPages = AssetDatabase.FindAssets($"t:{nameof(TutorialPage)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<TutorialPage>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();

            // Find all MaskingSettings referencing this MaskingPreset
            MaskingSettings[] matchingMaskingSettings = allPages
                .SelectMany(page => page.Paragraphs)
                .Where(paragraph => paragraph.MaskingSettings?.MaskPreset == this)
                .Select(paragraph => paragraph.MaskingSettings)
                .ToArray();

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
