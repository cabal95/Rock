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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Humanizer;

using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Financial;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Event
{
    /// <summary>
    /// Block used to register for a registration instance.
    /// </summary>
    [DisplayName( "Registration Entry2" )]
    [Category( "Event" )]
    [Description( "Block used to register for a registration instance." )]

    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT, "", 0 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS, "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING, "", 1 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE, "Source", "The Financial Source Type to use when creating transactions", false, false, Rock.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_WEBSITE, "", 2 )]
    [TextField( "Batch Name Prefix", "The batch prefix name to use when creating a new batch", false, "Event Registration", "", 3 )]
    [BooleanField( "Display Progress Bar", "Display a progress bar for the registration.", true, "", 4 )]
    [BooleanField( "Allow InLine Digital Signature Documents", "Should inline digital documents be allowed? This requires that the registration template is configured to display the document inline", true, "", 6, "SignInline" )]
    [SystemEmailField( "Confirm Account Template", "Confirm Account Email Template", false, Rock.SystemGuid.SystemEmail.SECURITY_CONFIRM_ACCOUNT, "", 7 )]
    [TextField( "Family Term", "The term to use for specifying which household or family a person is a member of.", true, "immediate family", "", 8 )]
    [BooleanField( "Force Email Update", "Force the email to be updated on the person's record.", false, "", 9 )]

    public partial class RegistrationEntry2 : RockBlock
    {
        #region Fields

        private bool _saveNavigationHistory = false;

        // Page (query string) parameter names
        private const string REGISTRATION_ID_PARAM_NAME = "RegistrationId";
        private const string SLUG_PARAM_NAME = "Slug";
        private const string START_AT_BEGINNING = "StartAtBeginning";
        private const string REGISTRATION_INSTANCE_ID_PARAM_NAME = "RegistrationInstanceId";
        private const string EVENT_OCCURRENCE_ID_PARAM_NAME = "EventOccurrenceId";
        private const string GROUP_ID_PARAM_NAME = "GroupId";
        private const string CAMPUS_ID_PARAM_NAME = "CampusId";

        // Viewstate keys
        private const string SIGN_INLINE_KEY = "SignInline";
        private const string DIGITAL_SIGNATURE_COMPONENT_TYPE_NAME_KEY = "DigitalSignatureComponentTypeName";
        private const string CURRENT_PANEL_KEY = "CurrentPanel";
        private const string MINIMUM_PAYMENT_KEY = "MinimumPayment";

        // protected variables
        protected Rock.Web.UI.Controls.Event.HowManyRegistrants _howMany;
        protected Rock.Web.UI.Controls.Event.RegistrantControl _registrantControl;
        protected Rock.Web.UI.Controls.Event.RegistrarInfo _registrarInfo;
        protected Rock.Web.UI.Controls.Event.ProgressBar _summaryProgress;
        protected Rock.Web.UI.Controls.Event.ProgressBar _successProgress;
        protected Rock.Web.UI.Controls.Event.RegistrantsReview _registrantsReview;
        protected Rock.Web.UI.Controls.Event.PaymentSummary _paymentSummary;
        protected Rock.Web.UI.Controls.Event.ProcessPayment _processPayment;

        #endregion

        #region Properties

        protected Rock.Web.UI.Controls.Event.RegistrationState RegistrationState;

        // Digital Signature Fields
        private bool SignInline { get; set; }
        private string DigitalSignatureComponentTypeName { get; set; }
        private DigitalSignatureComponent DigitalSignatureComponent { get; set; }
        protected string Step2IFrameUrl { get; set; }
        // TODO: Step2IFrameUrl = ResolveRockUrl( threeStepGateway.Step2FormUrl );

        // The current panel to display ( HowMany
        private int CurrentPanel { get; set; }

        // The minimum payment that is due 
        private decimal? minimumPayment { get; set; }

        /// <summary>
        /// Gets or sets the payment transaction code. Used to help double-charging
        /// </summary>
        protected string TransactionCode
        {
            get { return ViewState["TransactionCode"] as string ?? string.Empty; }
            set { ViewState["TransactionCode"] = value; }
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            if ( ViewState["RegistrationState"] != null )
            {
                RegistrationState = ( Rock.Web.UI.Controls.Event.RegistrationState ) ViewState["RegistrationState"];
            }

            SignInline = ViewState[SIGN_INLINE_KEY] as bool? ?? false;
            DigitalSignatureComponentTypeName = ViewState[DIGITAL_SIGNATURE_COMPONENT_TYPE_NAME_KEY] as string;
            if ( !string.IsNullOrWhiteSpace( DigitalSignatureComponentTypeName ) )
            {
                DigitalSignatureComponent = DigitalSignatureContainer.GetComponent( DigitalSignatureComponentTypeName );
            }

            CurrentPanel = ViewState[CURRENT_PANEL_KEY] as int? ?? 0;
            minimumPayment = ViewState[MINIMUM_PAYMENT_KEY] as decimal?;

            CreateDynamicControls( false );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // make sure that a URL with navigation history parameters is really from a browser navigation and not a Link or Refresh
            hfAllowNavigate.Value = false.ToTrueFalse();

            _howMany = new Rock.Web.UI.Controls.Event.HowManyRegistrants();
            _howMany.ID = "HowMany";
            phHowMany.Controls.Add( _howMany );

            _registrantControl = new Rock.Web.UI.Controls.Event.RegistrantControl();
            _registrantControl.ID = "RegistrantControl";
            _registrantControl.Next += lbRegistrantNext_Click;
            _registrantControl.SelectedPersonChanged += _registrantControl_SelectedPersonChanged;
            _registrantControl.FamilyTerm = GetAttributeValue( "FamilyTerm" );
            phRegistrant.Controls.Add( _registrantControl );

            _summaryProgress = new Rock.Web.UI.Controls.Event.ProgressBar();
            _summaryProgress.ID = "SummaryProgress";
            _summaryProgress.Visible = GetAttributeValue( "DisplayProgressBar" ).AsBoolean();
            phSummaryControls.Controls.Add( _summaryProgress );

            _registrarInfo = new Rock.Web.UI.Controls.Event.RegistrarInfo();
            _registrarInfo.ID = "RegistrarInfo";
            phSummaryControls.Controls.Add( _registrarInfo );

            _registrantsReview = new Rock.Web.UI.Controls.Event.RegistrantsReview();
            _registrantsReview.ID = "RegistrantsReview";
            phSummaryControls.Controls.Add( _registrantsReview );

            _successProgress = new Rock.Web.UI.Controls.Event.ProgressBar();
            _successProgress.ID = "SuccessProgress";
            _successProgress.Visible = GetAttributeValue( "DisplayProgressBar" ).AsBoolean();
            phSuccessProgress.Controls.Add( _successProgress );

            _paymentSummary = new Rock.Web.UI.Controls.Event.PaymentSummary();
            _paymentSummary.ID = "PaymentSummary";
            _paymentSummary.DiscountCodeChanged += _paymentSummary_DiscountApplied;
            phCostAndFees.Controls.Add( _paymentSummary );

            _processPayment = new Rock.Web.UI.Controls.Event.ProcessPayment();
            _processPayment.ID = "ProcessPayment";
            _processPayment.ValidationGroup = BlockValidationGroup;
            _processPayment.Step2Returned += processPayment_Step2Returned;
            phPayment.Controls.Add( _processPayment );

            RegisterClientScript();
        }

        private void processPayment_Step2Returned( object sender, EventArgs e )
        {
            if ( CurrentPanel == 2 || CurrentPanel == 3 )
            {
                int? registrationId = SaveChanges();
                if ( registrationId.HasValue )
                {
                    ShowSuccess( registrationId.Value );
                }
                else
                {
                    if ( CurrentPanel == 2 )
                    {
                        ShowSummary();
                    }
                    else
                    {
                        // Failure on entering payment info, resubmit step 1
                        string errorMessage = string.Empty;
                        if ( _processPayment.ProcessStep1( RegistrationState, out errorMessage ) )
                        {
                            ShowPayment();
                        }
                        else
                        {
                            ShowSummary();
                        }
                    }
                }
            }
            else
            {
                ShowHowMany();
            }

            hfTriggerScroll.Value = "true";
        }

        private void _paymentSummary_DiscountApplied( object sender, EventArgs e )
        {
            string error = null;

            _paymentSummary.UpdateRegistrationState( RegistrationState );
            RegistrationState.ApplyDiscountCode( _paymentSummary.DiscountCode, out error );
            _paymentSummary.SetRegistrationStateDetails( RegistrationState );

            if ( !string.IsNullOrWhiteSpace( error ) )
            {
                nbDiscountCode.Text = error;
                nbDiscountCode.Visible = true;
            }
        }

        private void _registrantControl_SelectedPersonChanged( object sender, Rock.Web.UI.Controls.Event.SelectedPersonChangedEventArgs e )
        {
            _registrantControl.UpdateRegistrationState( RegistrationState );
            _registrantControl.SetRegistrantFields( RegistrationState, e.PersonId );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            _registrantControl.ValidationGroup = BlockValidationGroup;

            // Reset warning/error messages
            nbMain.Visible = false;
            nbWaitingList.Visible = false;
            nbDiscountCode.Visible = false;

            // register navigation event to enable support for the back button
            var sm = ScriptManager.GetCurrent( Page );
            //sm.EnableSecureHistoryState = false;
            sm.Navigate += sm_Navigate;

            // Show save account info based on if checkbox is checked
            divSaveAccount.Style[HtmlTextWriterStyle.Display] = cbSaveAccount.Checked ? "block" : "none";

            // Change the labels for the family member radio buttons
            _registrarInfo.FamilyTerm = GetAttributeValue( "FamilyTerm" );

            if ( !Page.IsPostBack )
            {
                var personDv = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() );
                if ( CurrentPerson != null && CurrentPerson.RecordTypeValue != null && personDv != null && CurrentPerson.RecordTypeValue.Guid != personDv.Guid )
                {
                    ShowError( "Invalid Login", "Sorry, the login you are using doesn't appear to be tied to a valid person record. Try logging out and logging in with a different username, or create a new account before registering for the selected event." );
                }
                else
                {
                    // Get the a registration if it has not already been loaded ( breadcrumbs may have loaded it )
                    if ( RegistrationState.RegistrationInfo != null || SetRegistrationState() )
                    {
                        if ( RegistrationState.RegistrationTemplate != null )
                        {
                            if ( !RegistrationState.RegistrationTemplate.WaitListEnabled && RegistrationState.RegistrationInfo.SlotsAvailable.HasValue && RegistrationState.RegistrationInfo.SlotsAvailable.Value <= 0 )
                            {
                                ShowWarning(
                                    string.Format( "{0} Full", RegistrationState.RegistrationTerm ),
                                    string.Format( "<p>There are not any more {0} available for {1}.</p>", RegistrationState.RegistrationTerm.ToLower().Pluralize(), RegistrationState.RegistrationInstance.Name ) );

                            }
                            else
                            {
                                // Check Login Requirement
                                if ( RegistrationState.RegistrationTemplate.LoginRequired && CurrentUser == null )
                                {
                                    var site = RockPage.Site;
                                    if ( site.LoginPageId.HasValue )
                                    {
                                        site.RedirectToLoginPage( true );
                                    }
                                    else
                                    {
                                        System.Web.Security.FormsAuthentication.RedirectToLoginPage();
                                    }
                                }
                                else
                                {
                                    if ( SignInline &&
                                        !PageParameter( "redirected" ).AsBoolean() &&
                                        DigitalSignatureComponent != null &&
                                        !string.IsNullOrWhiteSpace( DigitalSignatureComponent.CookieInitializationUrl ) )
                                    {
                                        // Redirect for Digital Signature Cookie Initialization 
                                        var returnUrl = GlobalAttributesCache.Read().GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash() + Request.Url.PathAndQuery.RemoveLeadingForwardslash();
                                        returnUrl = returnUrl + ( returnUrl.Contains( "?" ) ? "&" : "?" ) + "redirected=True";
                                        string redirectUrl = string.Format( "{0}?redirect_uri={1}", DigitalSignatureComponent.CookieInitializationUrl, HttpUtility.UrlEncode( returnUrl ) );
                                        Response.Redirect( redirectUrl, false );
                                    }
                                    else
                                    {
                                        // show the panel for asking how many registrants ( it may be skipped )
                                        ShowHowMany();
                                    }
                                }
                            }
                        }
                        else
                        {
                            ShowWarning( "Sorry", string.Format( "The selected {0} could not be found or is no longer active.", RegistrationState.RegistrationTerm.ToLower() ) );
                        }
                    }
                }
            }
            else
            {
                // Load values from controls into the state objects
                ParseDynamicControls();
            }

        }

        public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            var breadCrumbs = new List<BreadCrumb>();

            if ( RegistrationState == null )
            {
                SetRegistrationState();
            }

            if ( RegistrationState.RegistrationInstance != null )
            {
                RockPage.Title = RegistrationState.RegistrationInstance.Name;
                breadCrumbs.Add( new BreadCrumb( RegistrationState.RegistrationInstance.Name, pageReference ) );
                return breadCrumbs;
            }

            breadCrumbs.Add( new BreadCrumb( this.PageCache.PageTitle, pageReference ) );
            return breadCrumbs;
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["RegistrationState"] = RegistrationState;
            ViewState[SIGN_INLINE_KEY] = SignInline;
            ViewState[DIGITAL_SIGNATURE_COMPONENT_TYPE_NAME_KEY] = DigitalSignatureComponentTypeName;
            ViewState[CURRENT_PANEL_KEY] = CurrentPanel;
            ViewState[MINIMUM_PAYMENT_KEY] = minimumPayment;

            return base.SaveViewState();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.PreRender" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnPreRender( EventArgs e )
        {
            if ( _saveNavigationHistory )
            {
                // make sure that a URL with navigation history parameters is really from a browser navigation and not a Link or Refresh
                hfAllowNavigate.Value = true.ToTrueFalse();
                if ( CurrentPanel != 1 )
                {
                    this.AddHistory( "event", string.Format( "{0},0,0", CurrentPanel ) );
                }
                else
                {
                    this.AddHistory( "event", string.Format( "1,{0},{1}", _registrantControl.RegistrantIndex, _registrantControl.FormIndex ) );
                }

            }

            double progress = 0;
            if ( RegistrationState != null && GetAttributeValue( "DisplayProgressBar" ).AsBoolean() )
            {
                int totalSteps = RegistrationState.TotalRegistrantSteps;

                totalSteps += 2; // HowMany + Summary
                totalSteps += RegistrationState.Using3StepGateway ? 1 : 0;

                if ( CurrentPanel == 1 )
                {
                    int completedSteps = 1;
                    completedSteps += RegistrationState.CompletedRegistrantSteps( _registrantControl.RegistrantIndex, _registrantControl.FormIndex );

                    progress = ( double ) completedSteps / totalSteps;

                    _registrantControl.Progress = progress * 100d;
                }
                else if ( CurrentPanel >= 2 )
                {
                    int completedSteps = CurrentPanel - 1;
                    completedSteps += RegistrationState.CompletedRegistrantSteps( null, null );

                    progress = ( double ) completedSteps / totalSteps;

                    _summaryProgress.Progress = progress * 100d;
                }
                else if ( CurrentPanel == 4 )
                {
                    int completedSteps = CurrentPanel - 1;
                    completedSteps += RegistrationState.CompletedRegistrantSteps( null, null );

                    progress = ( double ) completedSteps / totalSteps;

                    _successProgress.Progress = progress * 100d;
                }
            }

            base.OnPreRender( e );
        }

        #endregion

        #region Events

        #region Navigation Events

        /// <summary>
        /// Handles the Navigate event of the sm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="HistoryEventArgs"/> instance containing the event data.</param>
        void sm_Navigate( object sender, HistoryEventArgs e )
        {
            var state = e.State["event"];

            if ( CurrentPanel > 0 && state != null && hfAllowNavigate.Value.AsBoolean() )
            {
                string[] commands = state.Split( ',' );

                int panelId = 0;
                int registrantId = 0;
                int formId = 0;

                if ( commands.Count() == 3 )
                {
                    panelId = Int32.Parse( commands[0] );
                    registrantId = Int32.Parse( commands[1] );
                    formId = Int32.Parse( commands[2] );
                }

                switch ( panelId )
                {
                    case 1:
                        {
                            _registrantControl.SetRegistrationStateDetails( RegistrationState, registrantId, formId );
                            ShowRegistrantPanel();
                            break;
                        }
                    case 2:
                        {
                            ShowSummary();
                            break;
                        }
                    case 3:
                        {
                            ShowPayment();
                            break;
                        }
                    default:
                        {
                            ShowHowMany();
                            break;
                        }
                }
            }
            else
            {
                ShowHowMany();
            }
        }

        /// <summary>
        /// Handles the Click event of the lbHowManyNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbHowManyNext_Click( object sender, EventArgs e )
        {
            _saveNavigationHistory = true;

            // Create registrants based on the number selected
            _howMany.UpdateRegistrationState( RegistrationState, CurrentPerson );

            int registrantIndex;
            int registrantCount = RegistrationState.RegistrationInfo.RegistrantCount;
            for ( registrantIndex = 0; registrantIndex < registrantCount; registrantIndex++ )
            {
                if ( _registrantControl.SetRegistrationStateDetails( RegistrationState, registrantIndex, Rock.Web.UI.Controls.Event.RegistrantFormDirection.Start ) )
                {
                    break;
                }
            }

            if ( registrantIndex < RegistrationState.RegistrationInfo.RegistrantCount )
            {
                ShowRegistrantPanel();
            }
            else
            {
                ShowSummary();
            }

            hfTriggerScroll.Value = "true";
        }

        /// <summary>
        /// Handles the Click event of the lbRegistrantPrev control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbRegistrantPrev_Click( object sender, EventArgs e )
        {
            _registrantControl.UpdateRegistrationState( RegistrationState );

            if ( CurrentPanel == 1 )
            {
                _saveNavigationHistory = true;

                //TODO:hfRequiredDocumentLinkUrl.Value = string.Empty;

                if ( _registrantControl.UpdateRegistrationStateDetails( RegistrationState, Rock.Web.UI.Controls.Event.RegistrantFormDirection.Previous ) )
                {
                    ShowRegistrantPanel();
                }
                else
                {
                    int registrantIndex = _registrantControl.RegistrantIndex - 1;
                    for ( ; registrantIndex >= 0; registrantIndex-- )
                    {
                        if ( _registrantControl.SetRegistrationStateDetails( RegistrationState, registrantIndex, Rock.Web.UI.Controls.Event.RegistrantFormDirection.Last ) )
                        {
                            break;
                        }
                    }

                    if ( registrantIndex >= 0 )
                    {
                        ShowRegistrantPanel();
                    }
                    else
                    {
                        ShowHowMany();
                    }
                }
            }
            else
            {
                ShowHowMany();
            }

            hfTriggerScroll.Value = "true";
        }

        /// <summary>
        /// Handles the Click event of the lbRegistrantNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbRegistrantNext_Click( object sender, EventArgs e )
        {
            _registrantControl.UpdateRegistrationState( RegistrationState );

            if ( CurrentPanel == 1 )
            {
                _saveNavigationHistory = true;

                if ( _registrantControl.UpdateRegistrationStateDetails( RegistrationState, Rock.Web.UI.Controls.Event.RegistrantFormDirection.Next ) )
                {
                    ShowRegistrantPanel();
                }
                else
                {
                    int registrantIndex = _registrantControl.RegistrantIndex + 1;
                    for ( ; registrantIndex < RegistrationState.RegistrationInfo.RegistrantCount; registrantIndex++ )
                    {
                        if ( _registrantControl.SetRegistrationStateDetails( RegistrationState, registrantIndex, Rock.Web.UI.Controls.Event.RegistrantFormDirection.Start ) )
                        {
                            break;
                        }
                    }

                    if ( registrantIndex < RegistrationState.RegistrationInfo.RegistrantCount )
                    {
                        ShowRegistrantPanel();
                    }
                    else
                    {
                        ShowSummary();
                    }
                }
            }
            else
            {
                ShowHowMany();
            }

            hfTriggerScroll.Value = "true";
        }

        /// <summary>
        /// Handles the Click event of the lbSummaryPrev control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSummaryPrev_Click( object sender, EventArgs e )
        {
            if ( CurrentPanel == 2 )
            {
                _saveNavigationHistory = true;

                int registrantIndex = RegistrationState.RegistrationInfo != null ? RegistrationState.RegistrationInfo.RegistrantCount - 1 : 0;
                int formIndex = RegistrationState.FormCount - 1;

                RegistrationState.RegistrationInfo.PaymentAmount = null;

                for ( ; registrantIndex >= 0; registrantIndex-- )
                {
                    if ( _registrantControl.SetRegistrationStateDetails( RegistrationState, registrantIndex, Rock.Web.UI.Controls.Event.RegistrantFormDirection.Last ) )
                    {
                        break;
                    }
                }

                if ( registrantIndex >= 0 )
                {
                    ShowRegistrantPanel();
                }
                else
                {
                    ShowHowMany();
                }
            }
            else
            {
                ShowHowMany();
            }

            hfTriggerScroll.Value = "true";
        }

        /// <summary>
        /// Handles the Click event of the lbSummaryNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSummaryNext_Click( object sender, EventArgs e )
        {
            if ( CurrentPanel == 2 )
            {
                List<string> summaryErrors = ValidateSummary();
                if ( !summaryErrors.Any() )
                {
                    _saveNavigationHistory = true;

                    if ( RegistrationState.Using3StepGateway && RegistrationState.RegistrationInfo.PaymentAmount > 0.0M )
                    {
                        string errorMessage = string.Empty;
                        if ( _processPayment.ProcessStep1( RegistrationState, out errorMessage ) )
                        {
                            ShowPayment();
                        }
                        else
                        {
                            if ( !string.IsNullOrWhiteSpace( errorMessage ) )
                            {
                                throw new Exception( errorMessage );
                            }

                            ShowSummary();
                        }
                    }
                    else
                    {
                        var registrationId = SaveChanges();
                        if ( registrationId.HasValue )
                        {
                            ShowSuccess( registrationId.Value );
                        }
                        else
                        {
                            ShowSummary();
                        }
                    }
                }
                else
                {
                    ShowError( "Please Correct the Following", string.Format( "<ul><li>{0}</li></ul>", summaryErrors.AsDelimited( "</li><li>" ) ) );
                    ShowSummary();
                }
            }
            else
            {
                ShowHowMany();
            }

            hfTriggerScroll.Value = "true";
        }

        protected void lbPaymentPrev_Click( object sender, EventArgs e )
        {
            if ( CurrentPanel == 3 )
            {
                _saveNavigationHistory = true;

                ShowSummary();
            }
            else
            {
                ShowHowMany();
            }

            hfTriggerScroll.Value = "true";
        }

        protected void lbStep2Return_Click( object sender, EventArgs e )
        {
            if ( CurrentPanel == 2 || CurrentPanel == 3 )
            {
                int? registrationId = SaveChanges();
                if ( registrationId.HasValue )
                {
                    ShowSuccess( registrationId.Value );
                }
                else
                {
                    if ( CurrentPanel == 2 )
                    {
                        ShowSummary();
                    }
                    else
                    {
                        // Failure on entering payment info, resubmit step 1
                        string errorMessage = string.Empty;
                        if ( _processPayment.ProcessStep1( RegistrationState, out errorMessage ) )
                        {
                            ShowPayment();
                        }
                        else
                        {
                            ShowSummary();
                        }
                    }
                }
            }
            else
            {
                ShowHowMany();
            }

            hfTriggerScroll.Value = "true";
        }

        #endregion

        #region Success Panel Events

        /// <summary>
        /// Handles the Click event of the lbSaveAccount control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSaveAccount_Click( object sender, EventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( TransactionCode ) )
            {
                nbSaveAccount.Text = "Sorry, the account information cannot be saved as there's not a valid transaction code to reference";
                nbSaveAccount.Visible = true;
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                if ( phCreateLogin.Visible )
                {
                    if ( string.IsNullOrWhiteSpace( txtUserName.Text ) || string.IsNullOrWhiteSpace( txtPassword.Text ) )
                    {
                        nbSaveAccount.Title = "Missing Informaton";
                        nbSaveAccount.Text = "A username and password are required when saving an account";
                        nbSaveAccount.NotificationBoxType = NotificationBoxType.Danger;
                        nbSaveAccount.Visible = true;
                        return;
                    }

                    if ( new UserLoginService( rockContext ).GetByUserName( txtUserName.Text ) != null )
                    {
                        nbSaveAccount.Title = "Invalid Username";
                        nbSaveAccount.Text = "The selected Username is already being used.  Please select a different Username";
                        nbSaveAccount.NotificationBoxType = NotificationBoxType.Danger;
                        nbSaveAccount.Visible = true;
                        return;
                    }

                    if ( !UserLoginService.IsPasswordValid( txtPassword.Text ) )
                    {
                        nbSaveAccount.Title = string.Empty;
                        nbSaveAccount.Text = UserLoginService.FriendlyPasswordRules();
                        nbSaveAccount.NotificationBoxType = NotificationBoxType.Danger;
                        nbSaveAccount.Visible = true;
                        return;
                    }

                    if ( txtPasswordConfirm.Text != txtPassword.Text )
                    {
                        nbSaveAccount.Title = "Invalid Password";
                        nbSaveAccount.Text = "The password and password confirmation do not match";
                        nbSaveAccount.NotificationBoxType = NotificationBoxType.Danger;
                        nbSaveAccount.Visible = true;
                        return;
                    }
                }

                if ( !string.IsNullOrWhiteSpace( txtSaveAccount.Text ) )
                {
                    GatewayComponent gateway = null;
                    if ( RegistrationState.RegistrationTemplate != null && RegistrationState.RegistrationTemplate.FinancialGateway != null )
                    {
                        gateway = RegistrationState.RegistrationTemplate.FinancialGateway.GetGatewayComponent();
                    }

                    if ( gateway != null )
                    {
                        var ccCurrencyType = DefinedValueCache.Read( new Guid( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD ) );
                        string errorMessage = string.Empty;

                        PersonAlias authorizedPersonAlias = null;
                        string referenceNumber = string.Empty;
                        FinancialPaymentDetail paymentDetail = null;
                        int? currencyTypeValueId = ccCurrencyType.Id;

                        var transaction = new FinancialTransactionService( rockContext ).GetByTransactionCode( TransactionCode );
                        if ( transaction != null && transaction.AuthorizedPersonAlias != null )
                        {
                            authorizedPersonAlias = transaction.AuthorizedPersonAlias;
                            if ( transaction.FinancialGateway != null )
                            {
                                transaction.FinancialGateway.LoadAttributes( rockContext );
                            }
                            referenceNumber = gateway.GetReferenceNumber( transaction, out errorMessage );
                            paymentDetail = transaction.FinancialPaymentDetail;
                        }

                        if ( authorizedPersonAlias != null && authorizedPersonAlias.Person != null && paymentDetail != null )
                        {
                            if ( phCreateLogin.Visible )
                            {
                                var user = UserLoginService.Create(
                                    rockContext,
                                    authorizedPersonAlias.Person,
                                    Rock.Model.AuthenticationServiceType.Internal,
                                    EntityTypeCache.Read( Rock.SystemGuid.EntityType.AUTHENTICATION_DATABASE.AsGuid() ).Id,
                                    txtUserName.Text,
                                    txtPassword.Text,
                                    false );

                                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                                mergeFields.Add( "ConfirmAccountUrl", RootPath + "ConfirmAccount" );
                                mergeFields.Add( "Person", authorizedPersonAlias.Person );
                                mergeFields.Add( "User", user );

                                var emailMessage = new RockEmailMessage( GetAttributeValue( "ConfirmAccountTemplate" ).AsGuid() );
                                emailMessage.AddRecipient( new RecipientData( authorizedPersonAlias.Person.Email, mergeFields ) );
                                emailMessage.AppRoot = ResolveRockUrl( "~/" );
                                emailMessage.ThemeRoot = ResolveRockUrl( "~~/" );
                                emailMessage.CreateCommunicationRecord = false;
                                emailMessage.Send();
                            }

                            if ( errorMessage.Any() )
                            {
                                nbSaveAccount.Title = "Invalid Transaction";
                                nbSaveAccount.Text = "Sorry, the account information cannot be saved. " + errorMessage;
                                nbSaveAccount.NotificationBoxType = NotificationBoxType.Danger;
                                nbSaveAccount.Visible = true;
                            }
                            else
                            {
                                if ( authorizedPersonAlias != null )
                                {
                                    var savedAccount = new FinancialPersonSavedAccount();
                                    savedAccount.PersonAliasId = authorizedPersonAlias.Id;
                                    savedAccount.ReferenceNumber = referenceNumber;
                                    savedAccount.Name = txtSaveAccount.Text;
                                    savedAccount.TransactionCode = TransactionCode;
                                    savedAccount.FinancialGatewayId = RegistrationState.RegistrationTemplate.FinancialGateway.Id;
                                    savedAccount.FinancialPaymentDetail = new FinancialPaymentDetail();
                                    savedAccount.FinancialPaymentDetail.AccountNumberMasked = paymentDetail.AccountNumberMasked;
                                    savedAccount.FinancialPaymentDetail.CurrencyTypeValueId = paymentDetail.CurrencyTypeValueId;
                                    savedAccount.FinancialPaymentDetail.CreditCardTypeValueId = paymentDetail.CreditCardTypeValueId;
                                    savedAccount.FinancialPaymentDetail.NameOnCardEncrypted = paymentDetail.NameOnCardEncrypted;
                                    savedAccount.FinancialPaymentDetail.ExpirationMonthEncrypted = paymentDetail.ExpirationMonthEncrypted;
                                    savedAccount.FinancialPaymentDetail.ExpirationYearEncrypted = paymentDetail.ExpirationYearEncrypted;
                                    savedAccount.FinancialPaymentDetail.BillingLocationId = paymentDetail.BillingLocationId;

                                    var savedAccountService = new FinancialPersonSavedAccountService( rockContext );
                                    savedAccountService.Add( savedAccount );
                                    rockContext.SaveChanges();

                                    cbSaveAccount.Visible = false;
                                    txtSaveAccount.Visible = false;
                                    phCreateLogin.Visible = false;
                                    divSaveActions.Visible = false;

                                    nbSaveAccount.Title = "Success";
                                    nbSaveAccount.Text = "The account has been saved for future use";
                                    nbSaveAccount.NotificationBoxType = NotificationBoxType.Success;
                                    nbSaveAccount.Visible = true;
                                }
                            }
                        }
                        else
                        {
                            nbSaveAccount.Title = "Invalid Transaction";
                            nbSaveAccount.Text = "Sorry, the account information cannot be saved as there's not a valid transaction code to reference.";
                            nbSaveAccount.NotificationBoxType = NotificationBoxType.Danger;
                            nbSaveAccount.Visible = true;
                        }
                    }
                    else
                    {
                        nbSaveAccount.Title = "Invalid Gateway";
                        nbSaveAccount.Text = "Sorry, the financial gateway information for this type of transaction is not valid.";
                        nbSaveAccount.NotificationBoxType = NotificationBoxType.Danger;
                        nbSaveAccount.Visible = true;
                    }
                }
                else
                {
                    nbSaveAccount.Title = "Missing Account Name";
                    nbSaveAccount.Text = "Please enter a name to use for this account.";
                    nbSaveAccount.NotificationBoxType = NotificationBoxType.Danger;
                    nbSaveAccount.Visible = true;
                }
            }
        }

        #endregion

        #endregion

        #region Methods

        #region State Methods

        /// <summary>
        /// Sets the registration state
        /// </summary>
        private bool SetRegistrationState()
        {
            string registrationSlug = PageParameter( SLUG_PARAM_NAME );
            int? registrationInstanceId = PageParameter( REGISTRATION_INSTANCE_ID_PARAM_NAME ).AsIntegerOrNull();
            int? registrationId = PageParameter( REGISTRATION_ID_PARAM_NAME ).AsIntegerOrNull();
            int? groupId = PageParameter( GROUP_ID_PARAM_NAME ).AsIntegerOrNull();
            int? campusId = PageParameter( CAMPUS_ID_PARAM_NAME ).AsIntegerOrNull();
            int? eventOccurrenceId = PageParameter( EVENT_OCCURRENCE_ID_PARAM_NAME ).AsIntegerOrNull();

            Rock.Web.UI.Controls.Event.RegistrationError error;

            RegistrationState = new Rock.Web.UI.Controls.Event.RegistrationState();
            RegistrationState.SetRegistrationState( CurrentPerson, out error, registrationSlug, registrationInstanceId, registrationId, groupId, campusId, eventOccurrenceId );

            if ( error != null )
            {
                ShowRegistrationError( error );
                return false;
            }

            return true;
        }

        #endregion

        #region Save Methods

        private List<string> ValidateSummary()
        {
            var validationErrors = new List<string>();

            if ( ( RegistrationState.RegistrationInfo.DiscountCode ?? string.Empty ) != _paymentSummary.DiscountCode )
            {
                validationErrors.Add( "A discount code has not been applied! Please click the 'Apply' button to apply (or clear) a discount code." );
            }

            decimal balanceDue = RegistrationState.RegistrationInfo.DiscountedCost - RegistrationState.RegistrationInfo.PreviousPaymentTotal;
            if ( RegistrationState.RegistrationInfo.PaymentAmount > balanceDue )
            {
                validationErrors.Add( "Amount To Pay is greater than the amount due. Please check the amount you have selected to pay." );
            }

            if ( minimumPayment.HasValue && minimumPayment > 0.0M )
            {
                if ( RegistrationState.RegistrationInfo.PaymentAmount < minimumPayment )
                {
                    validationErrors.Add( string.Format( "Amount To Pay Today must be at least {0:C2}", minimumPayment ) );
                }

                validationErrors.AddRange( _processPayment.Validate() );
            }

            return validationErrors;
        }

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <returns></returns>
        private int? SaveChanges()
        {
            if ( !string.IsNullOrWhiteSpace( TransactionCode ) )
            {
                ShowError( string.Empty, "You have already completed this " + RegistrationState.RegistrationTerm.ToLower() );
                return null;
            }

            Registration registration = null;

            if ( RegistrationState.RegistrationInfo != null && RegistrationState.RegistrationInfo.Registrants.Any() && RegistrationState.RegistrationTemplate != null )
            {
                var rockContext = new RockContext();

                var registrationService = new RegistrationService( rockContext );

                bool isNewRegistration = true;
                var previousRegistrantPersonIds = new List<int>();
                if ( RegistrationState.RegistrationInfo.RegistrationId.HasValue )
                {
                    var previousRegistration = registrationService.Get( RegistrationState.RegistrationInfo.RegistrationId.Value );
                    if ( previousRegistration != null )
                    {
                        isNewRegistration = false;
                        previousRegistrantPersonIds = previousRegistration.Registrants
                            .Where( r => r.PersonAlias != null )
                            .Select( r => r.PersonAlias.PersonId )
                            .ToList();
                    }
                }

                try
                {
                    bool hasPayment = ( RegistrationState.RegistrationInfo.PaymentAmount ?? 0.0m ) > 0.0m;

                    var dvcConnectionStatus = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() );
                    var dvcRecordStatus = DefinedValueCache.Read( GetAttributeValue( "RecordStatus" ).AsGuid() );

                    // Save the registration
                    registration = RegistrationState.SaveRegistration( rockContext, hasPayment, RockPage, dvcConnectionStatus.Id, dvcRecordStatus.Id );
                    if ( registration != null )
                    {
                        // If there is a payment being made, process the payment
                        if ( hasPayment )
                        {
                            string errorMessage = string.Empty;
                            if ( RegistrationState.Using3StepGateway )
                            {
                                int? sourceTypeValueId = null;

                                Guid sourceGuid = Guid.Empty;
                                if ( Guid.TryParse( GetAttributeValue( "Source" ), out sourceGuid ) )
                                {
                                    var source = DefinedValueCache.Read( sourceGuid );
                                    if ( source != null )
                                    {
                                        sourceTypeValueId = source.Id;
                                    }
                                }

                                string batchPrefix = GetAttributeValue( "BatchNamePrefix" );

                                if ( !_processPayment.ProcessStep3( rockContext, RegistrationState, registration, batchPrefix, sourceTypeValueId, out errorMessage ) )
                                {
                                    throw new Exception( errorMessage );
                                }
                            }
                            else
                            {
                                int? sourceTypeValueId = null;

                                Guid sourceGuid = Guid.Empty;
                                if ( Guid.TryParse( GetAttributeValue( "Source" ), out sourceGuid ) )
                                {
                                    var source = DefinedValueCache.Read( sourceGuid );
                                    if ( source != null )
                                    {
                                        sourceTypeValueId = source.Id;
                                    }
                                }

                                string batchPrefix = GetAttributeValue( "BatchNamePrefix" );

                                if ( !_processPayment.ChargePayment( rockContext, RegistrationState, registration, batchPrefix, sourceTypeValueId, out errorMessage ) )
                                {
                                    throw new Exception( errorMessage );
                                }
                            }
                        }

                        // If there is a valid registration, and nothing went wrong processing the payment, add registrants to group and send the notifications
                        if ( registration != null && !registration.IsTemporary )
                        {
                            Rock.Web.UI.Controls.Event.RegistrationError error;

                            RegistrationState.ProcessPostSave( isNewRegistration, registration, previousRegistrantPersonIds, rockContext, RockPage, out error );
                            if ( error != null )
                            {
                                ShowRegistrationError( error );
                            }
                        }
                    }

                }
                catch ( Exception ex )
                {
                    ExceptionLogService.LogException( ex, Context, this.RockPage.PageId, this.RockPage.Site.Id, CurrentPersonAlias );

                    string message = ex.Message;
                    while ( ex.InnerException != null )
                    {
                        ex = ex.InnerException;
                        message = ex.Message;
                    }

                    ShowError( "An Error Occurred Processing Your " + RegistrationState.RegistrationTerm, ex.Message );

                    // Try to delete the registration if it was just created
                    try
                    {
                        if ( isNewRegistration && registration != null && registration.Id > 0 )
                        {
                            RegistrationState.RegistrationInfo.RegistrationId = null;
                            using ( var newRockContext = new RockContext() )
                            {
                                HistoryService.DeleteChanges( newRockContext, typeof( Registration ), registration.Id );

                                var newRegistrationService = new RegistrationService( newRockContext );
                                var newRegistration = newRegistrationService.Get( registration.Id );
                                if ( newRegistration != null )
                                {
                                    newRegistrationService.Delete( newRegistration );
                                    newRockContext.SaveChanges();
                                }
                            }
                        }
                    }
                    catch { }

                    return ( int? ) null;
                }
            }

            return registration != null ? registration.Id : ( int? ) null;
        }

        #endregion

        #region Display Methods

        /// <summary>
        /// Shows the how many panel
        /// </summary>
        private void ShowHowMany()
        {
            _howMany.SetRegistrationStateDetails( RegistrationState );

            // If this is an existing registration, go directly to the summary
            if ( !Page.IsPostBack && RegistrationState.RegistrationInfo != null && RegistrationState.RegistrationInfo.RegistrationId.HasValue && !PageParameter( START_AT_BEGINNING ).AsBoolean() )
            {
                // check if template does not allow updating the saved registration, if so hide the back button on the summary screen
                if ( !RegistrationState.RegistrationTemplate.AllowExternalRegistrationUpdates )
                {
                    lbSummaryPrev.Visible = false;
                }
                ShowSummary();
            }
            else
            {
                int max = RegistrationState.MaxRegistrants;
                if ( !RegistrationState.RegistrationTemplate.WaitListEnabled && RegistrationState.RegistrationInfo.SlotsAvailable.HasValue && RegistrationState.RegistrationInfo.SlotsAvailable.Value < max )
                {
                    max = RegistrationState.RegistrationInfo.SlotsAvailable.Value;
                }

                if ( max > RegistrationState.MinRegistrants )
                {
                    SetPanel( 0 );
                }
                else
                {
                    // ... else skip to the registrant panel
                    RegistrationState.UpdateRegistrantCount( RegistrationState.MinRegistrants, CurrentPerson );

                    int registrantCount = RegistrationState.RegistrationInfo.RegistrantCount;
                    for ( int registrantIndex = 0; registrantIndex < registrantCount; registrantIndex++ )
                    {
                        if ( _registrantControl.SetRegistrationStateDetails( RegistrationState, registrantIndex, Rock.Web.UI.Controls.Event.RegistrantFormDirection.Start ) )
                        {
                            return;
                        }
                    }

                    ShowSummary();
                }
            }
        }

        /// <summary>
        /// Shows the registrant panel
        /// </summary>
        private void ShowRegistrantPanel()
        {
            _registrantControl.UpdateRegistrationState( RegistrationState );

            if ( RegistrationState.RegistrationInfo != null && RegistrationState.RegistrationInfo.RegistrantCount > 0 )
            {
                int max = RegistrationState.MaxRegistrants;
                if ( !RegistrationState.RegistrationTemplate.WaitListEnabled && RegistrationState.RegistrationInfo.SlotsAvailable.HasValue && RegistrationState.RegistrationInfo.SlotsAvailable.Value < max )
                {
                    max = RegistrationState.RegistrationInfo.SlotsAvailable.Value;
                }

                if ( _registrantControl.RegistrantIndex == 0 && _registrantControl.FormIndex == 0 && (
                        PageParameter( START_AT_BEGINNING ).AsBoolean() ||
                        RegistrationState.RegistrationInfo.RegistrationId.HasValue ||
                        max <= RegistrationState.MinRegistrants ) )
                {
                    lbRegistrantPrev.Visible = false;
                }
                else
                {
                    lbRegistrantPrev.Visible = true;
                }

                var registrant = RegistrationState.RegistrationInfo.Registrants[_registrantControl.RegistrantIndex];
                if ( registrant != null )
                {
                    SetPanel( 1 );
                }
            }
        }

        /// <summary>
        /// Shows the summary panel
        /// </summary>
        private void ShowSummary()
        {
            _registrarInfo.SetRegistrationStateDetails( RegistrationState );
            _paymentSummary.SetRegistrationStateDetails( RegistrationState );

            SetPanel( 2 );
        }

        /// <summary>
        /// Shows the payment panel.
        /// </summary>
        private void ShowPayment()
        {
            decimal currentStep = ( RegistrationState.FormCount * RegistrationState.RegistrationInfo.RegistrantCount ) + 2;

            SetPanel( 3 );

            // TODO: Process as a non-3step
        }

        /// <summary>
        /// Shows the success panel
        /// </summary>
        private void ShowSuccess( int registrationId )
        {
            decimal currentStep = ( RegistrationState.FormCount * RegistrationState.RegistrationInfo.RegistrantCount ) + ( RegistrationState.Using3StepGateway ? 3 : 2 );

            lSuccessTitle.Text = "Congratulations";
            lSuccess.Text = "You have successfully completed this registration.";

            try
            {
                var rockContext = new RockContext();
                var registration = new RegistrationService( rockContext )
                    .Queryable( "RegistrationInstance.RegistrationTemplate" )
                    .FirstOrDefault( r => r.Id == registrationId );

                if ( registration != null &&
                    registration.RegistrationInstance != null &&
                    registration.RegistrationInstance.RegistrationTemplate != null )
                {
                    var template = registration.RegistrationInstance.RegistrationTemplate;

                    var mergeFields = new Dictionary<string, object>();
                    mergeFields.Add( "CurrentPerson", CurrentPerson );
                    mergeFields.Add( "RegistrationInstance", registration.RegistrationInstance );
                    mergeFields.Add( "Registration", registration );

                    if ( template != null && !string.IsNullOrWhiteSpace( template.SuccessTitle ) )
                    {
                        lSuccessTitle.Text = template.SuccessTitle.ResolveMergeFields( mergeFields );
                    }
                    else
                    {
                        lSuccessTitle.Text = "Congratulations";
                    }

                    if ( template != null && !string.IsNullOrWhiteSpace( template.SuccessText ) )
                    {
                        lSuccess.Text = template.SuccessText.ResolveMergeFields( mergeFields );
                    }
                    else
                    {
                        lSuccess.Text = "You have successfully completed this " + RegistrationState.RegistrationTerm.ToLower();
                    }

                }

                if ( _paymentSummary.AmountPaid.HasValue &&
                    _paymentSummary.AmountPaid.Value > 0.0M &&
                    ( false /* TODO:rblSavedCC.Items.Count == 0 || ( rblSavedCC.SelectedValueAsId() ?? 0 ) == 0*/ ) )
                {
                    cbSaveAccount.Visible = true;
                    pnlSaveAccount.Visible = true;
                    txtSaveAccount.Visible = true;

                    // If current person does not have a login, have them create a username and password
                    phCreateLogin.Visible = !new UserLoginService( rockContext ).GetByPersonId( CurrentPersonId ).Any();
                }
                else
                {
                    pnlSaveAccount.Visible = false;
                }
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, Context, this.RockPage.PageId, this.RockPage.Site.Id, CurrentPersonAlias );
            }

            SetPanel( 4 );
        }

        /// <summary>
        /// Creates the dynamic controls, and shows correct panel
        /// </summary>
        /// <param name="currentPanel">The current panel.</param>
        private void SetPanel( int currentPanel )
        {
            CurrentPanel = currentPanel;

            CreateDynamicControls( true );

            pnlHowMany.Visible = CurrentPanel <= 0;
            pnlRegistrant.Visible = CurrentPanel == 1;

            pnlSummaryAndPayment.Visible = CurrentPanel == 2 || CurrentPanel == 3;

            phSummaryControls.Visible = CurrentPanel == 2;
            if ( currentPanel != 2 )
            {
                pnlCostAndFees.Visible = false;
            }

            lbSummaryPrev.Visible = CurrentPanel == 2;
            lbSummaryNext.Visible = CurrentPanel == 2;

            lbPaymentPrev.Visible = CurrentPanel == 3;
            aStep2Submit.Visible = currentPanel == 3;

            pnlSuccess.Visible = CurrentPanel == 4;

            lSummaryAndPaymentTitle.Text = ( currentPanel == 2 && RegistrationState.RegistrationTemplate != null ) ? "Review " + RegistrationState.RegistrationTemplate.RegistrationTerm : "Payment Method";
            _processPayment.Title = currentPanel == 2 ? "Payment Method" : "";
        }

        /// <summary>
        /// Shows a warning message.
        /// </summary>
        /// <param name="heading">The heading.</param>
        /// <param name="text">The text.</param>
        private void ShowWarning( string heading, string text )
        {
            nbMain.Heading = heading;
            nbMain.Text = string.Format( "<p>{0}</p>", text );
            nbMain.NotificationBoxType = NotificationBoxType.Warning;
            nbMain.Visible = true;
        }

        /// <summary>
        /// Shows an error message.
        /// </summary>
        /// <param name="heading">The heading.</param>
        /// <param name="text">The text.</param>
        private void ShowError( string heading, string text )
        {
            nbMain.Heading = heading;
            nbMain.Text = string.Format( "<p>{0}</p>", text );
            nbMain.NotificationBoxType = NotificationBoxType.Danger;
            nbMain.Visible = true;
        }

        /// <summary>
        /// Shows an error message.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="text">The text.</param>
        private void ShowRegistrationError( Rock.Web.UI.Controls.Event.RegistrationError error )
        {
            nbMain.Heading = error.Title;
            nbMain.Text = string.Format( "<p>{0}</p>", error.Description );
            nbMain.NotificationBoxType = error.Type;
            nbMain.Visible = true;
        }

        /// <summary>
        /// Registers the client script.
        /// </summary>
        private void RegisterClientScript()
        {
            RockPage.AddScriptLink( ResolveUrl( "~/Scripts/jquery.creditCardTypeDetector.js" ) );

            string script = string.Format( @"
    // Hide or show a div based on selection of checkbox
    $('input:checkbox.toggle-input').unbind('click').on('click', function () {{
        $(this).parents('.checkbox').next('.toggle-content').slideToggle();
    }});

    if ( $('#{1}').val() == 'true' ) {{
        setTimeout('window.scrollTo(0,0)',0);
        $('#{1}').val('')
    }}

    $('#aStep2Submit').on('click', function(e) {{
        e.preventDefault();
        {0};
    }});
", _processPayment.Step3FinishJavascript             // {0}
            , hfTriggerScroll.ClientID               // {1}
);

            ScriptManager.RegisterStartupScript( Page, Page.GetType(), "registrationEntry", script, true );
        }

        #endregion

        #region Dynamic Control Methods

        /// <summary>
        /// Creates the dynamic controls fore each panel
        /// </summary>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void CreateDynamicControls( bool setValues )
        {
            switch ( CurrentPanel )
            {
                case 2:
                    CreateSummaryControls( setValues );
                    break;
            }
        }

        /// <summary>
        /// Parses the dynamic controls.
        /// </summary>
        private void ParseDynamicControls()
        {
            switch ( CurrentPanel )
            {
                case 2:
                    ParseSummaryControls();
                    break;
            }
        }

        #region Summary/Payment Controls

        private void CreateSummaryControls( bool setValues )
        {
            if ( setValues && RegistrationState.RegistrationInfo != null && RegistrationState.RegistrationInstance != null )
            {

                lbSummaryNext.Text = "Finish";

                decimal? minimumInitialPayment = RegistrationState.RegistrationTemplate.MinimumInitialPayment;
                if ( RegistrationState.RegistrationTemplate.SetCostOnInstance ?? false )
                {
                    minimumInitialPayment = RegistrationState.RegistrationInstance.MinimumInitialPayment;
                }

                // Get the cost/fee summary
                var costs = RegistrationState.CalculateCostSummary();

                minimumPayment = RegistrationState.CalculateMinimumPayment( costs );

                // If there were any costs
                if ( costs.Where( c => c.Cost > 0.0M ).Any() )
                {
                    _registrantsReview.Visible = false;
                    pnlCostAndFees.Visible = true;

                    _paymentSummary.SetRegistrationStateDetails( RegistrationState );

                    // Calculate balance due, and if a partial payment is still allowed
                    decimal balanceDue = RegistrationState.RegistrationInfo.DiscountedCost - RegistrationState.RegistrationInfo.PreviousPaymentTotal;

                    // Make sure payment amount is within minumum due and balance due. If not, set to balance due
                    if ( !RegistrationState.RegistrationInfo.PaymentAmount.HasValue ||
                        RegistrationState.RegistrationInfo.PaymentAmount.Value < minimumPayment.Value ||
                        RegistrationState.RegistrationInfo.PaymentAmount.Value > balanceDue )
                    {
                        RegistrationState.RegistrationInfo.PaymentAmount = balanceDue;
                    }

                    // Set payment options based on gateway settings
                    if ( balanceDue > 0 && RegistrationState.RegistrationTemplate.FinancialGateway != null )
                    {
                        _processPayment.SetRegistrationStateDetails( RegistrationState, CurrentPerson );

                        if ( _processPayment.Using3StepGateway )
                        {
                            lbSummaryNext.Text = "Next";
                        }
                    }
                    else
                    {
                        pnlPaymentInfo.Visible = false;
                    }
                }
                else
                {
                    _registrantsReview.SetRegistrationStateDetails( RegistrationState );

                    RegistrationState.RegistrationInfo.TotalCost = 0.0m;
                    RegistrationState.RegistrationInfo.DiscountedCost = 0.0m;
                    pnlCostAndFees.Visible = false;
                    pnlPaymentInfo.Visible = false;
                }
            }
        }

        private void ParseSummaryControls()
        {
            if ( RegistrationState.RegistrationInfo != null )
            {
                _registrarInfo.UpdateRegistrationState( RegistrationState );
                _paymentSummary.UpdateRegistrationState( RegistrationState );
            }
        }

        #endregion

        #endregion

        #endregion
    }
}

