using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LED_Controller.Math
{
    class BorderAlgorithm
    {

        public int ColorPixels { get; }
        public Bitmap Bitmap { get; set; }
        byte[] _topColors;

        public int OffsetW { get; set; }
        public int OffsetH { get; set; }

        public int BorderWidth { get; set; }


        public BorderAlgorithm(int amount, int borderWidth, Bitmap bitmap){
            ColorPixels = amount;
            _topColors = new byte[amount];
            OffsetW = bitmap.Width / amount;
            OffsetW = bitmap.Height / amount;
            BorderWidth = borderWidth;
        }

        public void CalculateBorderColors()
        {
            int arraySize = ColorPixels * BorderWidth;
            int[] pixelColorsTop = new int[arraySize];
            int[] pixelColorsBottom = new int[arraySize];
            int[] pixelColorsLeft = new int[arraySize];
            int[] pixelColorsRight = new int[arraySize];

            for (int x = 0; x < ColorPixels; x++)
            {

                for (int y = 0; y < BorderWidth; y++)
                {

                }
                pixelColorsTop[x] = Bitmap.GetPixel(x, 0).ToArgb();
                pixelColorsBottom[x] = Bitmap.GetPixel(x, Bitmap.Height).ToArgb();
            }
            for (int y = 1; y < ColorPixels; y+=OffsetH)
            {

                //TODO take average of borders
                for (int x = 0; x < BorderWidth; x++)
                {
                    pixelColorsLeft[y+x] = Bitmap.GetPixel(x, y).ToArgb();
                    pixelColorsBottom[y+x] = Bitmap.GetPixel(x, Bitmap.Height-y).ToArgb();
                    Console.WriteLine(pixelColorsBottom);
                }

            }
            
        }

    }
}
