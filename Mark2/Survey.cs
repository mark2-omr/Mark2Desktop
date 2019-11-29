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

        async public Task SetupItems()
        {
            var files = await folder.GetFilesAsync();

            LearningModel mnistModel;
            var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/mnist_7.onnx"));
            mnistModel = await LearningModel.LoadFromStorageFileAsync(modelFile);

            foreach (var file in files)
            {
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
                    Item item = new Item(file.Name, image, mnistModel);
                    items.Add(item);
                }
                catch
                {

                }
            }
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

        public async Task SetupLogFolder()
        {
            DateTime dateTime = DateTime.Now;
            logFolder = await folder.CreateFolderAsync($"log{dateTime.ToString("yyyyMMdd_HHmmss")}",
                Windows.Storage.CreationCollisionOption.ReplaceExisting);
            for(int i = 0; i < items.Count; i++)
            {
                items[i].logFolder = logFolder;
            }
        }

        public async Task Recognize(Action<int, int> action)
        {
            for (int i = 0; i < items.Count(); i++)
            {
                if (StopRecognize)
                {
                    break;
                }
                items[i].page = pages[i % pages.Count()];
                items[i].DetectSquares();
                await items[i].Recognize(threshold);

                action(i, items.Count());
            }

            StopRecognize = false;

            var buffer = "";

            var numQuestions = 0;
            foreach (var page in pages)
            {
                numQuestions += page.questions.Count();
            }
            for(var i = 0; i < numQuestions; i++)
            {
                buffer += "," + (i + 1).ToString();
            }
            buffer += "\n";

            var label = 1;
            for (int i = 0; i < items.Count(); i++)
            {
                if ((i + 1) % pages.Count() == 1)
                {
                    buffer += label.ToString() + ",";
                }
                foreach (var _answers in items[i].answers)
                {
                    buffer += String.Join(";", _answers);
                    buffer += ",";
                }

                if ((i + 1) % pages.Count() == 0)
                {
                    buffer += "\n";
                    label++;
                }
            }
            // return buffer;
            resultBuffer = buffer;
        }
    }
}
