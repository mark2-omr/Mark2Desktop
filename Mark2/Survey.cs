using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Mark2
{
    class Survey
    {
        public Windows.Storage.StorageFolder folder;
        public Windows.Storage.StorageFile csv;
        List<Item> items;
        List<Page> pages;

        // public Survey(Windows.Storage.StorageFolder folder, Windows.Storage.StorageFile csv)
        public Survey(){
            items = new List<Item>();
            pages = new List<Page>();
        }

        async public void SetupItems()
        {
            IReadOnlyList<Windows.Storage.StorageFile> files = await folder.GetFilesAsync();

            foreach (var file in files)
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

                SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image
                    = SixLabors.ImageSharp.Image.Load(fileBytes);
                Item item = new Item(image);
                items.Add(item);
            }
        }

        async public void SetupPositions()
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

        public void Recognize()
        {
            for (int i = 0; i < items.Count(); i++)
            {
                items[i].page = pages[i % pages.Count()];
                items[i].DetectSquares();
                items[i].Recognize();
            }
        }
    }
}
