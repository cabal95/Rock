<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MultiSessionAttendanceList.ascx.cs" Inherits="RockWeb.Plugins.com_centralaz.Crm.MultiSessionAttendanceList" %>
<style>
    .grid-select-field .checkbox .label-text::before,
    .grid-select-field .checkbox .label-text::after {
        left:auto;
    }
</style>
<script type="text/javascript">

    function pageLoad() {
        if ($('div.alert.alert-success').length > 0) {
    	        window.setTimeout("fadeAndClear()", 5000);
        }

        // This was needed because setting the Checkbox .Enabled property to false in C# was not doing it.
        $("input:checkbox.disabled").prop("disabled", true);
    }

    function fadeAndClear() {
    	$('div.alert.alert-success').animate({ opacity: 0 }, 2000 );
    }

</script>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal ID="lTitle" runat="server" Visible="true" /></h1>
            </div>
            <div class="panel-body">
                <asp:Panel ID="pnlNotification" runat="server">
                    <Rock:NotificationBox ID="nbConfigError" runat="server" NotificationBoxType="Warning"  />
                </asp:Panel>
                <asp:Panel ID="pnlPersonList" runat="server">
                    <div class="grid grid-panel">
                        <!-- Filter -->
                        <Rock:GridFilter ID="gfSettings" runat="server">
                            <Rock:CampusesPicker ID="cpCampus" runat="server" Label="Campuses" />
                            <Rock:SlidingDateRangePicker ID="sdrpRegistrationDateRange" runat="server" Label="Registration Date Range" />
                            <Rock:RockCheckBoxList ID="cblSessions" runat="server" Label="Only show people who haven't taken these sessions" DataTextField="Name" DataValueField="Id"
                            Help="Choose one or more sessions to only show people who have NOT taken that session. Leave all unchecked to show all matching people." />
                        </Rock:GridFilter>
                        <!-- Data/Grid -->
                        <Rock:Grid ID="gList" runat="server" AllowSorting="true" RowItemText="Attendance">
                            <Columns>
                                <Rock:SelectField></Rock:SelectField>
                                <Rock:RockBoundField DataField="FullName" HeaderText="Name" />
                            </Columns>
                        </Rock:Grid>
                    </div>
                    <br />
            
                    <Rock:NotificationBox ID="nbMessage" runat="server" NotificationBoxType="Success" Title="Success" Visible="false" Text=""></Rock:NotificationBox>

                    <div class="actions margin-t-sm">
                        <asp:LinkButton ID="lbSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="lbSave_Click" />
                    </div>
                </asp:Panel>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
