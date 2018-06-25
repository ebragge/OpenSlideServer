using SharpDX.WIC;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenSlideServer
{
    class Utilities
    {
        public static byte[] ImageDetails(string name)
        {
            var osr = OpenSlideInterface.Openslide_open(name);

            if (osr == IntPtr.Zero)
            {
                return null;
            }

            var vendor = Marshal.PtrToStringAnsi(OpenSlideInterface.Openslide_detect_vendor(name));
            var levels = OpenSlideInterface.Openslide_get_level_count(osr);

            var mpp_x = Marshal.PtrToStringAnsi(OpenSlideInterface.Openslide_get_property_value(osr, OpenSlideInterface.OPENSLIDE_PROPERTY_NAME_MPP_X));
            var mpp_y = Marshal.PtrToStringAnsi(OpenSlideInterface.Openslide_get_property_value(osr, OpenSlideInterface.OPENSLIDE_PROPERTY_NAME_MPP_X));

            var widths = new Int64[levels];
            var heights = new Int64[levels];
            var dimensions = "[\r\n\t\t";

            var offset = LoadText(name + ".txt");

            if (offset == null)
            {
                offset = "\"0,0\"";
            }

            unsafe
            {
                Int64 w, h;
                Int64* pw = &w;
                Int64* ph = &h;

                for (var l = 0; l < levels; l++)
                {
                    OpenSlideInterface.Openslide_get_level_dimensions(osr, l, pw, ph);
                    widths[l] = w;
                    heights[l] = h;
                    dimensions += "\"" + l.ToString() + "," 
                        + w.ToString() + "," 
                        + h.ToString() + "\"" 
                        + ((l < levels - 1) ? ",\r\n\t\t" : "");
                }
            }
            dimensions += "\r\n\t]";

            OpenSlideInterface.Openslide_close(osr);

            var json = "{\r\n\t\"Name\":\"" + Path.GetFileName(name)
                + "\", \r\n\t\"Vendor\":\"" + vendor
                + "\", \r\n\t\"Microns per pixel X\":\"" + mpp_x
                + "\", \r\n\t\"Microns per pixel Y\":\"" + mpp_y
                + "\", \r\n\t\"Levels\":" + levels.ToString()
                + ", \r\n\t\"Width\":" + widths[0].ToString()
                + ", \r\n\t\"Height\":" + heights[0].ToString()
                + ", \r\n\t\"Dimensions\":" + dimensions
                + ", \r\n\t\"Offset\":" + offset + "\r\n}";

            return Encoding.ASCII.GetBytes(json);
        }

        public static byte[] CreateRegion(string name, Int32 level, Int64 x, Int64 y, Int32 w, Int32 h, string format, bool mirror = false)
        {
            var osr = OpenSlideInterface.Openslide_open(name);

            if (osr == IntPtr.Zero)
            {
                Console.WriteLine("osr == IntPtr.Zero");
                return null;
            }

            var xx = x;
            Int64 multiplier = 1L;

            if (mirror)
            {
                unsafe
                {
                    Int64 tw, th;
                    Int64* pw = &tw;
                    Int64* ph = &th;

                    OpenSlideInterface.Openslide_get_level_dimensions(osr, 0, pw, ph);

                    xx = tw;

                    OpenSlideInterface.Openslide_get_level_dimensions(osr, level, pw, ph);

                    multiplier = xx / tw;
                }
            }

            if (mirror)
            {
                xx -= x;
                xx -= (w * multiplier);
            }

            var buffer = Marshal.AllocHGlobal(4 * w * h);

            OpenSlideInterface.Openslide_read_region(osr, buffer, xx, y, level, w, h);
            OpenSlideInterface.Openslide_close(osr);

            byte[] bytes = null;

            if (format == null || format.Equals("PNG"))
            {
                bytes = CreateFormat(buffer, w, h, System.Drawing.Imaging.ImageFormat.Png, mirror);
            }
            else if (format.Equals("RAW"))
            {
                bytes = CreateRaw(buffer, w, h, mirror);
            }
            else if (format.Equals("JPG"))
            {
                bytes = CreateFormat(buffer, w, h, System.Drawing.Imaging.ImageFormat.Jpeg, mirror);
            }
            else if (format.Equals("BMP"))
            {
                bytes = CreateFormat(buffer, w, h, System.Drawing.Imaging.ImageFormat.Bmp, mirror);
            }

            Marshal.FreeHGlobal(buffer);

            return bytes;
        }

        static byte[] CreateFormat(IntPtr pixels, int width, int height, System.Drawing.Imaging.ImageFormat format, bool mirror)
        {
            using (var bitmap = new System.Drawing.Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, pixels))
            {
                if (mirror)
                {
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                }

                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, format);
                    return stream.ToArray();
                }
            }
        }

        static byte[] CreateRaw(IntPtr pixels, int width, int height, bool mirror = false)
        {
            using (var bitmap = new System.Drawing.Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, pixels))
            {
                if (mirror)
                {
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                }

                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);

                    var factory = new ImagingFactory2();
                    using (var decoder = new BitmapDecoder(factory, stream, DecodeOptions.CacheOnDemand))
                    {
                        using (var formatConverter = new FormatConverter(factory))
                        {
                            formatConverter.Initialize(decoder.GetFrame(0), PixelFormat.Format32bppPRGBA);

                            var stride = formatConverter.Size.Width * 4;
                            using (var dataStream = new SharpDX.DataStream(formatConverter.Size.Height * stride, true, true))
                            {
                                formatConverter.CopyPixels(stride, dataStream);

                                byte[] b;

                                using (BinaryReader br = new BinaryReader(dataStream))
                                {
                                    b = br.ReadBytes((int)dataStream.Length);
                                }

                                return b;
                            }
                        }
                    }
                }
            }
        }

        public static byte[] LoadFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                return File.ReadAllBytes(fileName);
            }
            return null;
        }

        public static string LoadText(string fileName)
        {
            if (File.Exists(fileName))
            {
                return File.ReadAllText(fileName);
            }
            return null;
        }

        public static void SaveFile(string fileName, byte[] data)
        {
            File.WriteAllBytes(fileName, data);
        }

        public static byte[] CreateMacro(string name, int x, int y, int w, int h, string format)
        {
            if (format == null || format.Equals("PNG"))
            {
                return Crop(name, w, h, x, y, System.Drawing.Imaging.ImageFormat.Png);
            }
            else if (format.Equals("JPG"))
            {
                return Crop(name, w, h, x, y, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            else if (format.Equals("BMP"))
            {
                return Crop(name, w, h, x, y, System.Drawing.Imaging.ImageFormat.Bmp);
            }

            return null;
        }

        public static byte[] Crop(string fileName, int width, int height, int x, int y, System.Drawing.Imaging.ImageFormat format)
        {
            using (System.Drawing.Bitmap src = Image.FromFile(fileName) as System.Drawing.Bitmap)
            {
                using (System.Drawing.Bitmap target = new System.Drawing.Bitmap(width, height))
                {
                    using (Graphics g = Graphics.FromImage(target))
                    {
                        g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height),
                                         new Rectangle(x, y, width, height),
                                         GraphicsUnit.Pixel);
                    }

                    using (var stream = new MemoryStream())
                    {
                        target.Save(stream, format);
                        return stream.ToArray();
                    }
                }
            }
        }

        public static byte[] MacroDetails(string name)
        {
            using (System.Drawing.Bitmap src = Image.FromFile(name) as System.Drawing.Bitmap)
            {
                var width = src.Width;
                var height = src.Height;

                var json = "{\r\n\t\"Name\":\"" + Path.GetFileName(name)
                    + "\", \r\n\t\"Width\":" + width.ToString()
                    + ", \r\n\t\"Height\":" + height.ToString() + "\r\n}";

                return Encoding.ASCII.GetBytes(json);
            }
        }
    }
}
