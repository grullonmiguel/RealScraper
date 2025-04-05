using System;
using System.Diagnostics;

namespace RealScraper.Core.Services
{
    public static class ProcessService
    {
        public static string ErrorMessage { get; private set; }

        public static bool OpenUrl(string url)
        {
            bool result = false;
            ErrorMessage = string.Empty;
            try
            {
                if (!IsValidUrl(url))
                    throw new Exception($"Invalid URL: {url}. Skipping...");

                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                result = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error opening URL: {ex.Message}";
            }
            return result;
        }

        /// <summary>
        ///  Uri.TryCreate ensures the URL is well-formed
        ///  Only accepts  or  schemes (blocks invalid formats like ).
        ///  Prevents exceptions from malformed URLs.
        /// </summary>
        /// <param name="url"></param>
        /// <returns>true if successful</returns>
        public static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }


    }
}
