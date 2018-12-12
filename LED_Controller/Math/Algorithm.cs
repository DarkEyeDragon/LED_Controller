using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LED_Controller.Math
{
    class Algorithm
    {
        public int ColorPixels { get; }
        public Bitmap Bitmap { get; set; }

        public int OffsetW { get; set; }
        public int OffsetH { get; set; }

        public int BorderWidth { get; set; }


        private List<int> _borderColorsList;


        public Algorithm(int amount, int borderWidth, Bitmap bitmap)
        {
            ColorPixels = amount;
            OffsetW = bitmap.Width / amount;
            OffsetH = bitmap.Height / amount;
            BorderWidth = borderWidth;
            _borderColorsList = new List<int>();
        }

        public int CalculateBorderColors()
        {
            _borderColorsList.Clear();           

            for (int x = 1; x < ColorPixels; x++)
            {
                for (int y = 1; y < BorderWidth; y++)
                {
                    _borderColorsList.Add(Bitmap.GetPixel(x, y).ToArgb());
                    _borderColorsList.Add(Bitmap.GetPixel(x, Bitmap.Height - x - 1).ToArgb());
                }
            }

            for (int y = 1; y < ColorPixels; y += OffsetH)
            {
                //TODO take average of borders
                for (int x = 0; x < BorderWidth; x++)
                {
                    _borderColorsList.Add(Bitmap.GetPixel(x, y).ToArgb());
                    _borderColorsList.Add(Bitmap.GetPixel(x, Bitmap.Height - y - 1).ToArgb());
                }
            }
            return MostFrequent(_borderColorsList.ToArray(), _borderColorsList.Count);
        }

        //Calculate the most frequent pixel in the screenshot
        private int MostFrequent(int[] arr, int n)
        {
            // Insert all elements in hash 
            var hp =
                new Dictionary<int, int>();

            for (var i = 0; i < n; i++)
            {
                var key = arr[i];
                if (hp.ContainsKey(key))
                {
                    var freq = hp[key];
                    freq++;
                    hp[key] = freq;
                }
                else
                {
                    hp.Add(key, 1);
                }
            }

            // find max frequency. 
            int min_count = 0, res = -1;

            foreach (var pair in hp)
                if (min_count < pair.Value)
                {
                    res = pair.Key;
                    min_count = pair.Value;
                }

            return res;
        }
    }
}