using System;
using System.Text.RegularExpressions;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// Turns whatever the user typed into something the engine can load.
    /// Pure logic, no Unity types: this is the part of the browser that is
    /// worth unit-testing on a build machine with no headset attached.
    /// </summary>
    public static class UrlUtility
    {
        /// <summary>Matches "host.tld", "host.tld/path", "sub.host.tld:8080" without a scheme.</summary>
        private static readonly Regex BareHost = new Regex(
            @"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]*[a-zA-Z0-9])?\.)+[a-zA-Z]{2,63}(?::\d{1,5})?(?:[/?#].*)?$");

        /// <summary>Matches "localhost", "localhost:8080", and raw IPv4 with optional port.</summary>
        private static readonly Regex LocalHost = new Regex(
            @"^(?:localhost|\d{1,3}(?:\.\d{1,3}){3})(?::\d{1,5})?(?:[/?#].*)?$");

        /// <summary>Schemes we are willing to hand to the engine directly.</summary>
        private static readonly string[] AllowedSchemes =
        {
            "http://", "https://", "file://", "about:", "data:",
        };

        /// <summary>
        /// Resolve raw address-bar text into a loadable URL.
        /// Anything that does not look like a location becomes a search query.
        /// </summary>
        /// <param name="input">Raw text from the address bar.</param>
        /// <param name="searchTemplate">
        /// Search URL with a single "{0}" placeholder for the URL-encoded query.
        /// </param>
        public static string Resolve(string input, string searchTemplate)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var trimmed = input.Trim();

            foreach (var scheme in AllowedSchemes)
            {
                if (trimmed.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                    return trimmed;
            }

            // Checked before the scheme rejection below: "localhost:8080" parses
            // as scheme "localhost", and a dev server is the last thing that
            // should silently turn into a web search.
            if (LocalHost.IsMatch(trimmed))
                return "http://" + trimmed;

            // A scheme we do not allow (javascript:, intent:, content:, ...) is
            // never forwarded blindly - it is treated as a search term instead.
            if (HasScheme(trimmed))
                return Search(trimmed, searchTemplate);

            if (!trimmed.Contains(" ") && BareHost.IsMatch(trimmed))
                return "https://" + trimmed;

            return Search(trimmed, searchTemplate);
        }

        public static string Search(string query, string searchTemplate)
        {
            if (string.IsNullOrEmpty(searchTemplate))
                searchTemplate = DefaultSearchTemplate;

            return string.Format(searchTemplate, Uri.EscapeDataString(query ?? string.Empty));
        }

        public const string DefaultSearchTemplate = "https://duckduckgo.com/?q={0}";

        /// <summary>Host only, for a compact address bar. Falls back to the raw string.</summary>
        public static string DisplayName(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            return Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host)
                ? uri.Host
                : url;
        }

        public static bool IsSecure(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);

        private static bool HasScheme(string value)
        {
            var colon = value.IndexOf(':');
            if (colon <= 0)
                return false;

            for (var i = 0; i < colon; i++)
            {
                var c = value[i];
                var valid = char.IsLetterOrDigit(c) || c == '+' || c == '-' || c == '.';
                if (!valid)
                    return false;
            }

            return char.IsLetter(value[0]);
        }
    }
}
