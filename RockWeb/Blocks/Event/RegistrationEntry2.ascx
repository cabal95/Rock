<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RegistrationEntry2.ascx.cs" Inherits="RockWeb.Blocks.Event.RegistrationEntry2" %>

<style>
    iframe {
        width: 100%;
        height: 800px;
        overflow: hidden;
        border-style: none;
    }
</style>
<asp:UpdatePanel ID="upnlContent" runat="server">
<ContentTemplate>

    <asp:HiddenField ID="hfTriggerScroll" runat="server" Value="" />
    <asp:HiddenField ID="hfAllowNavigate" runat="server" Value="" />

    <asp:ValidationSummary ID="vsSummary" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />
    <Rock:NotificationBox ID="nbPaymentValidation" runat="server" NotificationBoxType="Danger" Visible="false" />

    <Rock:NotificationBox ID="nbMain" runat="server" Visible="false"></Rock:NotificationBox>
    <Rock:NotificationBox ID="nbWaitingList" runat="server" Visible="false" NotificationBoxType="Warning" />

    <asp:Panel ID="pnlHowMany" runat="server" Visible="false" CssClass="registrationentry-intro">
        <asp:PlaceHolder ID="phHowMany" runat="server" />

        <div class="actions">
            <Rock:BootstrapButton ID="lbHowManyNext" runat="server" AccessKey="n" ToolTip="Alt+n" Text="Next" DataLoadingText="Next" CssClass="btn btn-primary pull-right" CausesValidation="true" OnClick="lbHowManyNext_Click" />
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlRegistrant" runat="server" Visible="true" CssClass="registrationentry-registrant">
        <asp:PlaceHolder ID="phRegistrant" runat="server" />

        <div class="actions">
            <asp:LinkButton ID="lbRegistrantPrev" runat="server" AccessKey="p" ToolTip="Alt+p" Text="Previous" CssClass="btn btn-default" CausesValidation="false" OnClick="lbRegistrantPrev_Click"  />
            <Rock:BootstrapButton ID="lbRegistrantNext" runat="server" AccessKey="n" ToolTip="Alt+n" Text="Next" DataLoadingText="Next" CssClass="btn btn-primary pull-right" CausesValidation="true" OnClick="lbRegistrantNext_Click" />
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlSummaryAndPayment" runat="server" Visible="false" CssClass="registrationentry-summary">
        
        <h1><asp:Literal ID="lSummaryAndPaymentTitle" runat="server" /></h1>

        <asp:PlaceHolder ID="phSummaryControls" runat="server" />
        
        <asp:Panel ID="pnlCostAndFees" runat="server">

            <h4>Payment Summary</h4>

            <Rock:NotificationBox ID="nbDiscountCode" runat="server" Visible="false" NotificationBoxType="Warning"></Rock:NotificationBox>

            <asp:PlaceHolder ID="phCostAndFees" runat="server" />

        </asp:Panel>

        <asp:Panel ID="pnlPaymentInfo" runat="server" CssClass="well">
            <asp:PlaceHolder ID="phPayment" runat="server" />
        </asp:Panel>

        <div class="actions">
            <asp:LinkButton ID="lbSummaryPrev" runat="server" AccessKey="p" ToolTip="Alt+p" Text="Previous" CssClass="btn btn-default" CausesValidation="false" OnClick="lbSummaryPrev_Click" />
            <Rock:BootstrapButton ID="lbSummaryNext" runat="server" AccessKey="n" ToolTip="Alt+n" Text="Finish" DataLoadingText="Next" CssClass="btn btn-primary pull-right" CausesValidation="true" OnClick="lbSummaryNext_Click" />
            <asp:LinkButton ID="lbPaymentPrev" runat="server" AccessKey="p" ToolTip="Alt+p" Text="Previous" CssClass="btn btn-default" CausesValidation="false" OnClick="lbPaymentPrev_Click" />
            <asp:Label ID="aStep2Submit" runat="server" ClientIDMode="Static" CssClass="btn btn-primary pull-right" Text="Finish" />
        </div>


    </asp:Panel>

    <asp:Panel ID="pnlSuccess" runat="server" Visible="false" >
        
        <h1><asp:Literal ID="lSuccessTitle" runat="server" /></h1>

        <asp:PlaceHolder ID="phSuccessProgress" runat="server" />

        <asp:Literal ID="lSuccess" runat="server" />
        <asp:Literal ID="lSuccessDebug" runat="server" Visible="false" />

        <asp:Panel ID="pnlSaveAccount" runat="server" Visible="false">
            <div class="well">
                <legend>Make Payments Even Easier</legend>
                <fieldset>
                    <Rock:RockCheckBox ID="cbSaveAccount" runat="server" Text="Save account information for future transactions" CssClass="toggle-input" />
                    <div id="divSaveAccount" runat="server" class="toggle-content">
                        <Rock:RockTextBox ID="txtSaveAccount" runat="server" Label="Name for this account" CssClass="input-large"></Rock:RockTextBox>

                        <asp:PlaceHolder ID="phCreateLogin" runat="server" Visible="false">

                            <div class="control-group">
                                <div class="controls">
                                    <div class="alert alert-info">
                                        <b>Note:</b> For security purposes you will need to login to use your saved account information.  To create
	    			                a login account please provide a user name and password below. You will be sent an email with the account 
	    			                information above as a reminder.
                                    </div>
                                </div>
                            </div>

                            <Rock:RockTextBox ID="txtUserName" runat="server" Label="Username" CssClass="input-medium" />
                            <Rock:RockTextBox ID="txtPassword" runat="server" Label="Password" CssClass="input-medium" TextMode="Password" />
                            <Rock:RockTextBox ID="txtPasswordConfirm" runat="server" Label="Confirm Password" CssClass="input-medium" TextMode="Password" />

                        </asp:PlaceHolder>

                        <Rock:NotificationBox ID="nbSaveAccount" runat="server" Visible="false" NotificationBoxType="Danger"></Rock:NotificationBox>

                        <div id="divSaveActions" runat="server" class="actions">
                            <asp:LinkButton ID="lbSaveAccount" runat="server" Text="Save Account" CssClass="btn btn-primary" OnClick="lbSaveAccount_Click" />
                        </div>
                    </div>
                </fieldset>                    
            </div>
        </asp:Panel>

    </asp:Panel>

</ContentTemplate>
</asp:UpdatePanel>