namespace Rock.Web.UI.Controls.Event
{
    public class RegistrationError
    {
        public NotificationBoxType Type { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public RegistrationError( NotificationBoxType type, string title, string description )
        {
            Type = type;
            Title = title;
            Description = description;
        }
    }

    public class SelectedPersonChangedEventArgs : EventArgs
    {
        public int? PersonId { get; set; }

        public SelectedPersonChangedEventArgs( int? personId )
            : base()
        {
            PersonId = personId;
        }
    }
    public delegate void SelectedPersonChangedEventHandler( object sender, SelectedPersonChangedEventArgs e );

    /// <summary>
    /// Defines all the information needed to keep a registration in memory.
    /// </summary>
    [Serializable]
    public class RegistrationState : ISerializable
    {
        /// <summary>
        /// Information about the instance we are registering for.
        /// </summary>
        public RegistrationInstance RegistrationInstance { get; set; }

        /// <summary>
        /// Information about this specific registration.
        /// </summary>
        public RegistrationInfo RegistrationInfo { get; set; }

        /// <summary>
        /// The template information about all registrations of this type.
        /// </summary>
        public RegistrationTemplate RegistrationTemplate
        {
            get
            {
                return RegistrationInstance != null ? RegistrationInstance.RegistrationTemplate : null;
            }
        }

        /// <summary>
        /// Gets the registration term.
        /// </summary>
        /// <value>
        /// The registration term.
        /// </value>
        public string RegistrationTerm
        {
            get
            {
                if ( RegistrationTemplate != null && !string.IsNullOrWhiteSpace( RegistrationTemplate.RegistrationTerm ) )
                {
                    return RegistrationTemplate.RegistrationTerm;
                }
                return "Registration";
            }
        }

        /// <summary>
        /// Gets the registrant term.
        /// </summary>
        /// <value>
        /// The registrant term.
        /// </value>
        public string RegistrantTerm
        {
            get
            {
                if ( RegistrationTemplate != null && !string.IsNullOrWhiteSpace( RegistrationTemplate.RegistrantTerm ) )
                {
                    return RegistrationTemplate.RegistrantTerm;
                }
                return "Person";
            }
        }

        /// <summary>
        /// Gets the fee term.
        /// </summary>
        /// <value>
        /// The fee term.
        /// </value>
        public string FeeTerm
        {
            get
            {
                if ( RegistrationTemplate != null && !string.IsNullOrWhiteSpace( RegistrationTemplate.FeeTerm ) )
                {
                    return RegistrationTemplate.FeeTerm;
                }
                return "Additional Option";
            }
        }

        /// <summary>
        /// Gets the discount code term.
        /// </summary>
        /// <value>
        /// The discount code term.
        /// </value>
        public string DiscountCodeTerm
        {
            get
            {
                if ( RegistrationTemplate != null && !string.IsNullOrWhiteSpace( RegistrationTemplate.DiscountCodeTerm ) )
                {
                    return RegistrationTemplate.DiscountCodeTerm;
                }
                return "Discount Code";
            }
        }

        /// <summary>
        /// Gets the number of forms for the current registration template.
        /// </summary>
        public int FormCount
        {
            get
            {
                if ( RegistrationTemplate != null && RegistrationTemplate.Forms != null )
                {
                    return RegistrationTemplate.Forms.Count;
                }

                return 0;
            }
        }

        /// <summary>
        /// If the registration template allows multiple registrants per registration, returns the maximum allowed
        /// </summary>
        public int MaxRegistrants
        {
            get
            {
                // If this is an existing registration, max registrants is the number of registrants already 
                // on registration ( don't allow adding new registrants )
                if ( RegistrationInfo != null && RegistrationInfo.RegistrationId.HasValue )
                {
                    return RegistrationInfo.RegistrantCount;
                }

                // Otherwise if template allows multiple, set the max amount
                if ( RegistrationTemplate != null && RegistrationTemplate.AllowMultipleRegistrants )
                {
                    if ( RegistrationTemplate.MaxRegistrants <= 0 )
                    {
                        return int.MaxValue;
                    }
                    return RegistrationTemplate.MaxRegistrants;
                }

                // Default is a maximum of one
                return 1;
            }
        }

        /// <summary>
        /// Gets the minimum number of registrants allowed. Most of the time this is one, except for an existing
        /// registration that has existing registrants. The minimum in this case is the number of existing registrants
        /// </summary>
        public int MinRegistrants
        {
            get
            {
                // If this is an existing registration, min registrants is the number of registrants already 
                // on registration ( don't allow adding new registrants )
                if ( RegistrationInfo != null && RegistrationInfo.RegistrationId.HasValue )
                {
                    return RegistrationInfo.RegistrantCount;
                }

                // Default is a minimum of one
                return 1;
            }
        }

        /// <summary>
        /// The selected group from linkage.
        /// </summary>
        public int? GroupId { get; protected set; }

        /// <summary>
        /// The selected campus from event item occurrence or query string
        /// </summary>
        public int? CampusId { get; protected set; }

        public bool SignInline { get; protected set; }
        public string DigitalSignatureComponentTypeName { get; protected set; }
        public DigitalSignatureComponent DigitalSignatureComponent { get; protected set; }

        /// <summary>
        /// Returns the total number of registrant steps that should happen for
        /// this regisration state. This calculates the number of visible forms
        /// for each registrant.
        /// </summary>
        public int TotalRegistrantSteps
        {
            get
            {
                int steps = 0;

                for ( int r = 0; r < RegistrationInfo.RegistrantCount; r++ )
                {
                    steps += FormCountForRegistrant( r );
                }

                return steps;
            }
        }

