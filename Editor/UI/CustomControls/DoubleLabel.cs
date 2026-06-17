using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor.CustomControl
{
    /// <summary>
    /// Custom control to display a property in the Inspector that has a Label as "label" (on the left side),
    /// and another Label as its value (on the right), instead of a TextField.
    /// Both labels are not editable by the user.
    /// This can be used in custom Inspectors to display a small message in place of an editable field
    /// when certain conditions occur (e.g. the field is not available).
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor.UI.CustomControl", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class DoubleLabel : BaseField<string>
    {
        /// <summary>
        /// Creates a DoubleLabel.
        /// </summary>
        /// <param name="label">The text that goes in the left-side Label.</param>
        /// <param name="message">The text that goes in the right-side Label.</param>
        /// <param name="labelTooltip">The tooltip for the left-side Label. This is optional.</param>
        /// <param name="messageTooltip">The tooltip for the right-side Label. This is optional.</param>
        public DoubleLabel(string label, string message,
            string labelTooltip = "", string messageTooltip = "")
            : this(label, new Label(message){ tooltip = messageTooltip})
        {
            tooltip = labelTooltip;
            AddToClassList("unity-base-field__aligned");
        }

        private DoubleLabel(string label, VisualElement visualInput) : base(label, visualInput) { }
    }
}
