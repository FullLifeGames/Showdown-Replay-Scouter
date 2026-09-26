using System.Linq;

namespace ShowdownReplayScouter.Core.Util
{
    /// <summary>
    /// Values with several observed alternatives, e.g. an item "Leftovers | Life Orb".
    /// </summary>
    internal static class Alternatives
    {
        public const string Separator = " | ";

        /// <summary>
        /// Merges two lists of alternatives, keeping the order of first occurrence and dropping duplicates.
        /// </summary>
        public static string? Merge(string? existing, string? addition)
        {
            if (string.IsNullOrEmpty(addition))
            {
                return existing;
            }
            if (string.IsNullOrEmpty(existing))
            {
                return addition;
            }
            var values = existing.Split(Separator).ToList();
            foreach (var value in addition.Split(Separator))
            {
                if (!values.Contains(value))
                {
                    values.Add(value);
                }
            }
            return string.Join(Separator, values);
        }

        /// <summary>
        /// The alternatives of <paramref name="after"/> that are not part of <paramref name="before"/>.
        /// </summary>
        public static string? Added(string? before, string? after)
        {
            if (string.IsNullOrEmpty(after))
            {
                return null;
            }
            var previous = before?.Split(Separator) ?? [];
            var added = after.Split(Separator).Except(previous).ToList();
            return added.Count > 0 ? string.Join(Separator, added) : null;
        }
    }
}