        public bool ShouldUpdateRegistrarEmail { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [using three-step gateway].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [using three-step gateway]; otherwise, <c>false</c>.
        /// </value>
        public bool Using3StepGateway { get; protected set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public RegistrationState()
        {
        }

        protected RegistrationState( SerializationInfo info, StreamingContext context )
        {
            string json = info.GetString( "RegistrationInstance" );
            RegistrationInstance = JsonConvert.DeserializeObject<RegistrationInstance>( json );

            json = info.GetString( "RegistrationInfo" );
            RegistrationInfo = JsonConvert.DeserializeObject<RegistrationInfo>( json );
            if ( RegistrationInfo == null )
            {
                RegistrationInfo = new RegistrationInfo();
            }

            GroupId = ( int? ) info.GetValue( "GroupId", typeof( int? ) );
            CampusId = ( int? ) info.GetValue( "CampusId", typeof( int? ) );
            SignInline = info.GetBoolean( "SignInline" );
            Using3StepGateway = info.GetBoolean( "Using3StepGateway" );
            DigitalSignatureComponentTypeName = info.GetString( "DigitalSignatureComponentTypeName" );
            json = info.GetString( "DigitalSignatureComponent" );
            DigitalSignatureComponent = JsonConvert.DeserializeObject<DigitalSignatureComponent>( json );
            ShouldUpdateRegistrarEmail = info.GetBoolean( "ShouldUpdateRegistrarEmail" );
        }

        /// <summary>
        /// Sets the registration state
        /// </summary>
        public bool SetRegistrationState( Person currentPerson,
            out RegistrationError error,
            string registrationSlug = null,
            int? registrationInstanceId = null,
            int? registrationId = null,
            int? groupId = null,
            int? campusId = null,
            int? eventOccurrenceId = null )
        {
            // Not inside a "using" due to serialization needing context to still be active
            var rockContext = new RockContext();

            // An existing registration id was specified
            if ( registrationId.HasValue )
            {
                var registrationService = new RegistrationService( rockContext );
                var registration = registrationService
                    .Queryable( "Registrants.PersonAlias.Person,Registrants.GroupMember,RegistrationInstance.Account,RegistrationInstance.RegistrationTemplate.Fees,RegistrationInstance.RegistrationTemplate.Discounts,RegistrationInstance.RegistrationTemplate.Forms.Fields.Attribute,RegistrationInstance.RegistrationTemplate.FinancialGateway" )
                    .Where( r => r.Id == registrationId.Value )
                    .FirstOrDefault();

                if ( registration == null )
                {
                    error = new RegistrationError( NotificationBoxType.Danger, "Error", "Registration not found" );
                    return false;
                }

                if ( currentPerson == null )
                {
                    error = new RegistrationError( NotificationBoxType.Warning, "Please log in", "You must be logged in to access this registration." );
                    return false;
                }

                // Only allow the person that was logged in when this registration was created. 
                // If the logged in person, registered on someone elses behalf (for example, husband logged in, but entered wife's name as the Registrar), 
                // also allow that person to access the regisratiuon
                if ( ( registration.PersonAlias != null && registration.PersonAlias.PersonId == currentPerson.Id ) ||
                    ( registration.CreatedByPersonAlias != null && registration.CreatedByPersonAlias.PersonId == currentPerson.Id ) )
                {
                    RegistrationInstance = registration.RegistrationInstance;
                    RegistrationInfo = new RegistrationInfo( registration, rockContext );
                    RegistrationInfo.PreviousPaymentTotal = registrationService.GetTotalPayments( registration.Id );
                }
                else
                {
                    error = new RegistrationError( NotificationBoxType.Warning, "Sorry", "You are not allowed to view or edit the selected registration since you are not the one who created the registration." );
                    return false;
                }

                // set group id
                if ( groupId.HasValue )
                {
                    GroupId = groupId;
                }
                else if ( !string.IsNullOrWhiteSpace( registrationSlug ) )
                {
                    var dateTime = RockDateTime.Now;
                    var linkage = new EventItemOccurrenceGroupMapService( rockContext )
                        .Queryable().AsNoTracking()
                        .Where( l =>
                            l.UrlSlug == registrationSlug &&
                            l.RegistrationInstance != null &&
                            l.RegistrationInstance.IsActive &&
                            l.RegistrationInstance.RegistrationTemplate != null &&
                            l.RegistrationInstance.RegistrationTemplate.IsActive &&
                            ( !l.RegistrationInstance.StartDateTime.HasValue || l.RegistrationInstance.StartDateTime <= dateTime ) &&
                            ( !l.RegistrationInstance.EndDateTime.HasValue || l.RegistrationInstance.EndDateTime > dateTime ) )
                        .FirstOrDefault();
                    if ( linkage != null )
                    {
                        GroupId = linkage.GroupId;
                    }
                }
            }

            // A registration slug was specified
            if ( RegistrationInfo == null && !string.IsNullOrWhiteSpace( registrationSlug ) )
            {
                var dateTime = RockDateTime.Now;
                var linkage = new EventItemOccurrenceGroupMapService( rockContext )
                    .Queryable( "RegistrationInstance.Account,RegistrationInstance.RegistrationTemplate.Fees,RegistrationInstance.RegistrationTemplate.Discounts,RegistrationInstance.RegistrationTemplate.Forms.Fields.Attribute,RegistrationInstance.RegistrationTemplate.FinancialGateway" )
                    .Where( l =>
                        l.UrlSlug == registrationSlug &&
                        l.RegistrationInstance != null &&
                        l.RegistrationInstance.IsActive &&
                        l.RegistrationInstance.RegistrationTemplate != null &&
                        l.RegistrationInstance.RegistrationTemplate.IsActive &&
                        ( !l.RegistrationInstance.StartDateTime.HasValue || l.RegistrationInstance.StartDateTime <= dateTime ) &&
                        ( !l.RegistrationInstance.EndDateTime.HasValue || l.RegistrationInstance.EndDateTime > dateTime ) )
                    .FirstOrDefault();

                if ( linkage != null )
                {
                    RegistrationInstance = linkage.RegistrationInstance;
                    GroupId = linkage.GroupId;
                    RegistrationInfo = new RegistrationInfo( currentPerson );
                }
            }

            // A group id and campus id were specified
            if ( RegistrationInfo == null && groupId.HasValue && campusId.HasValue )
            {
                var dateTime = RockDateTime.Now;
                var linkage = new EventItemOccurrenceGroupMapService( rockContext )
                    .Queryable( "RegistrationInstance.Account,RegistrationInstance.RegistrationTemplate.Fees,RegistrationInstance.RegistrationTemplate.Discounts,RegistrationInstance.RegistrationTemplate.Forms.Fields.Attribute,RegistrationInstance.RegistrationTemplate.FinancialGateway" )
                    .Where( l =>
                        l.GroupId == groupId &&
                        l.EventItemOccurrence != null &&
                        l.EventItemOccurrence.CampusId == campusId &&
                        l.RegistrationInstance != null &&
                        l.RegistrationInstance.IsActive &&
                        l.RegistrationInstance.RegistrationTemplate != null &&
                        l.RegistrationInstance.RegistrationTemplate.IsActive &&
                        ( !l.RegistrationInstance.StartDateTime.HasValue || l.RegistrationInstance.StartDateTime <= dateTime ) &&
                        ( !l.RegistrationInstance.EndDateTime.HasValue || l.RegistrationInstance.EndDateTime > dateTime ) )
                    .FirstOrDefault();

                CampusId = campusId;
                if ( linkage != null )
                {
                    RegistrationInstance = linkage.RegistrationInstance;
                    GroupId = linkage.GroupId;
                    RegistrationInfo = new RegistrationInfo( currentPerson );
                }
            }

            // A registration instance id was specified
            if ( RegistrationInfo == null && registrationInstanceId.HasValue )
            {
                var dateTime = RockDateTime.Now;
                RegistrationInstance = new RegistrationInstanceService( rockContext )
                    .Queryable( "Account,RegistrationTemplate.Fees,RegistrationTemplate.Discounts,RegistrationTemplate.Forms.Fields.Attribute,RegistrationTemplate.FinancialGateway" )
                    .Where( r =>
                        r.Id == registrationInstanceId.Value &&
                        r.IsActive &&
                        r.RegistrationTemplate != null &&
                        r.RegistrationTemplate.IsActive &&
                        ( !r.StartDateTime.HasValue || r.StartDateTime <= dateTime ) &&
                        ( !r.EndDateTime.HasValue || r.EndDateTime > dateTime ) )
                    .FirstOrDefault();

                if ( RegistrationInstance != null )
                {
                    RegistrationInfo = new RegistrationInfo( currentPerson );
                }
            }

            // If registration instance id and event occurrence were specified, but a group (linkage) hasn't been loaded, find the first group for the event occurrence
            if ( RegistrationInstance != null && eventOccurrenceId.HasValue && !groupId.HasValue )
            {
                var eventItemOccurrence = new EventItemOccurrenceService( rockContext )
                    .Queryable()
                    .Where( o => o.Id == eventOccurrenceId.Value )
                    .FirstOrDefault();
                if ( eventItemOccurrence != null )
                {
                    CampusId = eventItemOccurrence.CampusId;

                    var linkage = eventItemOccurrence.Linkages
                        .Where( l => l.RegistrationInstanceId == RegistrationInstance.Id )
                        .FirstOrDefault();

                    if ( linkage != null )
                    {
                        GroupId = linkage.GroupId;
                    }
                }
            }

            if ( RegistrationInfo != null &&
                RegistrationInfo.FamilyGuid == Guid.Empty &&
                RegistrationTemplate != null &&
                RegistrationTemplate.RegistrantsSameFamily != RegistrantsSameFamily.Ask )
            {
                RegistrationInfo.FamilyGuid = Guid.NewGuid();
            }

            if ( RegistrationInfo != null )
            {
                if ( !RegistrationInfo.RegistrationId.HasValue && RegistrationInstance != null && RegistrationInstance.MaxAttendees > 0 )
                {
                    var existingRegistrantIds = RegistrationInfo.Registrants.Select( r => r.Id ).ToList();
                    int otherRegistrants = RegistrationInstance.Registrations
                        .Where( r => !r.IsTemporary )
                        .Sum( r => r.Registrants.Where( t => !existingRegistrantIds.Contains( t.Id ) ).Count() );

                    RegistrationInfo.SlotsAvailable = RegistrationInstance.MaxAttendees - otherRegistrants;
                }

                if ( !RegistrationInfo.Registrants.Any() )
                {
                    UpdateRegistrantCount( 1, currentPerson );
                }
            }

            if ( RegistrationTemplate != null &&
                RegistrationTemplate.FinancialGateway != null )
            {
                var threeStepGateway = RegistrationTemplate.FinancialGateway.GetGatewayComponent() as ThreeStepGatewayComponent;
                Using3StepGateway = threeStepGateway != null;
            }

            SignInline = false;
            if ( RegistrationTemplate != null &&
                RegistrationTemplate.RequiredSignatureDocumentTemplate != null &&
                RegistrationTemplate.RequiredSignatureDocumentTemplate.ProviderEntityType != null )
            {

                var provider = DigitalSignatureContainer.GetComponent( RegistrationTemplate.RequiredSignatureDocumentTemplate.ProviderEntityType.Name );
                if ( provider != null && provider.IsActive )
                {
                    SignInline = RegistrationTemplate.SignatureDocumentAction == SignatureDocumentAction.Embed;
                    DigitalSignatureComponentTypeName = RegistrationTemplate.RequiredSignatureDocumentTemplate.ProviderEntityType.Name;
                    DigitalSignatureComponent = provider;
                }
            }

            error = null;

            return true;
        }

        /// <summary>
        /// Adds (or removes) registrants to or from the registration. Only newly added registrants can
        /// can be removed. Any existing (saved) registrants cannot be removed from the registration
        /// </summary>
        /// <param name="registrantCount">The number of registrants that registration should have.</param>
        public void UpdateRegistrantCount( int registrantCount, Person currentPerson )
        {
            var registrationTemplate = RegistrationInstance.RegistrationTemplate;

            if ( RegistrationInfo != null )
            {
                decimal cost = registrationTemplate.Cost;
                if ( ( registrationTemplate.SetCostOnInstance ?? false ) && RegistrationInstance != null )
                {
                    cost = RegistrationInstance.Cost ?? 0.0m;
                }

                // If this is the first registrant being added, default it to the current person
                if ( RegistrationInfo.RegistrantCount == 0 && registrantCount == 1 && currentPerson != null )
                {
                    var registrant = new RegistrantInfo( RegistrationInstance, currentPerson );
                    if ( registrationTemplate.ShowCurrentFamilyMembers )
                    {
                        // If currentfamily members can be selected, the firstname and lastname fields will be 
                        // disabled so values need to be set (in case those fields did not have the 'showCurrentValue' 
                        // option selected
                        foreach ( var field in registrationTemplate.Forms
                            .SelectMany( f => f.Fields )
                            .Where( f =>
                                ( f.PersonFieldType == RegistrationPersonFieldType.FirstName ||
                                f.PersonFieldType == RegistrationPersonFieldType.LastName ) &&
                                f.FieldSource == RegistrationFieldSource.PersonField ) )
                        {
                            registrant.FieldValues.AddOrReplace( field.Id,
                                new FieldValueObject( field, field.PersonFieldType == RegistrationPersonFieldType.FirstName ? currentPerson.NickName : currentPerson.LastName ) );
                        }
                    }
                    registrant.Cost = cost;
                    registrant.FamilyGuid = RegistrationInfo.FamilyGuid;
                    if ( RegistrationInfo.Registrants.Count >= RegistrationInfo.SlotsAvailable )
                    {
                        registrant.OnWaitList = true;
                    }

                    RegistrationInfo.Registrants.Add( registrant );
                }

                // While the number of registrants belonging to registration is less than the selected count, addd another registrant
                while ( RegistrationInfo.RegistrantCount < registrantCount )
                {
                    var registrant = new RegistrantInfo { Cost = cost };
                    if ( registrationTemplate.RegistrantsSameFamily == RegistrantsSameFamily.No )
                    {
                        registrant.FamilyGuid = Guid.NewGuid();
                    }
                    else if ( registrationTemplate.RegistrantsSameFamily == RegistrantsSameFamily.Yes )
                    {
                        registrant.FamilyGuid = RegistrationInfo.FamilyGuid;
                    }

                    if ( RegistrationInfo.Registrants.Count >= RegistrationInfo.SlotsAvailable )
                    {
                        registrant.OnWaitList = true;
                    }
                    RegistrationInfo.Registrants.Add( registrant );
                }

                // Get the number of registrants that needs to be removed. 
                int removeCount = RegistrationInfo.RegistrantCount - registrantCount;
                if ( removeCount > 0 )
                {
                    // If removing any, reverse the order of registrants, so that most recently added will be removed first
                    RegistrationInfo.Registrants.Reverse();

                    // Try to get the registrants to remove. Most recently added will be taken first
                    foreach ( var registrant in RegistrationInfo.Registrants.Take( removeCount ).ToList() )
                    {
                        RegistrationInfo.Registrants.Remove( registrant );
                    }

                    // Reset the order after removing any registrants
                    RegistrationInfo.Registrants.Reverse();
                }
            }
        }

        public void GetObjectData( SerializationInfo info, StreamingContext context )
        {
            var jsonSetting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new Rock.Utility.IgnoreUrlEncodedKeyContractResolver()
            };

            info.AddValue( "RegistrationInstance", JsonConvert.SerializeObject( RegistrationInstance, Formatting.None, jsonSetting ) );
            info.AddValue( "RegistrationInfo", JsonConvert.SerializeObject( RegistrationInfo, Formatting.None, jsonSetting ) );
            info.AddValue( "GroupId", GroupId );
            info.AddValue( "CampusId", CampusId );
            info.AddValue( "SignInline", SignInline );
            info.AddValue( "Using3StepGateway", Using3StepGateway );
            info.AddValue( "DigitalSignatureComponentTypeName", DigitalSignatureComponentTypeName );
            info.AddValue( "DigitalSignatureComponent", JsonConvert.SerializeObject( DigitalSignatureComponent, Formatting.None, jsonSetting ) );
            info.AddValue( "ShouldUpdateRegistrarEmail", ShouldUpdateRegistrarEmail );
        }

        /// <summary>
        /// Adds the registrants to group.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="registration">The registration.</param>
        public void AddRegistrantsToGroup( RockContext rockContext, Registration registration )
        {
            // If the registration instance linkage specified a group to add registrant to, add them if they're not already
            // part of that group
            if ( registration.GroupId.HasValue )
            {
                var groupService = new GroupService( rockContext );
                var personAliasService = new PersonAliasService( rockContext );
                var groupMemberService = new GroupMemberService( rockContext );

                var group = groupService.Get( registration.GroupId.Value );
                if ( group != null )
                {
                    foreach ( var registrant in registration.Registrants.Where( r => !r.OnWaitList && r.PersonAliasId.HasValue ).ToList() )
                    {
                        var personAlias = personAliasService.Get( registrant.PersonAliasId.Value );
                        GroupMember groupMember = group.Members.Where( m => m.PersonId == personAlias.PersonId ).FirstOrDefault();
                        if ( groupMember == null )
                        {
                            groupMember = new GroupMember();
                            groupMemberService.Add( groupMember );
                            groupMember.GroupId = group.Id;
                            groupMember.PersonId = personAlias.PersonId;

                            if ( RegistrationTemplate.GroupTypeId.HasValue &&
                                RegistrationTemplate.GroupTypeId == group.GroupTypeId &&
                                RegistrationTemplate.GroupMemberRoleId.HasValue )
                            {
                                groupMember.GroupRoleId = RegistrationTemplate.GroupMemberRoleId.Value;
                            }
                            else
                            {
                                if ( group.GroupType.DefaultGroupRoleId.HasValue )
                                {
                                    groupMember.GroupRoleId = group.GroupType.DefaultGroupRoleId.Value;
                                }
                                else
                                {
                                    groupMember.GroupRoleId = group.GroupType.Roles.Select( r => r.Id ).FirstOrDefault();
                                }
                            }
                        }

                        groupMember.GroupMemberStatus = RegistrationTemplate.GroupMemberStatus;

                        rockContext.SaveChanges();

                        registrant.GroupMemberId = groupMember != null ? groupMember.Id : ( int? ) null;
                        rockContext.SaveChanges();

                        // Set any of the template's group member attributes 
                        groupMember.LoadAttributes();

                        var registrantInfo = RegistrationInfo.Registrants.FirstOrDefault( r => r.Guid == registrant.Guid );
                        if ( registrantInfo != null )
                        {
                            foreach ( var field in RegistrationTemplate.Forms
                                .SelectMany( f => f.Fields
                                    .Where( t =>
                                        t.FieldSource == RegistrationFieldSource.GroupMemberAttribute &&
                                        t.AttributeId.HasValue ) ) )
                            {
                                // Find the registrant's value
                                var fieldValue = registrantInfo.FieldValues
                                    .Where( f => f.Key == field.Id )
                                    .Select( f => f.Value.FieldValue )
                                    .FirstOrDefault();

                                if ( fieldValue != null )
                                {
                                    var attribute = AttributeCache.Read( field.AttributeId.Value );
                                    if ( attribute != null )
                                    {
                                        string originalValue = groupMember.GetAttributeValue( attribute.Key );
                                        string newValue = fieldValue.ToString();
                                        groupMember.SetAttributeValue( attribute.Key, fieldValue.ToString() );

                                        if ( ( originalValue ?? string.Empty ).Trim() != ( newValue ?? string.Empty ).Trim() )
                                        {
                                            string formattedOriginalValue = string.Empty;
                                            if ( !string.IsNullOrWhiteSpace( originalValue ) )
                                            {
                                                formattedOriginalValue = attribute.FieldType.Field.FormatValue( null, originalValue, attribute.QualifierValues, false );
                                            }

                                            string formattedNewValue = string.Empty;
                                            if ( !string.IsNullOrWhiteSpace( newValue ) )
                                            {
                                                formattedNewValue = attribute.FieldType.Field.FormatValue( null, newValue, attribute.QualifierValues, false );
                                            }

                                            Helper.SaveAttributeValue( groupMember, attribute, newValue, rockContext );
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if ( group.IsSecurityRole || group.GroupType.Guid.Equals( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() ) )
                    {
                        Rock.Security.Role.Flush( group.Id );
                    }
                }
            }
        }

        /// <summary>
        /// Determine if the specified form should be displayed to the registration. When
        /// a user is on the waiting list then only forms with fields marked as ShowOnWaitList will
        /// be displayed.
        /// </summary>
        /// <param name="state">The registration state that contains all the forms.</param>
        /// <param name="registrantIndex">The registrant who should be checked.</param>
        /// <param name="formIndex">Which form to check to see if it should be displayed.</param>
        /// <returns>true if the form should be displayed or false if it should be skipped.</returns>
        public bool IsFormVisibleForRegistrant( int registrantIndex, int formIndex )
        {
            if ( registrantIndex >= RegistrationInfo.RegistrantCount || formIndex >= RegistrationTemplate.Forms.Count )
            {
                return false;
            }

            if ( RegistrationInfo.Registrants[registrantIndex].OnWaitList )
            {
                var form = RegistrationTemplate.Forms
                    .OrderBy( f => f.Order )
                    .ToList()[formIndex];

                return form.Fields.Any( f => !f.IsInternal && f.ShowOnWaitlist );
            }

            return true;
        }

        /// <summary>
        /// Calculates the number of forms that will be displayed for this registrant.
        /// </summary>
        /// <param name="registrantIndex">The index number of the registrant to calculate for.</param>
        /// <returns>The number of forms that will be displayed for the registrant.</returns>
        public int FormCountForRegistrant( int registrantIndex )
        {
            int formCount = 0;

            for ( int i = 0; i < RegistrationTemplate.Forms.Count; i++ )
            {
                if ( IsFormVisibleForRegistrant( registrantIndex, i ) )
                {
                    formCount += 1;
                }
            }

            if ( SignInline )
            {
                formCount += 1;
            }

            return formCount;
        }

        /// <summary>
        /// Calculates and returns the number of registrant steps that have been completed.
        /// </summary>
        /// <param name="registrantIndex">The registrant that is currently being displayed. Pass null to indicate all registrants completed.</param>
        /// <param name="formIndex">The form that is currently being displayed. Pass null to indicate all forms completed.</param>
        /// <returns></returns>
        public int CompletedRegistrantSteps( int? registrantIndex, int? formIndex )
        {
            int steps = 0;

            if ( !registrantIndex.HasValue )
            {
                return TotalRegistrantSteps;
            }

            for ( int i = 0; i < registrantIndex.Value; i++ )
            {
                steps += FormCountForRegistrant( i );
            }

            return steps + ( formIndex ?? FormCountForRegistrant( registrantIndex.Value ) );
        }

        public List<RegistrationCostSummaryInfo> CalculateCostSummary()
        {
            decimal? minimumInitialPayment = RegistrationTemplate.MinimumInitialPayment;
            if ( RegistrationTemplate.SetCostOnInstance ?? false )
            {
                minimumInitialPayment = RegistrationInstance.MinimumInitialPayment;
            }

            // Get the cost/fee summary
            var costs = new List<RegistrationCostSummaryInfo>();
            foreach ( var registrant in RegistrationInfo.Registrants )
            {
                if ( registrant.Cost > 0 )
                {
                    var costSummary = new RegistrationCostSummaryInfo();
                    costSummary.Type = RegistrationCostSummaryType.Cost;
                    costSummary.Description = string.Format( "{0} {1}",
                        registrant.GetFirstName( RegistrationTemplate ),
                        registrant.GetLastName( RegistrationTemplate ) );

                    if ( registrant.OnWaitList )
                    {
                        costSummary.Description += " (Waiting List)";
                        costSummary.Cost = 0.0M;
                        costSummary.DiscountedCost = 0.0M;
                        costSummary.MinPayment = 0.0M;
                    }
                    else
                    {
                        costSummary.Cost = registrant.Cost;
                        if ( RegistrationInfo.DiscountPercentage > 0.0m && registrant.DiscountApplies )
                        {
                            costSummary.DiscountedCost = costSummary.Cost - ( costSummary.Cost * RegistrationInfo.DiscountPercentage );
                        }
                        else
                        {
                            costSummary.DiscountedCost = costSummary.Cost;
                        }
                        // If registration allows a minimum payment calculate that amount, otherwise use the discounted amount as minimum
                        costSummary.MinPayment = minimumInitialPayment.HasValue ? minimumInitialPayment.Value : costSummary.DiscountedCost;
                    }

                    costs.Add( costSummary );
                }

                foreach ( var fee in registrant.FeeValues )
                {
                    var templateFee = RegistrationTemplate.Fees.Where( f => f.Id == fee.Key ).FirstOrDefault();
                    if ( fee.Value != null )
                    {
                        foreach ( var feeInfo in fee.Value )
                        {
                            decimal cost = feeInfo.PreviousCost > 0.0m ? feeInfo.PreviousCost : feeInfo.Cost;
                            string desc = string.Format( "{0}{1} ({2:N0} @ {3})",
                                templateFee != null ? templateFee.Name : "(Previous Cost)",
                                string.IsNullOrWhiteSpace( feeInfo.Option ) ? "" : "-" + feeInfo.Option,
                                feeInfo.Quantity,
                                cost.FormatAsCurrency() );

                            var costSummary = new RegistrationCostSummaryInfo();
                            costSummary.Type = RegistrationCostSummaryType.Fee;
                            costSummary.Description = desc;
                            costSummary.Cost = feeInfo.Quantity * cost;

                            if ( RegistrationInfo.DiscountPercentage > 0.0m && templateFee != null && templateFee.DiscountApplies && registrant.DiscountApplies )
                            {
                                costSummary.DiscountedCost = costSummary.Cost - ( costSummary.Cost * RegistrationInfo.DiscountPercentage );
                            }
                            else
                            {
                                costSummary.DiscountedCost = costSummary.Cost;
                            }

                            // If template allows a minimum payment, then fees are not included, otherwise it is included
                            costSummary.MinPayment = minimumInitialPayment.HasValue ? 0 : costSummary.DiscountedCost;

                            costs.Add( costSummary );
                        }
                    }
                }
            }

            if ( costs.Where( c => c.Cost > 0.0M ).Any() )
            {
                // Add row for amount discount
                if ( RegistrationInfo.DiscountAmount > 0.0m )
                {
                    decimal totalDiscount = 0.0m - ( RegistrationInfo.Registrants.Where( r => r.DiscountApplies ).Count() * RegistrationInfo.DiscountAmount );
                    costs.Add( new RegistrationCostSummaryInfo
                    {
                        Type = RegistrationCostSummaryType.Discount,
                        Description = "Discount",
                        Cost = totalDiscount,
                        DiscountedCost = totalDiscount
                    } );
                }

                // Update the totals
                RegistrationInfo.TotalCost = costs.Sum( c => c.Cost );
                RegistrationInfo.DiscountedCost = costs.Sum( c => c.DiscountedCost );

                // Add row for totals
                costs.Add( new RegistrationCostSummaryInfo
                {
                    Type = RegistrationCostSummaryType.Total,
                    Description = "Total",
                    Cost = costs.Sum( c => c.Cost ),
                    DiscountedCost = RegistrationInfo.DiscountedCost,
                } );
            }

            return costs;
        }

        public decimal CalculateMinimumPayment( ICollection<RegistrationCostSummaryInfo> costs )
        {
            //
            // Get the total min payment for all costs and fees
            //
            var minimumPayment = costs
                .Where( c => c.Type == RegistrationCostSummaryType.Cost || c.Type == RegistrationCostSummaryType.Fee )
                .Sum( c => c.MinPayment );

            //
            // If minimum payment is greater than total discounted cost ( which
            // is possible with discounts ), adjust the minimum payment
            //
            if ( minimumPayment > RegistrationInfo.DiscountedCost )
            {
                minimumPayment = RegistrationInfo.DiscountedCost;
            }

            //
            // Reduce the minimum payment by how much they have already paid.
            // Catches some edge cases where the minimum payment amount changed
            // after an initial payment.
            //
            minimumPayment = minimumPayment - RegistrationInfo.PreviousPaymentTotal;

            //
            // If minimum payment is less than 0, set it to 0.
            //
            return minimumPayment < 0 ? 0 : minimumPayment;
        }

        /// <summary>
        /// Applies a discount code to the registration.
        /// </summary>
        /// <param name="discountCode">The code to be applied.</param>
        /// <param name="error">On return contains an error message that should be displayed to the user or null if no error.</param>
        /// <returns>true if the discount is valid, false if not valid or blank.</returns>
        public bool ApplyDiscountCode( string discountCode, out string error )
        {
            error = null;

            RegistrationInfo.Registrants.ForEach( r => r.DiscountApplies = true );

            RegistrationTemplateDiscount discount = null;
            bool validDiscount = true;

            if ( !string.IsNullOrWhiteSpace( discountCode ) )
            {
                discount = RegistrationTemplate.Discounts
                    .Where( d => d.Code.Equals( discountCode, StringComparison.OrdinalIgnoreCase ) )
                    .FirstOrDefault();

                if ( discount == null )
                {
                    error = string.Format( "'{0}' is not a valid {1}.", discountCode, DiscountCodeTerm );
                    validDiscount = false;
                }

                if ( validDiscount && discount.MinRegistrants.HasValue && RegistrationInfo.RegistrantCount < discount.MinRegistrants.Value )
                {
                    error = string.Format( "The '{0}' {1} requires at least {2} registrants.", discountCode, DiscountCodeTerm, discount.MinRegistrants.Value );
                    validDiscount = false;
                }

                if ( validDiscount && discount.StartDate.HasValue && RockDateTime.Today < discount.StartDate.Value )
                {
                    error = string.Format( "The '{0}' {1} is not available yet.", discountCode, DiscountCodeTerm );
                    validDiscount = false;
                }

                if ( validDiscount && discount.EndDate.HasValue && RockDateTime.Today > discount.EndDate.Value )
                {
                    error = string.Format( "The '{0}' {1} has expired.", discountCode, DiscountCodeTerm );
                    validDiscount = false;
                }

                if ( validDiscount && discount.MaxUsage.HasValue && RegistrationInstance != null )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var instances = new RegistrationService( rockContext )
                            .Queryable().AsNoTracking()
                            .Where( r =>
                                r.RegistrationInstanceId == RegistrationInstance.Id &&
                                ( !RegistrationInfo.RegistrationId.HasValue || r.Id != RegistrationInfo.RegistrationId.Value ) &&
                                r.DiscountCode == discountCode )
                            .Count();
                        if ( instances >= discount.MaxUsage.Value )
                        {
                            error = string.Format( "The '{0}' {1} is no longer available.", discountCode, DiscountCodeTerm );
                            validDiscount = false;
                        }
                    }
                }

                if ( validDiscount && discount.MaxRegistrants.HasValue )
                {
                    for ( int i = 0; i < RegistrationInfo.Registrants.Count; i++ )
                    {
                        RegistrationInfo.Registrants[i].DiscountApplies = i < discount.MaxRegistrants.Value;
                    }
                }
            }
            else
            {
                validDiscount = false;
            }

            RegistrationInfo.DiscountCode = validDiscount ? discountCode : string.Empty;
            RegistrationInfo.DiscountPercentage = validDiscount ? discount.DiscountPercentage : 0.0m;
            RegistrationInfo.DiscountAmount = validDiscount ? discount.DiscountAmount : 0.0m;

            return validDiscount;
        }

        public void ProcessPostSave( bool isNewRegistration, Registration registration, List<int> previousRegistrantPersonIds, RockContext rockContext, RockPage rockPage, out RegistrationError error )
        {
            try
            {
                if ( registration.PersonAlias != null && registration.PersonAlias.Person != null )
                {
                    registration.SavePersonNotesAndHistory( registration.PersonAlias.Person, rockPage.CurrentPersonAliasId, previousRegistrantPersonIds );
                }

                AddRegistrantsToGroup( rockContext, registration );

                string appRoot = rockPage.ResolveRockUrl( "~/" );
                string themeRoot = rockPage.ResolveRockUrl( "~~/" );

                // Send/Resend a confirmation
                var confirmation = new Rock.Transactions.SendRegistrationConfirmationTransaction();
                confirmation.RegistrationId = registration.Id;
                confirmation.AppRoot = appRoot;
                confirmation.ThemeRoot = themeRoot;
                Rock.Transactions.RockQueue.TransactionQueue.Enqueue( confirmation );

                if ( isNewRegistration )
                {
                    // Send notice of a new registration
                    var notification = new Rock.Transactions.SendRegistrationNotificationTransaction();
                    notification.RegistrationId = registration.Id;
                    notification.AppRoot = appRoot;
                    notification.ThemeRoot = themeRoot;
                    Rock.Transactions.RockQueue.TransactionQueue.Enqueue( notification );
                }

                var registrationService = new RegistrationService( new RockContext() );
                var newRegistration = registrationService.Get( registration.Id );
                if ( newRegistration != null )
                {
                    if ( isNewRegistration )
                    {
                        if ( RegistrationTemplate.RequiredSignatureDocumentTemplateId.HasValue )
                        {
                            string email = newRegistration.ConfirmationEmail;
                            if ( string.IsNullOrWhiteSpace( email ) && newRegistration.PersonAlias != null && newRegistration.PersonAlias.Person != null )
                            {
                                email = newRegistration.PersonAlias.Person.Email;
                            }

                            Guid? adultRole = Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid();
                            var groupMemberService = new GroupMemberService( rockContext );

                            foreach ( var registrant in newRegistration.Registrants.Where( r => r.PersonAlias != null && r.PersonAlias.Person != null ) )
                            {
                                var assignedTo = registrant.PersonAlias.Person;

                                var registrantIsAdult = adultRole.HasValue && groupMemberService
                                    .Queryable().AsNoTracking()
                                    .Any( m =>
                                        m.PersonId == registrant.PersonAlias.PersonId &&
                                        m.GroupRole.Guid.Equals( adultRole.Value ) );
                                if ( !registrantIsAdult && newRegistration.PersonAlias != null && newRegistration.PersonAlias.Person != null )
                                {
                                    assignedTo = newRegistration.PersonAlias.Person;
                                }
                                else
                                {
                                    if ( !string.IsNullOrWhiteSpace( registrant.PersonAlias.Person.Email ) )
                                    {
                                        email = registrant.PersonAlias.Person.Email;
                                    }
                                }

                                if ( DigitalSignatureComponent != null )
                                {
                                    var sendDocumentTxn = new Rock.Transactions.SendDigitalSignatureRequestTransaction();
                                    sendDocumentTxn.SignatureDocumentTemplateId = RegistrationTemplate.RequiredSignatureDocumentTemplateId.Value;
                                    sendDocumentTxn.AppliesToPersonAliasId = registrant.PersonAlias.Id;
                                    sendDocumentTxn.AssignedToPersonAliasId = assignedTo.PrimaryAliasId ?? 0;
                                    sendDocumentTxn.DocumentName = string.Format( "{0}_{1}", RegistrationInstance.Name.RemoveSpecialCharacters(), registrant.PersonAlias.Person.FullName.RemoveSpecialCharacters() );
                                    sendDocumentTxn.Email = email;
                                    Rock.Transactions.RockQueue.TransactionQueue.Enqueue( sendDocumentTxn );
                                }
                            }
                        }

                        newRegistration.LaunchWorkflow( RegistrationTemplate.RegistrationWorkflowTypeId, newRegistration.ToString() );
                        newRegistration.LaunchWorkflow( RegistrationInstance.RegistrationWorkflowTypeId, newRegistration.ToString() );
                    }

                    RegistrationInstance = newRegistration.RegistrationInstance;
                    RegistrationInfo = new RegistrationInfo( newRegistration, rockContext );
                    RegistrationInfo.PreviousPaymentTotal = registrationService.GetTotalPayments( registration.Id );
                }

                error = null;
            }

            catch ( Exception postSaveEx )
            {
                error = new RegistrationError(NotificationBoxType.Warning, "The following occurred after processing your " + RegistrationTerm, postSaveEx.Message );
                ExceptionLogService.LogException( postSaveEx, HttpContext.Current, rockPage.PageId, rockPage.Layout.SiteId, rockPage.CurrentPersonAlias );
            }
        }


        /// <summary>
        /// Saves the registration.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="hasPayment">if set to <c>true</c> [has payment].</param>
        /// <returns></returns>
        public Registration SaveRegistration( RockContext rockContext, bool hasPayment, RockPage rockPage, int? connectionStatusId, int? recordStatusId )
        {
            var registrationService = new RegistrationService( rockContext );
            var registrantService = new RegistrationRegistrantService( rockContext );
            var registrantFeeService = new RegistrationRegistrantFeeService( rockContext );
            var personService = new PersonService( rockContext );
            var groupService = new GroupService( rockContext );
            var documentService = new SignatureDocumentService( rockContext );

            // variables to keep track of the family that new people should be added to
            int? singleFamilyId = null;
            var multipleFamilyGroupIds = new Dictionary<Guid, int>();

            var familyGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
            var adultRoleId = familyGroupType.Roles
                .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) )
                .Select( r => r.Id )
                .FirstOrDefault();
            var childRoleId = familyGroupType.Roles
                .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ) )
                .Select( r => r.Id )
                .FirstOrDefault();

            bool newRegistration = false;
            Registration registration = null;
            Person registrar = null;
            var registrationChanges = new List<string>();

            if ( RegistrationInfo.RegistrationId.HasValue )
            {
                registration = registrationService.Get( RegistrationInfo.RegistrationId.Value );
            }

            if ( registration == null )
            {
                newRegistration = true;
                registration = new Registration();
                registrationService.Add( registration );
                registrationChanges.Add( "Created Registration" );
            }
            else
            {
                if ( registration.PersonAlias != null && registration.PersonAlias.Person != null )
                {
                    registrar = registration.PersonAlias.Person;
                }
            }

            registration.RegistrationInstanceId = RegistrationInstance.Id;

            // If the Registration Instance linkage specified a group, load it now
            Group group = null;
            if ( GroupId.HasValue )
            {
                group = new GroupService( rockContext ).Get( GroupId.Value );
                if ( group != null && ( !registration.GroupId.HasValue || registration.GroupId.Value != group.Id ) )
                {
                    registration.GroupId = group.Id;
                    History.EvaluateChange( registrationChanges, "Group", string.Empty, group.Name );
                }
            }

            bool newRegistrar = newRegistration ||
                registration.FirstName == null || !registration.FirstName.Equals( RegistrationInfo.FirstName, StringComparison.OrdinalIgnoreCase ) ||
                registration.LastName == null || !registration.LastName.Equals( RegistrationInfo.LastName, StringComparison.OrdinalIgnoreCase );

            History.EvaluateChange( registrationChanges, "First Name", registration.FirstName, RegistrationInfo.FirstName );
            registration.FirstName = RegistrationInfo.FirstName;

            History.EvaluateChange( registrationChanges, "Last Name", registration.LastName, RegistrationInfo.LastName );
            registration.LastName = RegistrationInfo.LastName;

            History.EvaluateChange( registrationChanges, "Confirmation Email", registration.ConfirmationEmail, RegistrationInfo.ConfirmationEmail );
            registration.ConfirmationEmail = RegistrationInfo.ConfirmationEmail;

            History.EvaluateChange( registrationChanges, "Discount Code", registration.DiscountCode, RegistrationInfo.DiscountCode );
            registration.DiscountCode = RegistrationInfo.DiscountCode;

            History.EvaluateChange( registrationChanges, "Discount Percentage", registration.DiscountPercentage, RegistrationInfo.DiscountPercentage );
            registration.DiscountPercentage = RegistrationInfo.DiscountPercentage;

            History.EvaluateChange( registrationChanges, "Discount Amount", registration.DiscountAmount, RegistrationInfo.DiscountAmount );
            registration.DiscountAmount = RegistrationInfo.DiscountAmount;

            if ( newRegistrar )
            {
                // Businesses have no first name.  This resolves null reference issues downstream.
                if ( rockPage.CurrentPerson != null && rockPage.CurrentPerson.FirstName == null )
                {
                    rockPage.CurrentPerson.FirstName = "";
                }

                if ( rockPage.CurrentPerson != null && rockPage.CurrentPerson.NickName == null )
                {
                    rockPage.CurrentPerson.NickName = rockPage.CurrentPerson.FirstName;
                }

                // If the 'your name' value equals the currently logged in person, use their person alias id
                if ( rockPage.CurrentPerson != null &&
                ( rockPage.CurrentPerson.NickName.Trim().Equals( registration.FirstName.Trim(), StringComparison.OrdinalIgnoreCase ) ||
                    rockPage.CurrentPerson.FirstName.Trim().Equals( registration.FirstName.Trim(), StringComparison.OrdinalIgnoreCase ) ) &&
                rockPage.CurrentPerson.LastName.Trim().Equals( registration.LastName.Trim(), StringComparison.OrdinalIgnoreCase ) )
                {
                    registrar = rockPage.CurrentPerson;
                    registration.PersonAliasId = rockPage.CurrentPerson.PrimaryAliasId;

                    // If email that logged in user used is different than their stored email address, update their stored value
                    if ( !string.IsNullOrWhiteSpace( registration.ConfirmationEmail ) &&
                        !registration.ConfirmationEmail.Trim().Equals( rockPage.CurrentPerson.Email.Trim(), StringComparison.OrdinalIgnoreCase ) &&
                        ShouldUpdateRegistrarEmail )
                    {
                        var person = personService.Get( rockPage.CurrentPerson.Id );
                        if ( person != null )
                        {
                            var personChanges = new List<string>();
                            History.EvaluateChange( personChanges, "Email", person.Email, registration.ConfirmationEmail );
                            person.Email = registration.ConfirmationEmail;

                            HistoryService.SaveChanges(
                                new RockContext(),
                                typeof( Person ),
                                Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(),
                                person.Id,
                                personChanges, true, rockPage.CurrentPersonAliasId );
                        }
                    }
                }
                else
                {
                    // otherwise look for one and one-only match by name/email
                    var personMatches = personService.GetByMatch( registration.FirstName, registration.LastName, registration.ConfirmationEmail );
                    if ( personMatches.Count() == 1 )
                    {
                        registrar = personMatches.First();
                        registration.PersonAliasId = registrar.PrimaryAliasId;
                    }
                    else
                    {
                        registrar = null;
                        registration.PersonAlias = null;
                        registration.PersonAliasId = null;
                    }
                }
            }

            // Set the family guid for any other registrants that were selected to be in the same family
            if ( registrar != null )
            {
                var family = registrar.GetFamilies( rockContext ).FirstOrDefault();
                if ( family != null )
                {
                    multipleFamilyGroupIds.AddOrIgnore( RegistrationInfo.FamilyGuid, family.Id );
                    if ( !singleFamilyId.HasValue )
                    {
                        singleFamilyId = family.Id;
                    }
                }
            }

            // Make sure there's an actual person associated to registration
            if ( !registration.PersonAliasId.HasValue )
            {
                // If a match was not found, create a new person
                var person = new Person();
                person.FirstName = registration.FirstName;
                person.LastName = registration.LastName;
                person.IsEmailActive = true;
                person.Email = registration.ConfirmationEmail;
                person.EmailPreference = EmailPreference.EmailAllowed;
                person.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                person.ConnectionStatusValueId = connectionStatusId.Value;
                person.RecordStatusValueId = recordStatusId.Value;

                registrar = SavePerson( rockContext, person, RegistrationInfo.FamilyGuid, CampusId, null, adultRoleId, childRoleId, multipleFamilyGroupIds, ref singleFamilyId );
                registration.PersonAliasId = registrar != null ? registrar.PrimaryAliasId : ( int? ) null;

                History.EvaluateChange( registrationChanges, "Registrar", string.Empty, registrar.FullName );
            }
            else
            {
                if ( newRegistration )
                {
                    History.EvaluateChange( registrationChanges, "Registrar", string.Empty, registration.ToString() );
                }


            }

            // if this registration was marked as temporary (started from another page, then specified in the url), set IsTemporary to False now that we are done
            if ( registration.IsTemporary )
            {
                registration.IsTemporary = false;
            }

            // Save the registration ( so we can get an id )
            rockContext.SaveChanges();
            RegistrationInfo.RegistrationId = registration.Id;

            try
            {

                Task.Run( () =>
                    HistoryService.SaveChanges(
                        new RockContext(),
                        typeof( Registration ),
                        Rock.SystemGuid.Category.HISTORY_EVENT_REGISTRATION.AsGuid(),
                        registration.Id,
                        registrationChanges, true, rockPage.CurrentPersonAliasId )
                );

                // Get each registrant
                foreach ( var registrantInfo in RegistrationInfo.Registrants.ToList() )
                {
                    var registrantChanges = new List<string>();
                    var personChanges = new List<string>();
                    var familyChanges = new List<string>();

                    RegistrationRegistrant registrant = null;
                    Person person = null;

                    string firstName = registrantInfo.GetFirstName( RegistrationTemplate );
                    string lastName = registrantInfo.GetLastName( RegistrationTemplate );
                    string email = registrantInfo.GetEmail( RegistrationTemplate );

                    if ( registrantInfo.Id > 0 )
                    {
                        registrant = registration.Registrants.FirstOrDefault( r => r.Id == registrantInfo.Id );
                        if ( registrant != null )
                        {
                            person = registrant.Person;
                            if ( person != null && (
                                ( registrant.Person.FirstName.Equals( firstName, StringComparison.OrdinalIgnoreCase ) || registrant.Person.NickName.Equals( firstName, StringComparison.OrdinalIgnoreCase ) ) &&
                                registrant.Person.LastName.Equals( lastName, StringComparison.OrdinalIgnoreCase ) ) )
                            {
                                //
                            }
                            else
                            {
                                person = null;
                                registrant.PersonAlias = null;
                                registrant.PersonAliasId = null;
                            }
                        }
                    }
                    else
                    {
                        if ( registrantInfo.PersonId.HasValue && RegistrationTemplate.ShowCurrentFamilyMembers )
                        {
                            person = personService.Get( registrantInfo.PersonId.Value );
                        }
                    }

                    if ( person == null )
                    {
                        // Try to find a matching person based on name and email address
                        var personMatches = personService.GetByMatch( firstName, lastName, email );
                        if ( personMatches.Count() == 1 )
                        {
                            person = personMatches.First();
                        }

                        // Try to find a matching person based on name within same family as registrar
                        if ( person == null && registrar != null && registrantInfo.FamilyGuid == RegistrationInfo.FamilyGuid )
                        {
                            var familyMembers = registrar.GetFamilyMembers( true, rockContext )
                                .Where( m =>
                                    ( m.Person.FirstName == firstName || m.Person.NickName == firstName ) &&
                                    m.Person.LastName == lastName )
                                .Select( m => m.Person )
                                .ToList();

                            if ( familyMembers.Count() == 1 )
                            {
                                person = familyMembers.First();
                                if ( !string.IsNullOrWhiteSpace( email ) )
                                {
                                    person.Email = email;
                                }
                            }

                            if ( familyMembers.Count() > 1 && !string.IsNullOrWhiteSpace( email ) )
                            {
                                familyMembers = familyMembers
                                    .Where( m =>
                                        m.Email != null &&
                                        m.Email.Equals( email, StringComparison.OrdinalIgnoreCase ) )
                                    .ToList();
                                if ( familyMembers.Count() == 1 )
                                {
                                    person = familyMembers.First();
                                }
                            }
                        }
                    }

                    if ( person == null )
                    {
                        // If a match was not found, create a new person
                        person = new Person();
                        person.FirstName = firstName;
                        person.LastName = lastName;
                        person.IsEmailActive = true;
                        person.Email = email;
                        person.EmailPreference = EmailPreference.EmailAllowed;
                        person.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                        person.ConnectionStatusValueId = connectionStatusId;
                        person.RecordStatusValueId = recordStatusId;
                    }

                    int? campusId = CampusId;
                    Location location = null;

                    // Set any of the template's person fields
                    foreach ( var field in RegistrationTemplate.Forms
                        .SelectMany( f => f.Fields
                            .Where( t => t.FieldSource == RegistrationFieldSource.PersonField ) ) )
                    {
                        // Find the registrant's value
                        var fieldValue = registrantInfo.FieldValues
                            .Where( f => f.Key == field.Id )
                            .Select( f => f.Value.FieldValue )
                            .FirstOrDefault();


                        if ( fieldValue != null )
                        {
                            switch ( field.PersonFieldType )
                            {
                                case RegistrationPersonFieldType.Campus:
                                    {
                                        if ( fieldValue != null )
                                        {
                                            campusId = fieldValue.ToString().AsIntegerOrNull();
                                        }
                                        break;
                                    }

                                case RegistrationPersonFieldType.Address:
                                    {
                                        location = fieldValue as Location;
                                        break;
                                    }

                                case RegistrationPersonFieldType.Birthdate:
                                    {
                                        var birthMonth = person.BirthMonth;
                                        var birthDay = person.BirthDay;
                                        var birthYear = person.BirthYear;

                                        person.SetBirthDate( fieldValue as DateTime? );

                                        History.EvaluateChange( personChanges, "Birth Month", birthMonth, person.BirthMonth );
                                        History.EvaluateChange( personChanges, "Birth Day", birthDay, person.BirthDay );
                                        History.EvaluateChange( personChanges, "Birth Year", birthYear, person.BirthYear );

                                        break;
                                    }

                                case RegistrationPersonFieldType.Grade:
                                    {
                                        var newGraduationYear = fieldValue.ToString().AsIntegerOrNull();
                                        History.EvaluateChange( personChanges, "Graduation Year", person.GraduationYear, newGraduationYear );
                                        person.GraduationYear = newGraduationYear;

                                        break;
                                    }

                                case RegistrationPersonFieldType.Gender:
                                    {
                                        var newGender = fieldValue.ToString().ConvertToEnumOrNull<Gender>() ?? Gender.Unknown;
                                        History.EvaluateChange( personChanges, "Gender", person.Gender, newGender );
                                        person.Gender = newGender;
                                        break;
                                    }

                                case RegistrationPersonFieldType.MaritalStatus:
                                    {
                                        if ( fieldValue != null )
                                        {
                                            int? newMaritalStatusId = fieldValue.ToString().AsIntegerOrNull();
                                            History.EvaluateChange( personChanges, "Marital Status", DefinedValueCache.GetName( person.MaritalStatusValueId ), DefinedValueCache.GetName( newMaritalStatusId ) );
                                            person.MaritalStatusValueId = newMaritalStatusId;
                                        }
                                        break;
                                    }

                                case RegistrationPersonFieldType.MobilePhone:
                                    {
                                        SavePhone( fieldValue, person, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid(), personChanges );
                                        break;
                                    }

                                case RegistrationPersonFieldType.HomePhone:
                                    {
                                        SavePhone( fieldValue, person, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid(), personChanges );
                                        break;
                                    }

                                case RegistrationPersonFieldType.WorkPhone:
                                    {
                                        SavePhone( fieldValue, person, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid(), personChanges );
                                        break;
                                    }
                            }
                        }
                    }

                    // Save the person ( and family if needed )
                    SavePerson( rockContext, person, registrantInfo.FamilyGuid, campusId, location, adultRoleId, childRoleId, multipleFamilyGroupIds, ref singleFamilyId );

                    // Load the person's attributes
                    person.LoadAttributes();

                    // Set any of the template's person fields
                    foreach ( var field in RegistrationTemplate.Forms
                        .SelectMany( f => f.Fields
                            .Where( t =>
                                t.FieldSource == RegistrationFieldSource.PersonAttribute &&
                                t.AttributeId.HasValue ) ) )
                    {
                        // Find the registrant's value
                        var fieldValue = registrantInfo.FieldValues
                            .Where( f => f.Key == field.Id )
                            .Select( f => f.Value.FieldValue )
                            .FirstOrDefault();

                        if ( fieldValue != null )
                        {
                            var attribute = AttributeCache.Read( field.AttributeId.Value );
                            if ( attribute != null )
                            {
                                string originalValue = person.GetAttributeValue( attribute.Key );
                                string newValue = fieldValue.ToString();
                                person.SetAttributeValue( attribute.Key, fieldValue.ToString() );

                                // DateTime values must be stored in ISO8601 format as http://www.rockrms.com/Rock/Developer/BookContent/16/16#datetimeformatting
                                if ( attribute.FieldType.Guid.Equals( Rock.SystemGuid.FieldType.DATE.AsGuid() ) ||
                                    attribute.FieldType.Guid.Equals( Rock.SystemGuid.FieldType.DATE_TIME.AsGuid() ) )
                                {
                                    DateTime aDateTime;
                                    if ( DateTime.TryParse( newValue, out aDateTime ) )
                                    {
                                        newValue = aDateTime.ToString( "o" );
                                    }
                                }

                                if ( ( originalValue ?? string.Empty ).Trim() != ( newValue ?? string.Empty ).Trim() )
                                {
                                    string formattedOriginalValue = string.Empty;
                                    if ( !string.IsNullOrWhiteSpace( originalValue ) )
                                    {
                                        formattedOriginalValue = attribute.FieldType.Field.FormatValue( null, originalValue, attribute.QualifierValues, false );
                                    }

                                    string formattedNewValue = string.Empty;
                                    if ( !string.IsNullOrWhiteSpace( newValue ) )
                                    {
                                        formattedNewValue = attribute.FieldType.Field.FormatValue( null, newValue, attribute.QualifierValues, false );
                                    }

                                    Helper.SaveAttributeValue( person, attribute, newValue, rockContext );
                                    History.EvaluateChange( personChanges, attribute.Name, formattedOriginalValue, formattedNewValue );

                                }
                            }
                        }
                    }

                    string registrantName = person.FullName + ": ";

                    personChanges.ForEach( c => registrantChanges.Add( c ) );

                    if ( registrant == null )
                    {
                        registrant = new RegistrationRegistrant();
                        registrant.Guid = registrantInfo.Guid;
                        registrantService.Add( registrant );
                        registrant.RegistrationId = registration.Id;
                    }

                    registrant.OnWaitList = registrantInfo.OnWaitList;
                    registrant.PersonAliasId = person.PrimaryAliasId;
                    registrant.Cost = registrantInfo.Cost;
                    registrant.DiscountApplies = registrantInfo.DiscountApplies;

                    // Remove fees
                    // Remove/delete any registrant fees that are no longer in UI with quantity 
                    foreach ( var dbFee in registrant.Fees.ToList() )
                    {
                        if ( !registrantInfo.FeeValues.Keys.Contains( dbFee.RegistrationTemplateFeeId ) ||
                            registrantInfo.FeeValues[dbFee.RegistrationTemplateFeeId] == null ||
                            !registrantInfo.FeeValues[dbFee.RegistrationTemplateFeeId]
                                .Any( f =>
                                    f.Option == dbFee.Option &&
                                    f.Quantity > 0 ) )
                        {
                            registrantChanges.Add( string.Format( "Removed '{0}' Fee (Quantity:{1:N0}, Cost:{2:C2}, Option:{3}",
                                dbFee.RegistrationTemplateFee.Name, dbFee.Quantity, dbFee.Cost, dbFee.Option ) );

                            registrant.Fees.Remove( dbFee );
                            registrantFeeService.Delete( dbFee );
                        }
                    }

                    // Add or Update fees
                    foreach ( var uiFee in registrantInfo.FeeValues.Where( f => f.Value != null ) )
                    {
                        foreach ( var uiFeeOption in uiFee.Value )
                        {
                            var dbFee = registrant.Fees
                                .Where( f =>
                                    f.RegistrationTemplateFeeId == uiFee.Key &&
                                    f.Option == uiFeeOption.Option )
                                .FirstOrDefault();

                            if ( dbFee == null )
                            {
                                dbFee = new RegistrationRegistrantFee();
                                dbFee.RegistrationTemplateFeeId = uiFee.Key;
                                dbFee.Option = uiFeeOption.Option;
                                registrant.Fees.Add( dbFee );
                            }

                            var templateFee = dbFee.RegistrationTemplateFee;
                            if ( templateFee == null )
                            {
                                templateFee = RegistrationTemplate.Fees.Where( f => f.Id == uiFee.Key ).FirstOrDefault();
                            }

                            string feeName = templateFee != null ? templateFee.Name : "Fee";
                            if ( !string.IsNullOrWhiteSpace( uiFeeOption.Option ) )
                            {
                                feeName = string.Format( "{0} ({1})", feeName, uiFeeOption.Option );
                            }

                            if ( dbFee.Id <= 0 )
                            {
                                registrantChanges.Add( feeName + " Fee Added" );
                            }

                            History.EvaluateChange( registrantChanges, feeName + " Quantity", dbFee.Quantity, uiFeeOption.Quantity );
                            dbFee.Quantity = uiFeeOption.Quantity;

                            History.EvaluateChange( registrantChanges, feeName + " Cost", dbFee.Cost, uiFeeOption.Cost );
                            dbFee.Cost = uiFeeOption.Cost;
                        }
                    }

                    rockContext.SaveChanges();
                    registrantInfo.Id = registrant.Id;

                    // Set any of the template's registrant attributes
                    registrant.LoadAttributes();
                    foreach ( var field in RegistrationTemplate.Forms
                        .SelectMany( f => f.Fields
                            .Where( t =>
                                t.FieldSource == RegistrationFieldSource.RegistrationAttribute &&
                                t.AttributeId.HasValue ) ) )
                    {
                        // Find the registrant's value
                        var fieldValue = registrantInfo.FieldValues
                            .Where( f => f.Key == field.Id )
                            .Select( f => f.Value.FieldValue )
                            .FirstOrDefault();

                        if ( fieldValue != null )
                        {
                            var attribute = AttributeCache.Read( field.AttributeId.Value );
                            if ( attribute != null )
                            {
                                string originalValue = registrant.GetAttributeValue( attribute.Key );
                                string newValue = fieldValue.ToString();
                                registrant.SetAttributeValue( attribute.Key, fieldValue.ToString() );

                                // DateTime values must be stored in ISO8601 format as http://www.rockrms.com/Rock/Developer/BookContent/16/16#datetimeformatting
                                if ( attribute.FieldType.Guid.Equals( Rock.SystemGuid.FieldType.DATE.AsGuid() ) ||
                                    attribute.FieldType.Guid.Equals( Rock.SystemGuid.FieldType.DATE_TIME.AsGuid() ) )
                                {
                                    DateTime aDateTime;
                                    if ( DateTime.TryParse( fieldValue.ToString(), out aDateTime ) )
                                    {
                                        newValue = aDateTime.ToString( "o" );
                                    }
                                }

                                if ( ( originalValue ?? string.Empty ).Trim() != ( newValue ?? string.Empty ).Trim() )
                                {
                                    string formattedOriginalValue = string.Empty;
                                    if ( !string.IsNullOrWhiteSpace( originalValue ) )
                                    {
                                        formattedOriginalValue = attribute.FieldType.Field.FormatValue( null, originalValue, attribute.QualifierValues, false );
                                    }

                                    string formattedNewValue = string.Empty;
                                    if ( !string.IsNullOrWhiteSpace( newValue ) )
                                    {
                                        formattedNewValue = attribute.FieldType.Field.FormatValue( null, newValue, attribute.QualifierValues, false );
                                    }

                                    Helper.SaveAttributeValue( registrant, attribute, newValue, rockContext );
                                    History.EvaluateChange( registrantChanges, attribute.Name, formattedOriginalValue, formattedNewValue );
                                }
                            }
                        }
                    }

                    Task.Run( () =>
                        HistoryService.SaveChanges(
                            new RockContext(),
                            typeof( Registration ),
                            Rock.SystemGuid.Category.HISTORY_EVENT_REGISTRATION.AsGuid(),
                            registration.Id,
                            registrantChanges,
                            "Registrant: " + person.FullName,
                            null, null, true, rockPage.CurrentPersonAliasId )
                    );

                    // Clear this registran't family guid so it's not updated again
                    registrantInfo.FamilyGuid = Guid.Empty;

                    // Save the signed document
                    try
                    {
                        if ( RegistrationTemplate.RequiredSignatureDocumentTemplateId.HasValue && !string.IsNullOrWhiteSpace( registrantInfo.SignatureDocumentKey ) )
                        {
                            var document = new SignatureDocument();
                            document.SignatureDocumentTemplateId = RegistrationTemplate.RequiredSignatureDocumentTemplateId.Value;
                            document.DocumentKey = registrantInfo.SignatureDocumentKey;
                            document.Name = string.Format( "{0}_{1}", RegistrationInstance.Name.RemoveSpecialCharacters(), person.FullName.RemoveSpecialCharacters() );
                            ;
                            document.AppliesToPersonAliasId = person.PrimaryAliasId;
                            document.AssignedToPersonAliasId = registrar.PrimaryAliasId;
                            document.SignedByPersonAliasId = registrar.PrimaryAliasId;
                            document.Status = SignatureDocumentStatus.Signed;
                            document.LastInviteDate = registrantInfo.SignatureDocumentLastSent;
                            document.LastStatusDate = registrantInfo.SignatureDocumentLastSent;
                            documentService.Add( document );
                            rockContext.SaveChanges();

                            var updateDocumentTxn = new Rock.Transactions.UpdateDigitalSignatureDocumentTransaction( document.Id );
                            Rock.Transactions.RockQueue.TransactionQueue.Enqueue( updateDocumentTxn );
                        }
                    }
                    catch ( System.Exception ex )
                    {
                        ExceptionLogService.LogException( ex, HttpContext.Current, rockPage.PageId, rockPage.Site.Id, rockPage.CurrentPersonAlias );
                    }
                }

                rockContext.SaveChanges();

            }

            catch ( Exception ex )
            {
                using ( var newRockContext = new RockContext() )
                {
                    if ( newRegistration )
                    {
                        var newRegistrationService = new RegistrationService( newRockContext );
                        var savedRegistration = new RegistrationService( newRockContext ).Get( registration.Id );
                        if ( savedRegistration != null )
                        {
                            HistoryService.DeleteChanges( newRockContext, typeof( Registration ), savedRegistration.Id );

                            newRegistrationService.Delete( savedRegistration );
                            newRockContext.SaveChanges();
                        }
                    }
                }

                throw ex;
            }

            return registration;

        }

        /// <summary>
        /// Saves the person.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="person">The person.</param>
        /// <param name="familyGuid">The family unique identifier.</param>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="location">The location.</param>
        /// <param name="adultRoleId">The adult role identifier.</param>
        /// <param name="childRoleId">The child role identifier.</param>
        /// <param name="multipleFamilyGroupIds">The multiple family group ids.</param>
        /// <param name="singleFamilyId">The single family identifier.</param>
        /// <returns></returns>
        private Person SavePerson( RockContext rockContext, Person person, Guid familyGuid, int? campusId, Location location, int adultRoleId, int childRoleId,
            Dictionary<Guid, int> multipleFamilyGroupIds, ref int? singleFamilyId )
        {
            int? familyId = null;

            if ( person.Id > 0 )
            {
                rockContext.SaveChanges();

                // Set the family guid for any other registrants that were selected to be in the same family
                var family = person.GetFamilies( rockContext ).FirstOrDefault();
                if ( family != null )
                {
                    familyId = family.Id;
                    multipleFamilyGroupIds.AddOrIgnore( familyGuid, family.Id );
                    if ( !singleFamilyId.HasValue )
                    {
                        singleFamilyId = family.Id;
                    }
                }
            }
            else
            {
                // If we've created the family aready for this registrant, add them to it
                if (
                        ( RegistrationTemplate.RegistrantsSameFamily == RegistrantsSameFamily.Ask && multipleFamilyGroupIds.ContainsKey( familyGuid ) ) ||
                        ( RegistrationTemplate.RegistrantsSameFamily == RegistrantsSameFamily.Yes && singleFamilyId.HasValue )
                    )
                {

                    // Add person to existing family
                    var age = person.Age;
                    int familyRoleId = age.HasValue && age < 18 ? childRoleId : adultRoleId;

                    familyId = RegistrationTemplate.RegistrantsSameFamily == RegistrantsSameFamily.Ask ?
                        multipleFamilyGroupIds[familyGuid] :
                        singleFamilyId.Value;
                    PersonService.AddPersonToFamily( person, true, multipleFamilyGroupIds[familyGuid], familyRoleId, rockContext );

                }

                // otherwise create a new family
                else
                {
                    // Create Person/Family
                    var familyGroup = PersonService.SaveNewPerson( person, rockContext, campusId, false );
                    if ( familyGroup != null )
                    {
                        familyId = familyGroup.Id;

                        // Store the family id for next person 
                        multipleFamilyGroupIds.AddOrIgnore( familyGuid, familyGroup.Id );
                        if ( !singleFamilyId.HasValue )
                        {
                            singleFamilyId = familyGroup.Id;
                        }
                    }
                }
            }


            if ( familyId.HasValue && location != null )
            {
                var homeLocationType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
                if ( homeLocationType != null )
                {
                    var familyGroup = new GroupService( rockContext ).Get( familyId.Value );
                    if ( familyGroup != null )
                    {
                        GroupService.AddNewGroupAddress(
                            rockContext,
                            familyGroup,
                            Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME,
                            location.Street1, location.Street2, location.City, location.State, location.PostalCode, location.Country, true );
                    }
                }
            }

            return new PersonService( rockContext ).Get( person.Id );
        }

        /// <summary>
        /// Saves the phone.
        /// </summary>
        /// <param name="fieldValue">The field value.</param>
        /// <param name="person">The person.</param>
        /// <param name="phoneTypeGuid">The phone type unique identifier.</param>
        /// <param name="changes">The changes.</param>
        private void SavePhone( object fieldValue, Person person, Guid phoneTypeGuid, List<string> changes )
        {
            var phoneNumber = fieldValue as PhoneNumber;
            if ( phoneNumber != null )
            {
                string cleanNumber = PhoneNumber.CleanNumber( phoneNumber.Number );
                if ( !string.IsNullOrWhiteSpace( cleanNumber ) )
                {
                    var numberType = DefinedValueCache.Read( phoneTypeGuid );
                    if ( numberType != null )
                    {
                        var phone = person.PhoneNumbers.FirstOrDefault( p => p.NumberTypeValueId == numberType.Id );
                        string oldPhoneNumber = string.Empty;
                        if ( phone == null )
                        {
                            phone = new PhoneNumber();
                            person.PhoneNumbers.Add( phone );
                            phone.NumberTypeValueId = numberType.Id;
                        }
                        else
                        {
                            oldPhoneNumber = phone.NumberFormattedWithCountryCode;
                        }
                        phone.CountryCode = PhoneNumber.CleanNumber( phoneNumber.CountryCode );
                        phone.Number = cleanNumber;

                        History.EvaluateChange(
                            changes,
                            string.Format( "{0} Phone", numberType.Value ),
                            oldPhoneNumber,
                            phoneNumber.NumberFormattedWithCountryCode );
                    }
                }
            }
        }

    }

