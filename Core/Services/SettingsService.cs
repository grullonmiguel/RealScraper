namespace RealScraper.Core.Services
{
    public static class SettingsService
    {
        /// <summary>
        /// Saves a value to the application's settings.
        /// </summary>
        /// <typeparam name="T">The type of the value to save.</typeparam>
        /// <param name="key">The name of the setting.</param>
        /// <param name="value">The value to save.</param>
        public static void SaveSetting<T>(string key, T value)
        {
            // Access and modify settings dynamically
            Properties.Settings.Default[key] = value;
            Properties.Settings.Default.Save(); // Persist the changes
        }

        /// <summary>
        /// Retrieves a value from the application's settings.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="key">The name of the setting.</param>
        /// <param name="defaultValue">The default value to return if the setting doesn't exist.</param>
        /// <returns>The value retrieved from the settings file.</returns>
        public static T LoadSetting<T>(string key, T defaultValue = default)
        {
            // Retrieve the setting and cast to the specified type
            object value = Properties.Settings.Default[key];
            return value is T ? (T)value : defaultValue;
        }
    }
}
