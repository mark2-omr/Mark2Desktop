﻿using System;
using System.Threading.Tasks;
using Mark2;

namespace Mark2CF
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Survey survey = new Survey();
            survey.areaThreshold = 0.4;
            survey.colorThreshold = 0.1;

            survey.folderPath = "gs://mark2-storage/images";
            survey.csvPath = "gs://mark2-storage/csv/input.csv";

            // CSVファイルを読み込む
            survey.SetupPositions();

            await survey.Recognize((i, max) => {
            });
            Console.WriteLine("OK");
        }
    }
}