    /// <summary>
    /// Prompts the user for how many registrants they want to register. Handles
    /// cases of waiting lists and other issues.
    /// </summary>
    public class HowManyRegistrants : CompositeControl
    {
        #region Protected Properties

        protected bool WaitListEnabled
        {
            get
            {
                return ( bool? ) ViewState["WaitListEnabled"] ?? false;
            }
            set
            {
                ViewState["WaitListEnabled"] = value;
            }
        }

        protected int? SlotsAvailable
        {
            get
            {
                return ( int? ) ViewState["SlotsAvailable"];
            }
            set
            {
                ViewState["SlotsAvailable"] = value;
            }
        }

        protected string RegistrationTerm
        {
            get
            {
                return ( string ) ViewState["RegistrationTerm"];
            }
            set
            {
                ViewState["RegistrationTerm"] = value;
            }
        }

        protected string RegistrantTerm
        {
            get
            {
                return ( string ) ViewState["RegistrantTerm"];
            }
            set
            {
                ViewState["RegistrantTerm"] = value;
            }
        }

        protected string RegistrationInstanceName
        {
            get
            {
                return ( string ) ViewState["RegistrationInstanceName"];
            }
            set
            {
                ViewState["RegistrationInstanceName"] = value;
            }
        }

        #endregion

