using Playnite.SDK;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CommonPluginsShared
{
    public enum ImageColor
    {
        None = 0,
        Gray = 1,
        Black = 2
    }


    public class ImageProperty
    {
        public int Width { get; set; } 
        public int Height { get; set; } 
    }


    public class ImageTools
    {
        private static readonly ILogger logger = LogManager.GetLogger();


        #region ImageProperty
        public static ImageProperty GetImapeProperty(string srcPath)
        {
            try
            {
                using (Image image = Image.FromFile(srcPath))
                {
                    return new ImageProperty
                    {
                        Width = image.Width,
                        Height = image.Height,
                    };
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true, $"Error on GetImapeProperty({srcPath})");
                logger.Error("Error on GetImapeProperty()");
                return null;
            }
        }

        public static ImageProperty GetImapeProperty(Stream imgStream)
        {
            try
            {
                using (Image image = Image.FromStream(imgStream))
                {
                    return new ImageProperty
                    {
                        Width = image.Width,
                        Height = image.Height,
                    };
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true);
                logger.Error("Error on GetImapeProperty()");
                return null;
            }
        }

        public static ImageProperty GetImapeProperty(Image image)
        {
            try
            {
                return new ImageProperty
                {
                    Width = image.Width,
                    Height = image.Height,
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true);
                logger.Error("Error on GetImapeProperty()");
                return null;
            }
        }
        #endregion


        #region Resize
        public static string Resize(string srcPath, int width, int height)
        {
            try
            {
                Image image = Image.FromFile(srcPath);
                Bitmap resultImage = Resize(image, width, height);
                string newPath = srcPath.Replace(".png", "_" + width + "x" + height + ".png");
                resultImage.Save(newPath);

                image.Dispose();
                resultImage.Dispose();

                return newPath;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }

        public static bool Resize(Stream imgStream, int width, int height, string path)
        {
            try
            {
                Common.LogDebug(true, $"Resize: {path}.png");

                Image image = Image.FromStream(imgStream);
                Bitmap resultImage = Resize(image, width, height);
                resultImage.Save(path + ".png");

                image.Dispose();
                resultImage.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true);
                return false;
            }
        }

        public static Bitmap Resize(Image image, int width, int height)
        {
            Rectangle destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        #endregion


        #region Convert
        public static FormatConvertedBitmap ConvertBitmapImage(BitmapImage IconImage, ImageColor imageColor = ImageColor.None)
        {
            if (IconImage is null)
            {
                return null;
            }


            FormatConvertedBitmap ConvertBitmapSource = new FormatConvertedBitmap();

            try
            {
                ConvertBitmapSource.BeginInit();
                ConvertBitmapSource.Source = IconImage;

                switch (imageColor)
                {
                    case ImageColor.Gray:
                        ConvertBitmapSource.DestinationFormat = PixelFormats.Gray32Float;
                        break;

                    case ImageColor.Black:
                        ConvertBitmapSource.Source = IconImage;
                        break;
                }

                ConvertBitmapSource.EndInit();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return ConvertBitmapSource;
        }

        public static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

        public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        public static BitmapImage ConvertImageToBitmapImage(Image image)
        {
            using (var memory = new MemoryStream())
            {
                image.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
        #endregion
    }
}
