using System;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WebpToJpg
{
    internal static class JpegConverter
    {
        public static string ConvertFile(string source, string folder, int quality)
        {
            using (var input = File.OpenRead(source))
            {
                var header = new byte[12];
                if (input.Read(header, 0, 12) != 12 ||
                    Encoding.ASCII.GetString(header, 0, 4) != "RIFF" ||
                    Encoding.ASCII.GetString(header, 8, 4) != "WEBP")
                    throw new InvalidDataException("This file is not a WebP image.");

                input.Position = 0;
                BitmapDecoder decoder;
                try
                {
                    decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnDemand);
                }
                catch (NotSupportedException ex)
                {
                    throw new InvalidDataException("Windows could not decode this WebP image. Check the file and make sure WebP Image Extensions is installed.", ex);
                }

                var frame = decoder.Frames[0];
                // Pbgra32 stores channels already multiplied by alpha.
                var pixels = new FormatConvertedBitmap(frame, PixelFormats.Pbgra32, null, 0);
                int stride = checked(pixels.PixelWidth * 4);
                var buffer = new byte[checked(stride * pixels.PixelHeight)];
                pixels.CopyPixels(buffer, stride, 0);
                for (int i = 0; i < buffer.Length; i += 4)
                {
                    int white = 255 - buffer[i + 3];
                    buffer[i] = (byte)(buffer[i] + white);
                    buffer[i + 1] = (byte)(buffer[i + 1] + white);
                    buffer[i + 2] = (byte)(buffer[i + 2] + white);
                }
                var opaque = BitmapSource.Create(pixels.PixelWidth, pixels.PixelHeight,
                    pixels.DpiX, pixels.DpiY, PixelFormats.Bgr32, null, buffer, stride);
                var encoder = new JpegBitmapEncoder { QualityLevel = quality };
                encoder.Frames.Add(BitmapFrame.Create(opaque));
                return SaveNew(encoder, folder, Path.GetFileNameWithoutExtension(source));
            }
        }

        private static string SaveNew(JpegBitmapEncoder encoder, string folder, string name)
        {
            string temp = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".tmp");
            var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            try
            {
                using (output) { encoder.Save(output); }
                for (int n = 0; ; n++)
                {
                    string file = n == 0 ? name + ".jpg" : string.Format("{0} ({1}).jpg", name, n);
                    string target = Path.Combine(folder, file);
                    try { File.Move(temp, target); return target; }
                    catch (IOException) when (File.Exists(target) || Directory.Exists(target)) { }
                }
            }
            finally { File.Delete(temp); }
        }
    }
}
