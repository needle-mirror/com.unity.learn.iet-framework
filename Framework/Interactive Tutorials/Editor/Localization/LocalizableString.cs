using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// String that is localized at run-time.
    /// </summary>
    [Serializable]
    public class LocalizableString
    {
        /// <summary>
        /// Setting Untranslated string overwrites Translated so make sure to translate again.
        /// </summary>
        [property: SerializeField]
        public string Untranslated
        {
            get => m_Untranslated;
            set => Translated = m_Untranslated = value;
        }

        [SerializeField, FormerlySerializedAs(OldPropertyPath)]
        string m_Untranslated;

        public string Translated { get; set; }

        public LocalizableString() : this(string.Empty) {}
        public LocalizableString(string untranslated) { Untranslated = untranslated; }

        public static implicit operator LocalizableString(string untranslated) => new LocalizableString(untranslated);
        /// <summary>
        /// Implicit conversion to string returns the translated string, if exists, untranslated otherwise.
        /// </summary>
        /// <param name="str"></param>
        public static implicit operator string(LocalizableString str) => str.Translated.AsNullIfEmpty() ?? str.Untranslated;

        public const string PropertyPath = "m_Untranslated";
        public const string OldPropertyPath = "<Untranslated>k__BackingField";
    }
}
