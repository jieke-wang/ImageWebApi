using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;

namespace ImageWebApi.Libs
{
    public class ImageCompress
    {
        private static volatile ImageCompress imageCompress;
        private Bitmap bitmap;
        private int width;
        private int height;
        private Image img;

        private ImageCompress()
        {
        }

        public static ImageCompress GetImageCompressObject
        {
            get
            {
                if (imageCompress == null)
                {
                    imageCompress = new ImageCompress();
                }
                return imageCompress;
            }
        }

        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        public int Width
        {
            get { return width; }
            set { width = value; }
        }

        public Bitmap GetImage
        {
            get { return bitmap; }
            set { bitmap = value; }
        }

        public void Save(string fileName, string path)
        {
            if (ISValidFileType(fileName))
            {
                string pathaname = Path.Combine(path, fileName);
                Save(pathaname, fileName, 60);
            }
        }

        private Image CompressImage()
        {
            if (GetImage != null)
            {
                Width = (Width == 0) ? GetImage.Width : Width;
                Height = (Height == 0) ? GetImage.Height : Height;
                // Bitmap newBitmap; //new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
                // newBitmap = bitmap;
                // newBitmap.SetResolution(80, 80);
                // return newBitmap.GetThumbnailImage(Width, Height, null, IntPtr.Zero);
                return ImageHelper.Zoom(bitmap, width, height);
            }
            else
            {
                throw new Exception("Please provide bitmap");
            }
        }

        private static bool ISValidFileType(string fileName)
        {
            bool isValidExt = false;
            string fileExt = Path.GetExtension(fileName);
            switch (fileExt?.ToLower())
            {
                case CommonConstant.JPEG:
                case CommonConstant.BTM:
                case CommonConstant.JPG:
                case CommonConstant.PNG:
                    isValidExt = true;
                    break;
            }
            return isValidExt;
        }

        private static ImageCodecInfo GetImageCoeInfo(string mimeType)
        {
            ImageCodecInfo[] codes = ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i < codes.Length; i++)
            {
                if (codes[i].MimeType == mimeType)
                {
                    return codes[i];
                }
            }
            return null;
        }

        private void Save(string path, string fileName, int quality)
        {
            new FileExtensionContentTypeProvider().TryGetContentType(fileName, out string contentType);
            contentType ??= "image/jpeg";
            img = CompressImage();
            //Setting the quality of the picture
            EncoderParameter qualityParam =
                new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            //Seting the format to save
            ImageCodecInfo imageCodec = GetImageCoeInfo(contentType);
            //Used to contain the poarameters of the quality
            EncoderParameters parameters = new EncoderParameters(1);
            parameters.Param[0] = qualityParam;
            //Used to save the image to a  given path
            img.Save(path, imageCodec, parameters);
        }
    }
}
