using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TwainControl
{
    public static class Picture
    {
        public enum Format
        {
            Tiff,

            TiffRenkli,

            Jpg,

            Png
        }

        public static Bitmap ConvertBlackAndWhite(this Bitmap bitmap, int bWthreshold, bool grayscale = false)
        {
            unsafe
            {
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                int heightInPixels = bitmapData.Height;
                int widthInBytes = bitmapData.Width * bytesPerPixel;
                byte* ptrFirstPixel = (byte*)bitmapData.Scan0;
                Parallel.For(0, heightInPixels, y =>
                {
                    byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
                    for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                    {
                        byte gray = (byte)((currentLine[x] * 0.299) + (currentLine[x + 1] * 0.587) + (currentLine[x + 2] * 0.114));
                        if (grayscale)
                        {
                            currentLine[x] = gray;
                            currentLine[x + 1] = gray;
                            currentLine[x + 2] = gray;
                        }
                        else
                        {
                            currentLine[x] = (byte)(gray < bWthreshold ? 0 : 255);
                            currentLine[x + 1] = (byte)(gray < bWthreshold ? 0 : 255);
                            currentLine[x + 2] = (byte)(gray < bWthreshold ? 0 : 255);
                        }
                    }
                });
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        public static BitmapSource Resize(this BitmapSource bfPhoto, double nWidth, double nHeight, double rotate = 0, int dpiX = 96, int dpiY = 96)
        {
            RotateTransform rotateTransform = new RotateTransform(rotate);
            ScaleTransform scaleTransform = new ScaleTransform(nWidth / 96 * dpiX / bfPhoto.PixelWidth, nHeight / 96 * dpiY / bfPhoto.PixelHeight, 0, 0);
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(scaleTransform);
            TransformedBitmap tb = new TransformedBitmap(bfPhoto, transformGroup);
            tb.Freeze();
            return tb;
        }

        public static string SetUniqueFile(this string path, string file, string extension)
        {
            int i;
            for (i = 0; File.Exists($"{path}\\{file}_{i}.{extension}"); i++)
            {
                _ = i + 1;
            }

            return $"{path}\\{file}_{i}.{extension}";
        }

        public static BitmapImage ToBitmapImage(this Image bitmap, ImageFormat format, double decodeheight = 0)
        {
            if (bitmap != null)
            {
                MemoryStream memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, format);
                memoryStream.Position = 0;
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                if (decodeheight != 0)
                {
                    image.DecodePixelHeight = bitmap.Height > (int)decodeheight ? (int)decodeheight : bitmap.Height;
                }

                image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                image.CacheOption = BitmapCacheOption.None;
                image.StreamSource = memoryStream;
                image.EndInit();
                bitmap.Dispose();
                if (!image.IsFrozen && image.CanFreeze)
                {
                    image.Freeze();
                }
                memoryStream = null;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

                return image;
            }

            return null;
        }

        public static byte[] ToTiffJpegByteArray(this BitmapSource bitmapsource, Format format)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                switch (format)
                {
                    case Format.TiffRenkli:
                        {
                            TiffBitmapEncoder encoder = new TiffBitmapEncoder { Compression = TiffCompressOption.Zip };
                            encoder.Frames.Add(BitmapFrame.Create(bitmapsource));
                            encoder.Save(outStream);
                            return outStream.ToArray();
                        }
                    case Format.Tiff:
                        {
                            TiffBitmapEncoder encoder = new TiffBitmapEncoder { Compression = TiffCompressOption.Ccitt4 };
                            encoder.Frames.Add(BitmapFrame.Create(bitmapsource));
                            encoder.Save(outStream);
                            return outStream.ToArray();
                        }
                    case Format.Jpg:
                        {
                            JpegBitmapEncoder encoder = new JpegBitmapEncoder { QualityLevel = 75 };
                            encoder.Frames.Add(BitmapFrame.Create(bitmapsource));
                            encoder.Save(outStream);
                            return outStream.ToArray();
                        }
                    case Format.Png:
                        {
                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmapsource));
                            encoder.Save(outStream);
                            return outStream.ToArray();
                        }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(format), format, null);
                }
            }
        }
    }
}