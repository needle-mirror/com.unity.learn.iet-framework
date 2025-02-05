namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Extension methods for System.String.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Indicates whether this string is null or an empty string ("").
        /// </summary>
        /// <param name="self">The string this function acts on</param>
        /// <returns>True if null or empty</returns>
        public static bool IsNullOrEmpty(this string self) => string.IsNullOrEmpty(self);
        /// <summary>
        /// Indicates whether this string is not null or an empty string ("").
        /// </summary>
        /// <param name="self">The string this function acts on</param>
        /// <returns>True if not null or empty</returns>
        public static bool IsNotNullOrEmpty(this string self) => !self.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="self">The string this function acts on</param>
        /// <returns>True if null or whitespace only</returns>
        public static bool IsNullOrWhiteSpace(this string self) => string.IsNullOrWhiteSpace(self);
        /// <summary>
        /// Indicates whether this string is not null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="self">The string this function acts on</param>
        /// <returns>True if not empty or only whitespace</returns>
        public static bool IsNotNullOrWhiteSpace(this string self) => !self.IsNullOrWhiteSpace();

        /// <summary>
        /// Returns null if this string is not null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="self">The string this function acts on</param>
        /// <returns>null if the string is only whitespace, the unmodified string otherwise</returns>
        public static string AsNullIfWhiteSpace(this string self) => string.IsNullOrWhiteSpace(self) ? null : self;
        /// <summary>
        /// Returns null if this string is null or an empty string ("").
        /// </summary>
        /// <param name="self">The string this function acts on</param>
        /// <returns>null if the string is empty, the unmodified string otherwise</returns>
        public static string AsNullIfEmpty(this string self) => self.IsNullOrEmpty() ? null : self;

        /// <summary>
        /// Returns an empty string ("") if this string is null.
        /// </summary>
        /// <param name="self">The string this function acts on</param>
        /// <returns>An empty string if the string is null</returns>
        public static string AsEmptyIfNull(this string self) => self ?? string.Empty;
    }

    /// <summary>
    /// Static helper functions for System.String.
    /// Useful for example in LINQ queries.
    /// </summary>
    public static class StringExt
    {
        /// <summary>
        /// Indicates whether a specified string is not null or an empty string ("").
        /// </summary>
        /// <param name="str">The string this function acts on</param>
        /// <returns>True if the string is not null or empty</returns>
        public static bool IsNotNullOrEmpty(string str) => !str.IsNullOrEmpty();
        /// <summary>
        /// Indicates whether a specified string is not null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="str">The string this function acts on</param>
        /// <returns>True if the string is not null or not only whitespace</returns>
        public static bool IsNotNullOrWhiteSpace(string str) => !string.IsNullOrWhiteSpace(str);
    }
}
