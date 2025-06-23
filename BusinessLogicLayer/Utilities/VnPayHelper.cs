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

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", amount.ToString());
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_TxnRef", orderId);
            vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
            vnpay.AddRequestData("vnp_OrderType", orderType);
            vnpay.AddRequestData("vnp_Locale", locale);
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnpay.AddRequestData("vnp_IpAddr", ipAddr);
            vnpay.AddRequestData("vnp_CreateDate", createDate);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, hashSecret);
            return paymentUrl;
        }
        public static bool ValidateSignature(IDictionary<string, string?> vnpayData, string hashSecret)
        {
            var vnpay = new VnPayLibrary();

            // Add all vnp_ parameters except vnp_SecureHash and vnp_SecureHashType
            foreach (var (key, value) in vnpayData)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_") &&
                    key != "vnp_SecureHash" && key != "vnp_SecureHashType")
                {
                    vnpay.AddResponseData(key, value ?? "");
                }
            }

            string inputHash = vnpayData["vnp_SecureHash"] ?? "";
            bool isValidSignature = vnpay.ValidateSignature(inputHash, hashSecret);
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
        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var data = new StringBuilder();

            // VNPAY requires parameters to be sorted alphabetically
            foreach (var (key, value) in _requestData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                data.Append(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "&");
            }

            string queryString = data.ToString();

            if (queryString.Length > 0)
            {
                queryString = queryString.Remove(data.Length - 1, 1);
            }

            // Create signature data without URL encoding for hash calculation
            var signData = new StringBuilder();
            foreach (var (key, value) in _requestData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                signData.Append(key + "=" + value + "&");
            }

            string signDataString = signData.ToString();
            if (signDataString.Length > 0)
            {
                signDataString = signDataString.Remove(signDataString.Length - 1, 1);
            }

            string vnpSecureHash = HmacSHA512(hashSecret, signDataString);

            return baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnpSecureHash;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            string rspRaw = GetResponseData();
            string myChecksum = HmacSHA512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHashType"))
            {
                _responseData.Remove("vnp_SecureHashType");
            }

            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }

            foreach (var (key, value) in _responseData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                data.Append(key + "=" + value + "&");
            }

            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }

        private static string HmacSHA512(string key, string inputData)
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
