using System.Text;
using System.Security.Cryptography;

namespace BusinessLogicLayer.Utilities
{
    public class VnPayHelper
    {
        public static string CreatePaymentUrl(string vnp_Url, string vnp_TmnCode, string hashSecret,
            string orderId, string orderInfo, long amount, string orderType, string locale,
            string returnUrl, string createDate, string ipAddr = "127.0.0.1")
        {
            var vnpay = new VnPayLibrary();

            // Log input parameters and returnUrl
            Console.WriteLine($"Creating VNPAY URL with returnUrl: {returnUrl}");

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode.Trim());
            vnpay.AddRequestData("vnp_Amount", (amount).ToString());
            vnpay.AddRequestData("vnp_CreateDate", createDate);
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", ipAddr.Trim());
            vnpay.AddRequestData("vnp_Locale", locale.Trim());
            vnpay.AddRequestData("vnp_OrderInfo", orderInfo.Trim());
            vnpay.AddRequestData("vnp_OrderType", orderType.Trim());
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl.Trim());
            vnpay.AddRequestData("vnp_TxnRef", orderId.Trim());

            // Log all request parameters
            Console.WriteLine("\nVNPAY Request Parameters:");
            foreach (var item in vnpay.GetRequestData())
            {
                Console.WriteLine($"{item.Key}: {item.Value}");
            }

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url.Trim(), hashSecret.Trim());

            // Log final URL
            Console.WriteLine($"\nGenerated Payment URL: {paymentUrl}");

            return paymentUrl;
        }
        public static bool ValidateSignature(IDictionary<string, string?> vnpayData, string hashSecret)
        {
            var vnpay = new VnPayLibrary();

            // Add all vnp_ parameters except vnp_SecureHash and vnp_SecureHashType
            foreach (var (key, value) in vnpayData.OrderBy(kvp => kvp.Key))
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_") &&
                    key != "vnp_SecureHash" && key != "vnp_SecureHashType" &&
                    !string.IsNullOrEmpty(value))
                {
                    vnpay.AddResponseData(key, value);
                }
            }

            string inputHash = vnpayData["vnp_SecureHash"] ?? "";
            bool isValidSignature = vnpay.ValidateSignature(inputHash, hashSecret);

            // Debug logging
            Console.WriteLine("Validating VNPAY signature:");
            Console.WriteLine($"Input Hash: {inputHash}");

            return isValidSignature;
        }
    }

    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new();
        private readonly SortedList<string, string> _responseData = new();

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        // New method to get request data
        public IDictionary<string, string> GetRequestData()
        {
            return new Dictionary<string, string>(_requestData);
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var data = new StringBuilder();
            var signData = new StringBuilder();

            // Sort the parameters by key
            var sortedParams = new SortedDictionary<string, string>(_requestData);

            // Build query string and signing data
            foreach (var (key, value) in sortedParams)
            {
                if (string.IsNullOrEmpty(value)) continue;

                // Build query string with URL encoded values
                if (data.Length > 0)
                {
                    data.Append('&');
                }
                data.Append(key).Append('=').Append(Uri.EscapeDataString(value));

                // Build signing string without URL encoding
                if (signData.Length > 0)
                {
                    signData.Append('&');
                }
                signData.Append(key).Append('=').Append(value);
            }

            // Calculate secure hash
            string signDataString = signData.ToString();
            Console.WriteLine($"\nData to hash: {signDataString}");
            Console.WriteLine($"Hash secret: {hashSecret}");

            string vnpSecureHash = HmacSHA512(hashSecret, signDataString);
            Console.WriteLine($"Generated hash: {vnpSecureHash}");

            // Build final URL
            string finalUrl = baseUrl;
            if (baseUrl.Contains("?"))
            {
                finalUrl += "&";
            }
            else
            {
                finalUrl += "?";
            }
            finalUrl += data.ToString() + "&vnp_SecureHash=" + vnpSecureHash;

            return finalUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            string rspRaw = GetResponseData();
            string myChecksum = HmacSHA512(secretKey, rspRaw);

            // Debug logging
            Console.WriteLine($"Data to verify: {rspRaw}");
            Console.WriteLine($"Hash secret: {secretKey}");
            Console.WriteLine($"Calculated hash: {myChecksum}");

            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();

            // Create a sorted dictionary to ensure parameters are in alphabetical order
            var sortedData = new SortedDictionary<string, string>(_responseData);

            foreach (var (key, value) in sortedData)
            {
                if (!string.IsNullOrEmpty(value) &&
                    key != "vnp_SecureHashType" &&
                    key != "vnp_SecureHash")
                {
                    if (data.Length > 0)
                    {
                        data.Append('&');
                    }
                    data.Append(key).Append('=').Append(value);
                }
            }

            return data.ToString();
        }

        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }
    }
}