        #region Child Control Fields

        protected NumberUpDown _upDown;
        protected NotificationBox _nbWaitingList;

        #endregion

        #region Base Method Overrides

        /// <summary>
        /// Create and register all child controls needed by this control.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            Controls.Clear();

            _nbWaitingList = new NotificationBox
            {
                NotificationBoxType = NotificationBoxType.Warning,
                Visible = false
            };
            Controls.Add( _nbWaitingList );

            _upDown = new NumberUpDown
            {
                ID = this.ID + "_numHowMany",
                CssClass = "input-lg",
                Visible = false
            };
            _upDown.NumberUpdated += Updown_NumberUpdated;
            Controls.Add( _upDown );
        }

        /// <summary>
        /// Render the contents of this control to the text writer.
        /// </summary>
        /// <param name="writer">The text writer that will contain the output.</param>
        protected override void Render( HtmlTextWriter writer )
        {
            if ( !_upDown.Visible )
            {
                return;
            }

            _nbWaitingList.RenderControl( writer );

            writer.RenderBeginTag( HtmlTextWriterTag.H1 );
            {
                writer.Write( string.Format( "How many {0} will you be registering?", RegistrantTerm.Pluralize() ) );
            }
            writer.RenderEndTag();

            _upDown.RenderControl( writer );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize all the settings for this control with the values from
        /// the RegistrationState.
        /// </summary>
        /// <param name="state">The current state of the registration to use.</param>
        public void SetRegistrationStateDetails( RegistrationState state )
        {
            EnsureChildControls();

            WaitListEnabled = state.RegistrationTemplate.WaitListEnabled;
            SlotsAvailable = state.RegistrationInfo.SlotsAvailable;
            RegistrationTerm = state.RegistrationTerm;
            RegistrantTerm = state.RegistrantTerm;
            RegistrationInstanceName = state.RegistrationInstance.Name;

            int max = state.MaxRegistrants;
            if ( !WaitListEnabled && SlotsAvailable.HasValue && SlotsAvailable.Value < max )
            {
                max = SlotsAvailable.Value;
            }

            _upDown.Minimum = state.MinRegistrants;
            _upDown.Maximum = max;
            _upDown.Value = state.RegistrationInfo.RegistrantCount;
            _upDown.Visible = true;
        }

        /// <summary>
        /// Update the Registration State with values in the UI.
        /// </summary>
        /// <param name="state">The registration state that is to be updated.</param>
        public void UpdateRegistrationState( RegistrationState state, Person currentPerson )
        {
            state.UpdateRegistrantCount( _upDown.Value, currentPerson );
        }

        #endregion

        #region Events

        /// <summary>
        /// Processes the NumberUpdated event for the UpDown control.
        /// </summary>
        /// <param name="sender">The object that has sent the event.</param>
        /// <param name="e">The event parameters.</param>
        private void Updown_NumberUpdated( object sender, EventArgs e )
        {
            if ( WaitListEnabled )
            {
                _nbWaitingList.Title = string.Format( "{0} Full", RegistrationTerm );

                if ( !SlotsAvailable.HasValue || SlotsAvailable.Value <= 0 )
                {
                    _nbWaitingList.Text = string.Format( "<p>This {0} has reached it's capacity. Complete the registration below to be added to the waitlist.</p>", RegistrationTerm );
                    _nbWaitingList.Visible = true;
                }
                else
                {
                    if ( _upDown.Value > SlotsAvailable )
                    {
                        int slots = SlotsAvailable.Value;
                        int wait = _upDown.Value - slots;
                        _nbWaitingList.Text = string.Format( "<p>This {0} only has capacity for {1} more {2}. The first {3}{2} you add will be registered for {4}. The remaining {5}{6} will be added to the waitlist.",
                            RegistrationTerm.ToLower(),
                            slots,
                            RegistrantTerm.PluralizeIf( slots > 1 ).ToLower(),
                            ( slots > 1 ? slots.ToString() + " " : "" ),
                            RegistrationInstanceName,
                            ( wait > 1 ? wait.ToString() + " " : "" ),
                            RegistrantTerm.PluralizeIf( wait > 1 ).ToLower() );
                        _nbWaitingList.Visible = true;
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Indicates which direction to move when displaying a new registrant form.
    /// </summary>
    public enum RegistrantFormDirection
    {
        Start = 0,
        Next = 1,
        Previous = 2,
        Last = 3
    }

    /// <summary>
    /// Displays a Bootstrap progress bar on the screen.
    /// </summary>
    public class ProgressBar : WebControl
    {
        public double Progress
        {
            get
            {
                return ( double? ) ViewState["Progress"] ?? 0;
            }
            set
            {
                ViewState["Progress"] = value;
            }
        }

        #region Base Method Overrides

        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( !Visible )
            {
                return;
            }

            if ( !string.IsNullOrWhiteSpace( CssClass ) )
            {
                writer.AddAttribute( HtmlTextWriterAttribute.Class, CssClass );
            }

            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            {
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "progress" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "progress-bar" );
                    writer.AddAttribute( "role", "progressbar" );
                    writer.AddAttribute( "aria-valuenow", Progress.ToString() );
                    writer.AddAttribute( "aria-valuemin", "0" );
                    writer.AddAttribute( "aria-valuemax", "100" );
                    writer.AddStyleAttribute( HtmlTextWriterStyle.Width, Progress.ToString() + "%" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "sr-only" );
                        writer.RenderBeginTag( HtmlTextWriterTag.Span );
                        {
                            writer.Write( "{0}% Complete", Progress.ToString() );
                        }
                        writer.RenderEndTag();
                    }
                    writer.RenderEndTag();
                }
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
        }

        #endregion
    }

    /// <summary>
    /// Displays and prompts for information about a single registrant in an event registration.
    /// </summary>
    public class RegistrantControl : CompositeControl
    {
        #region Protected Control Fields

        protected Literal lRegistrantTitle { get; private set; }
        protected NotificationBox nbType { get; private set; }
        protected NotificationBox nbError { get; private set; }
        protected ProgressBar progressBar { get; private set; }
        protected Literal lRegistrantProgressBar { get; private set; }
        protected Panel pnlRegistrantFields { get; private set; }
        protected Panel pnlFamilyOptions { get; private set; }
        protected Panel pnlFamilyMembers { get; private set; }
        protected Panel pnlDigitalSignature { get; private set; }
        protected RockRadioButtonList rblFamilyOptions { get; private set; }
        protected RockDropDownList ddlFamilyMembers { get; private set; }
        protected PlaceHolder phRegistrantControls { get; private set; }
        protected Panel pnlFees { get; private set; }
        protected Literal lRegistrantFeeCaption { get; private set; }
        protected PlaceHolder phFees { get; private set; }
        protected NotificationBox nbDigitalSignature { get; private set; }
        protected HiddenField hfRequiredDocumentLinkUrl { get; private set; }
        protected HiddenField hfRequiredDocumentQueryString { get; private set; }
        protected LinkButton lbRequiredDocumentNext { get; private set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Notification that the user used the UI control to change the selected family member.
        /// </summary>
        public event SelectedPersonChangedEventHandler SelectedPersonChanged;
        public event EventHandler Next;

        /// <summary>
        /// The index of the registrant being displayed by this control.
        /// </summary>
        public int RegistrantIndex
        {
            get
            {
                return ( int? ) ViewState["RegistrantIndex"] ?? 0;
            }
            protected set
            {
                ViewState["RegistrantIndex"] = value;
            }
        }

        /// <summary>
        /// The index of the form being displayed by this control. Note: this may be
        /// greater than the number of forms if the document signature form is being
        /// displayed.
        /// </summary>
        public int FormIndex
        {
            get
            {
                return ( int? ) ViewState["FormIndex"] ?? 0;
            }
            protected set
            {
                ViewState["FormIndex"] = value;
            }
        }

        /// <summary>
        /// The number of forms that will be displayed for the current registrant.
        /// </summary>
        public int FormCount
        {
            get
            {
                return ( int? ) ViewState["FormCount"] ?? 0;
            }
            protected set
            {
                ViewState["FormCount"] = value;
            }
        }

        /// <summary>
        /// The validation group to use for dynamically created controls.
        /// </summary>
        public string ValidationGroup { get; set; }

        /// <summary>
        /// The term used when "family" needs to be displayed to the user.
        /// </summary>
        public string FamilyTerm
        {
            get
            {
                return ( string ) ViewState["FamilyTerm"];
            }
            set
            {
                ViewState["FamilyTerm"] = value;
            }
        }

        /// <summary>
        /// The progress percentage (0-100) to display in the progress bar. If null then no
        /// progress bar will be displayed.
        /// </summary>
        public double? Progress
        {
            get
            {
                return ( double? ) ViewState["Progress"];
            }
            set
            {
                ViewState["Progress"] = value;
            }
        }

        #endregion

        #region Protected Properties

        protected RegistrationTemplateForm CurrentForm { get; private set; }

        protected ICollection<RegistrationTemplateFee> Fees { get; private set; }

        protected RegistrantInfo Registrant { get; private set; }

        protected string FeeTerm
        {
            get
            {
                return ( string ) ViewState["FeeTerm"];
            }
            private set
            {
                ViewState["FeeTerm"] = value;
            }
        }

        protected bool ShowCurrentFamilyMembers
        {
            get
            {
                return ( bool? ) ViewState["ShowCurrentFamilyMembers"] ?? false;
            }
            private set
            {
                ViewState["ShowCurrentFamilyMembers"] = value;
            }
        }

        #endregion

        public RegistrantControl()
            : base()
        {
            FamilyTerm = "family";
        }

        #region Base Method Overrides

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            Controls.Clear();

            var titleHeader = new HtmlGenericControl( "h1" );
            Controls.Add( titleHeader );

            lRegistrantTitle = new Literal
            {
                ID = this.ID + "_lRegistrantTitle"
            };
            titleHeader.Controls.Add( lRegistrantTitle );

            nbType = new NotificationBox
            {
                ID = this.ID + "_nbType",
                Visible = false
            };
            Controls.Add( nbType );

            nbError = new NotificationBox
            {
                ID = this.ID + "_nbError",
                Visible = false
            };
            Controls.Add( nbError );

            progressBar = new ProgressBar
            {
                ID = this.ID + "_progressBar",
                Visible = false
            };
            Controls.Add( progressBar );

            pnlRegistrantFields = new Panel
            {
                ID = this.ID + "_pnlRegistrantFields"
            };
            Controls.Add( pnlRegistrantFields );

            pnlFamilyOptions = new Panel
            {
                ID = this.ID + "_pnlFamilyOptions",
                CssClass = "well js-registration-same-family"
            };
            Controls.Add( pnlFamilyOptions );

            rblFamilyOptions = new RockRadioButtonList
            {
                ID = this.ID + "_rblFamilyOptions",
                Label = "Individual is in the same immediate family as",
                RepeatDirection = RepeatDirection.Vertical,
                Required = true,
                RequiredErrorMessage = "Answer to which family is required.",
                DataTextField = "Value",
                DataValueField = "Key"
            };
            pnlFamilyOptions.Controls.Add( rblFamilyOptions );

            pnlFamilyMembers = new Panel
            {
                ID = this.ID + "_pnlFamilyMembers",
                Visible = false,
                CssClass = "row"
            };
            Controls.Add( pnlFamilyMembers );

            var familyMembersDiv = new HtmlGenericControl( "div" );
            familyMembersDiv.Attributes.Add( "class", "col-md-6" );
            pnlFamilyMembers.Controls.Add( familyMembersDiv );

            ddlFamilyMembers = new RockDropDownList
            {
                ID = this.ID + "_ddlFamilyMembers",
                Label = "Family Member",
                AutoPostBack = true
            };
            ddlFamilyMembers.SelectedIndexChanged += ddlFamilyMembers_SelectedIndexChanged;
            familyMembersDiv.Controls.Add( ddlFamilyMembers );

            phRegistrantControls = new PlaceHolder
            {
                ID = this.ID + "_phRegistrantControls"
            };
            Controls.Add( phRegistrantControls );

            pnlFees = new Panel
            {
                ID = this.ID + "_pnlFees",
                CssClass = "well registration-additional-options"
            };
            pnlRegistrantFields.Controls.Add( pnlFees );

            var feesHeading = new HtmlGenericControl( "h4" );
            pnlFees.Controls.Add( feesHeading );

            lRegistrantFeeCaption = new Literal
            {
                ID = this.ID + "_lRegistrantFeeCaption"
            };
            feesHeading.Controls.Add( lRegistrantFeeCaption );

            phFees = new PlaceHolder
            {
                ID = this.ID + "_phFees"
            };
            pnlFees.Controls.Add( phFees );

            pnlDigitalSignature = new Panel
            {
                ID = this.ID + "_pnlDigitalSignature",
                Visible = false
            };
            Controls.Add( pnlDigitalSignature );

            nbDigitalSignature = new NotificationBox
            {
                ID = this.ID + "_nbDigitalSignature",
                NotificationBoxType = NotificationBoxType.Info
            };
            pnlDigitalSignature.Controls.Add( nbDigitalSignature );

            hfRequiredDocumentLinkUrl = new HiddenField
            {
                ID = this.ID + "_hfRequiredDocumentLinkUrl"
            };
            pnlDigitalSignature.Controls.Add( hfRequiredDocumentLinkUrl );

            hfRequiredDocumentQueryString = new HiddenField
            {
                ID = this.ID + "_hfRequiredDocumentQueryString"
            };
            pnlDigitalSignature.Controls.Add( hfRequiredDocumentQueryString );

            var iframe = new HtmlIframe
            {
                ID = "iframeRequiredDocument"
            };
            iframe.Attributes.Add( "frameborder", "0" );
            pnlDigitalSignature.Controls.Add( iframe );

            var buttonSpan = new HtmlGenericControl( "span" );
            buttonSpan.Style.Add( "display", "none" );
            pnlDigitalSignature.Controls.Add( buttonSpan );

            lbRequiredDocumentNext = new LinkButton
            {
                ID = this.ID + "_lbRequiredDocumentNext",
                Text = "Required Document Return",
                CausesValidation = false
            };
            lbRequiredDocumentNext.Click += lbRequiredDocumentNext_Click;
            buttonSpan.Controls.Add( lbRequiredDocumentNext );
        }

        public override void RenderControl( HtmlTextWriter writer )
        {
            progressBar.Visible = Progress.HasValue;
            progressBar.Progress = Progress ?? 0;

            base.RenderControl( writer );

            var script = string.Format( @"
    // Evaluates the current url whenever the iframe is loaded and if it includes a qrystring parameter
    // The qry parameter value is saved to a hidden field and a post back is performed
    $('#iframeRequiredDocument').on('load', function(e) {{
        var location = this.contentWindow.location;
        try {{
            var qryString = this.contentWindow.location.search;
            if ( qryString && qryString != '' && qryString.startsWith('?document_id') ) {{ 
                $('#{0}').val(qryString);
                {1};
            }}
        }}
        catch (e) {{
            console.log(e.message);
        }}
    }});

    if ($('#{2}').val() != '' ) {{
        $('#iframeRequiredDocument').attr('src', $('#{2}').val() );
    }}
"
            , hfRequiredDocumentQueryString.ClientID // {0}
            , this.Page.ClientScript.GetPostBackEventReference( lbRequiredDocumentNext, "" ) // {1}
            , hfRequiredDocumentLinkUrl.ClientID     // {2}
            );

            ScriptManager.RegisterStartupScript( Page, Page.GetType(), "registrationRegistrantEntry", script, true );

        }

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            EnsureChildControls();

            RegisterClientScript();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            if ( ViewState["CurrentForm"] != null )
            {
                CurrentForm = JsonConvert.DeserializeObject<RegistrationTemplateForm>( ( string ) ViewState["CurrentForm"] );
            }

            if ( ViewState["Fees"] != null )
            {
                Fees = JsonConvert.DeserializeObject<ICollection<RegistrationTemplateFee>>( ( string ) ViewState["Fees"] );
            }

            if ( ViewState["Registrant"] != null )
            {
                Registrant = JsonConvert.DeserializeObject<RegistrantInfo>( ( string ) ViewState["Registrant"] );
            }

            if ( CurrentForm != null )
            {
                CreateRegistrantControls( null );
            }
        }

        protected override object SaveViewState()
        {
            var jsonSetting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new Rock.Utility.IgnoreUrlEncodedKeyContractResolver()
            };

            ViewState["CurrentForm"] = JsonConvert.SerializeObject( CurrentForm, Formatting.None, jsonSetting );
            ViewState["Fees"] = JsonConvert.SerializeObject( Fees, Formatting.None, jsonSetting );
            ViewState["Registrant"] = JsonConvert.SerializeObject( Registrant, Formatting.None, jsonSetting );

            return base.SaveViewState();
        }

        #endregion

        #region Dynamic Controls

        /// <summary>
        /// Creates the registrant controls.
        /// </summary>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void CreateRegistrantControls( RegistrationState state )
        {
            lRegistrantFeeCaption.Text = FeeTerm.Pluralize();

            phRegistrantControls.Controls.Clear();
            phFees.Controls.Clear();

            if ( CurrentForm != null && Registrant != null )
            {
                // Get the current and previous registrant ( previous is used when a field has the 'IsSharedValue' property )
                // so that current registrant can use the previous registrants value
                RegistrantInfo previousRegistrant = null;

                // If this is not the first person, then check to see if option for asking about family should be displayed
                if ( state != null )
                {
                    if ( FormIndex == 0 && RegistrantIndex > 0 &&
                        state.RegistrationTemplate.RegistrantsSameFamily == RegistrantsSameFamily.Ask )
                    {
                        var familyOptions = state.RegistrationInfo.GetFamilyOptions( state.RegistrationTemplate, RegistrantIndex );
                        if ( familyOptions.Any() )
                        {
                            familyOptions.Add( familyOptions.ContainsKey( Registrant.FamilyGuid ) ?
                                Guid.NewGuid() :
                                Registrant.FamilyGuid.Equals( Guid.Empty ) ? Guid.NewGuid() : Registrant.FamilyGuid,
                                "None of the above" );

                            rblFamilyOptions.Items.Clear();
                            familyOptions.ToList()
                                .ForEach( d => rblFamilyOptions.Items.Add( new ListItem( d.Value, d.Key.ToString() ) ) );

                            pnlFamilyOptions.Visible = true;
                        }
                        else
                        {
                            pnlFamilyOptions.Visible = false;
                        }
                    }
                    else
                    {
                        pnlFamilyOptions.Visible = false;
                    }
                }

                if ( state != null )
                {
                    if ( RegistrantIndex > 0 )
                    {
                        previousRegistrant = state.RegistrationInfo.Registrants[RegistrantIndex - 1];
                    }

                    rblFamilyOptions.SetValue( Registrant.FamilyGuid.ToString() );
                }

                var familyMemberSelected = Registrant.Id <= 0 && Registrant.PersonId.HasValue && ShowCurrentFamilyMembers;

                foreach ( var field in CurrentForm.Fields
                    .Where( f =>
                        !f.IsInternal &&
                        ( !Registrant.OnWaitList || f.ShowOnWaitlist ) )
                    .OrderBy( f => f.Order ) )
                {
                    object value = null;
                    if ( Registrant != null && Registrant.FieldValues.ContainsKey( field.Id ) )
                    {
                        value = Registrant.FieldValues[field.Id].FieldValue;
                    }

                    if ( value == null && field.IsSharedValue && previousRegistrant != null && previousRegistrant.FieldValues.ContainsKey( field.Id ) )
                    {
                        value = previousRegistrant.FieldValues[field.Id].FieldValue;
                    }

                    if ( !string.IsNullOrWhiteSpace( field.PreText ) )
                    {
                        phRegistrantControls.Controls.Add( new LiteralControl( field.PreText ) );
                    }

                    if ( field.FieldSource == RegistrationFieldSource.PersonField )
                    {
                        CreatePersonField( field, state != null, value, familyMemberSelected );
                    }
                    else
                    {
                        CreateAttributeField( field, state != null, value );
                    }

                    if ( !string.IsNullOrWhiteSpace( field.PostText ) )
                    {
                        phRegistrantControls.Controls.Add( new LiteralControl( field.PostText ) );
                    }
                }

                // If the current form, is the last one, add any fee controls
                if ( Fees != null && FormCount - 1 == FormIndex && !Registrant.OnWaitList )
                {
                    foreach ( var fee in Fees )
                    {
                        var feeValues = new List<FeeInfo>();
                        if ( Registrant != null && Registrant.FeeValues.ContainsKey( fee.Id ) )
                        {
                            feeValues = Registrant.FeeValues[fee.Id];
                        }
                        CreateFeeField( fee, state != null, feeValues );
                    }
                }
            }

            pnlFees.Visible = phFees.Controls.Count > 0;
        }

        /// <summary>
        /// Creates the person field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="setValue">if set to <c>true</c> [set value].</param>
        /// <param name="fieldValue">The field value.</param>
        private void CreatePersonField( RegistrationTemplateFormField field, bool setValue, object fieldValue, bool familyMemberSelected )
        {

            switch ( field.PersonFieldType )
            {
                case RegistrationPersonFieldType.FirstName:
                    {
                        var tbFirstName = new RockTextBox();
                        tbFirstName.ID = "tbFirstName";
                        tbFirstName.Label = "First Name";
                        tbFirstName.Required = field.IsRequired;
                        tbFirstName.ValidationGroup = ValidationGroup;
                        tbFirstName.AddCssClass( "js-first-name" );
                        tbFirstName.Enabled = !familyMemberSelected;
                        phRegistrantControls.Controls.Add( tbFirstName );

                        if ( setValue && fieldValue != null )
                        {
                            tbFirstName.Text = fieldValue.ToString();
                        }

                        break;
                    }

                case RegistrationPersonFieldType.LastName:
                    {
                        var tbLastName = new RockTextBox();
                        tbLastName.ID = "tbLastName";
                        tbLastName.Label = "Last Name";
                        tbLastName.Required = field.IsRequired;
                        tbLastName.ValidationGroup = ValidationGroup;
                        tbLastName.Enabled = !familyMemberSelected;
                        phRegistrantControls.Controls.Add( tbLastName );

                        if ( setValue && fieldValue != null )
                        {
                            tbLastName.Text = fieldValue.ToString();
                        }

                        break;
                    }

                case RegistrationPersonFieldType.Campus:
                    {
                        var cpHomeCampus = new CampusPicker();
                        cpHomeCampus.ID = "cpHomeCampus";
                        cpHomeCampus.Label = "Campus";
                        cpHomeCampus.Required = field.IsRequired;
                        cpHomeCampus.ValidationGroup = ValidationGroup;
                        cpHomeCampus.Campuses = CampusCache.All( false );

                        phRegistrantControls.Controls.Add( cpHomeCampus );

                        if ( setValue && fieldValue != null )
                        {
                            cpHomeCampus.SelectedCampusId = fieldValue.ToString().AsIntegerOrNull();
                        }
                        break;
                    }

                case RegistrationPersonFieldType.Address:
                    {
                        var acAddress = new AddressControl();
                        acAddress.ID = "acAddress";
                        acAddress.Label = "Address";
                        acAddress.UseStateAbbreviation = true;
                        acAddress.UseCountryAbbreviation = false;
                        acAddress.Required = field.IsRequired;
                        acAddress.ValidationGroup = ValidationGroup;

                        phRegistrantControls.Controls.Add( acAddress );

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue as Location;
                            acAddress.SetValues( value );
                        }

                        break;
                    }

                case RegistrationPersonFieldType.Email:
                    {
                        var tbEmail = new EmailBox();
                        tbEmail.ID = "tbEmail";
                        tbEmail.Label = "Email";
                        tbEmail.Required = field.IsRequired;
                        tbEmail.ValidationGroup = ValidationGroup;
                        phRegistrantControls.Controls.Add( tbEmail );

                        if ( setValue && fieldValue != null )
                        {
                            tbEmail.Text = fieldValue.ToString();
                        }

                        break;
                    }

                case RegistrationPersonFieldType.Birthdate:
                    {
                        var bpBirthday = new BirthdayPicker();
                        bpBirthday.ID = "bpBirthday";
                        bpBirthday.Label = "Birthday";
                        bpBirthday.Required = field.IsRequired;
                        bpBirthday.ValidationGroup = ValidationGroup;
                        phRegistrantControls.Controls.Add( bpBirthday );

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue as DateTime?;
                            bpBirthday.SelectedDate = value;
                        }

                        break;
                    }

                case RegistrationPersonFieldType.Grade:
                    {
                        var gpGrade = new GradePicker();
                        gpGrade.ID = "gpGrade";
                        gpGrade.Label = "Grade";
                        gpGrade.Required = field.IsRequired;
                        gpGrade.ValidationGroup = ValidationGroup;
                        gpGrade.UseAbbreviation = true;
                        gpGrade.UseGradeOffsetAsValue = true;
                        gpGrade.CssClass = "input-width-md";
                        phRegistrantControls.Controls.Add( gpGrade );

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.ToString().AsIntegerOrNull();
                            gpGrade.SetValue( Person.GradeOffsetFromGraduationYear( value ) );
                        }

                        break;
                    }

