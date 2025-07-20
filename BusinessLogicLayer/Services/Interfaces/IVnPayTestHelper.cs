namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IVnPayTestHelper
    {
        void TestVnPaySignature();
        string CreateTestPaymentUrl(string orderId, decimal amount, string orderInfo);
    }
}