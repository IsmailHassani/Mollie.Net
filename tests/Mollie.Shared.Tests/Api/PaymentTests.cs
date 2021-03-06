﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Mollie.Client;
using Mollie.Models;
using Mollie.Models.List;
using Mollie.Models.Payment;
using Mollie.Models.Payment.Request;
using Mollie.Models.Payment.Response;
using Mollie.Models.Payment.Response.Specific;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mollie.Models.Customer;
using Mollie.Models.Mandate;
using Mollie.Enumerations;
using Mollie.Services;

namespace Mollie.Tests.Api
{
    [TestClass]
    public class PaymentTests : BaseApiTestFixture
    {
        [TestMethod]
        public async Task CanRetrievePaymentList() {
            // When: Retrieve payment list with default settings
            ListResponse<PaymentResponse> response = await PaymentClient.GetPaymentListAsync();

            // Then
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Items);
        }

        [TestMethod]
        public async Task ListPaymentsNeverReturnsMorePaymentsThenTheNumberOfRequestedPayments() {
            // When: Number of payments requested is 5
            int numberOfPayments = 5;

            // When: Retrieve 5 payments
            ListResponse<PaymentResponse> response = await PaymentClient.GetPaymentListAsync(null, numberOfPayments);

            // Then
            Assert.IsTrue(response.Items.Count <= numberOfPayments);
        }

        [TestMethod]
        public async Task WhenRetrievingAListOfPaymentsPaymentSubclassesShouldBeInitialized() {
            // Given: We create a new payment 
            IdealPaymentRequest paymentRequest = new IdealPaymentRequest() {
                Amount = new Amount(Currency.EUR, "100.00"),
                Description = "Description",
                RedirectUrl = DefaultRedirectUrl
            };
            await PaymentClient.CreatePaymentAsync(paymentRequest);

            // When: We retrieve it in a list
            ListResponse<PaymentResponse> result = await PaymentClient.GetPaymentListAsync(null, 5);

            // Then: We expect a list with a single ideal payment            
            Assert.IsInstanceOfType(result.Items.First(), typeof(IdealPaymentResponse));
        }

        [TestMethod]
        public async Task CanCreateDefaultPaymentWithOnlyRequiredFields() {
            // When: we create a payment request with only the required parameters
            PaymentRequest paymentRequest = new PaymentRequest() {
                Amount = new Amount(Currency.EUR, "100.00"),
                Description = "Description",
                RedirectUrl = DefaultRedirectUrl
            };

            // When: We send the payment request to Mollie
            PaymentResponse result = await PaymentClient.CreatePaymentAsync(paymentRequest);

            // Then: Make sure we get a valid response
            Assert.IsNotNull(result);
            Assert.AreEqual(paymentRequest.Amount.Currency, result.Amount.Currency);
            Assert.AreEqual(paymentRequest.Amount.Value, result.Amount.Value);
            Assert.AreEqual(paymentRequest.Description, result.Description);
            Assert.AreEqual(paymentRequest.RedirectUrl, result.RedirectUrl);
        }
        
        [TestMethod]
        public async Task CanCreateDefaultPaymentWithAllFields() {
            // If: we create a payment request where all parameters have a value
            PaymentRequest paymentRequest = new PaymentRequest() {
                Amount = new Amount(Currency.EUR, "100.00"),
                Description = "Description",
                RedirectUrl = DefaultRedirectUrl,
                Locale = Locale.nl_NL,
                Metadata = "{\"firstName\":\"John\",\"lastName\":\"Doe\"}",
                Method = PaymentMethods.BankTransfer,
                WebhookUrl = DefaultWebhookUrl
            };

            // When: We send the payment request to Mollie
            PaymentResponse result = await PaymentClient.CreatePaymentAsync(paymentRequest);

            // Then: Make sure all requested parameters match the response parameter values
            Assert.IsNotNull(result);
            Assert.AreEqual(paymentRequest.Amount.Currency, result.Amount.Currency);
            Assert.AreEqual(paymentRequest.Amount.Value, result.Amount.Value);
            Assert.AreEqual(paymentRequest.Description, result.Description);
            Assert.AreEqual(paymentRequest.RedirectUrl, result.RedirectUrl);
            Assert.AreEqual(paymentRequest.Locale, result.Locale);
            Assert.AreEqual(paymentRequest.Metadata, result.Metadata);
            Assert.AreEqual(paymentRequest.WebhookUrl, result.WebhookUrl);
        }

