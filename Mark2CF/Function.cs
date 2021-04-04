using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using Mark2;
using Google.Cloud.Storage.V1;

namespace Mark2CF
{
    class Function : IHttpFunction
    {
        public async Task HandleAsync(HttpContext context)
        {
            string keyPath = "";
            if (context.Request.Query.ContainsKey("key") == true)
            {
                keyPath = "/" + context.Request.Query["key"];
            }

            string bucketName = "mark2-storage-dev";
            string folderPath = "images" + keyPath;

            //var storage = StorageClient.Create();
            //var objects = storage.ListObjects(bucketName);


            string output = "";
            //foreach (var file in objects)
            //{
            //    output += file.Name + "#";
            //}


            Survey survey = new Survey();
            survey.areaThreshold = 0.4;
            survey.colorThreshold = 0.1;

            survey.bucketName = bucketName;
            survey.folderPath = folderPath;
            survey.csvPath = "gs://" + bucketName + "/csv" + keyPath + "/input.csv";

            // TODO: キーのパスにファイルがなければ処理を中止するようにする
            survey.SetupPositions();

            await survey.Recognize((i, max) =>
            {

            });

            await context.Response.WriteAsync(output);
        }
    }
}
