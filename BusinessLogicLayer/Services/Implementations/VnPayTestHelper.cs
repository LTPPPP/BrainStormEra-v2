using System;
using System.Collections.Generic;
using System.Text;
using BusinessLogicLayer.Utilities;
using BusinessLogicLayer.Services.Interfaces;

namespace BusinessLogicLayer.Services.Implementations
{
    public class VnPayTestHelper : IVnPayTestHelper
    {
        // VNPAY Test Configuration
        private const string _vnp_TmnCode = "2QXUI4J4";
        private const string _vnp_HashSecret = "OHVTVTPZNEJ6X6FUVXTZCRJNPUBOMVTZ";
        private const string _vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

        public void TestVnPaySignature()
        {
            Console.WriteLine("=== VNPAY Test Signature Generation ===");

            var testData = new Dictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _vnp_TmnCode,
                ["vnp_Amount"] = "10000000", // 100,000 VND
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = "TEST123456789",
                ["vnp_OrderInfo"] = "Test payment",
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = "vi",
                ["vnp_ReturnUrl"] = "https://localhost:5001/Payment/PaymentReturn",
                ["vnp_IpAddr"] = "127.0.0.1",
                ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss")
            };

            // Create signature using VNPAY library
            var vnpay = new VnPayLibrary();
            foreach (var kvp in testData)
            {
                vnpay.AddRequestData(kvp.Key, kvp.Value);
            }

            var paymentUrl = vnpay.CreateRequestUrl(_vnp_Url, _vnp_HashSecret);
            Console.WriteLine($"Generated Payment URL: {paymentUrl}");

            // Extract signature for verification
            var uri = new Uri(paymentUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var signature = query["vnp_SecureHash"];
            Console.WriteLine($"Generated Signature: {signature}");

            // Test signature validation
            var responseData = new Dictionary<string, string?>();
            foreach (var key in query.AllKeys)
            {
                if (key != null)
                {
                    responseData[key] = query[key];
                }
            }

            bool isValid = VnPayHelper.ValidateSignature(responseData, _vnp_HashSecret);
            Console.WriteLine($"Signature Validation Result: {isValid}");

            Console.WriteLine("=== End VNPAY Test ===");
        }

        public string CreateTestPaymentUrl(string orderId, decimal amount, string orderInfo)
        {
            return VnPayHelper.CreatePaymentUrl(
                _vnp_Url,
                _vnp_TmnCode,
                _vnp_HashSecret,
                orderId,
                orderInfo,
                (long)(amount * 100),
                "other",
                "vi",
                "https://localhost:5001/Payment/PaymentReturn",
                DateTime.Now.ToString("yyyyMMddHHmmss"),
                "127.0.0.1"
            );
        }
    }
}