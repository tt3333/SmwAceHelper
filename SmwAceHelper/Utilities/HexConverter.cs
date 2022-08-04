using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmwAceHelper.Utilities
{
    [ValueConversion(typeof(short), typeof(string))]
    public class HexConverter : IValueConverter
    {
        /// <summary>
        /// Convert short value to hexadecimal string.
        /// </summary>
        /// <param name="value">short value</param>
        /// <param name="targetType">unused</param>
        /// <param name="parameter">unused</param>
        /// <param name="culture">unused</param>
        /// <returns>Hexadecimal string</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is short shortValue)
            {
                return shortValue.ToString("X4");
            }
            else if (value == null)
            {
                return string.Empty;
            }
            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// Convert hexadecimal string to short value.
        /// </summary>
        /// <param name="value">Hexadecimal string</param>
        /// <param name="targetType">unused</param>
        /// <param name="parameter">unused</param>
        /// <param name="culture">unused</param>
        /// <returns>short value</returns>
        /// <remarks>This overload is implementation of IValueConverter.</remarks>
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertBack(value);
        }

        /// <summary>
        /// Convert hexadecimal string to short value.
        /// </summary>
        /// <param name="value">Hexadecimal string</param>
        /// <returns>short value</returns>
        /// <remarks>This overload is used for pasting.</remarks>
        public static object? ConvertBack(object value)
        {
            if (value is string stringValue)
            {
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }
                try
                {
                    return System.Convert.ToInt16(stringValue, 16);
                }
                catch
                {
                }
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
