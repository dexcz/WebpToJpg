using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WebpToJpg
{
    internal static class WebpConverter
    {
        public static string ConvertFile(string source, string folder, int quality)
        {
            source = Path.GetFullPath(source);
            using (var input = File.OpenRead(source))
            {
                if (input.ReadByte() != 0xff || input.ReadByte() != 0xd8 || input.ReadByte() != 0xff)
                    throw new InvalidDataException("This file is not a JPEG image.");
                input.Position = 0;
                var frame = new JpegBitmapDecoder(input, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0];
                var metadata = frame.Metadata as BitmapMetadata;
                object orientation = metadata == null ? null : metadata.GetQuery("/app1/ifd/{ushort=274}");
                return ConversionOutput.Save(folder, Path.GetFileNameWithoutExtension(source), ".webp", output =>
                {
                    var transform = OrientationTransform(orientation is ushort ? (ushort)orientation : 1);
                    if (transform == null) Encode(source, quality, output);
                    else EncodeOriented(frame, transform, quality, output);
                });
            }
        }

        private static MatrixTransform OrientationTransform(int orientation)
        {
            switch (orientation)
            {
                case 2: return new MatrixTransform(-1, 0, 0, 1, 0, 0);
                case 3: return new MatrixTransform(-1, 0, 0, -1, 0, 0);
                case 4: return new MatrixTransform(1, 0, 0, -1, 0, 0);
                case 5: return new MatrixTransform(0, 1, 1, 0, 0, 0);
                case 6: return new MatrixTransform(0, 1, -1, 0, 0, 0);
                case 7: return new MatrixTransform(0, -1, -1, 0, 0, 0);
                case 8: return new MatrixTransform(0, -1, 1, 0, 0, 0);
                default: return null;
            }
        }

        private static void EncodeOriented(BitmapFrame frame, MatrixTransform transform, int quality, Stream output)
        {
            string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(new TransformedBitmap(frame, transform), null, null, frame.ColorContexts));
            var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            try
            {
                using (file) { png.Save(file); }
                Encode(temp, quality, output);
            }
            finally { File.Delete(temp); }
        }

        private static void Encode(string source, int quality, Stream output)
        {
            string appFolder = Path.GetDirectoryName(typeof(WebpConverter).Assembly.Location);
            string tool = Path.Combine(appFolder, "Tools", "WebP", "cwebp.exe");
            if (!File.Exists(tool))
                throw new InvalidDataException("The WebP encoder is missing. Keep the Tools folder next to WebpToJpg.exe.");
            var options = new ProcessStartInfo
            {
                FileName = tool, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                Arguments = "-quiet -q " + quality.ToString(CultureInfo.InvariantCulture) +
                    " -metadata icc -o - -- \"" + source + "\""
            };
            try
            {
                using (var process = Process.Start(options))
                {
                    var errors = process.StandardError.ReadToEndAsync();
                    using (var encoded = process.StandardOutput.BaseStream) { encoded.CopyTo(output); }
                    process.WaitForExit();
                    errors.GetAwaiter().GetResult();
                    if (process.ExitCode != 0 || output.Position == 0)
                        throw new InvalidDataException("Could not encode this image as WebP. Check that the JPEG is valid and each dimension is at most 16,383 pixels.");
                }
            }
            catch (Win32Exception ex)
            {
                throw new InvalidDataException("Windows could not start the bundled x64 WebP encoder. " + ex.Message, ex);
            }
        }
    }
}
