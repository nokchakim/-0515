using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using SPX_Weight;


namespace SPX_Weight
{
    class ValueToBruchConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            string temp = (string)value;
            double input = Double.Parse(temp);
            if (input > 504) return Brushes.LightBlue;
            else return DependencyProperty.UnsetValue;
            //switch (input)
            //{
            //    case "John":
            //        return Brushes.LightGreen;
            //    default:
            //        return DependencyProperty.UnsetValue;
            //}
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
