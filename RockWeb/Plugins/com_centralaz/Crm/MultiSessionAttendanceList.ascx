<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MultiSessionAttendanceList.ascx.cs" Inherits="RockWeb.Plugins.com_centralaz.Crm.MultiSessionAttendanceList" %>

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
                        <Rock:GridFilter ID="gfSettings" runat="server">
                            <Rock:CampusesPicker ID="cpCampus" runat="server" Label="Campuses" />
                            <Rock:SlidingDateRangePicker ID="sdrpRegistrationDateRange" runat="server" Label="Registration Date Range" />
                        </Rock:GridFilter>
                        <Rock:Grid ID="gList" runat="server" AllowSorting="true">
                            <Columns>
                                <Rock:SelectField></Rock:SelectField>
                                <Rock:RockBoundField DataField="FullName" HeaderText="Name" />
                            </Columns>
                        </Rock:Grid>
                    </div>
                    <br />
                    <div class="actions pull-right">
                        <asp:LinkButton ID="lbSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="lbSave_Click" />
                    </div>
                </asp:Panel>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
