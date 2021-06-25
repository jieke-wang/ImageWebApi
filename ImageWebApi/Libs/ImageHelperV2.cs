using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ImageWebApi.Libs
{
    public class ImageHelperV2
    {
        #region Pad
        public static Bitmap Pad(Bitmap original, int width, int height, Color backgrouColor)
        {
            float widthRate = (float)width / original.Width;
            float heightRate = (float)height / original.Height;
            float scaleFactor = Math.Min(widthRate, heightRate);
            float drawWidth = original.Width * scaleFactor;
            float drawHeight = original.Height * scaleFactor;

            float x = Math.Abs(drawWidth - width) / 2;
            float y = Math.Abs(drawHeight - height) / 2;

            ImageFormat imageFormat = original.RawFormat;

            ImageCodecInfo encoder = FindEncoder(imageFormat);
            if (encoder == null)
                encoder = FindEncoder(ImageFormat.Png);

            bool isPng = string.Equals("png", encoder.FormatDescription, StringComparison.OrdinalIgnoreCase);

            original = CheckAndFixImage(original, isPng);

            PixelFormat pixelFormat = original.PixelFormat;

            Bitmap destination = new Bitmap(width, height, pixelFormat);
            try
            {
                destination.SetResolution(original.HorizontalResolution, original.VerticalResolution);
            }
            catch
            {
                destination.SetResolution(96, 96);
            }

            using (var graphic = Graphics.FromImage(destination))
            {
                graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                //if (isPng)
                //    graphic.Clear(Color.Transparent);
                //else
                //    graphic.Clear(Color.White);
                //graphic.Clear(backgrouColor);
                //graphic.Clear(Color.Transparent);

                using SolidBrush brush = new SolidBrush(backgrouColor);
                graphic.FillRectangle(brush, new Rectangle(0, 0, destination.Width, destination.Height));

                graphic.DrawImage(original, x, y, drawWidth, drawHeight);
            }

            original.Dispose();

            //if (isPng)
            //    destination.MakeTransparent(backgrouColor);

            if (isPng)
                encoder = FindEncoder(ImageFormat.Png);
            else
                encoder = FindEncoder(ImageFormat.Jpeg);

            MemoryStream ms = new MemoryStream();
            destination.Save(ms, encoder, null);
            ms.Seek(0, SeekOrigin.Begin);
            destination.Dispose();

            destination = Bitmap.FromStream(ms, true) as Bitmap;
            if (isPng)
                ms.Dispose();

            return destination;
        }

        public static Bitmap Pad(Bitmap original, int width, int height)
        {
            return Pad(original, width, height, Color.White);
        }

        public static Bitmap Pad(Stream stream, int width, int height)
        {
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            Bitmap original = Bitmap.FromStream(stream, true) as Bitmap;

            return Pad(original, width, height);
        }

        public static Bitmap Pad(byte[] data, int width, int height)
        {
            MemoryStream memoryStream = new MemoryStream(data);

            return Pad(memoryStream, width, height);
        }

        public static Bitmap Pad(string fullFilename, int width, int height)
        {
            Bitmap original = Bitmap.FromFile(fullFilename, true) as Bitmap;
            return Pad(original, width, height);
        }
        #endregion

        #region Crop

        public static Bitmap Crop(Bitmap original, int x, int y, int width, int height)
        {
            ImageFormat imageFormat = original.RawFormat;

            ImageCodecInfo encoder = FindEncoder(imageFormat);
            if (encoder == null)
                encoder = FindEncoder(ImageFormat.Png);

            bool isPng = string.Equals("png", encoder.FormatDescription, StringComparison.OrdinalIgnoreCase);

            original = CheckAndFixImage(original, isPng);

            PixelFormat pixelFormat = original.PixelFormat;

            Bitmap destination = new Bitmap(width, height, pixelFormat);
            try
            {
                destination.SetResolution(original.HorizontalResolution, original.VerticalResolution);
            }
            catch
            {
                destination.SetResolution(96, 96);
            }

            using (var graphic = Graphics.FromImage(destination))
            {
                graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                if (isPng)
                    graphic.Clear(Color.Transparent);
                else
                    graphic.Clear(Color.White);

                graphic.DrawImage(original, new Rectangle(0, 0, width, height), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
            }

            original.Dispose();

            if (isPng)
                encoder = FindEncoder(ImageFormat.Png);
            else
                encoder = FindEncoder(ImageFormat.Jpeg);

            MemoryStream ms = new MemoryStream();
            destination.Save(ms, encoder, null);
            ms.Seek(0, SeekOrigin.Begin);
            destination.Dispose();

            destination = Bitmap.FromStream(ms, true) as Bitmap;
            if (isPng)
                ms.Dispose();

            return destination;
        }

        public static Bitmap Crop(Stream stream, int x, int y, int width, int height)
        {
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            Bitmap original = Bitmap.FromStream(stream, true) as Bitmap;

            return Crop(original, x, y, width, height);
        }

        public static Bitmap Crop(byte[] data, int x, int y, int width, int height)
        {
            MemoryStream memoryStream = new MemoryStream(data);

            return Crop(memoryStream, x, y, width, height);
        }

        public static Bitmap Crop(string fullFilename, int x, int y, int width, int height)
        {
            Bitmap original = Bitmap.FromFile(fullFilename, true) as Bitmap;
            return Crop(original, x, y, width, height);
        }

        #endregion

        #region Zoom

        public static Bitmap Zoom(Bitmap original, int width, int height)
        {
            ImageFormat imageFormat = original.RawFormat;

            ImageCodecInfo encoder = FindEncoder(imageFormat);
            if (encoder == null)
                encoder = FindEncoder(ImageFormat.Png);

            bool isPng = string.Equals("png", encoder.FormatDescription, StringComparison.OrdinalIgnoreCase);

            original = CheckAndFixImage(original, isPng);

            PixelFormat pixelFormat = original.PixelFormat;

            Bitmap destination = new Bitmap(width, height, pixelFormat);
            try
            {
                destination.SetResolution(original.HorizontalResolution, original.VerticalResolution);
            }
            catch
            {
                destination.SetResolution(96, 96);
            }

            using (var graphic = Graphics.FromImage(destination))
            {
                graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                if (isPng)
                    graphic.Clear(Color.Transparent);
                else
                    graphic.Clear(Color.White);

                //graphic.DrawImage(original, new Rectangle(0, 0, width, height), new Rectangle(0, 0, width, height), GraphicsUnit.Pixel);

                graphic.DrawImage(original, new Rectangle(0, 0, width, height));
            }

            original.Dispose();

            if (isPng)
                encoder = FindEncoder(ImageFormat.Png);
            else
                encoder = FindEncoder(ImageFormat.Jpeg);

            MemoryStream ms = new MemoryStream();
            destination.Save(ms, encoder, null);
            ms.Seek(0, SeekOrigin.Begin);
            destination.Dispose();

            destination = Bitmap.FromStream(ms, true) as Bitmap;
            if (isPng)
                ms.Dispose();

            return destination;
        }

        public static Bitmap Zoom(Stream stream, int width, int height)
        {
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            Bitmap original = Bitmap.FromStream(stream, true) as Bitmap;

            return Zoom(original, width, height);
        }

        public static Bitmap Zoom(byte[] data, int width, int height)
        {
            MemoryStream memoryStream = new MemoryStream(data);

            return Zoom(memoryStream, width, height);
        }

        public static Bitmap Zoom(string fullFilename, int width, int height)
        {
            Bitmap original = Bitmap.FromFile(fullFilename, true) as Bitmap;
            return Zoom(original, width, height);
        }

        public static Bitmap Zoom(Bitmap original, float zoomFactor)
        {
            int width = Convert.ToInt32(original.Width * zoomFactor);
            int height = Convert.ToInt32(original.Height * zoomFactor);

            return Zoom(original, width, height);
        }

        public static Bitmap Zoom(Stream stream, float zoomFactor)
        {
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            Bitmap original = Bitmap.FromStream(stream, true) as Bitmap;

            return Zoom(original, zoomFactor);
        }

        public static Bitmap Zoom(byte[] data, float zoomFactor)
        {
            MemoryStream memoryStream = new MemoryStream(data);

            return Zoom(memoryStream, zoomFactor);
        }

        public static Bitmap Zoom(string fullFilename, float zoomFactor)
        {
            Bitmap original = Bitmap.FromFile(fullFilename, true) as Bitmap;
            return Zoom(original, zoomFactor);
        }

        #endregion

        #region Rotate

        // http://www.vcskicks.com/image-rotate.php

        public static Bitmap Rotate(Bitmap original, float angle)
        {
            ImageFormat imageFormat = original.RawFormat;

            ImageCodecInfo encoder = FindEncoder(imageFormat);
            if (encoder == null)
                encoder = FindEncoder(ImageFormat.Png);

            bool isPng = string.Equals("png", encoder.FormatDescription, StringComparison.OrdinalIgnoreCase);

            original = CheckAndFixImage(original, isPng);

            PixelFormat pixelFormat = original.PixelFormat;

            //Corners of the image
            PointF[] rotationPoints =
                { new PointF(0, 0),
                  new PointF(original.Width, 0),
                  new PointF(0, original.Height),
                  new PointF(original.Width, original.Height)};

            //Rotate the corners
            PointMath.RotatePoints(rotationPoints, new PointF(original.Width / 2.0f, original.Height / 2.0f), angle);

            //Get the new bounds given from the rotation of the corners
            //(avoid clipping of the image)
            Rectangle bounds = PointMath.GetBounds(rotationPoints);

            //An empy bitmap to draw the rotated image
            Bitmap destination = new Bitmap(bounds.Width, bounds.Height, pixelFormat);
            try
            {
                destination.SetResolution(original.HorizontalResolution, original.VerticalResolution);
            }
            catch
            {
                destination.SetResolution(96, 96);
            }

            using (var graphic = Graphics.FromImage(destination))
            {
                graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                if (isPng)
                    graphic.Clear(Color.Transparent);
                else
                    graphic.Clear(Color.White);


                //Transformation matrix
                Matrix m = new Matrix();
                m.RotateAt((float)angle, new PointF(original.Width / 2.0f, original.Height / 2.0f));
                m.Translate(-bounds.Left, -bounds.Top, MatrixOrder.Append); //shift to compensate for the rotation

                graphic.Transform = m;
                graphic.DrawImage(original, 0, 0);
            }

            original.Dispose();

            if (isPng)
                encoder = FindEncoder(ImageFormat.Png);
            else
                encoder = FindEncoder(ImageFormat.Jpeg);

            MemoryStream ms = new MemoryStream();
            destination.Save(ms, encoder, null);
            ms.Seek(0, SeekOrigin.Begin);
            destination.Dispose();

            destination = Bitmap.FromStream(ms, true) as Bitmap;
            if (isPng)
                ms.Dispose();

            return destination;
        }

        public static Bitmap Rotate(Stream stream, float angle)
        {
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            Bitmap original = Bitmap.FromStream(stream, true) as Bitmap;

            return Rotate(original, angle);
        }

        public static Bitmap Rotate(byte[] data, float angle)
        {
            MemoryStream memoryStream = new MemoryStream(data);

            return Rotate(memoryStream, angle);
        }

        public static Bitmap Rotate(string fullFilename, float angle)
        {
            Bitmap original = Bitmap.FromFile(fullFilename, true) as Bitmap;
            return Rotate(original, angle);
        }

        #endregion

        #region Flip

        // http://www.vcskicks.com/image-flip.php

        public static Bitmap Flip(Bitmap original, bool flipHorizontally, bool flipVertically)
        {
            ImageFormat imageFormat = original.RawFormat;

            ImageCodecInfo encoder = FindEncoder(imageFormat);
            if (encoder == null)
                encoder = FindEncoder(ImageFormat.Png);

            bool isPng = string.Equals("png", encoder.FormatDescription, StringComparison.OrdinalIgnoreCase);

            original = CheckAndFixImage(original, isPng);

            PixelFormat pixelFormat = original.PixelFormat;

            Bitmap destination = new Bitmap(original.Width, original.Height, pixelFormat);
            try
            {
                destination.SetResolution(original.HorizontalResolution, original.VerticalResolution);
            }
            catch
            {
                destination.SetResolution(96, 96);
            }

            using (var graphic = Graphics.FromImage(destination))
            {
                graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                if (isPng)
                    graphic.Clear(Color.Transparent);
                else
                    graphic.Clear(Color.White);

                //Matrix transformation
                Matrix m = null;
                if (flipVertically && flipHorizontally)
                {
                    m = new Matrix(-1, 0, 0, -1, 0, 0);
                    m.Translate(destination.Width, destination.Height, MatrixOrder.Append);
                }
                else if (flipVertically)
                {
                    m = new Matrix(1, 0, 0, -1, 0, 0);
                    m.Translate(0, destination.Height, MatrixOrder.Append);
                }
                else if (flipHorizontally)
                {
                    m = new Matrix(-1, 0, 0, 1, 0, 0);
                    m.Translate(destination.Width, 0, MatrixOrder.Append);
                }

                //Draw
                graphic.Transform = m;
                graphic.DrawImage(original, 0, 0);
            }

            original.Dispose();

            if (isPng)
                encoder = FindEncoder(ImageFormat.Png);
            else
                encoder = FindEncoder(ImageFormat.Jpeg);

            MemoryStream ms = new MemoryStream();
            destination.Save(ms, encoder, null);
            ms.Seek(0, SeekOrigin.Begin);
            destination.Dispose();

            destination = Bitmap.FromStream(ms, true) as Bitmap;
            if (isPng)
                ms.Dispose();

            return destination;
        }

        public static Bitmap Flip(Stream stream, bool flipHorizontally, bool flipVertically)
        {
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            Bitmap original = Bitmap.FromStream(stream, true) as Bitmap;

            return Flip(original, flipHorizontally, flipVertically);
        }

        public static Bitmap Flip(byte[] data, bool flipHorizontally, bool flipVertically)
        {
            MemoryStream memoryStream = new MemoryStream(data);

            return Flip(memoryStream, flipHorizontally, flipVertically);
        }

        public static Bitmap Flip(string fullFilename, bool flipHorizontally, bool flipVertically)
        {
            Bitmap original = Bitmap.FromFile(fullFilename, true) as Bitmap;
            return Flip(original, flipHorizontally, flipVertically);
        }

        #endregion

        #region Helper

        internal static ImageCodecInfo FindEncoder(ImageFormat imageFormat)
        {
            foreach (ImageCodecInfo info in ImageCodecInfo.GetImageEncoders())
            {
                if (info.FormatID.Equals(imageFormat.Guid))
                {
                    return info;
                }
            }

            return null;
        }

        // https://www.cnblogs.com/qixuejia/archive/2010/09/03/1817248.html 无法从带有索引像素格式的图像创建graphics对象
        // https://blog.csdn.net/johnsuna/article/details/871557 关于无法从带INDEX格式的GIF图片创建Graphics的解决方案
        /// <summary>
        /// 会产生graphics异常的PixelFormat
        /// </summary>
        private static PixelFormat[] indexedPixelFormats = { PixelFormat.Undefined, PixelFormat.DontCare,
            PixelFormat.Format16bppArgb1555, PixelFormat.Format1bppIndexed, PixelFormat.Format4bppIndexed,
            PixelFormat.Format8bppIndexed
        };

        /// <summary>
        /// 判断图片的PixelFormat 是否在 引发异常的 PixelFormat 之中
        /// </summary>
        /// <param name="imgPixelFormat">原图片的PixelFormat</param>
        /// <returns></returns>
        private static bool IsPixelFormatIndexed(PixelFormat imgPixelFormat)
        {
            foreach (PixelFormat pf in indexedPixelFormats)
            {
                if (pf.Equals(imgPixelFormat)) return true;
            }

            return false;
        }

        internal static Bitmap CheckAndFixImage(Bitmap original, bool isPng)
        {
            if (!IsPixelFormatIndexed(original.PixelFormat))
                return original;

            Bitmap destination = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
            try
            {
                destination.SetResolution(original.HorizontalResolution, original.VerticalResolution);
            }
            catch
            {
                destination.SetResolution(96, 96);
            }
            using (Graphics g = Graphics.FromImage(destination))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                if (isPng)
                    g.Clear(Color.Transparent);
                else
                    g.Clear(Color.White);

                g.DrawImage(original, 0, 0);
            }

            original.Dispose();
            return destination;
        }

        #endregion
        internal static class PointMath
        {
            private static double DegreeToRadian(double angle)
            {
                return Math.PI * angle / 180.0;
            }

            public static PointF RotatePoint(PointF pnt, double degreeAngle)
            {
                return RotatePoint(pnt, new PointF(0, 0), degreeAngle);
            }

            public static PointF RotatePoint(PointF pnt, PointF origin, double degreeAngle)
            {
                double radAngle = DegreeToRadian(degreeAngle);

                PointF newPoint = new PointF();

                double deltaX = pnt.X - origin.X;
                double deltaY = pnt.Y - origin.Y;

                newPoint.X = (float)(origin.X + (Math.Cos(radAngle) * deltaX - Math.Sin(radAngle) * deltaY));
                newPoint.Y = (float)(origin.Y + (Math.Sin(radAngle) * deltaX + Math.Cos(radAngle) * deltaY));

                return newPoint;
            }

            public static void RotatePoints(PointF[] pnts, double degreeAngle)
            {
                for (int i = 0; i < pnts.Length; i++)
                {
                    pnts[i] = RotatePoint(pnts[i], degreeAngle);
                }
            }

            public static void RotatePoints(PointF[] pnts, PointF origin, double degreeAngle)
            {
                for (int i = 0; i < pnts.Length; i++)
                {
                    pnts[i] = RotatePoint(pnts[i], origin, degreeAngle);
                }
            }

            public static Rectangle GetBounds(PointF[] pnts)
            {
                RectangleF boundsF = GetBoundsF(pnts);
                return new Rectangle((int)Math.Round(boundsF.Left),
                                     (int)Math.Round(boundsF.Top),
                                     (int)Math.Round(boundsF.Width),
                                     (int)Math.Round(boundsF.Height));
            }

            public static RectangleF GetBoundsF(PointF[] pnts)
            {
                float left = pnts[0].X;
                float right = pnts[0].X;
                float top = pnts[0].Y;
                float bottom = pnts[0].Y;

                for (int i = 1; i < pnts.Length; i++)
                {
                    if (pnts[i].X < left)
                        left = pnts[i].X;
                    else if (pnts[i].X > right)
                        right = pnts[i].X;

                    if (pnts[i].Y < top)
                        top = pnts[i].Y;
                    else if (pnts[i].Y > bottom)
                        bottom = pnts[i].Y;
                }

                return new RectangleF(left,
                                      top,
                                     (float)Math.Abs(right - left),
                                     (float)Math.Abs(bottom - top));
            }
        }
    }
}