                case RegistrationPersonFieldType.Gender:
                    {
                        var ddlGender = new RockDropDownList();
                        ddlGender.ID = "ddlGender";
                        ddlGender.Label = "Gender";
                        ddlGender.Required = field.IsRequired;
                        ddlGender.ValidationGroup = ValidationGroup;
                        ddlGender.BindToEnum<Gender>( false );

                        // change the 'Unknow' value to be blank instead
                        ddlGender.Items.FindByValue( "0" ).Text = string.Empty;

                        phRegistrantControls.Controls.Add( ddlGender );

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.ToString().ConvertToEnumOrNull<Gender>() ?? Gender.Unknown;
                            ddlGender.SetValue( value.ConvertToInt() );
                        }

                        break;
                    }

                case RegistrationPersonFieldType.MaritalStatus:
                    {
                        var ddlMaritalStatus = new RockDropDownList();
                        ddlMaritalStatus.ID = "ddlMaritalStatus";
                        ddlMaritalStatus.Label = "Marital Status";
                        ddlMaritalStatus.Required = field.IsRequired;
                        ddlMaritalStatus.ValidationGroup = ValidationGroup;
                        ddlMaritalStatus.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() ), true );
                        phRegistrantControls.Controls.Add( ddlMaritalStatus );

                        if ( setValue && fieldValue != null )
                        {
                            var value = fieldValue.ToString().AsInteger();
                            ddlMaritalStatus.SetValue( value );
                        }

