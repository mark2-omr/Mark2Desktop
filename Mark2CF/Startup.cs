using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mark2;

namespace Mark2CF
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    int x = 0;

                    string key = context.Request.Query["key"];

                    if (key == null)
                    {
                        key = "";
                    }

                    if (key.Length > 10)
                    {
                        string bucketName = "mark2-storage-dev";

                        string keyPath = key + "/";

                        string folderPath = keyPath + "images/";

                        Survey survey = new Survey();
                        survey.areaThreshold = 0.4;
                        survey.colorThreshold = 0.1;

                        survey.bucketName = bucketName;
                        survey.folderPath = folderPath;
                        survey.csvPath = keyPath + "positions/positions.txt";

                        survey.SetupPositions();

                        await survey.Recognize((i, max) =>
                        {
                            Console.WriteLine("{0}/{1}", i, max);
                            x++;
                        });
                    }

                    await context.Response.WriteAsync(x.ToString());
                });
            });
        }
    }
}