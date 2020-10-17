﻿using System;
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

namespace Mark2
{
    public class Item : ItemBase
    {
      
        public StorageFolder textFolder;
        public StorageFolder logFolder;
        public LearningModel mnistModel;
        List<Square> squares;

        public Item(int pid, string name, Image<Rgba32> image, StorageFolder textFolder, StorageFolder logFolder, LearningModel mnistModel)
        {
            this.pid = pid;
            this.name = name;
            this.image = image;
            this.logImage = image.Clone();
            this.textFolder = textFolder;
            this.logFolder = logFolder;
            this.mnistModel = mnistModel;
            answers = new List<List<int>>();
        }

        public async Task Recognize(double areaThreshold, double colorThreshold)
        {
            answers = new List<List<int>>();
            var mnistSession = new LearningModelSession(mnistModel, new LearningModelDevice(LearningModelDeviceKind.Default));
            foreach (var (question, qid) in page.questions.Select((question, qid) => (question, qid)))
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
                                if (cloneImage[x, y].R < (int)((1 - colorThreshold) * 255))
                                {
                                    data[i] = 1.0f;
                                }
                                i++;
                            }
                        }

                        TensorFloat tensor = TensorFloat.CreateFromArray(new long[] { 1, 1, 28, 28 }, data);
                        binding.Bind("input.1", tensor);

                        var modelOutput = await mnistSession.EvaluateAsync(binding, "run");
                        List<float> v = new List<float>();
                        foreach (var item in modelOutput.Outputs)
                        {
                            TensorFloat outTensor = (TensorFloat)item.Value;
                            v = outTensor.GetAsVectorView().ToList();
                        }

                        if (v.Max() > 0.4)
                        {
                            _answers.Add(v.IndexOf(v.Max()));
                        }

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
