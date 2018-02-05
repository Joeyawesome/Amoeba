﻿using System;
using System.Globalization;
using System.Windows.Data;
using Omnius.Base;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(ManagerState), typeof(string))]
    class ManagerStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ManagerState type)
            {
                if (type == ManagerState.Start)
                {
                    return LanguagesManager.Instance.ManagerState_Start;
                }
                if (type == ManagerState.Stop)
                {
                    return LanguagesManager.Instance.ManagerState_Stop;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
