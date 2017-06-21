using Mollie.Net.Enumerations;
using Mollie.Net.Models.Payment.Response;

namespace Mollie.Net.Models.Payment.Responses {
    public class SofortPaymentResponse : PaymentResponse {
        public SofortPaymentResponseDetails Details { get; set; }
    }

    public class SofortPaymentResponseDetails {
        /// <summary>
        /// Only available if the payment has been completed � The consumer's name.
        /// </summary>
        public string ConsumerName { get; set; }

        /// <summary>
        /// Only available if the payment has been completed � The consumer's IBAN.
        /// </summary>
        public string ConsumerAccount { get; set; }

        /// <summary>
        /// Only available if the payment has been completed � The consumer's bank's BIC.
        /// </summary>
        public string ConsumerBic { get; set; }
    }
}