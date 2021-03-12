using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;

namespace ImageWebApi.Libs
{
    public class ImageCompressV2
    {
        private static volatile ImageCompressV2 imageCompress;
        private Bitmap bitmap;
        private int width;
        private int height;
        private Image img;

        private ImageCompressV2()
        {
        }

        public static ImageCompressV2 GetImageCompressObject
        {
            get
            {
                if (imageCompress == null)
                {
                    imageCompress = new ImageCompressV2();
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

        public string BackgrouColor { get; set; }
        public string Quantity { get; set; }

        public void Save(string fileName, string path)
        {
            if (ISValidFileType(fileName))
            {
                if (int.TryParse(Quantity, out int quantity) == false) quantity = 80;
                string pathaname = Path.Combine(path, fileName);
                //Save(pathaname, fileName, 60);
                Save(pathaname, fileName, quantity);
            }
        }

        private Image CompressImage()
        {
            if (GetImage != null)
            {
                Width = (Width == 0) ? GetImage.Width : Width;
                Height = (Height == 0) ? GetImage.Height : Height;

                Color bgColor = Color.White;
                if (string.IsNullOrWhiteSpace(BackgrouColor) == false)
                {
                    BackgrouColor = BackgrouColor.Trim('#');
                    if (BackgrouColor.Length == 6 && int.TryParse(BackgrouColor, System.Globalization.NumberStyles.HexNumber, null, out int _))
                    {
                        bgColor = Color.FromArgb(
                            Convert.ToInt32(BackgrouColor.Substring(0, 2), 16),
                            Convert.ToInt32(BackgrouColor.Substring(2, 2), 16),
                            Convert.ToInt32(BackgrouColor.Substring(4, 2), 16));
                    }
                }

                return ImageHelperV2.Pad(bitmap, width, height, bgColor);
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
            //EncoderParameters parameters = new EncoderParameters(2);
            //parameters.Param[0] = qualityParam;
            //parameters.Param[1] = new EncoderParameter(Encoder.ColorDepth, 32);

            EncoderParameters parameters = new EncoderParameters(1);
            parameters.Param[0] = qualityParam;

            //Used to save the image to a  given path
            img.Save(path, imageCodec, parameters);
        }
    }
}
