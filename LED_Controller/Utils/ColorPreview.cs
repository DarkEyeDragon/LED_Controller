using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using Image = System.Windows.Controls.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace LED_Controller.Utils
{
    internal class ColorPreview
    {
        public Color Color { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        private Bitmap bitmap;
        private BitmapImage bitmapImage;

        public ImageSource ImageSource { get; set; }

        private readonly Image _image;

        public ColorPreview(Image image) : this(image, 30, 30)
        {
        }

        public ColorPreview(Image image, int width, int height)
        {
            Height = height;
            Width = width;
            bitmap = new Bitmap(Width, Height);
            _image = image;
            ImageSource = image.Source;
        }

        public void Update()
        {
            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    bitmap.SetPixel(x, y, Color);
                }
            }
            var imageSrc = new ImageSourceConverter();
            _image.Source = ConvertFromImage(bitmap);
            ImageSource = _image.Source;
        }

        //Convert Image/Bitmap to ImageSource
        public ImageSource ConvertFromImage(System.Drawing.Image image)
        {
            using (var ms = new MemoryStream())
            {
                bitmapImage = new BitmapImage();
                image.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                ms.Flush();
                return bitmapImage;
            }
        }
    }
}