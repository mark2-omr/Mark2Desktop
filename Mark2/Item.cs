using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using OpenCvSharp;

namespace Mark2
{
    class Item
    {
        public SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image;
        List<Square> squares;

        public Item(SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image)
        {
            this.image = image;
        }

        public void DetectSquares()
        {
            System.Diagnostics.Debug.WriteLine("detect");
            var squares = new List<Square>();
            var margin = new int[] { (int)(image.Width * 0.01), (int)(image.Height * 0.01) };
            var size = new int[] { (int)(image.Width * 0.3), (int)(image.Height * 0.08) };

            squares.Add(DetectSquare(margin, size));
            squares.Add(DetectSquare(new int[] { image.Width - margin[0] - size[0], margin[1] }, size));
            squares.Add(DetectSquare(new int[] { image.Width - margin[0] - size[0],
                image.Height - margin[1] - size[1] }, size));
            squares.Add(DetectSquare(new int[] { margin[0], image.Height - margin[1] - size[1] }, size));

            this.squares = squares;
        }

        Square DetectSquare(int[] topLeft, int[] size)
        {
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            image.Clone(cloneImage => cloneImage.Crop(new Rectangle(topLeft[0], topLeft[1], size[0], size[1])))
                .SaveAsPng(memoryStream);
            var img = Cv2.ImDecode(memoryStream.ToArray(), ImreadModes.Grayscale);
            var contours = new Mat[] { };
            img.FindContours(out contours, new Mat(), RetrievalModes.List, ContourApproximationModes.ApproxNone);

            Square square = null;
            foreach (Mat contour in contours)
            {
                var sq = Cv2.BoundingRect(contour);
                if (size[0] * size[1] * 0.02 < sq.Width * sq.Height
                    && size[0] * size[1] * 0.1 > sq.Width * sq.Height
                    && sq.Width / (double)(sq.Width + sq.Height) > 0.3
                    && sq.Width / (double)(sq.Width + sq.Height) < 0.7)
                {
                    square = new Square(topLeft[0] + sq.X, topLeft[1] + sq.Y, sq.Width, sq.Height);
                    System.Diagnostics.Debug.WriteLine(sq.ToString());
                }
            }
            // Cv2.ImShow("debug", img);
            // Cv2.WaitKey();

            return square;
        }
    }
}
