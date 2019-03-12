<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ReservationsByDate.ascx.cs" Inherits="RockWeb.Plugins.com_centralaz.Utility.ReservationsByDate" %>

<asp:UpdatePanel ID="upPanel" runat="server">
    <ContentTemplate>

        <Rock:ModalAlert ID="mdGridWarning" runat="server" />
        <div class="row">
            <div class="pull-right">
                <div class="form-horizontal label-md">
                    <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" AutoPostBack="true" OnTextChanged="cpCampus_TextChanged" />
                </div>
            </div>
        </div>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">Reservations</h1>
                <div class="pull-right">
                    <div class="form-horizontal label-md">

                        <Rock:DatePicker ID="dpDate" runat="server" AutoPostBack="true" OnTextChanged="dpDate_TextChanged" />
                    </div>
                </div>
            </div>
            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gReservations" runat="server" RowItemText="Reservation" OnRowSelected="gReservations_Edit" TooltipField="Description">
                        <Columns>
                            <Rock:RockBoundField DataField="Id" HeaderText="Id" Visible="false" />
                            <Rock:RockBoundField DataField="ReservationName" HeaderText="Name" />
                            <Rock:RockBoundField DataField="EventDateTimeDescription" HeaderText="Event Time" />
                            <Rock:RockBoundField DataField="ReservationDateTimeDescription" HeaderText="Reservation Time" />
                            <Rock:RockBoundField DataField="Locations" HeaderText="Locations" />
                            <Rock:RockBoundField DataField="EventContact" HeaderText="Event Contact" />
                            <Rock:RockBoundField DataField="Resources" HeaderText="Resources" />
                            <Rock:RockBoundField DataField="Notes" HeaderText="Notes" />
                            <Rock:RockBoundField DataField="Setup" HeaderText="Setup" HtmlEncode="false" />
                            <Rock:RockBoundField DataField="ApprovalState" HeaderText="Approval State" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
