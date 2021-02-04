using System;
using System.Threading.Tasks;
using Mark2;

namespace Mark2CF
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Survey survey = new Survey();

            await survey.Recognize((i, max) => {
            });
            Console.WriteLine("OK");
        }
    }
}
