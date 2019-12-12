using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.AI.MachineLearning;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

using System.Threading;

namespace Mark2
{
    public class Survey
    {
        public StorageFolder folder;
        public StorageFolder textFolder;
        public StorageFolder logFolder;
        public StorageFile csv;
        public double threshold;
        public List<Item> items;
        List<Page> pages;
        public string resultBuffer;
        public bool StopRecognize { get; set; }

        public Survey()
        {
            items = new List<Item>();
            pages = new List<Page>();
            StopRecognize = false;
        }

        async public Task SetupPositions()
        {
            byte[] fileBytes = null;
            using (var stream = await csv.OpenReadAsync())
            {
                fileBytes = new byte[stream.Size];
                using (var reader = new Windows.Storage.Streams.DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(fileBytes);
                }
            }
            string csvString = System.Text.Encoding.ASCII.GetString(fileBytes);
            List<string> lines = csvString.Split("\n").ToList();

            List<int> vs = new List<int>();
            List<string> headers = lines[0].Split(',').ToList();
            headers.RemoveRange(0, 3);
            for (int i = 0; i < headers.Count() / 4; i++)
            {
                vs.Add(int.Parse(headers[i * 4]));
            }

            lines.RemoveRange(0, 3);
            foreach (string line in lines)
            {
                List<string> values = line.Split(',').ToList();
                if (values.Count() < 3)
                {
                    continue;
                }

                int pageNumber = int.Parse(values[1]);
                while (pages.Count() < pageNumber)
                {
                    pages.Add(new Page());
                }

                Question question = new Question();
                question.type = int.Parse(values[2]);

                values.RemoveRange(0, 3);
                for (int i = 0; i < values.Count() / 4; i++)
                {
                    if (values[i * 4].Length > 0 && values[(i * 4) + 1].Length > 0 &&
                        values[(i * 4) + 2].Length > 0 && values[(i * 4) + 2].Length > 0)
                    {
                        Area area = new Area(int.Parse(values[i * 4]), int.Parse(values[i * 4 + 1]),
                            int.Parse(values[i * 4 + 2]), int.Parse(values[i * 4 + 3]));
                        area.v = vs[i];

                        question.areas.Add(area);
                    }
                }
                pages[pageNumber - 1].questions.Add(question);
            }
        }

        public async Task SetupOutputFolders()
        {
            DateTime dateTime = DateTime.Now;
            textFolder = await folder.CreateFolderAsync($"text_{dateTime.ToString("yyyyMMdd_HHmmss")}",
                Windows.Storage.CreationCollisionOption.ReplaceExisting);
            logFolder = await folder.CreateFolderAsync($"log_{dateTime.ToString("yyyyMMdd_HHmmss")}",
                Windows.Storage.CreationCollisionOption.ReplaceExisting);
        }

        public async Task Recognize(Action<int, int> action)
        {
            var files = await folder.GetFilesAsync();
            LearningModel mnistModel;
            var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/mnist_8.onnx"));
            mnistModel = await LearningModel.LoadFromStorageFileAsync(modelFile);

            var buffer = "";
            var numQuestions = 0;
            foreach (var page in pages)
            {
                numQuestions += page.questions.Count();
            }
            for (var qid = 0; qid < numQuestions; qid++)
            {
                buffer += "," + (qid + 1).ToString();
            }
            buffer += "\n";

            var i = 0;
            var pid = 1;
            foreach (var file in files)
            {
                if (StopRecognize)
                {
                    break;
                }

                try
                {
                    byte[] fileBytes = null;
                    using (var stream = await file.OpenReadAsync())
                    {
                        fileBytes = new byte[stream.Size];
                        using (var reader = new Windows.Storage.Streams.DataReader(stream))
                        {
                            await reader.LoadAsync((uint)stream.Size);
                            reader.ReadBytes(fileBytes);
                        }
                    }

                    var image = Image.Load(fileBytes);
                    Item item = new Item(pid, file.Name, image, textFolder, logFolder, mnistModel);

                    item.page = pages[i % pages.Count()];
                    item.DetectSquares();
                    await item.Recognize(threshold);

                    if ((i + 1) % pages.Count() == 1)
                    {
                        buffer += pid.ToString() + ",";
                    }
                    foreach (var _answers in item.answers)
                    {
                        buffer += String.Join(";", _answers);
                        buffer += ",";
                    }

                    if ((i + 1) % pages.Count() == 0)
                    {
                        buffer += "\n";
                        pid++;
                    }

                    action(i, files.Count());
                    i++;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
            }

            StopRecognize = false;
            resultBuffer = buffer;
        }
    }
}