        [Ignore("Outcome depends on payment methods active in portal")]
        [DataTestMethod]
        [DataRow(typeof(IdealPaymentRequest), PaymentMethods.Ideal, typeof(IdealPaymentResponse))]
        [DataRow(typeof(CreditCardPaymentRequest), PaymentMethods.CreditCard, typeof(CreditCardPaymentResponse))]
        [DataRow(typeof(PaymentRequest), PaymentMethods.Bancontact, typeof(BancontactPaymentResponse))]
        [DataRow(typeof(PaymentRequest), PaymentMethods.Sofort, typeof(SofortPaymentResponse))]
        [DataRow(typeof(BankTransferPaymentRequest), PaymentMethods.BankTransfer, typeof(BankTransferPaymentResponse))]
        [DataRow(typeof(PayPalPaymentRequest), PaymentMethods.PayPal, typeof(PayPalPaymentResponse))]
        [DataRow(typeof(BitcoinPaymentRequest), PaymentMethods.Bitcoin, typeof(BitcoinPaymentResponse))]
        [DataRow(typeof(PaymentRequest), PaymentMethods.Belfius, typeof(BelfiusPaymentResponse))]
        [DataRow(typeof(KbcPaymentRequest), PaymentMethods.Kbc, typeof(KbcPaymentResponse))]
        [DataRow(typeof(PaymentRequest), null, typeof(PaymentResponse))]
        //[TestCase(typeof(Przelewy24PaymentRequest), PaymentMethod.Przelewy24, typeof(PaymentResponse))] // Payment option is not enabled in website profile
        public async Task CanCreateSpecificPaymentType(Type paymentType, PaymentMethods? paymentMethod, Type expectedResponseType) {
            // If: we create a specific payment type with some bank transfer specific values
            PaymentRequest paymentRequest = (PaymentRequest) Activator.CreateInstance(paymentType);
            paymentRequest.Amount = new Amount(Currency.EUR, "100.00");
            paymentRequest.Description = "Description";
            paymentRequest.RedirectUrl = DefaultRedirectUrl;
            paymentRequest.Method = paymentMethod;

            // Set required billing email for Przelewy24
            if (paymentRequest is Przelewy24PaymentRequest request)
            {
                request.BillingEmail = "example@example.com";
            }

            // When: We send the payment request to Mollie
            PaymentResponse result = await PaymentClient.CreatePaymentAsync(paymentRequest);

            // Then: Make sure all requested parameters match the response parameter values
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResponseType, result.GetType());
            Assert.AreEqual(paymentRequest.Amount.Currency, result.Amount.Currency);
            Assert.AreEqual(paymentRequest.Amount.Value, result.Amount.Value);
            Assert.AreEqual(paymentRequest.Description, result.Description);
            Assert.AreEqual(paymentRequest.RedirectUrl, result.RedirectUrl);
        }

        [TestMethod]
        public async Task CanCreatePaymentAndRetrieveIt() {
            // If: we create a new payment request
            PaymentRequest paymentRequest = new PaymentRequest() {
                Amount = new Amount(Currency.EUR, "100.00"),
                Description = "Description",
                RedirectUrl = DefaultRedirectUrl,
                Locale = Locale.de_DE
            };

            // When: We send the payment request to Mollie and attempt to retrieve it
            PaymentResponse paymentResponse = await PaymentClient.CreatePaymentAsync(paymentRequest);
            PaymentResponse result = await PaymentClient.GetPaymentAsync(paymentResponse.Id);

            // Then
            Assert.IsNotNull(result);
            Assert.AreEqual(paymentResponse.Id, result.Id);
            Assert.AreEqual(paymentResponse.Amount.Currency, result.Amount.Currency);
            Assert.AreEqual(paymentResponse.Amount.Value, result.Amount.Value);
            Assert.AreEqual(paymentResponse.Description, result.Description);
            Assert.AreEqual(paymentResponse.RedirectUrl, result.RedirectUrl);
        }

        [TestMethod]
        public async Task CanCreateRecurringPaymentAndRetrieveIt() {
            // If: we create a new recurring payment
            MandateResponse mandate = await GetFirstValidMandate();
            CustomerResponse customer = await CustomerClient.GetCustomerAsync(mandate.Links.Customer);
            PaymentRequest paymentRequest = new PaymentRequest() {
                Amount = new Amount(Currency.EUR, "100.00"),
                Description = "Description",
                RedirectUrl = DefaultRedirectUrl,
                SequenceType = SequenceType.First,
                CustomerId = customer.Id
            };

            // When: We send the payment request to Mollie and attempt to retrieve it
            PaymentResponse paymentResponse = await PaymentClient.CreatePaymentAsync(paymentRequest);
            PaymentResponse result = await PaymentClient.GetPaymentAsync(paymentResponse.Id);

            // Then: Make sure the recurringtype parameter is entered
            Assert.AreEqual(SequenceType.First, result.SequenceType);
        }

        [TestMethod]
        public async Task CanCreatePaymentWithMetaData() {
            // If: We create a payment with meta data
            string metadata = "this is my metadata";
            PaymentRequest paymentRequest = new PaymentRequest() {
                Amount = new Amount(Currency.EUR, "100.00"),
                Description = "Description",
                RedirectUrl = DefaultRedirectUrl,
                Metadata = metadata
            };

            // When: We send the payment request to Mollie
            PaymentResponse result = await PaymentClient.CreatePaymentAsync(paymentRequest);

            // Then: Make sure we get the same json result as metadata
            Assert.AreEqual(metadata, result.Metadata);
        }

        [TestMethod]
        public async Task CanCreatePaymentWithJsonMetaData() {
            // If: We create a payment with meta data
            string json = "{\"order_id\":\"4.40\"}";
            PaymentRequest paymentRequest = new PaymentRequest() {
                Amount = new Amount(Currency.EUR, "100.00"),
                Description = "Description",
                RedirectUrl = DefaultRedirectUrl,
                Metadata = json
            };

            // When: We send the payment request to Mollie
            PaymentResponse result = await PaymentClient.CreatePaymentAsync(paymentRequest);

            // Then: Make sure we get the same json result as metadata
            Assert.AreEqual(json, result.Metadata);
        }

        [TestMethod]
        public async Task CanCreatePaymentWithCustomMetaDataClass() {
            // If: We create a payment with meta data
            CustomMetadataClass metadataRequest = new CustomMetadataClass() {
                OrderId = 1,
                Description = "Custom description"
            };

            PaymentRequest paymentRequest = new PaymentRequest() {
                Amount = new Amount(Currency.EUR, "100.00"),
                Description = "Description",
                RedirectUrl = DefaultRedirectUrl,
            };
            paymentRequest.SetMetadata(metadataRequest);

            // When: We send the payment request to Mollie
            PaymentResponse result = await PaymentClient.CreatePaymentAsync(paymentRequest);
            CustomMetadataClass metadataResponse = result.GetMetadata<CustomMetadataClass>();

            // Then: Make sure we get the same json result as metadata
            Assert.IsNotNull(metadataResponse);
            Assert.AreEqual(metadataRequest.OrderId, metadataResponse.OrderId);
            Assert.AreEqual(metadataRequest.Description, metadataResponse.Description);
        }

        [TestMethod]
        public async Task CanCreatePaymentWithMandate() {
            // If: We create a payment with a mandate id
            MandateResponse validMandate = await GetFirstValidMandate();
            CustomerResponse customer = await CustomerClient.GetCustomerAsync(validMandate.Links.Customer);
            PaymentRequest paymentRequest = new PaymentRequest() {
                Amount = new Amount(Currency.EUR, "100.00"),
                Description = "Description",
                RedirectUrl = DefaultRedirectUrl,
                SequenceType = SequenceType.Recurring,
                CustomerId = customer.Id,
                MandateId = validMandate.Id
            };

            // When: We send the payment request to Mollie
            PaymentResponse result = await PaymentClient.CreatePaymentAsync(paymentRequest);

            // Then: Make sure we get the mandate id back in the details
            Assert.AreEqual(validMandate.Id, result.MandateId);
        }

        //[TestMethod]
        //public async Task PaymentWithDifferentHttpInstance() {
        //    // If: We create a PaymentClient with our own HttpClient instance
        //    HttpClient myHttpClientInstance = new HttpClient();
        //    PaymentClient paymentClient = new PaymentClient(new ClientService(ApiTestKey, myHttpClientInstance));
        //    PaymentRequest paymentRequest = new PaymentRequest() {
        //        Amount = new Amount(Currency.EUR, "100.00"),
        //        Description = "Description",
        //        RedirectUrl = DefaultRedirectUrl
        //    };

        //    // When: I create a new payment
        //    PaymentResponse result = await paymentClient.CreatePaymentAsync(paymentRequest);

        //    // Then: It should still work... lol
        //    Assert.IsNotNull(result);
        //    Assert.AreEqual(paymentRequest.Amount.Currency, result.Amount.Currency);
        //    Assert.AreEqual(paymentRequest.Amount.Value, result.Amount.Value);
        //    Assert.AreEqual(paymentRequest.Description, result.Description);
        //    Assert.AreEqual(paymentRequest.RedirectUrl, result.RedirectUrl);
        //}

        private async Task<MandateResponse> GetFirstValidMandate() {
            ListResponse<CustomerResponse> customers = await CustomerClient.GetCustomerListAsync();
            if (!customers.Items.Any()) {
                Assert.Inconclusive("No customers found. Unable to test recurring payment tests");
            }

            foreach (CustomerResponse customer in customers.Items) {
                ListResponse<MandateResponse> customerMandates = await MandateClient.GetMandateListAsync(customer.Id);
                MandateResponse firstValidMandate = customerMandates.Items.FirstOrDefault(x => x.Status == MandateStatus.Valid);
                if (firstValidMandate != null) {
                    return firstValidMandate;
                }
            }

            Assert.Inconclusive("No mandates found. Unable to test recurring payments");
            return null;
        }
    }

    public class CustomMetadataClass {
        public int OrderId { get; set; }
        public string Description { get; set; }
    }
}
