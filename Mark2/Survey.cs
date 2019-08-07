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
        Windows.Storage.StorageFolder folder;
        Windows.Storage.StorageFile csv;
        List<Item> items;

        public Survey(Windows.Storage.StorageFolder folder, Windows.Storage.StorageFile csv)
        {
            items = new List<Item>();
            System.Diagnostics.Debug.WriteLine(folder.Path);
            this.folder = folder;
            this.csv = csv;
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
                Item item = new Item();
                item.image = image;
                System.Diagnostics.Debug.WriteLine(item.image[0, 0].R.ToString());
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
            string buffer = System.Text.Encoding.ASCII.GetString(fileBytes);
            System.Diagnostics.Debug.WriteLine(buffer);
        }
    }
}
