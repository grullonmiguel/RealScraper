using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RealScraper.Core.Base
{
    /// <summary>
    /// A base class for observable objects, implementing INotifyPropertyChanged.
    /// </summary>
    public class Observable : INotifyPropertyChanged
    {
        /// <summary>
        /// Raised when a property's value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets the value of a property and raises the PropertyChanged event if the value changes.
        /// </summary>
        protected void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return;

            storage = value;
            OnPropertyChanged(propertyName ?? string.Empty); // Ensure propertyName is never null
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
