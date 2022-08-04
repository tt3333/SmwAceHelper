using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmwAceHelper.Utilities
{
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type valueType = value.GetType();
            if (parameter is string paramString)
            {
                if (valueType.IsEnum && Enum.IsDefined(valueType, paramString))
                {
                    parameter = Enum.Parse(valueType, paramString);
                    return value.Equals(parameter);
                }
                else if (int.TryParse(paramString, out int intParam))
                {
                    parameter = intParam;
                }
            }
            if (parameter is int)
            {
                if (valueType.IsEnum || value is int)
                {
                    return (int)value == (int)parameter;
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string paramString)
            {
                if (targetType.IsEnum && Enum.IsDefined(targetType, paramString))
                {
                    return Enum.Parse(targetType, paramString);
                }
                else if (int.TryParse(paramString, out int intParam))
                {
                    parameter = intParam;
                }
            }
            if (parameter is int)
            {
                return parameter;
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
