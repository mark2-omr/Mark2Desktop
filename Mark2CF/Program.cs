using System;
using System.Threading.Tasks;
using Mark2;
//using Google.Cloud.Storage.V1;

namespace Mark2CF
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string bucketName = "mark2-storage-dev";

            //var storage = StorageClient.Create();
            //var objects = storage.ListObjects(bucketName);

            //foreach (var file in objects)
            //{
            //    Console.WriteLine("{0}", file.Name);
            //}

            Survey survey = new Survey();
            survey.areaThreshold = 0.4;
            survey.colorThreshold = 0.1;

            survey.folderPath = "gs://" + bucketName + "/images";
            survey.csvPath = "gs://" + bucketName + "/csv/input.csv";

            // CSVファイルを読み込む
            survey.SetupPositions();

            await survey.Recognize((i, max) =>
            {
            });
            Console.WriteLine("OK");
        }
    }
}
