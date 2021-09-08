using CommonPluginsShared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace CommonPluginsShared.Converters
{
    public class ImageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is string && !((string)values[0]).IsNullOrEmpty() && File.Exists((string)values[0]))
            {
                string[] extensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".jfif", ".tga", ".webp" };
                if (!extensions.Contains(Path.GetExtension((string)values[0])))
                {
                    return values[0];
                }

                BitmapLoadProperties bitmapLoadProperties = null;
                if (parameter is string && (string)parameter == "-")
                {
                    bitmapLoadProperties = null;
                }
                if (parameter is string && (string)parameter == "1")
                {
                    bitmapLoadProperties = new BitmapLoadProperties(100, 0)
                    {
                        Source = (string)values[0]
                    };
                }
                if (parameter is string && (string)parameter == "2")
                {
                    bitmapLoadProperties = new BitmapLoadProperties(200, 0)
                    {
                        Source = (string)values[0]
                    };
                }
                if (parameter is string && (string)parameter == "3")
                {
                    bitmapLoadProperties = new BitmapLoadProperties(300, 0)
                    {
                        Source = (string)values[0]
                    };
                }
                if (parameter is string && (string)parameter == "4")
                {
                    bitmapLoadProperties = new BitmapLoadProperties(400, 0)
                    {
                        Source = (string)values[0]
                    };
                }
                if (parameter is string && (string)parameter == "0")
                {
                    double ActualHeight = (double)values[1];

                    if (ActualHeight < 100)
                    {
                        bitmapLoadProperties = new BitmapLoadProperties(100, 0)
                        {
                            Source = (string)values[0]
                        };
                    }
                    else if (ActualHeight < 200)
                    {
                        bitmapLoadProperties = new BitmapLoadProperties(200, 0)
                        {
                            Source = (string)values[0]
                        };
                    }
                    else if (ActualHeight < 300)
                    {
                        bitmapLoadProperties = new BitmapLoadProperties(300, 0)
                        {
                            Source = (string)values[0]
                        };
                    }
                    else if (ActualHeight < 400)
                    {
                        bitmapLoadProperties = new BitmapLoadProperties(400, 0)
                        {
                            Source = (string)values[0]
                        };
                    }
                    else if (ActualHeight < 500)
                    {
                        bitmapLoadProperties = new BitmapLoadProperties(500, 0)
                        {
                            Source = (string)values[0]
                        };
                    }
                    else if (ActualHeight < 600)
                    {
                        bitmapLoadProperties = new BitmapLoadProperties(600, 0)
                        {
                            Source = (string)values[0]
                        };
                    }
                    else if (ActualHeight >= 600)
                    {
                        bitmapLoadProperties = null;
                    }
                    else
                    {
                        bitmapLoadProperties = new BitmapLoadProperties(200, 0)
                        {
                            Source = (string)values[0]
                        };
                    }
                }


                if (((string)values[0]).EndsWith(".tga", StringComparison.OrdinalIgnoreCase))
                {
                    BitmapImage bitmapImage = BitmapExtensions.TgaToBitmap((string)values[0]);

                    if (bitmapLoadProperties == null)
                    {
                        return bitmapImage;
                    }
                    else
                    {
                        return bitmapImage.GetClone(bitmapLoadProperties);
                    }
                }


                if (bitmapLoadProperties == null)
                {
                    return BitmapExtensions.BitmapFromFile((string)values[0]);
                }
                else
                {
                    return BitmapExtensions.BitmapFromFile((string)values[0], bitmapLoadProperties);
                }
            }

            return values[0];
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
