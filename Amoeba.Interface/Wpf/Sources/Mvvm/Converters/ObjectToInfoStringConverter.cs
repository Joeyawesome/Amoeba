using System;
using System.Globalization;
using System.Windows.Data;
using Amoeba.Messages;
using Amoeba.Rpc;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(object), typeof(string))]
    class ObjectToInfoStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Seed seed)
            {
                return ItemUtils.ToInfoMessage(seed);
            }
            else if (value is Box box)
            {
                return ItemUtils.ToInfoMessage(box);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
