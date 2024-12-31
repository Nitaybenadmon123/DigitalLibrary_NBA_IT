using PayPal.Api;
using System.Collections.Generic;

namespace DigitalLibrary_NBA_IT
{

    public class PayPalService
    {
        private readonly string _clientId = "ATipu-7HyqwQ6UcEWeXgTlJms3dlJdj1ukgj8hCVR1iK48yNp_Nmyi5cj3Z-6MmxfSNgBTrj12SRIrNZ";
        private readonly string _clientSecret = "EBAdF0uuZe0Oz7EJ81X_vnwjfehsrwBSH_POQKeCgcebpHjeJ8B-_R1zztbyPgG_PUHmmw8-eVns2TR5";

        public APIContext GetAPIContext()
        {
            var config = new Dictionary<string, string>
        {
            { "mode", "sandbox" }, // החלף ל-"live" עבור ייצור
            { "clientId", _clientId },
            { "clientSecret", _clientSecret }
        };

            var accessToken = new OAuthTokenCredential(_clientId, _clientSecret, config).GetAccessToken();
            return new APIContext(accessToken);
        }

        public Payment CreatePayment(string returnUrl, string cancelUrl, decimal totalAmount, string currency = "USD")
        {
            var apiContext = GetAPIContext();

            var payment = new Payment
            {
                intent = "sale",
                payer = new Payer { payment_method = "paypal" },
                transactions = new List<Transaction>
            {
                new Transaction
                {
                    description = "Digital Library Purchase",
                    amount = new Amount
                    {
                        currency = currency,
                        total = totalAmount.ToString("F2")
                    }
                }
            },
                redirect_urls = new RedirectUrls
                {
                    cancel_url = cancelUrl,
                    return_url = returnUrl
                }
            };

            return payment.Create(apiContext);
        }
    }

}