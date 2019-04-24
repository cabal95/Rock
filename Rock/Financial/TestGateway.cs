// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Web.UI;

using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace Rock.Financial
{
    /// <summary>
    /// Test Payment Gateway
    /// </summary>
    [DisplayName( "Test Payment Gateway" )]
    [Description( "Provides a way to test CC transactions without actually charging any money." )]
    [Export( typeof( GatewayComponent ) )]
    [ExportMetadata( "ComponentName", "TestGateway" )]

    [TextField( "Declined Card Numbers", "Enter partial card numbers that you wish to be declined separated by commas. Any card number that ends with a number matching a value entered here will be declined.", false, "", "", 0 )]
    public class TestGateway : GatewayComponent, IAutomatedGatewayComponent, IHostedGatewayComponent
    {
        #region IHostedGatewayComponent Implementation

        /// <summary>
        /// Gets the URL that the Gateway Information UI will navigate to when they click the 'Configure' link
        /// </summary>
        /// <value>
        /// The configure URL.
        /// </value>
        public string ConfigureURL => "https://www.rockrms.com/";

        /// <summary>
        /// Gets the URL that the Gateway Information UI will navigate to when they click the 'Learn More' link
        /// </summary>
        /// <value>
        /// The learn more URL.
        /// </value>
        public string LearnMoreURL => "https://www.rockrms.com/pricing/";

        /// <summary>
        /// Gets the hosted payment information control which will be used to collect CreditCard, ACH fields
        /// Note: A HostedPaymentInfoControl can optionally implement <seealso cref="IHostedGatewayPaymentControl" />
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="controlId">The control identifier.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public Control GetHostedPaymentInfoControl( FinancialGateway financialGateway, string controlId, HostedPaymentInfoControlOptions options )
        {
            return new CreditCard
            {
                ID = controlId,
                PromptForNameOnCard = false
            };
        }

        /// <summary>
        /// Gets the JavaScript needed to tell the hostedPaymentInfoControl to get send the paymentInfo and get a token.
        /// Have your 'Next' or 'Submit' call this so that the hostedPaymentInfoControl will fetch the token/response
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="hostedPaymentInfoControl">The hosted payment information control.</param>
        /// <returns></returns>
        public string GetHostPaymentInfoSubmitScript( FinancialGateway financialGateway, Control hostedPaymentInfoControl )
        {
            return $"window.location = $('#{hostedPaymentInfoControl.ClientID}').data('postback-script');";
        }

        /// <summary>
        /// Gets the paymentInfoToken that the hostedPaymentInfoControl returned (see also <seealso cref="M:Rock.Financial.IHostedGatewayComponent.GetHostedPaymentInfoControl(Rock.Model.FinancialGateway,System.String)" />)
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="hostedPaymentInfoControl">The hosted payment information control.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public string GetHostedPaymentInfoToken( FinancialGateway financialGateway, Control hostedPaymentInfoControl, out string errorMessage )
        {
            errorMessage = null;

            if ( hostedPaymentInfoControl is CreditCard creditCardControl )
            {
                //
                // If they have not entered any information but we have a token, re-use the token.
                //
                if ( creditCardControl.IsEmpty && creditCardControl.Token.IsNotNullOrWhiteSpace() )
                {
                    return creditCardControl.Token;
                }

                if ( !creditCardControl.IsValid )
                {
                    errorMessage = "Invalid or incomplete payment information";
                    return null;
                }

                var paymentInfo = new CreditCardPaymentInfo
                {
                    Number = creditCardControl.CardNumber,
                    ExpirationDate = creditCardControl.Expiration.Value,
                    Code = creditCardControl.SecurityCode
                };

                if ( !ValidateCard( financialGateway, paymentInfo, out errorMessage ) )
                {
                    return null;
                }

                string token = $"TTK{RockDateTime.Now.ToString( "yyyyMMddHHmmssFFF" )}";
                creditCardControl.Clear();
                creditCardControl.Token = token;

                AddCachedToken( token, ( CreditCardPaymentInfo.GetCreditCardType( paymentInfo.Number )?.Id ?? 0 ).ToString() );

                return token;
            }
            else
            {
                errorMessage = "Invalid control";
                return null;
            }
        }

        /// <summary>
        /// Creates the customer account using a token received from the HostedPaymentInfoControl <seealso cref="GetHostedPaymentInfoControl(FinancialGateway, bool, string)" />
        /// and returns a customer account token that can be used for future transactions.
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="paymentToken">The payment token.</param>
        /// <param name="paymentInfo">The payment information.</param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public string CreateCustomerAccount( FinancialGateway financialGateway, string paymentToken, PaymentInfo paymentInfo, out string errorMessage )
        {
            if ( !paymentToken.StartsWith( "TTK" ) )
            {
                errorMessage = "Invalid payment token";
                return null;
            }

            errorMessage = null;

            return paymentToken;
        }

        /// <summary>
        /// Gets the earliest scheduled start date that the gateway will accept for the start date, based on the current local time.
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <returns></returns>
        public DateTime GetEarliestScheduledStartDate( FinancialGateway financialGateway )
        {
            return DateTime.SpecifyKind( RockDateTime.Now, DateTimeKind.Local ).AddDays( 1 ).ToUniversalTime().Date;
        }

        #endregion

        #region Automated Gateway Component

        /// <summary>
        /// The most recent exception thrown by the gateway's remote API
        /// </summary>
        public Exception MostRecentException { get; private set; }
        
        /// <summary>
        /// Handle a payment from a REST endpoint or other automated means. This payment can only be made with a saved account.
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="metadata">Optional. Metadata key value pairs to send to the gateway</param>
        /// <returns></returns>
        public Payment AutomatedCharge( FinancialGateway financialGateway, ReferencePaymentInfo paymentInfo, out string errorMessage, Dictionary<string, string> metadata = null )
        {
            MostRecentException = null;
            errorMessage = string.Empty;
            var transaction = Charge( financialGateway, paymentInfo, out errorMessage );

            if ( !string.IsNullOrEmpty( errorMessage ) )
            {
                MostRecentException = new Exception( errorMessage );
                return null;
            }

            if ( transaction == null )
            {
                errorMessage = "No error was indicated but the transaction was null";
                MostRecentException = new Exception( errorMessage );
                return null;
            }

            return new Payment
            {
                TransactionCode = transaction.TransactionCode
            };
        }

        #endregion

        #region Gateway Component Implementation

        /// <summary>
        /// Gets the supported payment schedules.
        /// </summary>
        /// <value>
        /// The supported payment schedules.
        /// </value>
        public override List<DefinedValueCache> SupportedPaymentSchedules
        {
            get
            {
                var values = new List<DefinedValueCache>();
                values.Add( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME ) );
                values.Add( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_WEEKLY ) );
                values.Add( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_BIWEEKLY ) );
                values.Add( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_TWICEMONTHLY ) );
                values.Add( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_MONTHLY ) );
                return values;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the gateway requires the name on card for CC processing
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <returns></returns>
        /// <value>
        ///   <c>true</c> if [name on card required]; otherwise, <c>false</c>.
        /// </value>
        public override bool PromptForNameOnCard( FinancialGateway financialGateway )
        {
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether [address required].
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <returns></returns>
        /// <value>
        ///   <c>true</c> if [address required]; otherwise, <c>false</c>.
        /// </value>
        public override bool PromptForBillingAddress( FinancialGateway financialGateway )
        {
            return false;
        }

        /// <summary>
        /// Charges the specified payment info.
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override FinancialTransaction Charge( FinancialGateway financialGateway, PaymentInfo paymentInfo, out string errorMessage )
        {
            errorMessage = string.Empty;

            if ( ValidateCard( financialGateway, paymentInfo, out errorMessage ) )
            {
                var transaction = new FinancialTransaction();
                transaction.TransactionCode = "T" + RockDateTime.Now.ToString( "yyyyMMddHHmmssFFF" );
                transaction.FinancialPaymentDetail = new FinancialPaymentDetail
                {
                    CurrencyTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD.AsGuid() )
                };

                if ( paymentInfo is CreditCardPaymentInfo ccInfo )
                {
                    transaction.FinancialPaymentDetail.CreditCardTypeValueId = CreditCardPaymentInfo.GetCreditCardType( ccInfo.Number )?.Id;
                }

                if ( paymentInfo is ReferencePaymentInfo refInfo )
                {
                    var cardTypeId = GetCachedToken( refInfo.GatewayPersonIdentifier ).AsIntegerOrNull();
                    if ( cardTypeId.HasValue && cardTypeId.Value != 0 )
                    {
                        transaction.FinancialPaymentDetail.CreditCardTypeValueId = cardTypeId;
                    }

                }

                return transaction;
            }

            return null;
        }

        /// <summary>
        /// Credits the specified transaction.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override FinancialTransaction Credit( FinancialTransaction transaction, decimal amount, string comment, out string errorMessage )
        {
            errorMessage = string.Empty;

            var refundTransaction = new FinancialTransaction();
            refundTransaction.TransactionCode = "T" + RockDateTime.Now.ToString( "yyyyMMddHHmmssFFF" );
            return refundTransaction;
        }

        /// <summary>
        /// Adds the scheduled payment.
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override FinancialScheduledTransaction AddScheduledPayment( FinancialGateway financialGateway, PaymentSchedule schedule, PaymentInfo paymentInfo, out string errorMessage )
        {
            errorMessage = string.Empty;

            if ( ValidateCard( financialGateway, paymentInfo, out errorMessage ) )
            {
                var scheduledTransaction = new FinancialScheduledTransaction();
                scheduledTransaction.IsActive = true;
                scheduledTransaction.StartDate = schedule.StartDate;
                scheduledTransaction.NextPaymentDate = schedule.StartDate;
                scheduledTransaction.TransactionCode = "T" + RockDateTime.Now.ToString( "yyyyMMddHHmmssFFF" );
                scheduledTransaction.GatewayScheduleId = "P" + RockDateTime.Now.ToString( "yyyyMMddHHmmssFFF" );
                scheduledTransaction.LastStatusUpdateDateTime = RockDateTime.Now;
                return scheduledTransaction;
            }

            return null;
        }

        /// <summary>
        /// Reactivates the scheduled payment.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool ReactivateScheduledPayment( FinancialScheduledTransaction transaction, out string errorMessage )
        {
            transaction.IsActive = true;
            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Updates the scheduled payment.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool UpdateScheduledPayment( FinancialScheduledTransaction transaction, PaymentInfo paymentInfo, out string errorMessage )
        {
            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Cancels the scheduled payment.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool CancelScheduledPayment( FinancialScheduledTransaction transaction, out string errorMessage )
        {
            transaction.IsActive = false;
            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Gets the scheduled payment status.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool GetScheduledPaymentStatus( FinancialScheduledTransaction transaction, out string errorMessage )
        {
            transaction.LastStatusUpdateDateTime = RockDateTime.Now;
            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Gets the payments that have been processed for any scheduled transactions
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override List<Payment> GetPayments( FinancialGateway financialGateway, DateTime startDate, DateTime endDate, out string errorMessage )
        {
            errorMessage = string.Empty;
            var fakePayments = new List<Payment>();
            var randomNumberOfPayments = new Random().Next( 1, 1000 );
            var rockContext = new Rock.Data.RockContext();
            var scheduledTransactionList = new FinancialScheduledTransactionService( rockContext ).Queryable().ToList();

            var transactionDateTime = startDate;
            for( int paymentNumber = 0; paymentNumber < randomNumberOfPayments; paymentNumber++  )
            {
                var scheduledTransaction = scheduledTransactionList.OrderBy( a => a.Guid ).FirstOrDefault();
                var fakePayment = new Payment
                {
                    Amount = scheduledTransaction.TotalAmount,
                    TransactionDateTime = startDate,
                    CreditCardTypeValue = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.FINANCIAL_CREDIT_CARD_TYPE.AsGuid() ).DefinedValues.OrderBy( a => Guid.NewGuid() ).First(),
                    CurrencyTypeValue = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD.AsGuid() ),
                    TransactionCode = Guid.NewGuid().ToString("N"),
                    GatewayScheduleId = scheduledTransaction.GatewayScheduleId
                };

                fakePayments.Add( fakePayment );
            }
            

            return fakePayments;
        }

        /// <summary>
        /// Gets an optional reference identifier needed to process future transaction from saved account.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override string GetReferenceNumber( FinancialTransaction transaction, out string errorMessage )
        {
            errorMessage = string.Empty;
            return string.Empty;
        }

        /// <summary>
        /// Gets an optional reference identifier needed to process future transaction from saved account.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override string GetReferenceNumber( FinancialScheduledTransaction scheduledTransaction, out string errorMessage )
        {
            errorMessage = string.Empty;
            return string.Empty;
        }

        /// <summary>
        /// Gets the next payment date.
        /// </summary>
        /// <param name="scheduledTransaction">The transaction.</param>
        /// <param name="lastTransactionDate">The last transaction date.</param>
        /// <returns></returns>
        public override DateTime? GetNextPaymentDate( FinancialScheduledTransaction scheduledTransaction, DateTime? lastTransactionDate )
        {
            return CalculateNextPaymentDate( scheduledTransaction, lastTransactionDate );
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the card.
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="paymentInfo">The payment information.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        private bool ValidateCard( FinancialGateway financialGateway, PaymentInfo paymentInfo, out string errorMessage )
        {
            string cardNumber = string.Empty;
            string code = string.Empty;

            CreditCardPaymentInfo ccPayment = paymentInfo as CreditCardPaymentInfo;
            if ( ccPayment != null )
            {
                cardNumber = ccPayment.Number;
                code = ccPayment.Code;
            }

            SwipePaymentInfo swipePayment = paymentInfo as SwipePaymentInfo;
            if ( swipePayment != null )
            {
                cardNumber = swipePayment.Number;
            }

            if ( paymentInfo is ReferencePaymentInfo referencePaymentInfo )
            {
                errorMessage = string.Empty;
                return GetCachedToken( referencePaymentInfo.GatewayPersonIdentifier ) != null;
            }

            if ( code == "911" )
            {
                errorMessage = "Error processing Credit Card!";
                return false;
            }

            if ( !string.IsNullOrWhiteSpace( cardNumber ) )
            {
                var declinedNumers = GetAttributeValue( financialGateway, "DeclinedCardNumbers" );
                if ( !string.IsNullOrWhiteSpace( declinedNumers ) )
                {
                    if ( declinedNumers.SplitDelimitedValues().Any( n => cardNumber.EndsWith( n ) ) )
                    {
                        errorMessage = "Error processing Credit Card!";
                        return false;
                    }
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Adds a cached token for later processing.
        /// </summary>
        private void AddCachedToken( string token, string value )
        {
            RockCache.AddOrUpdate( $"core.testgateway.cachedtoken.{token}", null, value, TimeSpan.FromHours( 1 ) );
        }

        /// <summary>
        /// Gets the cached token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>The cached token value or null.</returns>
        private string GetCachedToken( string token )
        {
            return ( string ) RockCache.Get( $"core.testgateway.cachedtoken.{token}", null );
        }

        #endregion
    }
}
