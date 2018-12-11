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

        public void CalculateBorderColorss()
        {
            int[] pixelColorsTop = new int[ColorPixels];
            int[] pixelColorsBottom = new int[ColorPixels];
            int[] pixelColorsLeft = new int[ColorPixels];
            int[] pixelColorsRight = new int[ColorPixels];

            for (int x = 0; x < ColorPixels; x++)
            {
                pixelColorsTop[x] = Bitmap.GetPixel(x, 0).ToArgb();
                pixelColorsBottom[x] = Bitmap.GetPixel(x, Bitmap.Height).ToArgb();
            }
            for (int y = 0; y < ColorPixels; y+=OffsetH)
            {

                //TODO take average of borders
                for (int x = 0; x < BorderWidth; x++)
                {
                    pixelColorsLeft[y] = Bitmap.GetPixel(0, y).ToArgb();
                    pixelColorsBottom[y] = Bitmap.GetPixel(Bitmap.Height, Bitmap.Height).ToArgb();
                }
                
            }
            
        }

    }
}
