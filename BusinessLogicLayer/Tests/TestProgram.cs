using System;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Tests
{
    public class TestProgram
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Payment Test Program");
            Console.WriteLine("============================");

            // PaymentTests is temporarily disabled due to refactoring
            // var paymentTests = new PaymentTests();
            // await paymentTests.RunPaymentTest();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
