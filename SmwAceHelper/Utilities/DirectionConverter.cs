using SmwAceHelper.Models;
using SmwAceHelper.Properties;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WPFLocalizeExtension.Extensions;

namespace SmwAceHelper.Utilities
{
    [ValueConversion(typeof(PlayerDirection), typeof(string))]
    public class DirectionConverter : IValueConverter
    {
        /// <summary>
        /// Convert PlayerDirection to string.
        /// </summary>
        /// <param name="value">Player direction</param>
        /// <param name="targetType">unused</param>
        /// <param name="parameter">unused</param>
        /// <param name="culture">unused</param>
        /// <returns>String representing the direction of the player.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlayerDirection direction)
            {
                switch (direction)
                {
                    case PlayerDirection.Left:
                        return LocExtension.GetLocalizedValue<string>(nameof(StringResources.DIRECTION_LEFT));

                    case PlayerDirection.Right:
                        return LocExtension.GetLocalizedValue<string>(nameof(StringResources.DIRECTION_RIGHT));

                    default:
                        break;
                }
            }
            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// Convert string to PlayerDirection.
        /// </summary>
        /// <param name="value">String representing the direction of the player.</param>
        /// <param name="targetType">unused</param>
        /// <param name="parameter">unused</param>
        /// <param name="culture">unused</param>
        /// <returns>Player direction</returns>
        /// <remarks>This overload is implementation of IValueConverter.</remarks>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertBack(value);
        }

        /// <summary>
        /// Convert string to PlayerDirection.
        /// </summary>
        /// <param name="value">String representing the direction of the player.</param>
        /// <returns>Player direction</returns>
        /// <remarks>This overload is used for pasting.</remarks>
        public static object ConvertBack(object value)
        {
            foreach(CultureInfo culture in Model.Languages)
            {
                if (LocExtension.GetLocalizedValue<string>(nameof(StringResources.DIRECTION_LEFT), culture).Equals(value))
                {
                    return PlayerDirection.Left;
                }
                if (LocExtension.GetLocalizedValue<string>(nameof(StringResources.DIRECTION_RIGHT), culture).Equals(value))
                {
                    return PlayerDirection.Right;
                }
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
