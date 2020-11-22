using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;
//using Windows.AI.MachineLearning;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Mark2
{
    public class Item : ItemBase
    {
      
        public StorageFolder textFolder;
        public StorageFolder logFolder;
        //public LearningModel mnistModel;
        public byte[] mnistModelByte;
        List<Square> squares;

        public Item(int pid, string name, Image<Rgba32> image, StorageFolder textFolder, StorageFolder logFolder, byte[] mnistModel)
        {
            this.pid = pid;
            this.name = name;
            this.image = image;
            this.logImage = image.Clone();
            this.textFolder = textFolder;
            this.logFolder = logFolder;
            //this.mnistModel = mnistModel;
            this.mnistModelByte = mnistModel;
            answers = new List<List<int>>();
        }

        public async Task Recognize(double areaThreshold, double colorThreshold)
        {
            answers = new List<List<int>>();
            //var mnistSession = new LearningModelSession(mnistModel, new LearningModelDevice(LearningModelDeviceKind.Default));
            var session = new InferenceSession(this.mnistModelByte);

            foreach (var (question, qid) in page.questions.Select((question, qid) => (question, qid)))
            {
                System.Diagnostics.Debug.WriteLine("{0}", question.type);
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
                                if (image[i, j].R < (int)((1 - colorThreshold) * 255))
                                {
                                    count++;
                                }
                            }
                        }
                        if ((double)count / ((bottomRight[0] - topLeft[0]) * (bottomRight[1] - topLeft[1])) > areaThreshold)
                        {
                            _answers.Add(area.v);
                            fillRect(topLeft[0], topLeft[1], bottomRight[0] - topLeft[0], bottomRight[1] - topLeft[1], Rgba32.ParseHex("#00FF00FF"), 0.4f);
                        }
                        else
                        {
                            fillRect(topLeft[0], topLeft[1], bottomRight[0] - topLeft[0], bottomRight[1] - topLeft[1], Rgba32.ParseHex("#FF0000FF"), 0.4f);
                        }

                    }
                }
                else if (question.type == 2)
                {
                    //var binding = new LearningModelBinding(mnistSession);

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
                        for (i = 0; i < data.Length; i++)
                        {
                            data[i] = 0.0f;
                        }
                        i = 0;


                        // 入力されている座標の平均を計算
                        float average_x = 0.0f;
                        float average_y = 0.0f;
                        float average_count = 0.0f;

                        for (int y = 2; y < 26; y++)
                        {
                            for (int x = 2; x < 26; x++)
                            {
                                if (cloneImage[x,y].R < (int)((1-colorThreshold) * 255))
                                {
                                    average_x += (float)x;
                                    average_y += (float)y;
                                    average_count += 1.0f;
                                }
                            }
                        }

                        average_x = average_x / average_count;
                        average_y = average_y / average_count;

                        // 移動方向
                        int dx = 14 - (int)average_x;
                        int dy = 14 - (int)average_y;

                        float color_scale = 1.0f;

                        for (int y = 0; y < 28; y++)
                        {
                            for (int x = 0; x < 28; x++)
                            {
                                //data[i] = 0.0f;
                                //if (cloneImage[x, y].R < (int)((1 - colorThreshold) * 255))
                                //{
                                //    data[i] = 1.0f;
                                //}
                                //i++;

                                // なるべく中心に移動する
                                int px = x + dx;
                                int py = y + dy;

                                if (px > 0 && py > 0 && px < 28 && py < 28)
                                {
                                    i = 28 * py + px;
                                    data[i] = (float)(255 - cloneImage[x,y].R)  / 255.0f;
                                }
                            }
                        }

                        // 最大値に合わせて全体の値を調整する
                        color_scale = 1.0f / data.Max();

                        for (i = 0; i < data.Length; i++)
                        {
                            data[i] = data[i] * color_scale;
                            if (data[i] > 1.0f)
                            {
                                data[i] = 1.0f;
                            }
                        }

                        //TensorFloat tensor = TensorFloat.CreateFromArray(new long[] { 1, 1, 28, 28 }, data);
                        //binding.Bind("0", tensor);

                        //var modelOutput = await mnistSession.EvaluateAsync(binding, "run");

                        Tensor<float> tensor = new DenseTensor<float>(data, session.InputMetadata.First().Value.Dimensions);
                        var namedValues = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input.1", tensor) };
                        var results = session.Run(namedValues);
                        var resultValues = results.First().AsTensor<float>().ToArray();

                        if (resultValues.Max() > -1.85f)
                        {
                            _answers.Add(Array.IndexOf(resultValues, resultValues.Max()));
                        }

                        //List<float> v = new List<float>();
                        //foreach (var item in modelOutput.Outputs)
                        //{
                        //    TensorFloat outTensor = (TensorFloat)item.Value;
                        //    v = outTensor.GetAsVectorView().ToList();
                        //}

                        //if (v.Max() > 0.4)
                        //{
                        //    _answers.Add(v.IndexOf(v.Max()));
                        //}
                        string result_value = resultValues.Max().ToString().Replace(".", "__");
                        var name = String.Format("mnist_{0:0000}_{1:0000}_answer_{2}_{3}.png", qid, pid, Array.IndexOf(resultValues, resultValues.Max()), result_value);

                        StorageFile textFile = await logFolder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
                        var stream = await textFile.OpenStreamForWriteAsync();
                        var encoder = new PngEncoder();
                        encoder.ColorType = PngColorType.Rgb;
                        encoder.BitDepth = PngBitDepth.Bit8;

                        cloneImage.SaveAsPng(stream, encoder);
                        

                        fillRect(topLeft[0], topLeft[1], bottomRight[0] - topLeft[0], bottomRight[1] - topLeft[1], Rgba32.ParseHex("#0000FFFF"), 0.4f);


                      
                    }
                }
                else if (question.type == 3)
                {
                    var name = String.Format("{0:0000}_{1:0000}.png", qid, pid);
                    foreach (var area in question.areas)
                    {
                        var topLeft = BiLenearInterpoltation(area.x, area.y);
                        var bottomRight = BiLenearInterpoltation(area.x + area.w, area.y + area.h);
                        fillRect(topLeft[0], topLeft[1], bottomRight[0] - topLeft[0], bottomRight[1] - topLeft[1], Rgba32.ParseHex("#0000FFFF"), 0.4f);
                        var textImage = image.Clone(img => img
                            .Crop(new Rectangle(topLeft[0], topLeft[1],
                                bottomRight[0] - topLeft[0], bottomRight[1] - topLeft[1])));
                        StorageFile textFile = await textFolder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
                        var stream = await textFile.OpenStreamForWriteAsync();
                        var encoder = new PngEncoder();
                        encoder.ColorType = PngColorType.Rgb;
                        encoder.BitDepth = PngBitDepth.Bit8;
                        textImage.SaveAsPng(stream, encoder);
                    }
                }
                answers.Add(_answers);
            }

            _ = Task.Run(async () => {
                StorageFile logFile = await logFolder.CreateFileAsync(Path.GetFileNameWithoutExtension(name) + ".png", CreationCollisionOption.ReplaceExisting);
                var stream = await logFile.OpenStreamForWriteAsync();
                var encoder = new PngEncoder();
                encoder.ColorType = PngColorType.Rgb;
                encoder.BitDepth = PngBitDepth.Bit8;
                logImage.SaveAsPng(stream, encoder);
            });
        }

       
    }
}
