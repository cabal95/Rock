using Rock.Financial;

using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Rock.Web.UI.Controls
{
    /// <summary>
    /// Provides a simple user interface for collecting credit card information.
    /// </summary>
    /// <seealso cref="System.Web.UI.WebControls.CompositeControl" />
    /// <seealso cref="System.Web.UI.INamingContainer" />
    /// <seealso cref="Rock.Financial.IHostedGatewayPaymentControlTokenEvent" />
    public class CreditCard : CompositeControl, INamingContainer, IHostedGatewayPaymentControlTokenEvent
    {
        #region Events

        /// <summary>
        /// Occurs when a payment token is received from the hosted gateway
        /// </summary>
        public event EventHandler TokenReceived;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CreditCard"/> is required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if required; otherwise, <c>false</c>.
        /// </value>
        public virtual bool Required
        {
            get
            {
                EnsureChildControls();

                return tbNameOnCard.Required;
            }
            set
            {
                EnsureChildControls();
                tbNameOnCard.Required = value;
                tbCardNumber.Required = value;
                mypExpiration.Required = value;
                tbSecurityCode.Required = value;
            }
        }

        /// <summary>
        /// Gets or sets the name on card.
        /// </summary>
        /// <value>
        /// The name on card.
        /// </value>
        public string NameOnCard
        {
            get
            {
                EnsureChildControls();
                return tbNameOnCard.Text;
            }
            set
            {
                EnsureChildControls();
                tbNameOnCard.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the card number.
        /// </summary>
        /// <value>
        /// The card number.
        /// </value>
        public string CardNumber
        {
            get
            {
                EnsureChildControls();
                return tbCardNumber.Text;
            }
            set
            {
                EnsureChildControls();
                tbCardNumber.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the expiration.
        /// </summary>
        /// <value>
        /// The expiration.
        /// </value>
        public DateTime? Expiration
        {
            get
            {
                EnsureChildControls();
                return mypExpiration.SelectedDate;
            }
            set
            {
                EnsureChildControls();
                mypExpiration.SelectedDate = value;
            }
        }

        /// <summary>
        /// Gets or sets the security code.
        /// </summary>
        /// <value>
        /// The security code.
        /// </value>
        public string SecurityCode
        {
            get
            {
                EnsureChildControls();
                return tbSecurityCode.Text;
            }
            set
            {
                EnsureChildControls();
                tbSecurityCode.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the name on card should be prompted for.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the name on card should be prompted for; otherwise, <c>false</c>.
        /// </value>
        public virtual bool PromptForNameOnCard
        {
            get
            {
                EnsureChildControls();
                return tbNameOnCard.Visible;
            }
            set
            {
                EnsureChildControls();
                tbNameOnCard.Visible = value;
            }
        }

        /// <summary>
        /// Returns true if the credit card information is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsValid
        {
            get
            {
                if ( CardNumber.IsNullOrWhiteSpace() || SecurityCode.IsNullOrWhiteSpace() || !Expiration.HasValue )
                {
                    return false;
                }

                if ( PromptForNameOnCard && NameOnCard.IsNullOrWhiteSpace() )
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsEmpty
        {
            get
            {
                EnsureChildControls();

                return NameOnCard.IsNullOrWhiteSpace() && CardNumber.IsNullOrWhiteSpace() && !Expiration.HasValue && SecurityCode.IsNullOrWhiteSpace();
            }
        }

        /// <summary>
        /// Gets or sets the token, this is often used to store the authorization token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        public string Token
        {
            get
            {
                EnsureChildControls();
                return hfToken.Value;
            }
            set
            {
                EnsureChildControls();
                hfToken.Value = value;
            }
        }

        #endregion

        #region Child Controls

        protected RockTextBox tbNameOnCard;

        protected RockTextBox tbCardNumber;

        protected MonthYearPicker mypExpiration;

        protected RockTextBox tbSecurityCode;

        protected HiddenField hfToken;

        #endregion

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( this.Page.IsPostBack )
            {
                string[] eventArgs = ( this.Page.Request.Form["__EVENTARGUMENT"] ?? string.Empty ).Split( new[] { "=" }, StringSplitOptions.RemoveEmptyEntries );

                if ( eventArgs.Length >= 1 )
                {
                    if ( eventArgs[0] == this.ID )
                    {
                        TokenReceived?.Invoke( this, new EventArgs() );
                    }
                }
            }
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            tbNameOnCard = new RockTextBox
            {
                ID = "tbNameOnCard",
                Label = "Name on Card"
            };
            this.Controls.Add( tbNameOnCard );

            tbCardNumber = new RockTextBox
            {
                ID = "tbCardNumber",
                Label = "Card Number",
                MaxLength = 19,
                AppendText = "<i class='fa fa-credit-card' style='font-size: 1.5em;'></i>"
            };
            this.Controls.Add( tbCardNumber );

            mypExpiration = new MonthYearPicker
            {
                ID = "mypExpiration",
                Label = "Expiration Date",
                MinimumYear = RockDateTime.Now.Year
            };
            this.Controls.Add( mypExpiration );

            tbSecurityCode = new RockTextBox
            {
                ID = "tbSecurityCode",
                Label = "Card Security Code",
                MaxLength = 4,
                CssClass = "input-width-xs"
            };
            this.Controls.Add( tbSecurityCode );

            hfToken = new HiddenField
            {
                ID = "hfToken"
            };
            this.Controls.Add( hfToken );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.PreRender" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );

            var script = $@"(function() {{
    var visa_regex = new RegExp('^4[0-9]{{0,15}}$');
    var mastercard_regex = new RegExp('^5$|^5[1-5][0-9]{{0,14}}$');
    var amex_regex = new RegExp('^3$|^3[47][0-9]{{0,13}}$');
    var discover_regex = new RegExp('^6$|^6[05]$|^601[1]?$|^65[0-9][0-9]?$|^6(?:011|5[0-9]{{2}})[0-9]{{0,12}}$');

    function updateCardIcon() {{
        var $icon = $(this).closest('.input-group').find('.fa');
        var value = $(this).val();
        var css = 'fa ';

        if (value.match(visa_regex)) {{
            css += 'fa-cc-visa';
        }} else if (value.match(mastercard_regex)) {{
            css += 'fa-cc-mastercard';
        }} else if (value.match(amex_regex)) {{
            css += 'fa-cc-amex';
        }} else if (value.match(discover_regex)) {{
            css += 'fa-cc-discover';
        }} else {{
            css += 'fa-credit-card';
        }}
        
        $icon.attr('class', css);
    }}

    $('#{tbCardNumber.ClientID}').on('keyup', updateCardIcon);
    updateCardIcon.call($('#{tbCardNumber.ClientID}'));
}})();
";

            ScriptManager.RegisterStartupScript( this, GetType(), $"cc-detection-{ClientID}", script, true );
        }

        /// <summary>
        /// Writes the <see cref="T:System.Web.UI.WebControls.CompositeControl" /> content to the specified <see cref="T:System.Web.UI.HtmlTextWriter" /> object, for display on the client.
        /// </summary>
        /// <param name="writer">An <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        protected override void Render( HtmlTextWriter writer )
        {
            if ( TokenReceived != null )
            {
                var updatePanel = this.ParentUpdatePanel();
                string postbackControlId;
                if ( updatePanel != null )
                {
                    postbackControlId = updatePanel.ClientID;
                }
                else
                {
                    postbackControlId = this.ID;
                }

                this.Attributes["data-postback-script"] = $"javascript:__doPostBack('{postbackControlId}', '{this.ID}')";
            }

            base.Render( writer );
        }

        /// <summary>
        /// Renders the contents of the control to the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        protected override void RenderContents( HtmlTextWriter writer )
        {
            tbNameOnCard.RenderControl( writer );

            writer.AddAttribute( HtmlTextWriterAttribute.Class, "row" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            {
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-lg-5 col-md-12" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    tbCardNumber.RenderControl( writer );
                }
                writer.RenderEndTag();

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-lg-4 col-md-6 col-sm-7" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    mypExpiration.RenderControl( writer );
                }
                writer.RenderEndTag();

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-lg-3 col-md-6 col-sm-5" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    tbSecurityCode.RenderControl( writer );
                }
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public virtual void Clear()
        {
            NameOnCard = string.Empty;
            CardNumber = string.Empty;
            Expiration = null;
            SecurityCode = string.Empty;
        }

        #endregion
    }
}
