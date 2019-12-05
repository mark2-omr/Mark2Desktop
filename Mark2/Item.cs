using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.AI.MachineLearning;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace Mark2
{
    public class Item
    {
        public string name;
        public Image<Rgba32> image;
        public Image<Rgba32> logImage;
        public StorageFolder logFolder;
        public LearningModel mnistModel;
        List<Square> squares;
        public Page page;
        public List<List<int>> answers;

        public Item(string name, Image<Rgba32> image, StorageFolder logFolder, LearningModel mnistModel)
        {
            this.name = name;
            this.image = image;
            this.logImage = image.Clone();
            this.logFolder = logFolder;
            this.mnistModel = mnistModel;
            answers = new List<List<int>>();
        }

        public void DetectSquares()
        {
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
            int[,] pixels = new int[size[0], size[1]];

            var index = 1;
            for (var i = 0; i < size[0]; i++)
            {
                for (var j = 0; j < size[1]; j++)
                {
                    if (image[i + topLeft[0], j + topLeft[1]].R < 128)
                    {
                        pixels[i, j] = index;
                        index++;
                    }
                    else
                    {
                        pixels[i, j] = 0;
                    }
                }
            }

            var previousPattern = pixels.ToString();
            while (true)
            {
                for (var i = 1; i < size[0] - 1; i++)
                {
                    for (var j = 1; j < size[1] - 1; j++)
                    {
                        for (var dx = -1; dx < 2; dx++)
                        {
                            for (var dy = -1; dy < 2; dy++)
                            {
                                if (dx == 0 && dy == 0)
                                {
                                    continue;
                                }
                                else if (pixels[i, j] == 0 || pixels[i + dx, j + dy] == 0)
                                {
                                    continue;
                                }
                                else if (pixels[i, j] < pixels[i + dx, j + dy])
                                {
                                    pixels[i + dx, j + dy] = pixels[i, j];
                                }
                                else if (pixels[i, j] > pixels[i + dx, j + dy])
                                {
                                    pixels[i, j] = pixels[i + dx, j + dy];
                                }
                            }
                        }
                    }
                }

                if (previousPattern == pixels.ToString())
                {
                    break;
                }
                else
                {
                    previousPattern = pixels.ToString();
                }
            }

            var frequency = new Dictionary<int, int>();
            for (var i = 0; i < size[0]; i++)
            {
                for (var j = 0; j < size[1]; j++)
                {
                    var value = pixels[i, j];
                    if (value > 0 && frequency.ContainsKey(value))
                    {
                        frequency[value]++;
                    }
                    else if (value > 0)
                    {
                        frequency[value] = 1;
                    }
                }
            }
            var mostFrequent = frequency.OrderByDescending(v => v.Value).First().Key;

            var xs = new List<int>();
            var ys = new List<int>();
            for (var i = 0; i < size[0]; i++)
            {
                for (var j = 0; j < size[1]; j++)
                {
                    if (pixels[i, j] == mostFrequent)
                    {
                        xs.Add(i);
                        ys.Add(j);
                    }
                }
            }

            var square = new Square(topLeft[0] + xs.Min(), topLeft[1] + ys.Min(),
                xs.Max() - xs.Min(), ys.Max() - ys.Min(),
                topLeft[0] + (int)xs.Average(), topLeft[1] + (int)ys.Average());

            fillRect(square.x, square.y, square.w, square.h, Rgba32.Red, 0.8f);

            return square;
        }

        public async Task Recognize(double threshold)
        {
            answers = new List<List<int>>();
            var mnistSession = new LearningModelSession(mnistModel, new LearningModelDevice(LearningModelDeviceKind.Default));
            foreach (var question in page.questions)
            {
                var _answers = new List<int>();
                if (question.type == 1)
                {
                    foreach (var area in question.areas)
                    {
                        var topLeft = BiLenearInterpoltation(area.x, area.y);
                        var bottomRight = BiLenearInterpoltation(area.x + area.w, area.y + area.h);

                        int count = 0;
                        for (int i = topLeft[0]; i < bottomRight[0]; i++)
                        {
                            for (int j = topLeft[1]; j < bottomRight[1]; j++)
                            {
                                if (image[i, j].R < 128)
                                {
                                    count++;
                                }
                            }
                        }
                        if ((double)count / ((bottomRight[0] - topLeft[0]) * (bottomRight[1] - topLeft[1])) > threshold)
                        {
                            _answers.Add(area.v);
                            fillRect(topLeft[0], topLeft[1], bottomRight[0] - topLeft[0], bottomRight[1] - topLeft[1], Rgba32.Green, 0.4f);
                        } else
                        {
                            fillRect(topLeft[0], topLeft[1], bottomRight[0] - topLeft[0], bottomRight[1] - topLeft[1], Rgba32.Red, 0.4f);
                        }
                        
                    }
                }
                else if (question.type == 2)
                {
                    var binding = new LearningModelBinding(mnistSession);

                    foreach (var area in question.areas)
                    {
                        var topLeft = BiLenearInterpoltation(area.x, area.y);
                        var bottomRight = BiLenearInterpoltation(area.x + area.w, area.y + area.h);

                        var cloneImage = image.Clone(img => img
                            .Crop(new Rectangle(topLeft[0], topLeft[1],
                                bottomRight[0] - topLeft[0], bottomRight[1] - topLeft[1]))
                            .Resize(28, 28));

                        var data = new float[1 * 1 * 28 * 28];
                        int i = 0;
                        for (int y = 0; y < 28; y++)
                        {
                            for (int x = 0; x < 28; x++)
                            {
                                data[i] = 0.0f;
                                if (cloneImage[x, y].R < 128)
                                {
                                    data[i] = 1.0f;
                                }
                                i++;
                            }
                        }

                        TensorFloat tensor = TensorFloat.CreateFromArray(new long[] { 1, 1, 28, 28 }, data);
                        binding.Bind("0", tensor);

                        var modelOutput = await mnistSession.EvaluateAsync(binding, "run");
                        List<float> v = new List<float>();
                        foreach (var item in modelOutput.Outputs)
                        {
                            System.Diagnostics.Debug.WriteLine("{0}:{1}", item.Key, item.Value);
                            TensorFloat outTensor = (TensorFloat)item.Value;
                            v = outTensor.GetAsVectorView().ToList();
                        }

                        if(v.Max() > 0.4)
                        {
                            _answers.Add(v.IndexOf(v.Max()));
                        }

                        fillRect(topLeft[0], topLeft[1], bottomRight[0] - topLeft[0], bottomRight[1] - topLeft[1], Rgba32.Blue, 0.4f);
                    }
                }
                answers.Add(_answers);
            }


            _ = Task.Run(async () => {
                StorageFile logFile = await logFolder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
                var stream = await logFile.OpenStreamForWriteAsync();
                var encoder = new PngEncoder();
                encoder.ColorType = PngColorType.Rgb;
                encoder.BitDepth = PngBitDepth.Bit8;
                logImage.SaveAsPng(stream, encoder);
            });
        }

        public int[] BiLenearInterpoltation(int xp, int yp)
        {
            double w = 595.0;
            double h = 842.0;
            double xp1 = w * (0.14 + 0.015);
            double yp1 = h * (0.03 + 0.01);
            double xp2 = w * (0.83 + 0.015);
            double yp2 = h * (0.03 + 0.01);
            double xp3 = w * (0.83 + 0.015);
            double yp3 = h * (0.95 + 0.01);
            double xp4 = w * (0.14 + 0.015);
            double yp4 = h * (0.95 + 0.01);
            double u = 0.5;
            double v = 0.5;

            int maxIteration = 100;
            double er = 1.0e+6; // 誤差初期値
            // 誤差(修正量)の許容値
            // 1.0e-10 で u, vの精度は 1.0e-5
            // 1.0e-6 にする u, vの精度は 1.0e-3 で反復回数はだいたい 1 / 2 に減少
            double erMax = 1.0e-10;
            int iteration = 0;

            while (true)
            {
                // 連立方程式の右辺
                var ex = xp1 - xp + (xp2 - xp1) * u + (xp4 - xp1) * v + (xp1 - xp2 + xp3 - xp4) * u * v;
                var ex2 = ex * ex * 0.5;
                var ey = yp1 - yp + (yp2 - yp1) * u + (yp4 - yp1) * v + (yp1 - yp2 + yp3 - yp4) * u * v;
                var ey2 = ey * ey * 0.5;

                // 連立方程式の係数行列
                var exu = ex * ((xp2 - xp1) + (xp1 - xp2 + xp3 - xp4) * v);
                var exv = ex * ((xp4 - xp1) + (xp1 - xp2 + xp3 - xp4) * u);
                var eyu = ey * ((yp2 - yp1) + (yp1 - yp2 + yp3 - yp4) * v);
                var eyv = ey * ((yp4 - yp1) + (yp1 - yp2 + yp3 - yp4) * u);

                // p ex2, ey2, exu, exv, eyu, eyv
                // 係数行列の行列式(逆行列計算用)
                var d = exu * eyv - exv * eyu;

                // 初期値で誤差が極小化される場合は傾斜が0になって補正量が発散する問題あり
                // 発散を避けるために乱数で(u, v)を変更
                if (Math.Abs(d) < 1.0e-6)
                {
                    // STDERR.printf "d: %+e\n", d
                    u = new Random().NextDouble();
                    v = new Random().NextDouble();
                    continue;
                }

                // 係数行列の逆行列を連立方程式の右辺にかけて解を計算
                var du = (eyv * ex2 - exv * ey2) / d;
                var dv = (-eyu * ex2 + exu * ey2) / d;

                // STDERR.printf "i: %d E: +%.2e u: %+.2e v: %+.2e xp: %+.2e yp: %+.2e\n",
                // iteration, er, @u, @v, @xp, @yp
                // 規格化座標を修正
                u -= du;
                v -= dv;

                // 誤差(修正量)
                er = du * du + dv * dv;
                iteration++;

                if (!(er > erMax && iteration < maxIteration))
                {
                    break;
                }
            }

            var xq = squares[0].cx + (squares[1].cx - squares[0].cx) * u + (squares[3].cx - squares[0].cx) * v
                + (squares[0].cx - squares[1].cx + squares[2].cx - squares[3].cx) * u * v;

            var yq = squares[0].cy + (squares[1].cy - squares[0].cy) * u + (squares[3].cy - squares[0].cy) * v
                + (squares[0].cy - squares[1].cy + squares[2].cy - squares[3].cy) * u * v;

            return new int[] { (int)xq, (int)yq };
        }

        public void fillRect(int x, int y, int w, int h, Rgba32 c, float a)
        {
            for (int i = x; i < w + x; i++)
            {
                for (int j = y; j < h + y; j++)
                {
                    logImage[i, j] = new Rgba32(
                        ((float)logImage[i, j].R * (1.0f - a) + (float)c.R * a) / 255.0f,
                        ((float)logImage[i, j].G * (1.0f - a) + (float)c.G * a) / 255.0f,
                        ((float)logImage[i, j].B * (1.0f - a) + (float)c.B * a) / 255.0f);
                }
            }
        }
    }
}