                        break;
                    }

                case RegistrationPersonFieldType.MobilePhone:
                    {
                        var dv = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );
                        if ( dv != null )
                        {
                            var ppMobile = new PhoneNumberBox();
                            ppMobile.ID = "ppMobile";
                            ppMobile.Label = dv.Value;
                            ppMobile.Required = field.IsRequired;
                            ppMobile.ValidationGroup = ValidationGroup;
                            ppMobile.CountryCode = PhoneNumber.DefaultCountryCode();

                            phRegistrantControls.Controls.Add( ppMobile );

                            if ( setValue && fieldValue != null )
                            {
                                var value = fieldValue as PhoneNumber;
                                if ( value != null )
                                {
                                    ppMobile.CountryCode = value.CountryCode;
                                    ppMobile.Number = value.ToString();
                                }
                            }
                        }

                        break;
                    }
                case RegistrationPersonFieldType.HomePhone:
                    {
                        var dv = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME );
                        if ( dv != null )
                        {
                            var ppHome = new PhoneNumberBox();
                            ppHome.ID = "ppHome";
                            ppHome.Label = dv.Value;
                            ppHome.Required = field.IsRequired;
                            ppHome.ValidationGroup = ValidationGroup;
                            ppHome.CountryCode = PhoneNumber.DefaultCountryCode();

                            phRegistrantControls.Controls.Add( ppHome );

                            if ( setValue && fieldValue != null )
                            {
                                var value = fieldValue as PhoneNumber;
                                if ( value != null )
                                {
                                    ppHome.CountryCode = value.CountryCode;
                                    ppHome.Number = value.ToString();
                                }
                            }
                        }

                        break;
                    }

                case RegistrationPersonFieldType.WorkPhone:
                    {
                        var dv = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK );
                        if ( dv != null )
                        {
                            var ppWork = new PhoneNumberBox();
                            ppWork.ID = "ppWork";
                            ppWork.Label = dv.Value;
                            ppWork.Required = field.IsRequired;
                            ppWork.ValidationGroup = ValidationGroup;
                            ppWork.CountryCode = PhoneNumber.DefaultCountryCode();

                            phRegistrantControls.Controls.Add( ppWork );

                            if ( setValue && fieldValue != null )
                            {
                                var value = fieldValue as PhoneNumber;
                                if ( value != null )
                                {
                                    ppWork.CountryCode = value.CountryCode;
                                    ppWork.Number = value.ToString();
                                }
                            }
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Creates the attribute field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="setValue">if set to <c>true</c> [set value].</param>
        /// <param name="fieldValue">The field value.</param>
        private void CreateAttributeField( RegistrationTemplateFormField field, bool setValue, object fieldValue )
        {
            if ( field.AttributeId.HasValue )
            {
                var attribute = AttributeCache.Read( field.AttributeId.Value );

                string value = string.Empty;
                if ( setValue && fieldValue != null )
                {
                    value = fieldValue.ToString();
                }

                attribute.AddControl( phRegistrantControls.Controls, value, ValidationGroup, setValue, true, field.IsRequired, null, string.Empty );
            }
        }

        /// <summary>
        /// Creates the fee field.
        /// </summary>
        /// <param name="fee">The fee.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        /// <param name="feeValues">The fee values.</param>
        private void CreateFeeField( RegistrationTemplateFee fee, bool setValues, List<FeeInfo> feeValues )
        {
            if ( fee.FeeType == RegistrationFeeType.Single )
            {
                string label = fee.Name;
                var cost = fee.CostValue.AsDecimalOrNull();
                if ( cost.HasValue && cost.Value != 0.0M )
                {
                    label = string.Format( "{0} ({1})", fee.Name, cost.Value.FormatAsCurrency() );
                }

                if ( fee.AllowMultiple )
                {
                    // Single Option, Multi Quantity
                    var numUpDown = new NumberUpDown();
                    numUpDown.ID = "fee_" + fee.Id.ToString();
                    numUpDown.Label = label;
                    numUpDown.Minimum = 0;
                    phFees.Controls.Add( numUpDown );

                    if ( setValues && feeValues != null && feeValues.Any() )
                    {
                        numUpDown.Value = feeValues.First().Quantity;
                    }
                }
                else
                {
                    // Single Option, Single Quantity
                    var cb = new RockCheckBox();
                    cb.ID = "fee_" + fee.Id.ToString();
                    cb.Label = label;
                    cb.SelectedIconCssClass = "fa fa-check-square-o fa-lg";
                    cb.UnSelectedIconCssClass = "fa fa-square-o fa-lg";
                    phFees.Controls.Add( cb );

                    if ( setValues && feeValues != null && feeValues.Any() )
                    {
                        cb.Checked = feeValues.First().Quantity > 0;
                    }
                }
            }
            else
            {
                // Parse the options to get name and cost for each
                var options = new Dictionary<string, string>();
                string[] nameValues = fee.CostValue.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries );
                foreach ( string nameValue in nameValues )
                {
                    string[] nameAndValue = nameValue.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );
                    if ( nameAndValue.Length == 1 )
                    {
                        options.AddOrIgnore( nameAndValue[0], nameAndValue[0] );
                    }
                    if ( nameAndValue.Length == 2 )
                    {
                        options.AddOrIgnore( nameAndValue[0], string.Format( "{0} ({1})", nameAndValue[0], nameAndValue[1].AsDecimal().FormatAsCurrency() ) );
                    }
                }

                if ( fee.AllowMultiple )
                {
                    HtmlGenericControl feeAllowMultiple = new HtmlGenericControl( "div" );
                    phFees.Controls.Add( feeAllowMultiple );

                    feeAllowMultiple.AddCssClass( "feetype-allowmultiples" );

                    Label titleLabel = new Label();
                    feeAllowMultiple.Controls.Add( titleLabel );
                    titleLabel.CssClass = "control-label";
                    titleLabel.Text = fee.Name;

                    foreach ( var optionKeyVal in options )
                    {
                        var numUpDown = new NumberUpDown();
                        numUpDown.ID = string.Format( "fee_{0}_{1}", fee.Id, optionKeyVal.Key );
                        numUpDown.Label = string.Format( "{0}", optionKeyVal.Value );
                        numUpDown.Minimum = 0;
                        numUpDown.CssClass = "fee-allowmultiple";
                        feeAllowMultiple.Controls.Add( numUpDown );

                        if ( setValues && feeValues != null && feeValues.Any() )
                        {
                            numUpDown.Value = feeValues
                                .Where( f => f.Option == optionKeyVal.Key )
                                .Select( f => f.Quantity )
                                .FirstOrDefault();
                        }
                    }
                }
                else
                {
                    // Multi Option, Single Quantity
                    var ddl = new RockDropDownList();
                    ddl.ID = "fee_" + fee.Id.ToString();
                    ddl.AddCssClass( "input-width-md" );
                    ddl.Label = fee.Name;
                    ddl.DataValueField = "Key";
                    ddl.DataTextField = "Value";
                    ddl.DataSource = options;
                    ddl.DataBind();
                    ddl.Items.Insert( 0, "" );
                    phFees.Controls.Add( ddl );

                    if ( setValues && feeValues != null && feeValues.Any() )
                    {
                        ddl.SetValue( feeValues
                            .Where( f => f.Quantity > 0 )
                            .Select( f => f.Option )
                            .FirstOrDefault() );
                    }
                }
            }
        }

        /// <summary>
        /// Parses the person field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        private object ParsePersonField( RegistrationTemplateFormField field )
        {
            switch ( field.PersonFieldType )
            {
                case RegistrationPersonFieldType.FirstName:
                    {
                        var tbFirstName = phRegistrantControls.FindControl( "tbFirstName" ) as RockTextBox;
                        string value = tbFirstName != null ? tbFirstName.Text : null;
                        return string.IsNullOrWhiteSpace( value ) ? null : value;
                    }

                case RegistrationPersonFieldType.LastName:
                    {
                        var tbLastName = phRegistrantControls.FindControl( "tbLastName" ) as RockTextBox;
                        string value = tbLastName != null ? tbLastName.Text : null;
                        return string.IsNullOrWhiteSpace( value ) ? null : value;
                    }

                case RegistrationPersonFieldType.Campus:
                    {
                        var cpHomeCampus = phRegistrantControls.FindControl( "cpHomeCampus" ) as CampusPicker;
                        return cpHomeCampus != null ? cpHomeCampus.SelectedCampusId : null;
                    }

                case RegistrationPersonFieldType.Address:
                    {
                        var location = new Location();
                        var acAddress = phRegistrantControls.FindControl( "acAddress" ) as AddressControl;
                        if ( acAddress != null )
                        {
                            acAddress.GetValues( location );
                            return location;
                        }
                        break;
                    }

                case RegistrationPersonFieldType.Email:
                    {
                        var tbEmail = phRegistrantControls.FindControl( "tbEmail" ) as EmailBox;
                        string value = tbEmail != null ? tbEmail.Text : null;
                        return string.IsNullOrWhiteSpace( value ) ? null : value;
                    }

                case RegistrationPersonFieldType.Birthdate:
                    {
                        var bpBirthday = phRegistrantControls.FindControl( "bpBirthday" ) as BirthdayPicker;
                        return bpBirthday != null ? bpBirthday.SelectedDate : null;
                    }

                case RegistrationPersonFieldType.Grade:
                    {
                        var gpGrade = phRegistrantControls.FindControl( "gpGrade" ) as GradePicker;
                        return gpGrade != null ? Person.GraduationYearFromGradeOffset( gpGrade.SelectedValueAsInt() ) : null;
                    }

                case RegistrationPersonFieldType.Gender:
                    {
                        var ddlGender = phRegistrantControls.FindControl( "ddlGender" ) as RockDropDownList;
                        return ddlGender != null ? ddlGender.SelectedValueAsInt() : null;
                    }

                case RegistrationPersonFieldType.MaritalStatus:
                    {
                        var ddlMaritalStatus = phRegistrantControls.FindControl( "ddlMaritalStatus" ) as RockDropDownList;
                        return ddlMaritalStatus != null ? ddlMaritalStatus.SelectedValueAsInt() : null;
                    }

                case RegistrationPersonFieldType.MobilePhone:
                    {
                        var phoneNumber = new PhoneNumber();
                        var ppMobile = phRegistrantControls.FindControl( "ppMobile" ) as PhoneNumberBox;
                        if ( ppMobile != null )
                        {
                            phoneNumber.CountryCode = PhoneNumber.CleanNumber( ppMobile.CountryCode );
                            phoneNumber.Number = PhoneNumber.CleanNumber( ppMobile.Number );
                            return phoneNumber;
                        }
                        break;
                    }

                case RegistrationPersonFieldType.HomePhone:
                    {
                        var phoneNumber = new PhoneNumber();
                        var ppHome = phRegistrantControls.FindControl( "ppHome" ) as PhoneNumberBox;
                        if ( ppHome != null )
                        {
                            phoneNumber.CountryCode = PhoneNumber.CleanNumber( ppHome.CountryCode );
                            phoneNumber.Number = PhoneNumber.CleanNumber( ppHome.Number );
                            return phoneNumber;
                        }
                        break;
                    }

                case RegistrationPersonFieldType.WorkPhone:
                    {
                        var phoneNumber = new PhoneNumber();
                        var ppWork = phRegistrantControls.FindControl( "ppWork" ) as PhoneNumberBox;
                        if ( ppWork != null )
                        {
                            phoneNumber.CountryCode = PhoneNumber.CleanNumber( ppWork.CountryCode );
                            phoneNumber.Number = PhoneNumber.CleanNumber( ppWork.Number );
                            return phoneNumber;
                        }
                        break;
                    }
            }

            return null;

        }

        /// <summary>
        /// Parses the attribute field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        private object ParseAttributeField( RegistrationTemplateFormField field )
        {
            if ( field.AttributeId.HasValue )
            {
                var attribute = AttributeCache.Read( field.AttributeId.Value );
                string fieldId = "attribute_field_" + attribute.Id.ToString();

                Control control = phRegistrantControls.FindControl( fieldId );
                if ( control != null )
                {
                    return attribute.FieldType.Field.GetEditValue( control, attribute.QualifierValues );
                }
            }

            return null;
        }

        /// <summary>
        /// Parses the fee.
        /// </summary>
        /// <param name="fee">The fee.</param>
        /// <returns></returns>
        private List<FeeInfo> ParseFee( RegistrationTemplateFee fee )
        {
            string fieldId = string.Format( "fee_{0}", fee.Id );

            if ( fee.FeeType == RegistrationFeeType.Single )
            {
                if ( fee.AllowMultiple )
                {
                    // Single Option, Multi Quantity
                    var numUpDown = phFees.FindControl( fieldId ) as NumberUpDown;
                    if ( numUpDown != null && numUpDown.Value > 0 )
                    {
                        return new List<FeeInfo> { new FeeInfo( string.Empty, numUpDown.Value, fee.CostValue.AsDecimal() ) };
                    }
                }
                else
                {
                    // Single Option, Single Quantity
                    var cb = phFees.FindControl( fieldId ) as RockCheckBox;
                    if ( cb != null && cb.Checked )
                    {
                        return new List<FeeInfo> { new FeeInfo( string.Empty, 1, fee.CostValue.AsDecimal() ) };
                    }
                }
            }
            else
            {
                // Parse the options to get name and cost for each
                var options = new Dictionary<string, string>();
                var optionCosts = new Dictionary<string, decimal>();

                string[] nameValues = fee.CostValue.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries );
                foreach ( string nameValue in nameValues )
                {
                    string[] nameAndValue = nameValue.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );
                    if ( nameAndValue.Length == 1 )
                    {
                        options.AddOrIgnore( nameAndValue[0], nameAndValue[0] );
                        optionCosts.AddOrIgnore( nameAndValue[0], 0.0m );
                    }
                    if ( nameAndValue.Length == 2 )
                    {
                        options.AddOrIgnore( nameAndValue[0], string.Format( "{0} ({1})", nameAndValue[0], nameAndValue[1].AsDecimal().FormatAsCurrency() ) );
                        optionCosts.AddOrIgnore( nameAndValue[0], nameAndValue[1].AsDecimal() );
                    }
                }

                if ( fee.AllowMultiple )
                {
                    // Multi Option, Multi Quantity
                    var result = new List<FeeInfo>();

                    foreach ( var optionKeyVal in options )
                    {
                        string optionFieldId = string.Format( "{0}_{1}", fieldId, optionKeyVal.Key );
                        var numUpDown = phFees.FindControl( optionFieldId ) as NumberUpDown;
                        if ( numUpDown != null && numUpDown.Value > 0 )
                        {
                            result.Add( new FeeInfo( optionKeyVal.Key, numUpDown.Value, optionCosts[optionKeyVal.Key] ) );
                        }
                    }

                    if ( result.Any() )
                    {
                        return result;
                    }
                }
                else
                {
                    // Multi Option, Single Quantity
                    var ddl = phFees.FindControl( fieldId ) as RockDropDownList;
                    if ( ddl != null && ddl.SelectedValue != "" )
                    {
                        return new List<FeeInfo> { new FeeInfo( ddl.SelectedValue, 1, optionCosts[ddl.SelectedValue] ) };
                    }
                }
            }

            return null;
        }

        #endregion

        #region Registrant Panel Events

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlFamilyMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlFamilyMembers_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( SelectedPersonChanged != null )
            {
                SelectedPersonChanged( this, new SelectedPersonChangedEventArgs( ddlFamilyMembers.SelectedValueAsInt() ) );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbRequiredDocumentNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbRequiredDocumentNext_Click( object sender, EventArgs e )
        {
            hfRequiredDocumentLinkUrl.Value = string.Empty;

            string qryString = hfRequiredDocumentQueryString.Value;
            if ( qryString.StartsWith( "?document_id=" ) )
            {
                if ( Registrant != null )
                {
                    Registrant.SignatureDocumentKey = qryString.Substring( 13 );
                    Registrant.SignatureDocumentLastSent = RockDateTime.Now;
                }

                Next( sender, e );
            }
            else
            {
                //TODO:ShowError( "Invalid or Missing Document Signature",
                //string.Format( "This {0} requires that you sign a {1} for each registrant, but it appears that you may have cancelled or skipped signing this document.",
                //RegistrationState.RegistrationTemplate.RegistrationTerm, RegistrationState.RegistrationTemplate.RequiredSignatureDocumentTemplate.Name ) );
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Shows an error message.
        /// </summary>
        /// <param name="heading">The heading.</param>
        /// <param name="text">The text.</param>
        protected void ShowError( string heading, string text )
        {
            nbError.Heading = heading;
            nbError.Text = string.Format( "<p>{0}</p>", text );
            nbError.NotificationBoxType = NotificationBoxType.Danger;
            nbError.Visible = true;
        }

        /// <summary>
        /// Initialize data and show the registration form specified by the state information
        /// and the CurrentFormIndex.
        /// </summary>
        /// <param name="state">The object that contains the current registration state.</param>
        protected void SetRegistrantForm( RegistrationState state )
        {
            string title = state.RegistrationInfo.RegistrantCount <= 1 ? state.RegistrantTerm :
                    ( RegistrantIndex + 1 ).ToOrdinalWords().Humanize( LetterCasing.Title ) + " " + state.RegistrantTerm;
            if ( FormIndex > 0 )
            {
                title += " (cont)";
            }
            lRegistrantTitle.Text = title;

            nbType.Visible = state.RegistrationInfo.RegistrantCount > state.RegistrationInfo.SlotsAvailable;
            nbType.Text = Registrant.OnWaitList ? string.Format( "This {0} will be on the waiting list", state.RegistrantTerm.ToLower() ) : string.Format( "This {0} will be fully registered.", state.RegistrantTerm.ToLower() );
            nbType.NotificationBoxType = Registrant.OnWaitList ? NotificationBoxType.Warning : NotificationBoxType.Success;

            if ( state.SignInline && FormIndex >= state.FormCount )
            {
                CurrentForm = null;

                string registrantName = state.RegistrantTerm;
                if ( state.RegistrationInfo != null && state.RegistrationInfo.RegistrantCount > RegistrantIndex )
                {
                    registrantName = Registrant.GetFirstName( state.RegistrationTemplate );
                }

                nbDigitalSignature.Heading = "Signature Required";
                nbDigitalSignature.Text = string.Format(
                    "This {0} requires that you sign a {1} for each registrant, please follow the prompts below to digitally sign this document for {2}.",
                    state.RegistrationTerm, state.RegistrationTemplate.RequiredSignatureDocumentTemplate.Name, registrantName );

                var errors = new List<string>();
                string inviteLink = state.DigitalSignatureComponent.GetInviteLink( state.RegistrationTemplate.RequiredSignatureDocumentTemplate.ProviderTemplateKey, out errors );
                if ( !string.IsNullOrWhiteSpace( inviteLink ) )
                {
                    string returnUrl = GlobalAttributesCache.Read().GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash() +
                        this.RockBlock().ResolveRockUrl( "~/Blocks/Event/DocumentReturn.html" );
                    hfRequiredDocumentLinkUrl.Value = string.Format( "{0}?redirect_uri={1}", inviteLink, returnUrl );
                }
                else
                {
                    ShowError( "Digital Signature Error", string.Format( "An Error Occurred Trying to Get Document Link... <ul><li>{0}</li></ul>", errors.AsDelimited( "</li><li>" ) ) );
                    return;
                }

                pnlRegistrantFields.Visible = false;
                pnlDigitalSignature.Visible = true;
            }
            else
            {
                CurrentForm = state.RegistrationTemplate.Forms.OrderBy( f => f.Order ).ToList()[FormIndex];

                pnlRegistrantFields.Visible = true;
                pnlDigitalSignature.Visible = false;

                ddlFamilyMembers.Items.Clear();

                if ( FormIndex == 0 && state.RegistrationInfo != null && state.RegistrationInfo.RegistrantCount > RegistrantIndex )
                {
                    if ( Registrant.Id <= 0 &&
                        FormIndex == 0 &&
                        state.RegistrationTemplate.RegistrantsSameFamily == RegistrantsSameFamily.Yes &&
                        state.RegistrationTemplate.ShowCurrentFamilyMembers &&
                        this.RockBlock().CurrentPerson != null )
                    {
                        var familyMembers = this.RockBlock().CurrentPerson.GetFamilyMembers( true )
                            .Select( m => m.Person )
                            .ToList();

                        for ( int i = 0; i < RegistrantIndex; i++ )
                        {
                            int? personId = state.RegistrationInfo.Registrants[i].PersonId;
                            if ( personId.HasValue )
                            {
                                foreach ( var familyMember in familyMembers.Where( p => p.Id == personId.Value ).ToList() )
                                {
                                    familyMembers.Remove( familyMember );
                                }
                            }
                        }

                        if ( familyMembers.Any() )
                        {
                            ddlFamilyMembers.Visible = true;
                            ddlFamilyMembers.Items.Add( new ListItem() );

                            foreach ( var familyMember in familyMembers )
                            {
                                ListItem listItem = new ListItem( familyMember.FullName, familyMember.Id.ToString() );
                                listItem.Selected = familyMember.Id == Registrant.PersonId;
                                ddlFamilyMembers.Items.Add( listItem );
                            }
                        }
                    }
                }

                pnlFamilyMembers.Visible = ddlFamilyMembers.Items.Count > 0;
            }

            CreateRegistrantControls( state );
        }

        /// <summary>
        /// Shows the next form for the current registrant.
        /// </summary>
        /// <param name="state">The registration state that contains all the forms to display.</param>
        /// <returns>true if a form was displayed, false if there are no more forms available.</returns>
        protected bool ShowNextForm( RegistrationState state )
        {
            int formCount = state.SignInline ? state.FormCount + 1 : state.FormCount;

            do
            {
                FormIndex++;
                if ( FormIndex >= formCount )
                {
                    CurrentForm = null;
                    return false;
                }
            } while ( !state.IsFormVisibleForRegistrant( RegistrantIndex, FormIndex ) );

            SetRegistrantForm( state );

            return true;
        }

        /// <summary>
        /// Shows the previous form for the current registrant.
        /// </summary>
        /// <param name="state">The registration state that contains all the forms to display.</param>
        /// <returns>true if a form was displayed, false if there are no previous forms available.</returns>
        protected bool ShowPreviousForm( RegistrationState state )
        {
            do
            {
                FormIndex--;
                if ( FormIndex < 0 )
                {
                    CurrentForm = null;
                    return false;
                }
            } while ( !state.IsFormVisibleForRegistrant( RegistrantIndex, FormIndex ) );

            SetRegistrantForm( state );

            return true;
        }

        /// <summary>
        /// Register any javascript needed for this control to function correctly.
        /// </summary>
        protected void RegisterClientScript()
        {
            string script = string.Format( @"
    // Adjust the label of 'is in the same family' based on value of first name entered
    $('input.js-first-name').change( function() {{
        var name = $(this).val();
        if ( name == null || name == '') {{
            name = 'Individual';
        }}
        var $lbl = $('div.js-registration-same-family').find('label.control-label')
        $lbl.text( name + ' is in the same {0} as');
    }} );
", FamilyTerm );

            ScriptManager.RegisterStartupScript( Page, GetType(), "registrant_" + ID, script, true );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the state details of this registrant control.
        /// </summary>
        /// <param name="state">The object that contains the current registration data.</param>
        /// <param name="registrantIndex">The index of the registrant to display.</param>
        /// <param name="direction">Whether to display the first form or the last form.</param>
        /// <returns>true if a form was displayed for this registrant, false if not.</returns>
        public bool SetRegistrationStateDetails( RegistrationState state, int registrantIndex, RegistrantFormDirection direction )
        {
            CurrentForm = null;
            RegistrantIndex = registrantIndex;
            Fees = state.RegistrationTemplate.Fees;
            Registrant = state.RegistrationInfo.Registrants[registrantIndex];
            FeeTerm = state.FeeTerm;
            ShowCurrentFamilyMembers = state.RegistrationTemplate.ShowCurrentFamilyMembers;
            FormCount = state.FormCountForRegistrant( registrantIndex );

            return UpdateRegistrationStateDetails( state, direction );
        }

        /// <summary>
        /// Updates the state details of this registrant control. This method moves an existing registrant
        /// to the next or previous form.
        /// </summary>
        /// <param name="state">The object that contains the current registration data.</param>
        /// <param name="direction">Whether to display the next form or the previous form.</param>
        /// <returns>true if a form was displayed for this registrant, false if not.</returns>
        public bool UpdateRegistrationStateDetails( RegistrationState state, RegistrantFormDirection direction )
        {
            switch ( direction )
            {
                case RegistrantFormDirection.Start:
                    FormIndex = -1;
                    break;

                case RegistrantFormDirection.Last:
                    FormIndex = state.FormCount;
                    break;
            }

            if ( direction == RegistrantFormDirection.Start || direction == RegistrantFormDirection.Next )
            {
                return ShowNextForm( state );
            }
            else
            {
                return ShowPreviousForm( state );
            }
        }

        /// <summary>
        /// This is a special use method designed to be used with quick navigation, for example
        /// the Back button. It does not perform checking on if the form should be shown to
        /// the user so it should only be used with form indexes that were previously displayed.
        /// </summary>
        /// <param name="state">The registration state that contains the information we will display.</param>
        /// <param name="registrantIndex">The index of the registrant whose form will be displayed.</param>
        /// <param name="formIndex">The index of the form to be displayed.</param>
        public void SetRegistrationStateDetails( RegistrationState state, int registrantIndex, int formIndex )
        {
            Fees = state.RegistrationTemplate.Fees;
            Registrant = state.RegistrationInfo.Registrants[registrantIndex];
            FeeTerm = state.FeeTerm;
            ShowCurrentFamilyMembers = state.RegistrationTemplate.ShowCurrentFamilyMembers;
            RegistrantIndex = registrantIndex;
            FormIndex = formIndex;
            FormCount = state.FormCountForRegistrant( registrantIndex );

            SetRegistrantForm( state );
        }

        /// <summary>
        /// Update the Registration State with values in the UI.
        /// </summary>
        /// <param name="state">The registration state that is to be updated.</param>
        public void UpdateRegistrationState( RegistrationState state )
        {
            if ( state.RegistrationInfo != null && state.RegistrationInfo.Registrants.Count > RegistrantIndex && RegistrantIndex >= 0 )
            {
                Registrant = state.RegistrationInfo.Registrants[RegistrantIndex];

                if ( rblFamilyOptions.Visible )
                {
                    Registrant.FamilyGuid = rblFamilyOptions.SelectedValue.AsGuid();
                }

                if ( Registrant.FamilyGuid.Equals( Guid.Empty ) )
                {
                    Registrant.FamilyGuid = Guid.NewGuid();
                }

                foreach ( var field in CurrentForm.Fields
                    .Where( f =>
                        !f.IsInternal &&
                        ( !Registrant.OnWaitList || f.ShowOnWaitlist ) )
                    .OrderBy( f => f.Order ) )
                {
                    object value = null;

                    if ( field.FieldSource == RegistrationFieldSource.PersonField )
                    {
                        value = ParsePersonField( field );
                    }
                    else
                    {
                        value = ParseAttributeField( field );
                    }

                    if ( value != null )
                    {
                        Registrant.FieldValues.AddOrReplace( field.Id, new FieldValueObject( field, value ) );
                    }
                    else
                    {
                        Registrant.FieldValues.Remove( field.Id );
                    }
                }

                if ( state.FormCount - 1 == FormIndex )
                {
                    foreach ( var fee in state.RegistrationTemplate.Fees )
                    {
                        List<FeeInfo> feeValues = ParseFee( fee );
                        if ( fee != null )
                        {
                            Registrant.FeeValues.AddOrReplace( fee.Id, feeValues );
                        }
                        else
                        {
                            Registrant.FeeValues.Remove( fee.Id );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the registrant fields.
        /// </summary>
        /// <param name="personId">The person identifier.</param>
        public void SetRegistrantFields( RegistrationState state, int? personId )
        {
            if ( Registrant != null )
            {
                using ( var rockContext = new RockContext() )
                {
                    if ( Registrant != null )
                    {
                        Person person = null;
                        Group family = null;

                        if ( personId.HasValue )
                        {
                            person = new PersonService( rockContext ).Get( personId.Value );
                        }

                        if ( person != null )
                        {
                            Registrant.PersonId = person.Id;
                            Registrant.PersonName = person.FullName;
                            family = person.GetFamilies( rockContext ).FirstOrDefault();
                        }
                        else
                        {
                            Registrant.PersonId = null;
                            Registrant.PersonName = string.Empty;
                        }

                        foreach ( var field in state.RegistrationTemplate.Forms
                            .SelectMany( f => f.Fields ) )
                        {
                            object dbValue = null;

                            if ( field.ShowCurrentValue ||
                                ( ( field.PersonFieldType == RegistrationPersonFieldType.FirstName ||
                                field.PersonFieldType == RegistrationPersonFieldType.LastName ) &&
                                field.FieldSource == RegistrationFieldSource.PersonField ) )
                            {
                                dbValue = Registrant.GetRegistrantValue( null, person, family, field, rockContext );
                            }

                            if ( dbValue != null )
                            {
                                Registrant.FieldValues.AddOrReplace( field.Id, new FieldValueObject( field, dbValue ) );
                            }
                            else
                            {
                                Registrant.FieldValues.Remove( field.Id );
                            }
                        }
                    }
                }
            }

            CreateRegistrantControls( state );
        }

        #endregion
    }

    /// <summary>
    /// Displays and requests information from the user about who is performing
    /// the registration that is in progress.
    /// </summary>
    public class RegistrarInfo : CompositeControl
    {
        #region Public Properties

        /// <summary>
        /// If true then the user interface will indicate that the e-mail will be
        /// automatically saved to the entered value.
        /// </summary>
        public bool ForceEmailUpdate { get; set; }

        /// <summary>
        /// The term to use for "family" when displaying text to the user.
        /// </summary>
        public string FamilyTerm { get; set; }

        /// <summary>
        /// Indicates if the e-mail should be updated on the registrar based on
        /// current settings and user selection.
        /// </summary>
        public bool ShouldUpdateEmail
        {
            get
            {
                EnsureChildControls();

                return !cbUpdateEmail.Visible || cbUpdateEmail.Checked;
            }
        }

        #endregion

        #region Protected Properties

        /// <summary>
        /// The term to use for the registration when displaying our content.
        /// </summary>
        protected string RegistrationTerm
        {
            get
            {
                return ( string ) ViewState["RegistrationTerm"];
            }
            set
            {
                ViewState["RegistrationTerm"] = value;
            }
        }

        #endregion

        #region Private Fields

        private bool _setValues;

        #endregion

        #region Child Controls Fields

        protected RockTextBox tbYourFirstName;
        protected RockTextBox tbYourLastName;
        protected EmailBox tbConfirmationEmail;
        protected RockCheckBox cbUpdateEmail;
        protected RockRadioButtonList rblRegistrarFamilyOptions;

        #endregion

        #region Constructors

        /// <summary>
        /// Initialize a new instance of the RegistrarInfo conrol.
        /// </summary>
        public RegistrarInfo()
        {
            FamilyTerm = "Family";
        }

        #endregion

        #region Base Method Overrides

        /// <summary>
        /// Control is being initialized.
        /// </summary>
        /// <param name="e">Arguments describing this event.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            EnsureChildControls();

            RegisterClientScript();
        }

        /// <summary>
        /// Create and register all child controls needed by this control.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            tbYourFirstName = new RockTextBox
            {
                ID = this.ID + "_tbYourFirstName",
                Label = "First Name",
                CssClass = "js-your-first-name",
                Required = true
            };
            Controls.Add( tbYourFirstName );

            tbYourLastName = new RockTextBox
            {
                ID = this.ID + "_tbYourLastName",
                Label = "Last Name",
                Required = true
            };
            Controls.Add( tbYourLastName );

            tbConfirmationEmail = new EmailBox
            {
                ID = this.ID + "_tbConfirmationEmail",
                Label = "Send Confirmation Emails To",
                Required = true
            };
            Controls.Add( tbConfirmationEmail );

            cbUpdateEmail = new RockCheckBox
            {
                ID = this.ID + "_cbUpdateEmail",
                Text = "Should Your Account Be Updated To Use This Email Address?",
                Checked = true
            };
            Controls.Add( cbUpdateEmail );

            rblRegistrarFamilyOptions = new RockRadioButtonList
            {
                ID = this.ID + "_rblRegistrarFamilyOptions",
                Label = "you are in the same immediate family as",
                RepeatDirection = RepeatDirection.Horizontal,
                Required = true,
                DataTextField = "Value",
                DataValueField = "Key",
                RequiredErrorMessage = "Answer to which family is required."
            };
            Controls.Add( rblRegistrarFamilyOptions );
        }

        /// <summary>
        /// Do final processing before we render our contents out.
        /// </summary>
        /// <param name="e">The arguments related to this event.</param>
        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );

            //
            // Update the label now that we are guaranteed to have the FamilyTerm set.
            //
            rblRegistrarFamilyOptions.Label = string.IsNullOrWhiteSpace( tbYourFirstName.Text ) ?
                "You are in the same " + FamilyTerm + " as" :
                tbYourFirstName.Text + " is in the same " + FamilyTerm + " as";
        }

        /// <summary>
        /// Render the contents of this control to the text writer.
        /// </summary>
        /// <param name="writer">The text writer that will contain the output.</param>
        protected override void Render( HtmlTextWriter writer )
        {
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "well " + CssClass );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            {
                writer.RenderBeginTag( HtmlTextWriterTag.H4 );
                {
                    writer.Write( "This {0} Was Completed By", RegistrationTerm );
                }
                writer.RenderEndTag();

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "row" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-md-6" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        tbYourFirstName.RenderControl( writer );
                    }
                    writer.RenderEndTag();

                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-md-6" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        tbYourLastName.RenderControl( writer );
                    }
                    writer.RenderEndTag();
                }
                writer.RenderEndTag();

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "row" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-md-6" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        tbConfirmationEmail.RenderControl( writer );

                        if ( cbUpdateEmail.Visible )
                        {
                            cbUpdateEmail.RenderControl( writer );
                        }

                        if ( this.RockBlock().CurrentPerson != null && ForceEmailUpdate )
                        {
                            writer.Write( "Note: Your account will automatically be updated with this email address." );
                        }
                    }
                    writer.RenderEndTag();

                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-md-6" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        if ( rblRegistrarFamilyOptions.DataSource != null || true )
                        {
                            writer.AddAttribute( HtmlTextWriterAttribute.Class, "js-registration-same-family" );

                            writer.RenderBeginTag( HtmlTextWriterTag.Div );
                            {
                                rblRegistrarFamilyOptions.RenderControl( writer );
                            }
                            writer.RenderEndTag();
                        }
                    }
                    writer.RenderEndTag();
                }
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Register any javascript needed for this control to function correctly.
        /// </summary>
        protected void RegisterClientScript()
        {
            string script = string.Format( @"
    $('input.js-your-first-name').change( function() {{
        var name = $(this).val();
        if ( name == null || name == '') {{
            name = 'You are';
        }} else {{
            name += ' is';
        }}
        var $lbl = $('div.js-registration-same-family').find('label.control-label')
        $lbl.text( name + ' in the same {0} as');
    }} );
", FamilyTerm );

            ScriptManager.RegisterStartupScript( Page, GetType(), "registrar_" + ID, script, true );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize all the settings for this control with the values from
        /// the RegistrationState.
        /// </summary>
        /// <param name="state">The current state of the registration to use.</param>
        public void SetRegistrationStateDetails( RegistrationState state )
        {
            var currentPerson = this.RockBlock().CurrentPerson;

            EnsureChildControls();

            if ( state == null )
            {
                return;
            }

            RegistrationTerm = state.RegistrationTerm;

            //
            // Update the list of radio buttons for selecting the related family.
            //
            if ( state.RegistrationTemplate.RegistrantsSameFamily == RegistrantsSameFamily.Ask )
            {
                var familyOptions = state.RegistrationInfo.GetFamilyOptions( state.RegistrationTemplate, state.RegistrationInfo.RegistrantCount );
                if ( familyOptions.Any() )
                {
                    Guid? selectedGuid = rblRegistrarFamilyOptions.SelectedValueAsGuid();

                    familyOptions.Add( familyOptions.ContainsKey( state.RegistrationInfo.FamilyGuid ) ?
                        Guid.NewGuid() :
                        state.RegistrationInfo.FamilyGuid.Equals( Guid.Empty ) ? Guid.NewGuid() : state.RegistrationInfo.FamilyGuid,
                        "None" );

                    rblRegistrarFamilyOptions.Items.Clear();
                    familyOptions.ToList()
                        .ForEach( d => rblRegistrarFamilyOptions.Items.Add( new ListItem( d.Value, d.Key.ToString() ) ) );

                    if ( selectedGuid.HasValue )
                    {
                        rblRegistrarFamilyOptions.SetValue( selectedGuid );
                    }

                    rblRegistrarFamilyOptions.Visible = true;
                }
                else
                {
                    rblRegistrarFamilyOptions.Visible = false;
                }
            }
            else
            {
                rblRegistrarFamilyOptions.Visible = false;
            }

            // Check to see if this is an existing registration or information has already been entered
            if ( state.RegistrationInfo.RegistrationId.HasValue ||
                !string.IsNullOrWhiteSpace( state.RegistrationInfo.FirstName ) ||
                !string.IsNullOrWhiteSpace( state.RegistrationInfo.LastName ) ||
                !string.IsNullOrWhiteSpace( state.RegistrationInfo.ConfirmationEmail ) )
            {
                // If so, use it
                tbYourFirstName.Text = state.RegistrationInfo.FirstName;
                tbYourLastName.Text = state.RegistrationInfo.LastName;
                tbConfirmationEmail.Text = state.RegistrationInfo.ConfirmationEmail;
            }
            else
            {
                if ( currentPerson != null )
                {
                    tbYourFirstName.Text = currentPerson.NickName;
                    tbYourLastName.Text = currentPerson.LastName;
                    tbConfirmationEmail.Text = currentPerson.Email;
                }
                else
                {
                    tbYourFirstName.Text = string.Empty;
                    tbYourLastName.Text = string.Empty;
                    tbConfirmationEmail.Text = string.Empty;
                }
            }

            cbUpdateEmail.Visible = currentPerson != null && !string.IsNullOrWhiteSpace( currentPerson.Email ) && !ForceEmailUpdate;
        }

        /// <summary>
        /// Update the Registration State with values in the UI.
        /// </summary>
        /// <param name="state">The registration state that is to be updated.</param>
        public void UpdateRegistrationState( RegistrationState state )
        {
            state.RegistrationInfo.FirstName = tbYourFirstName.Text;
            state.RegistrationInfo.LastName = tbYourLastName.Text;
            state.RegistrationInfo.ConfirmationEmail = tbConfirmationEmail.Text;
            state.ShouldUpdateRegistrarEmail = ShouldUpdateEmail;

            if ( rblRegistrarFamilyOptions.Visible )
            {
                state.RegistrationInfo.FamilyGuid = rblRegistrarFamilyOptions.SelectedValue.AsGuid();
            }

            if ( state.RegistrationInfo.FamilyGuid.Equals( Guid.Empty ) )
            {
                state.RegistrationInfo.FamilyGuid = Guid.NewGuid();
            }
        }

        #endregion
    }

    public class RegistrantsReview : WebControl
    {
        #region Protected Properties

        protected ICollection<string> RegisteredNames { get; set; }

        protected ICollection<string> WaitListNames { get; set; }

        protected string RegistrantTerm
        {
            get
            {
                return ( string ) ViewState["RegistrantTerm"];
            }
            set
            {
                ViewState["RegistrantTerm"] = value;
            }
        }

        protected string RegistrationInstanceName
        {
            get
            {
                return ( string ) ViewState["RegistrationInstanceName"];
            }
            set
            {
                ViewState["RegistrationInstanceName"] = value;
            }
        }

        #endregion

        #region Base Method Overrides

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            if ( !string.IsNullOrWhiteSpace( ( string ) ViewState["RegisteredNames"] ) )
            {
                RegisteredNames = ( ( string ) ViewState["RegisteredNames"] ).Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries );
            }

            if ( !string.IsNullOrWhiteSpace( ( string ) ViewState["WaitListNames"] ) )
            {
                WaitListNames = ( ( string ) ViewState["WaitListNames"] ).Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries );
            }
        }

        protected override object SaveViewState()
        {
            ViewState["RegisteredNames"] = string.Join( "|", RegisteredNames ?? new string[] { } );
            ViewState["WaitListNames"] = string.Join( "|", WaitListNames ?? new string[] { } );

            return base.SaveViewState();
        }

        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( !Visible )
            {
                return;
            }

            if ( RegisteredNames != null && RegisteredNames.Any() )
            {
                RenderNames( writer, RegisteredNames, false );
            }

            if ( WaitListNames != null && WaitListNames.Any() )
            {
                RenderNames( writer, WaitListNames, true );
            }
        }

        #endregion

        protected void RenderNames( HtmlTextWriter writer, ICollection<string> names, bool isWaitList )
        {
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "margin-b-md" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            {
                if ( !isWaitList )
                {
                    writer.Write( string.Format( "<p>The following {0} will be registered for {1}:",
                        RegistrantTerm.PluralizeIf( names.Count() > 1 ).ToLower(),
                        RegistrationInstanceName ) );
                }
                else
                {
                    writer.Write( string.Format( "<p>The following {0} will be added to the waiting list for {1}:",
                        RegistrantTerm.PluralizeIf( names.Count() > 1 ).ToLower(),
                        RegistrationInstanceName ) );
                }

                writer.RenderBeginTag( HtmlTextWriterTag.Ul );
                {
                    foreach ( string name in names )
                    {
                        writer.RenderBeginTag( HtmlTextWriterTag.Li );
                        {
                            writer.RenderBeginTag( HtmlTextWriterTag.Strong );
                            {
                                writer.Write( name );
                            }
                            writer.RenderEndTag();
                        }
                        writer.RenderEndTag();
                    }
                }
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
        }

        #region Public Methods

        /// <summary>
        /// Initialize all the settings for this control with the values from
        /// the RegistrationState.
        /// </summary>
        /// <param name="state">The current state of the registration to use.</param>
        public void SetRegistrationStateDetails( RegistrationState state )
        {
            RegistrantTerm = state.RegistrantTerm;
            RegistrationInstanceName = state.RegistrationInstance.Name;

            RegisteredNames = state.RegistrationInfo.Registrants
                .Where( r => !r.OnWaitList )
                .Select( r => r.GetFirstName( state.RegistrationTemplate ) + " " + r.GetLastName( state.RegistrationTemplate ) )
                .ToArray();

            WaitListNames = state.RegistrationInfo.Registrants
                .Where( r => r.OnWaitList )
                .Select( r => r.GetFirstName( state.RegistrationTemplate ) + " " + r.GetLastName( state.RegistrationTemplate ) )
                .ToArray();
        }

        #endregion
    }

    public class PaymentSummary : CompositeControl
    {
        #region Protected Control Fields

        protected HiddenField hfTotalCost;

        protected RockLiteral lTotalCost;

        protected HiddenField hfPreviouslyPaid;

        protected RockLiteral lPreviouslyPaid;

        protected HiddenField hfMinimumDue;

        protected RockLiteral lMinimumDue;

        protected CurrencyBox nbAmountPaid;

        protected RockLiteral lRemainingDue;

        protected RockLiteral lAmountDue;

        protected NotificationBox nbDiscountCode;

        protected TextBox tbDiscountCode;

        protected LinkButton lbDiscountApply;

        #endregion

        #region Protected Properties

        protected ICollection<RegistrationCostSummaryInfo> Costs { get; set; }

        protected string DiscountCodesEnabled
        {
            get
            {
                return ( string ) ViewState["DiscountCodesEnabled"];
            }
            set
            {
                ViewState["DiscountCodesEnabled"] = value;
            }
        }

        protected string RegistrantTerm
        {
            get
            {
                return ( string ) ViewState["RegistrantTerm"];
            }
            set
            {
                ViewState["RegistrantTerm"] = value;
            }
        }

        protected string RegistrationInstanceName
        {
            get
            {
                return ( string ) ViewState["RegistrationInstanceName"];
            }
            set
            {
                ViewState["RegistrationInstanceName"] = value;
            }
        }

        protected decimal DiscountPercentage
        {
            get
            {
                return ( decimal? ) ViewState["DiscountPercentage"] ?? 0m;
            }
            set
            {
                ViewState["DiscountPercentage"] = value;
            }
        }

        protected string DiscountCodeTerm
        {
            get
            {
                return ( string ) ViewState["DiscountCodeTerm"];
            }
            set
            {
                ViewState["DiscountCodeTerm"] = value;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the value of the number entered into the Amount Paid field.
        /// </summary>
        public decimal? AmountPaid
        {
            get
            {
                EnsureChildControls();

                if ( !nbAmountPaid.Visible )
                {
                    return null;
                }

                return nbAmountPaid.Text.AsDecimalOrNull();
            }
        }

        /// <summary>
        /// Get the current discount code text applied to the registration.
        /// </summary>
        public string DiscountCode
        {
            get
            {
                EnsureChildControls();

                return tbDiscountCode.Text;
            }
        }

        /// <summary>
        /// This event is triggered when the user applies a discount code to the registration.
        /// </summary>
        public event EventHandler DiscountCodeChanged;

        #endregion

        #region Base Method Overrides

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            EnsureChildControls();

            RegisterClientScript();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            EnsureChildControls();

            nbDiscountCode.Visible = false;
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            hfTotalCost = new HiddenField
            {
                ID = this.ID + "_hfTotalCost"
            };
            Controls.Add( hfTotalCost );

            lTotalCost = new RockLiteral
            {
                ID = this.ID + "_lTotalCost",
                Label = "Total Cost"
            };
            Controls.Add( lTotalCost );

            hfPreviouslyPaid = new HiddenField
            {
                ID = this.ID + "_hfPreviouslyPaid"
            };
            Controls.Add( hfPreviouslyPaid );

            lPreviouslyPaid = new RockLiteral
            {
                ID = this.ID + "_lPreviouslyPaid",
                Label = "Previously Paid"
            };
            Controls.Add( lPreviouslyPaid );

            hfMinimumDue = new HiddenField
            {
                ID = this.ID + "_hfMinimumDue"
            };
            Controls.Add( hfMinimumDue );

            lMinimumDue = new RockLiteral
            {
                ID = this.ID + "_lMinimumDue",
                Label = "Minimum Due Today"
            };
            Controls.Add( lMinimumDue );

            nbAmountPaid = new CurrencyBox
            {
                ID = this.ID + "_nbAmountPaid",
                CssClass = "input-width-md amount-to-pay",
                NumberType = ValidationDataType.Currency,
                Label = "Amount To Pay Today",
                Required = true
            };
            Controls.Add( nbAmountPaid );

            lRemainingDue = new RockLiteral
            {
                ID = this.ID + "_lRemainingDue",
                Label = "Amount Remaining"
            };
            Controls.Add( lRemainingDue );

            lAmountDue = new RockLiteral
            {
                ID = this.ID + "_lAmount Due",
                Label = "Amount Due"
            };
            Controls.Add( lAmountDue );

            nbDiscountCode = new NotificationBox
            {
                ID = this.ID + "_nbDiscountCode",
                NotificationBoxType = NotificationBoxType.Warning,
                Visible = false
            };
            Controls.Add( nbDiscountCode );

            tbDiscountCode = new TextBox
            {
                ID = this.ID + "_tbDiscountCode",
                CssClass = "form-control input-width-md input-sm",
                Visible = false
            };
            Controls.Add( tbDiscountCode );

            lbDiscountApply = new LinkButton
            {
                ID = this.ID + "_lbDiscountApply",
                CssClass = "btn btn-default btn-sm margin-l-sm",
                CausesValidation = false,
                Text = "Apply"
            };
            lbDiscountApply.Click += lbDiscountApply_Click;
            Controls.Add( lbDiscountApply );
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            if ( !string.IsNullOrWhiteSpace( ( string ) ViewState["Costs"] ) )
            {
                Costs = JsonConvert.DeserializeObject<ICollection<RegistrationCostSummaryInfo>>( ( string ) ViewState["Costs"] );
            }
        }

        protected override object SaveViewState()
        {
            var jsonSetting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new Rock.Utility.IgnoreUrlEncodedKeyContractResolver()
            };

            ViewState["Costs"] = JsonConvert.SerializeObject( Costs, Formatting.None, jsonSetting );

            return base.SaveViewState();
        }

        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( !Visible )
            {
                return;
            }

            nbDiscountCode.RenderControl( writer );

            if ( tbDiscountCode.Visible )
            {
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "clearfix" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "form-group pull-right" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "control-label" );
                        writer.RenderBeginTag( HtmlTextWriterTag.Label );
                        {
                            writer.Write( DiscountCodeTerm );
                        }
                        writer.RenderEndTag();

                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "input-group" );
                        writer.RenderBeginTag( HtmlTextWriterTag.Div );
                        {
                            tbDiscountCode.RenderControl( writer );
                            lbDiscountApply.RenderControl( writer );
                        }
                        writer.RenderEndTag();
                    }
                    writer.RenderEndTag();
                }
                writer.RenderEndTag();
            }

            writer.AddAttribute( HtmlTextWriterAttribute.Class, "fee-table" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            {
                RenderCostsHeader( writer );
                foreach ( var cost in Costs )
                {
                    RenderCost( writer, cost );
                }
            }
            writer.RenderEndTag();

            writer.AddAttribute( HtmlTextWriterAttribute.Class, "row fee-totals" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            {
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-sm-offset-8 col-sm-4 fee-totals-options" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    hfTotalCost.RenderControl( writer );
                    lTotalCost.RenderControl( writer );

                    hfPreviouslyPaid.RenderControl( writer );
                    lPreviouslyPaid.RenderControl( writer );

                    hfMinimumDue.RenderControl( writer );
                    lMinimumDue.RenderControl( writer );

                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "form-right" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        nbAmountPaid.RenderControl( writer );
                    }
                    writer.RenderEndTag();

                    lRemainingDue.RenderControl( writer );

                    lAmountDue.RenderControl( writer );
                }
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
        }

        #endregion

        #region Protected Methods

        protected void RenderCostsHeader( HtmlTextWriter writer )
        {
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "row hidden-xs fee-header" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            {
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-sm-6" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    writer.Write( "<strong>Description</strong>" );
                }
                writer.RenderEndTag();

                if ( DiscountPercentage > 0.0m )
                {
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-sm-3 fee-value" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        writer.Write( "<strong>Discounted Amount</strong>" );
                    }
                    writer.RenderEndTag();
                }

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-sm-3 fee-value" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    writer.Write( "<strong>Amount</strong>" );
                }
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
        }

        protected void RenderCost( HtmlTextWriter writer, RegistrationCostSummaryInfo cost )
        {
            writer.AddAttribute( HtmlTextWriterAttribute.Class, string.Format( "row fee-row-{0}", cost.Type.ToString().ToLower() ) );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            {
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-sm-6 fee-caption" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    writer.Write( cost.Description );
                }
                writer.RenderEndTag();

                if ( DiscountPercentage > 0.0m )
                {
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-sm-3 fee-value" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "visible-xs-inline" );
                        writer.RenderBeginTag( HtmlTextWriterTag.Span );
                        {
                            writer.Write( "Discounted Amount:" );
                        }
                        writer.RenderEndTag();

                        writer.Write( " {0} {1:N}", GlobalAttributesCache.Value( "CurrencySymbol" ), cost.DiscountedCost );
                    }
                    writer.RenderEndTag();
                }

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-sm-3 fee-value" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "visible-xs-inline" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Span );
                    {
                        writer.Write( "Amount:" );
                    }
                    writer.RenderEndTag();

                    writer.Write( " {0} {1:N}", GlobalAttributesCache.Value( "CurrencySymbol" ), cost.Cost );
                }
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
        }

        /// <summary>
        /// Registers the client script.
        /// </summary>
        private void RegisterClientScript()
        {
            string script = string.Format( @"
    $('#{0}').on('change', function() {{

        var totalCost = Number($('#{1}').val());
        var minDue = Number($('#{2}').val());
        var previouslyPaid = Number($('#{3}').val());
        var balanceDue = totalCost - previouslyPaid;

        // Format and validate the amount entered
        var amountPaid = minDue;
        var amountValue = $(this).val();
        if ( amountValue != null && amountValue != '' && !isNaN( amountValue ) ) {{
            amountPaid = Number( amountValue );
            if ( amountPaid < minDue ) {{
                amountPaid = minDue;
            }}
            if ( amountPaid > balanceDue ) {{
                amountPaid = balanceDue
            }}
        }}
        $(this).val(amountPaid.toFixed(2));

        var amountRemaining = totalCost - ( previouslyPaid + amountPaid );
        $('#{4}').text( '{5}' + amountRemaining.toFixed(2) );
        
    }});
", nbAmountPaid.ClientID                 // {0}
            , hfTotalCost.ClientID                   // {1}
            , hfMinimumDue.ClientID                  // {2}
            , hfPreviouslyPaid.ClientID              // {3}
            , lRemainingDue.ClientID                 // {4}
            , GlobalAttributesCache.Value( "CurrencySymbol" ) // {5}
            );

            ScriptManager.RegisterStartupScript( Page, GetType(), "registrantControl_" + this.ID, script, true );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize all the settings for this control with the values from
        /// the RegistrationState.
        /// </summary>
        /// <param name="state">The current state of the registration to use.</param>
        public void SetRegistrationStateDetails( RegistrationState state )
        {
            EnsureChildControls();

            RegistrantTerm = state.RegistrantTerm;
            RegistrationInstanceName = state.RegistrationInstance.Name;
            DiscountCodeTerm = state.DiscountCodeTerm;
            DiscountPercentage = state.RegistrationInfo.DiscountPercentage;

            Costs = state.CalculateCostSummary();

            // Build Discount info if template has discounts and this is a new registration
            if ( state.RegistrationTemplate != null
                && state.RegistrationTemplate.Discounts.Any()
                && !state.RegistrationInfo.RegistrationId.HasValue )
            {
                tbDiscountCode.Visible = true;

                string discountCode = state.RegistrationInfo.DiscountCode;
                if ( !string.IsNullOrWhiteSpace( discountCode ) )
                {
                    var discount = state.RegistrationTemplate.Discounts
                        .Where( d => d.Code.Equals( discountCode, StringComparison.OrdinalIgnoreCase ) )
                        .FirstOrDefault();

                    if ( discount == null )
                    {
                        nbDiscountCode.Text = string.Format( "'{1}' is not a valid {1}.", discountCode, DiscountCodeTerm );
                        nbDiscountCode.Visible = true;
                    }
                }
            }
            else
            {
                tbDiscountCode.Text = state.RegistrationInfo.DiscountCode;
            }

            var minimumPayment = state.CalculateMinimumPayment( Costs );

            // Set the total cost
            hfTotalCost.Value = state.RegistrationInfo.DiscountedCost.ToString();
            lTotalCost.Text = state.RegistrationInfo.DiscountedCost.FormatAsCurrency();

            // Check for previous payments
            lPreviouslyPaid.Visible = state.RegistrationInfo.PreviousPaymentTotal != 0.0m;
            hfPreviouslyPaid.Value = state.RegistrationInfo.PreviousPaymentTotal.ToString();
            lPreviouslyPaid.Text = state.RegistrationInfo.PreviousPaymentTotal.FormatAsCurrency();

            // Calculate balance due, and if a partial payment is still allowed
            decimal balanceDue = state.RegistrationInfo.DiscountedCost - state.RegistrationInfo.PreviousPaymentTotal;
            bool allowPartialPayment = balanceDue > 0 && minimumPayment < balanceDue;

            // If partial payment is allowed, show the minimum payment due
            lMinimumDue.Visible = allowPartialPayment;
            hfMinimumDue.Value = minimumPayment.ToString();
            lMinimumDue.Text = minimumPayment.FormatAsCurrency();

            // Make sure payment amount is within minumum due and balance due. If not, set to balance due
            if ( !state.RegistrationInfo.PaymentAmount.HasValue ||
                state.RegistrationInfo.PaymentAmount.Value < minimumPayment ||
                state.RegistrationInfo.PaymentAmount.Value > balanceDue )
            {
                state.RegistrationInfo.PaymentAmount = balanceDue;
            }

            nbAmountPaid.Visible = allowPartialPayment;
            nbAmountPaid.Text = ( state.RegistrationInfo.PaymentAmount ?? 0.0m ).ToString( "N2" );

            // If a previous payment was made, or partial payment is allowed, show the amount remaining after selected payment amount
            lRemainingDue.Visible = allowPartialPayment;
            lRemainingDue.Text = ( state.RegistrationInfo.DiscountedCost - ( state.RegistrationInfo.PreviousPaymentTotal + ( state.RegistrationInfo.PaymentAmount ?? 0.0m ) ) ).FormatAsCurrency();

            lAmountDue.Visible = !allowPartialPayment;
            lAmountDue.Text = ( state.RegistrationInfo.PaymentAmount ?? 0.0m ).FormatAsCurrency();
        }

        /// <summary>
        /// Updates the registration state with information from out controls.
        /// </summary>
        /// <param name="state">The registration state to be updated.</param>
        /// <returns>false if an error occurred that should prevent further processing.</returns>
        public void UpdateRegistrationState( RegistrationState state )
        {
            state.RegistrationInfo.PaymentAmount = nbAmountPaid.Text.AsDecimal();
        }

        #endregion

        /// <summary>
        /// Handles the Click event of the lbDiscountApply control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbDiscountApply_Click( object sender, EventArgs e )
        {
            if ( DiscountCodeChanged != null )
            {
                DiscountCodeChanged( this, new EventArgs() );
            }
        }
    }

    public class ProcessPayment : CompositeControl
    {
        #region Protected Control Fields

        protected RockRadioButtonList rblSavedCC { get; set; }
        protected RockTextBox txtCardFirstName { get; set; }
        protected RockTextBox txtCardLastName { get; set; }
        protected RockTextBox txtCardName { get; set; }
        protected RockTextBox txtCreditCard { get; set; }
        protected MonthYearPicker mypExpiration { get; set; }
        protected RockTextBox txtCVV { get; set; }
        protected AddressControl acBillingAddress { get; set; }

        protected HiddenField hfStep2AutoSubmit { get; set; }
        protected HiddenField hfStep2Url { get; set; }
        protected HiddenField hfStep2ReturnQueryString { get; set; }
        protected LinkButton lbStep2Return { get; set; }

        #endregion

        #region Public Properties

        public string Title
        {
            get
            {
                return ( string ) ViewState["Title"];
            }
            set
            {
                ViewState["Title"] = value;
            }
        }

        public string Step2IFrameUrl { get; set; }

        public string ValidationGroup
        {
            get
            {
                return ( string ) ViewState["ValidationGroup"];
            }
            set
            {
                ViewState["ValidationGroup"] = value;
            }
        }

        public bool Using3StepGateway
        {
            get
            {
                return ( bool? ) ViewState["Using3StepGateway"] ?? false;
            }
            set
            {
                ViewState["Using3StepGateway"] = value;
            }
        }

        protected bool NewCardVisible
        {
            get
            {
                return ( bool? ) ViewState["NewCardVisible"] ?? false;
            }
            set
            {
                ViewState["NewCardVisible"] = value;
            }
        }

        public FinancialGateway FinancialGateway
        {
            get
            {
                if ( _financialGateway == null )
                {
                    int? financialGatewayId = ( int? ) ViewState["FinancialGatewayId"];
                    if ( financialGatewayId.HasValue )
                    {
                        FinancialGateway = new FinancialGatewayService( new RockContext() ).Get( financialGatewayId.Value );
                    }
                }

                return _financialGateway;
            }
            set
            {
                _financialGateway = value;
                ViewState["FinancialGatewayId"] = ( _financialGateway != null ? ( int? ) _financialGateway.Id : null );

                var threeStepGatewayComponent = _financialGateway.GetGatewayComponent() as ThreeStepGatewayComponent;
                if ( threeStepGatewayComponent != null )
                {
                    Step2IFrameUrl = threeStepGatewayComponent.Step2FormUrl;
                    Using3StepGateway = true;
                }
                else
                {
                    Step2IFrameUrl = string.Empty;
                    Using3StepGateway = false;
                }

            }
        }
        private FinancialGateway _financialGateway;

        public FinancialAccount Account
        {
            get
            {
                if ( _account == null )
                {
                    int? accountId = ( int? ) ViewState["AccountId"];
                    if ( accountId.HasValue )
                    {
                        _account = new FinancialAccountService( new RockContext() ).Get( accountId.Value );
                    }
                }

                return _account;
            }
            set
            {
                _account = value;
                ViewState["AccountId"] = ( _account != null ? ( int? ) _account.Id : null );
            }
        }
        private FinancialAccount _account;

        public decimal Amount
        {
            get
            {
                return ( decimal ) ViewState["Amount"];
            }
            set
            {
                ViewState["Amount"] = value;
            }
        }

        public string TransactionCode
        {
            get
            {
                return ( string ) ViewState["TransactionCode"];
            }
            protected set
            {
                ViewState["TransactionCode"] = value;
            }
        }

        public int? CurrentPersonAliasId
        {
            get
            {
                return ( int? ) ViewState["CurrentPersonAliasId"];
            }
            set
            {
                ViewState["CurrentPersonAliasId"] = value;
            }
        }

        public string Step3FinishJavascript
        {
            get
            {
                return "$('#xaStep2Submit').click();";
            }
        }

        #endregion

        public event EventHandler Step2Returned;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RegisterClientScript();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            hfStep2AutoSubmit.Value = "false";
        }

        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );

            rblSavedCC.ValidationGroup = ValidationGroup;
            txtCardFirstName.ValidationGroup = ValidationGroup;
            txtCardLastName.ValidationGroup = ValidationGroup;
            txtCardName.ValidationGroup = ValidationGroup;
            txtCreditCard.ValidationGroup = ValidationGroup;
            mypExpiration.ValidationGroup = ValidationGroup;
            txtCVV.ValidationGroup = ValidationGroup;
            acBillingAddress.ValidationGroup = ValidationGroup;
            lbStep2Return.ValidationGroup = ValidationGroup;
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            rblSavedCC = new RockRadioButtonList
            {
                ID = this.ID + "_rblSavedCC",
                CssClass = "radio-list margin-b-lg",
                RepeatDirection = RepeatDirection.Vertical,
                DataValueField = "Id",
                DataTextField = "Name"
            };
            Controls.Add( rblSavedCC );

            txtCardFirstName = new RockTextBox
            {
                ID = this.ID + "_txtCardFirstName",
                Label = "First Name on Card",
                Visible = false
            };
            Controls.Add( txtCardFirstName );

            txtCardLastName = new RockTextBox
            {
                ID = this.ID + "_txtCardLastName",
                Label = "Last Name on Card",
                Visible = false
            };
            Controls.Add( txtCardLastName );

            txtCardName = new RockTextBox
            {
                ID = this.ID + "_txtCardName",
                Label = "Name on Card",
                Visible = false
            };
            Controls.Add( txtCardName );

            txtCreditCard = new RockTextBox
            {
                ID = this.ID + "_txtCreditCard",
                Label = "Card Number",
                MaxLength = 19,
                CssClass = "credit-card"
            };
            Controls.Add( txtCreditCard );

            mypExpiration = new MonthYearPicker
            {
                ID = this.ID + "_mypExpiration",
                Label = "Expiration Date"
            };
            Controls.Add( mypExpiration );

            txtCVV = new RockTextBox
            {
                ID = this.ID + "_txtCVV",
                Label = "Card Security Code",
                CssClass = "input-width-xs",
                MaxLength = 4
            };
            Controls.Add( txtCVV );

            acBillingAddress = new AddressControl
            {
                ID = this.ID + "_acBillingAddress",
                Label = "Billing Address",
                UseStateAbbreviation = true,
                UseCountryAbbreviation = false,
                ShowAddressLine2 = false
            };
            Controls.Add( acBillingAddress );

            hfStep2AutoSubmit = new HiddenField
            {
                ID = this.ID + "_hfStep2AutoSubmit",
                Value = "false"
            };
            Controls.Add( hfStep2AutoSubmit );

            hfStep2Url = new HiddenField
            {
                ID = this.ID + "_hfStep2Url"
            };
            Controls.Add( hfStep2Url );

            hfStep2ReturnQueryString = new HiddenField
            {
                ID = this.ID + "_hfStep2ReturnQueryString"
            };
            Controls.Add( hfStep2ReturnQueryString );

            lbStep2Return = new LinkButton
            {
                ID = this.ID + "_lbStep2Return",
                Text = "Step 2 Return",
                CausesValidation = false
            };
            lbStep2Return.Click += lbStep2Return_Click;
            Controls.Add( lbStep2Return );
        }

        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( !Visible )
            {
                return;
            }

            writer.AddAttribute( HtmlTextWriterAttribute.Class, CssClass );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            {
                if ( !string.IsNullOrWhiteSpace( Title ) )
                {
                    writer.Write( "<h2>{0}</h2>", Title );
                }

                if ( rblSavedCC.Items.Count > 0 )
                {
                    rblSavedCC.RenderControl( writer );
                }

                if ( NewCardVisible )
                {
                    writer.AddStyleAttribute( HtmlTextWriterStyle.Display, ( rblSavedCC.Items.Count == 0 || rblSavedCC.Items[rblSavedCC.Items.Count - 1].Selected ) ? "block" : "none" );
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "radio-content" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        txtCardFirstName.RenderControl( writer );
                        txtCardLastName.RenderControl( writer );
                        txtCardName.RenderControl( writer );
                        txtCreditCard.RenderControl( writer );

                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "card-logos list-unstyled" );
                        writer.RenderBeginTag( HtmlTextWriterTag.Ul );
                        {
                            writer.Write( "<li class='card-visa'></li>" );
                            writer.Write( "<li class='card-mastercard'></li>" );
                            writer.Write( "<li class='card-amex'></li>" );
                            writer.Write( "<li class='card-discover'></li>" );
                        }
                        writer.RenderEndTag();

                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "row" );
                        writer.RenderBeginTag( HtmlTextWriterTag.Div );
                        {
                            writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-sm-6" );
                            writer.RenderBeginTag( HtmlTextWriterTag.Div );
                            {
                                mypExpiration.RenderControl( writer );
                            }
                            writer.RenderEndTag();

                            writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-sm-6" );
                            writer.RenderBeginTag( HtmlTextWriterTag.Div );
                            {
                                txtCVV.RenderControl( writer );
                            }
                            writer.RenderEndTag();
                        }
                        writer.RenderEndTag();

                        acBillingAddress.RenderControl( writer );
                    }
                    writer.RenderEndTag();
                }
            }
            writer.RenderEndTag();

            writer.Write( "<span id='aStep2Submit' class='hidden'>Finish</span>" );

            writer.Write( "<iframe id='iframeStep2' src='{0}' style='display: none;'></iframe>", Step2IFrameUrl );

            hfStep2AutoSubmit.RenderControl( writer );
            hfStep2Url.RenderControl( writer );
            hfStep2ReturnQueryString.RenderControl( writer );

            writer.AddAttribute( HtmlTextWriterAttribute.Style, "display: none;" );
            writer.RenderBeginTag( HtmlTextWriterTag.Span );
            {
                lbStep2Return.RenderControl( writer );
            }
            writer.RenderEndTag();
        }

        /// <summary>
        /// Registers the client script.
        /// </summary>
        private void RegisterClientScript()
        {
            EnsureChildControls();

            this.RockBlock().RockPage.AddScriptLink( ResolveUrl( "~/Scripts/jquery.creditCardTypeDetector.js" ) );

            string script = string.Format( @"
    // Detect credit card type
    $('.credit-card').creditCardTypeDetector({{ 'credit_card_logos': '.card-logos' }});

    // Toggle credit card display if saved card option is available
    $('div.radio-content').prev('div.radio-list').find('input:radio').unbind('click').on('click', function () {{
        $content = $(this).parents('div.radio-list:first').next('.radio-content');
        var radioDisplay = $content.css('display');
        if ($(this).val() == 0 && radioDisplay == 'none') {{
            $content.slideToggle();
        }}
        else if ($(this).val() != 0 && radioDisplay != 'none') {{
            $content.slideToggle();
        }}
    }});

    $('#aStep2Submit').on('click', function(e) {{
        e.preventDefault();
        if (typeof (Page_ClientValidate) == 'function') {{
            if (Page_IsValid && Page_ClientValidate('{10}') ) {{
                $(this).prop('disabled', true);
                $('#updateProgress').show();
                var src = $('#{7}').val();
                var $form = $('#iframeStep2').contents().find('#Step2Form');

                $form.find('.js-cc-first-name').val( $('#{16}').val() );
                $form.find('.js-cc-last-name').val( $('#{17}').val() );
                $form.find('.js-cc-full-name').val( $('#{18}').val() );

                $form.find('.js-cc-number').val( $('#{11}').val() );
                var mm = $('#{12}_monthDropDownList').val();
                var yy = $('#{12}_yearDropDownList_').val();
                mm = mm.length == 1 ? '0' + mm : mm;
                yy = yy.length == 4 ? yy.substring(2,4) : yy;
                $form.find('.js-cc-expiration').val( mm + yy );
                $form.find('.js-cc-cvv').val( $('#{13}').val() );

                $form.find('.js-billing-address1').val( $('#{15}_tbStreet1').val() );
                $form.find('.js-billing-city').val( $('#{15}_tbCity').val() );

                if ( $('#{15}_ddlState').length ) {{
                    $form.find('.js-billing-state').val( $('#{15}_ddlState').val() );
                }} else {{
                    $form.find('.js-billing-state').val( $('#{15}_tbState').val() );
                }}            
                $form.find('.js-billing-postal').val( $('#{15}_tbPostalCode').val() );
                $form.find('.js-billing-country').val( $('#{15}_ddlCountry').val() );

                $form.attr('action', src );
                $form.submit();
            }}
        }}
    }});

    // Evaluates the current url whenever the iframe is loaded and if it includes a qrystring parameter
    // The qry parameter value is saved to a hidden field and a post back is performed
    $('#iframeStep2').on('load', function(e) {{
        var location = this.contentWindow.location;
        var qryString = this.contentWindow.location.search;
        if ( qryString && qryString != '' && qryString.startsWith('?token-id') ) {{ 
            $('#{8}').val(qryString);
            {9};
        }} else {{
            if ( $('#{14}').val() == 'true' ) {{
                $('#updateProgress').show();
                var src = $('#{7}').val();
                var $form = $('#iframeStep2').contents().find('#Step2Form');
                $form.attr('action', src );
                $form.submit();
            }}
        }}
    }});

"
            , ""                 // {0}
            , ""                 // {1}
            , ""                 // {2}
            , ""                 // {3}
            , ""                 // {4}
            , ""                 // {5}
            , ""                 // {6}
            , hfStep2Url.ClientID                    // {7}
            , hfStep2ReturnQueryString.ClientID      // {8}
            , this.Page.ClientScript.GetPostBackEventReference( lbStep2Return, "" ) // {9}
            , this.ValidationGroup                   // {10}
            , txtCreditCard.ClientID                 // {11}
            , mypExpiration.ClientID                 // {12}
            , txtCVV.ClientID                        // {13}
            , hfStep2AutoSubmit.ClientID             // {14}
            , acBillingAddress.ClientID              // {15}
            , txtCardFirstName.ClientID              // {16}
            , txtCardLastName.ClientID               // {17}
            , txtCardName.ClientID                   // {18}
            , ""                                     // {19}
);

            ScriptManager.RegisterStartupScript( this, this.GetType(), "registrationPayment", script, true );

            if ( Using3StepGateway )
            {
                string submitScript = string.Format( @"
    $('#{0}').val('');
    $('#{1}_monthDropDownList').val('');
    $('#{1}_yearDropDownList_').val('');
    $('#{2}').val('');
",
                txtCreditCard.ClientID,     // {0}
                mypExpiration.ClientID,     // {1}
                txtCVV.ClientID             // {2}
                );

                ScriptManager.RegisterOnSubmitStatement( Page, Page.GetType(), "clearCCFields", submitScript );
            }
        }

        public void SetRegistrationStateDetails( RegistrationState state, Person currentPerson )
        {
            FinancialGateway = state.RegistrationTemplate.FinancialGateway;
            if ( FinancialGateway.Attributes == null )
            {
                FinancialGateway.LoadAttributes();
            }

            var component = FinancialGateway.GetGatewayComponent();
            if ( component != null )
            {
                BindSavedAccounts( state, component, currentPerson );

                if ( rblSavedCC.Items.Count > 0 )
                {
                    rblSavedCC.Items[0].Selected = true;
                    rblSavedCC.Visible = true;
                }
                else
                {
                    rblSavedCC.Visible = false;
                }

                txtCardFirstName.Visible = component.PromptForNameOnCard( state.RegistrationTemplate.FinancialGateway ) && component.SplitNameOnCard;
                txtCardLastName.Visible = component.PromptForNameOnCard( state.RegistrationTemplate.FinancialGateway ) && component.SplitNameOnCard;
                txtCardName.Visible = component.PromptForNameOnCard( state.RegistrationTemplate.FinancialGateway ) && !component.SplitNameOnCard;

                mypExpiration.MinimumYear = RockDateTime.Now.Year;
                mypExpiration.MaximumYear = mypExpiration.MinimumYear + 15;

                acBillingAddress.Visible = component.PromptForBillingAddress( state.RegistrationTemplate.FinancialGateway );

                NewCardVisible = !Using3StepGateway;
            }
        }

        /// <summary>
        /// Processes the first step of a 3-step charge.
        /// </summary>
        /// <param name="registration">The registration.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public bool ProcessStep1( RegistrationState state, out string errorMessage )
        {
            ThreeStepGatewayComponent gateway = null;
            
            if ( state.RegistrationTemplate != null && state.RegistrationTemplate.FinancialGateway != null )
            {
                gateway = state.RegistrationTemplate.FinancialGateway.GetGatewayComponent() as ThreeStepGatewayComponent;
            }

            if ( gateway == null )
            {
                errorMessage = "There was a problem creating the payment gateway information";
                return false;
            }

            if ( !state.RegistrationInstance.AccountId.HasValue || state.RegistrationInstance.Account == null )
            {
                errorMessage = "There was a problem with the account configuration for this " + state.RegistrationTerm.ToLower();
                return false;
            }

            Account = state.RegistrationInstance.Account;
            CurrentPersonAliasId = this.RockBlock().CurrentPersonAliasId;
            Step2IFrameUrl = this.RockBlock().ResolveRockUrl( gateway.Step2FormUrl );

            PaymentInfo paymentInfo = null;
            if ( rblSavedCC.Items.Count > 0 && ( rblSavedCC.SelectedValueAsId() ?? 0 ) > 0 )
            {
                var rockContext = new RockContext();
                var savedAccount = new FinancialPersonSavedAccountService( rockContext ).Get( rblSavedCC.SelectedValueAsId().Value );
                if ( savedAccount != null )
                {
                    paymentInfo = savedAccount.GetReferencePayment();
                    paymentInfo.Amount = state.RegistrationInfo.PaymentAmount ?? 0.0m;
                    hfStep2AutoSubmit.Value = "true";
                }
                else
                {
                    errorMessage = "There was a problem retrieving the saved account";
                    return false;
                }
            }
            else
            {
                paymentInfo = new PaymentInfo();
                paymentInfo.Amount = state.RegistrationInfo.PaymentAmount ?? 0.0m;
                paymentInfo.Email = state.RegistrationInfo.ConfirmationEmail;

                paymentInfo.FirstName = state.RegistrationInfo.FirstName;
                paymentInfo.LastName = state.RegistrationInfo.LastName;
            }

            paymentInfo.Description = string.Format( "{0} ({1})", state.RegistrationInstance.Name, state.RegistrationInstance.Account.GlCode );
            paymentInfo.IPAddress = this.RockBlock().GetClientIpAddress();
            paymentInfo.AdditionalParameters = gateway.GetStep1Parameters( this.RockBlock().ResolveRockUrlIncludeRoot( "~/GatewayStep2Return.aspx" ) );

            var result = gateway.ChargeStep1( state.RegistrationTemplate.FinancialGateway, paymentInfo, out errorMessage );
            if ( string.IsNullOrWhiteSpace( errorMessage ) && !string.IsNullOrWhiteSpace( result ) )
            {
                hfStep2Url.Value = result;
            }

            NewCardVisible = string.IsNullOrWhiteSpace( errorMessage );
            rblSavedCC.Visible = false;

            return string.IsNullOrWhiteSpace( errorMessage );
        }

        /// <summary>
        /// Processes the third step of a 3-step charge.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="registration">The registration.</param>
        /// <param name="resultQueryString">The query string result from step 2.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public bool ProcessStep3( RockContext rockContext, RegistrationState state, Registration registration, string batchPrefix, int? sourceTypeValueId, out string errorMessage )
        {
            ThreeStepGatewayComponent gateway = null;

            if ( FinancialGateway != null )
            {
                gateway = FinancialGateway.GetGatewayComponent() as ThreeStepGatewayComponent;
            }

            if ( gateway == null )
            {
                errorMessage = "There was a problem creating the payment gateway information";
                return false;
            }

            if ( Account == null )
            {
                errorMessage = "There was a problem with the account configuration";
                return false;
            }

            var transaction = gateway.ChargeStep3( FinancialGateway, hfStep2ReturnQueryString.Value, out errorMessage );
            return SaveTransaction( gateway, state, registration, batchPrefix, sourceTypeValueId, transaction, null, rockContext );
        }

        public bool SaveTransaction( GatewayComponent gateway, RegistrationState state, Registration registration, string batchPrefix, int? sourceTypeValueId, FinancialTransaction transaction, PaymentInfo paymentInfo, RockContext rockContext )
        {
            if ( transaction != null )
            {
                transaction.AuthorizedPersonAliasId = registration.PersonAliasId;
                transaction.TransactionDateTime = RockDateTime.Now;
                transaction.FinancialGatewayId = FinancialGateway.Id;

                var txnType = DefinedValueCache.Read( new Guid( Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_EVENT_REGISTRATION ) );
                transaction.TransactionTypeValueId = txnType.Id;

                if ( transaction.FinancialPaymentDetail == null )
                {
                    transaction.FinancialPaymentDetail = new FinancialPaymentDetail();
                }

                DefinedValueCache currencyType = null;
                DefinedValueCache creditCardType = null;

                if ( paymentInfo != null )
                {
                    transaction.FinancialPaymentDetail.SetFromPaymentInfo( paymentInfo, gateway, rockContext );
                    currencyType = paymentInfo.CurrencyTypeValue;
                    creditCardType = paymentInfo.CreditCardTypeValue;
                }

                transaction.SourceTypeValueId = sourceTypeValueId;

                transaction.Summary = registration.GetSummary( state.RegistrationInstance );

                var transactionDetail = new FinancialTransactionDetail();
                transactionDetail.Amount = state.RegistrationInfo.PaymentAmount ?? 0.0m;
                transactionDetail.AccountId = Account.Id;
                transactionDetail.EntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Registration ) ).Id;
                transactionDetail.EntityId = registration.Id;
                transaction.TransactionDetails.Add( transactionDetail );

                var batchChanges = new List<string>();

                rockContext.WrapTransaction( () =>
                {
                    var batchService = new FinancialBatchService( rockContext );

                    // determine batch prefix
                    if ( !string.IsNullOrWhiteSpace( state.RegistrationTemplate.BatchNamePrefix ) )
                    {
                        batchPrefix = state.RegistrationTemplate.BatchNamePrefix;
                    }

                    // Get the batch
                    var batch = batchService.Get(
                        batchPrefix,
                        currencyType,
                        creditCardType,
                        transaction.TransactionDateTime.Value,
                        FinancialGateway.GetBatchTimeOffset() );

                    if ( batch.Id == 0 )
                    {
                        batchChanges.Add( "Generated the batch" );
                        History.EvaluateChange( batchChanges, "Batch Name", string.Empty, batch.Name );
                        History.EvaluateChange( batchChanges, "Status", null, batch.Status );
                        History.EvaluateChange( batchChanges, "Start Date/Time", null, batch.BatchStartDateTime );
                        History.EvaluateChange( batchChanges, "End Date/Time", null, batch.BatchEndDateTime );
                    }

                    decimal newControlAmount = batch.ControlAmount + transaction.TotalAmount;
                    History.EvaluateChange( batchChanges, "Control Amount", batch.ControlAmount.FormatAsCurrency(), newControlAmount.FormatAsCurrency() );
                    batch.ControlAmount = newControlAmount;

                    transaction.BatchId = batch.Id;
                    batch.Transactions.Add( transaction );

                    rockContext.SaveChanges();
                } );

                if ( transaction.BatchId.HasValue )
                {
                    Task.Run( () =>
                        HistoryService.SaveChanges(
                            new RockContext(),
                            typeof( FinancialBatch ),
                            Rock.SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                            transaction.BatchId.Value,
                            batchChanges, true, CurrentPersonAliasId )
                    );
                }

                List<string> registrationChanges = new List<string>();
                registrationChanges.Add( string.Format( "Made {0} payment", transaction.TotalAmount.FormatAsCurrency() ) );
                Task.Run( () =>
                    HistoryService.SaveChanges(
                        new RockContext(),
                        typeof( Registration ),
                        Rock.SystemGuid.Category.HISTORY_EVENT_REGISTRATION.AsGuid(),
                        registration.Id,
                        registrationChanges, true, CurrentPersonAliasId )
                );

                TransactionCode = transaction.TransactionCode;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Processes the payment.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="registration">The registration.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public bool ChargePayment( RockContext rockContext, RegistrationState state, Registration registration, string batchPrefix, int? sourceTypeValueId, out string errorMessage )
        {
            GatewayComponent gateway = null;
            if ( state.RegistrationTemplate != null && state.RegistrationTemplate.FinancialGateway != null )
            {
                gateway = state.RegistrationTemplate.FinancialGateway.GetGatewayComponent();
            }

            if ( gateway == null )
            {
                errorMessage = "There was a problem creating the payment gateway information";
                return false;
            }

            if ( !state.RegistrationInstance.AccountId.HasValue || state.RegistrationInstance.Account == null )
            {
                errorMessage = "There was a problem with the account configuration for this " + state.RegistrationTerm.ToLower();
                return false;
            }

            Account = state.RegistrationInstance.Account;

            PaymentInfo paymentInfo = null;
            if ( rblSavedCC.Items.Count > 0 && ( rblSavedCC.SelectedValueAsId() ?? 0 ) > 0 )
            {
                var savedAccount = new FinancialPersonSavedAccountService( rockContext ).Get( rblSavedCC.SelectedValueAsId().Value );
                if ( savedAccount != null )
                {
                    paymentInfo = savedAccount.GetReferencePayment();
                    paymentInfo.Amount = state.RegistrationInfo.PaymentAmount ?? 0.0m;
                }
                else
                {
                    errorMessage = "There was a problem retrieving the saved account";
                    return false;
                }
            }
            else
            {
                paymentInfo = GetCCPaymentInfo( state, gateway );
            }

            paymentInfo.Comment1 = string.Format( "{0} ({1})", state.RegistrationInstance.Name, state.RegistrationInstance.Account.GlCode );

            var transaction = gateway.Charge( state.RegistrationTemplate.FinancialGateway, paymentInfo, out errorMessage );

            return SaveTransaction( gateway, state, registration, batchPrefix, sourceTypeValueId, transaction, paymentInfo, rockContext );
        }

        public void BindSavedAccounts( RegistrationState state, GatewayComponent component, Person currentPerson )
        {
            var currentValue = rblSavedCC.SelectedValue;

            rblSavedCC.Items.Clear();

            if ( currentPerson != null )
            {
                // Get the saved accounts for the currently logged in user
                var savedAccounts = new FinancialPersonSavedAccountService( new RockContext() )
                    .GetByPersonId( currentPerson.Id );

                // Verify component is valid and that it supports using saved accounts for one-time, credit card transactions
                var ccCurrencyType = DefinedValueCache.Read( new Guid( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD ) );
                if ( component != null &&
                    component.SupportsSavedAccount( false ) &&
                    component.SupportsSavedAccount( ccCurrencyType ) )
                {
                    rblSavedCC.DataSource = savedAccounts
                        .Where( a =>
                            a.FinancialGatewayId == state.RegistrationTemplate.FinancialGateway.Id &&
                            a.FinancialPaymentDetail != null &&
                            a.FinancialPaymentDetail.CurrencyTypeValueId == ccCurrencyType.Id )
                        .OrderBy( a => a.Name )
                        .Select( a => new
                        {
                            Id = a.Id,
                            Name = "Use " + a.Name + " (" + a.FinancialPaymentDetail.AccountNumberMasked + ")"
                        } ).ToList();
                    rblSavedCC.DataBind();
                    if ( rblSavedCC.Items.Count > 0 )
                    {
                        rblSavedCC.Items.Add( new ListItem( "Use a different card", "0" ) );
                        rblSavedCC.SetValue( currentValue );
                    }
                }
            }
        }

        private CreditCardPaymentInfo GetCCPaymentInfo( RegistrationState state, GatewayComponent gateway )
        {
            var ccPaymentInfo = new CreditCardPaymentInfo( txtCreditCard.Text, txtCVV.Text, mypExpiration.SelectedDate.Value );

            ccPaymentInfo.NameOnCard = gateway != null && gateway.SplitNameOnCard ? txtCardFirstName.Text : txtCardName.Text;
            ccPaymentInfo.LastNameOnCard = txtCardLastName.Text;

            ccPaymentInfo.BillingStreet1 = acBillingAddress.Street1;
            ccPaymentInfo.BillingStreet2 = acBillingAddress.Street2;
            ccPaymentInfo.BillingCity = acBillingAddress.City;
            ccPaymentInfo.BillingState = acBillingAddress.State;
            ccPaymentInfo.BillingPostalCode = acBillingAddress.PostalCode;
            ccPaymentInfo.BillingCountry = acBillingAddress.Country;

            ccPaymentInfo.Amount = state.RegistrationInfo.PaymentAmount ?? 0.0m;
            ccPaymentInfo.Email = state.RegistrationInfo.ConfirmationEmail;

            ccPaymentInfo.FirstName = state.RegistrationInfo.FirstName;
            ccPaymentInfo.LastName = state.RegistrationInfo.LastName;

            return ccPaymentInfo;
        }

        public List<string> Validate()
        {
            var validationErrors = new List<string>();

            // If not using a saved account validate cc fields
            if ( !Using3StepGateway && ( rblSavedCC.Items.Count == 0 || ( rblSavedCC.SelectedValueAsInt() ?? 0 ) == 0 ) )
            {
                if ( txtCardFirstName.Visible && string.IsNullOrWhiteSpace( txtCardFirstName.Text ) )
                {
                    validationErrors.Add( "First Name on Card is required" );
                }
                if ( txtCardLastName.Visible && string.IsNullOrWhiteSpace( txtCardLastName.Text ) )
                {
                    validationErrors.Add( "Last Name on Card is required" );
                }
                if ( txtCardName.Visible && string.IsNullOrWhiteSpace( txtCardName.Text ) )
                {
                    validationErrors.Add( "Name on Card is required" );
                }
                var rgx = new System.Text.RegularExpressions.Regex( @"[^\d]" );
                string ccNum = rgx.Replace( txtCreditCard.Text, "" );
                if ( string.IsNullOrWhiteSpace( ccNum ) )
                {
                    validationErrors.Add( "Card Number is required" );
                }
                if ( !mypExpiration.SelectedDate.HasValue )
                {
                    validationErrors.Add( "Card Expiration Date is required" );
                }
                if ( string.IsNullOrWhiteSpace( txtCVV.Text ) )
                {
                    validationErrors.Add( "Card Security Code is required" );
                }
                if ( acBillingAddress.Visible && (
                    string.IsNullOrWhiteSpace( acBillingAddress.Street1 ) ||
                    string.IsNullOrWhiteSpace( acBillingAddress.City ) ||
                    string.IsNullOrWhiteSpace( acBillingAddress.State ) ||
                    string.IsNullOrWhiteSpace( acBillingAddress.PostalCode ) ) )
                {
                    validationErrors.Add( "Billing Address is required" );
                }
            }

            return validationErrors;
        }

        private void lbStep2Return_Click( object sender, EventArgs e )
        {
            if ( Step2Returned != null )
            {
                Step2Returned( this, new EventArgs() );
            }
        }
    }
}
