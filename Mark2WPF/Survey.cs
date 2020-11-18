﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using SixLabors.ImageSharp;

namespace Mark2
{
    class Survey
    {
        //public StorageFolder folder;
        //public StorageFolder textFolder;
        //public StorageFolder logFolder;
        //public StorageFile csv;

        public string folderPath;
        public string csvPath;

        public string textFolderPath;
        public string logFolderPath;

        public double areaThreshold;
        public double colorThreshold;
        public List<Item> items;
        List<Page> pages;
        public List<List<string>> resultRows;
        public bool StopRecognize { get; set; }

        public Survey()
        {
            folderPath = null;
            csvPath = null;

            items = new List<Item>();
            pages = new List<Page>();
            StopRecognize = false;
        }

        async public Task SetupPositions()
        {
            //byte[] fileBytes = null;
            //using (var stream = await csv.OpenReadAsync())
            //{
            //    fileBytes = new byte[stream.Size];
            //    using (var reader = new Windows.Storage.Streams.DataReader(stream))
            //    {
            //        await reader.LoadAsync((uint)stream.Size);
            //        reader.ReadBytes(fileBytes);
            //    }
            //}

            //string csvString = System.Text.Encoding.GetEncoding("UTF-8").GetString(fileBytes);
            StreamReader csvStreamReader = new StreamReader(this.csvPath);
            string csvString = csvStreamReader.ReadToEnd();
            List<string> lines = csvString.Split("\n").ToList();

            List<int> vs = new List<int>();
            List<string> headers = lines[0].Split("\t").ToList();
            headers.RemoveRange(0, 4);
            for (int i = 0; i < headers.Count() / 4; i++)
            {
                vs.Add(int.Parse(headers[i * 4]));
            }

            lines.RemoveRange(0, 3);
            foreach (string line in lines)
            {
                List<string> values = line.Split("\t").ToList();
                if (values.Count() < 4)
                {
                    continue;
                }

                int pageNumber = int.Parse(values[2]);
                while (pages.Count() < pageNumber)
                {
                    pages.Add(new Page());
                }

                Question question = new Question();
                question.text = values[1];
                question.type = int.Parse(values[3]);

                values.RemoveRange(0, 4);
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
            //textFolder = await folder.CreateFolderAsync($"text_{dateTime.ToString("yyyyMMdd_HHmmss")}",
            //    Windows.Storage.CreationCollisionOption.ReplaceExisting);
            //logFolder = await folder.CreateFolderAsync($"log_{dateTime.ToString("yyyyMMdd_HHmmss")}",
            //    Windows.Storage.CreationCollisionOption.ReplaceExisting);

            textFolderPath = folderPath + Path.DirectorySeparatorChar + $"text_{dateTime.ToString("yyyyMMdd_HHmmss")}";
            logFolderPath = folderPath + Path.DirectorySeparatorChar + $"log_{dateTime.ToString("yyyyMMdd_HHmmss")}";

            // TODO: 動作を確認する
            Directory.CreateDirectory(textFolderPath);
            Directory.CreateDirectory(logFolderPath);

        }

        public async Task Recognize(Action<int, int> action)
        {
            //var files = await folder.GetFilesAsync();
            var files = Directory.GetFiles(this.folderPath);

            // TODO: 文字認識は後で対応する
            //LearningModel mnistModel;
            //var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/mnist_8.onnx"));
            //mnistModel = await LearningModel.LoadFromStorageFileAsync(modelFile);

            resultRows = new List<List<string>>();

            var resultHeader = new List<string>();
            var numQuestions = 0;
            foreach (var page in pages)
            {
                numQuestions += page.questions.Count();
            }
            resultHeader.Add("No");
            resultHeader.Add("File");
            for (var qid = 0; qid < numQuestions; qid++)
            {
                resultHeader.Add((qid + 1).ToString());
            }
            resultRows.Add(resultHeader);

            resultHeader = new List<string>();
            resultHeader.Add("");
            resultHeader.Add("");
            foreach (var page in pages)
            {
                foreach (var question in page.questions)
                {
                    resultHeader.Add(question.text);
                }
            }
            resultRows.Add(resultHeader);

            var i = 0;
            var pid = 1;
            var resultRow = new List<string>();
            var fileNames = new List<string>();

            foreach (var file in files)
            {
                if (StopRecognize)
                {
                    break;
                }

                try
                {
                    byte[] fileBytes = null;

                    //using (var stream = await file.OpenReadAsync())
                    //{
                    //    fileBytes = new byte[stream.Size];
                    //    using (var reader = new Windows.Storage.Streams.DataReader(stream))
                    //    {
                    //        await reader.LoadAsync((uint)stream.Size);
                    //        reader.ReadBytes(fileBytes);
                    //    }
                    //}
                    fileBytes = File.ReadAllBytes(file);

                    string fileName = Path.GetFileName(file);

                    var image = Image.Load(fileBytes);
                    //Item item = new Item(pid, file.Name, image, textFolder, logFolder, mnistModel);
                    Item item = new Item(pid, fileName, image, textFolderPath, logFolderPath);

                    item.page = pages[i % pages.Count()];
                    item.DetectSquares();
                    await item.Recognize(areaThreshold, colorThreshold);

                    //fileNames.Add(file.Name);
                    fileNames.Add(fileName);

                    if ((i + 1) % pages.Count() == 1 || pages.Count() == 1)
                    {
                        resultRow.Add(pid.ToString());
                        resultRow.Add("");
                    }
                    foreach (var _answers in item.answers)
                    {
                        resultRow.Add(String.Join(";", _answers));
                    }

                    if ((i + 1) % pages.Count() == 0)
                    {
                        resultRow[1] = String.Join(";", fileNames);
                        resultRows.Add(resultRow);
                        resultRow = new List<string>();
                        fileNames = new List<string>();
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
        }
    }
}
