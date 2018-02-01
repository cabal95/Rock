<%@ Control Language="C#" AutoEventWireup="true" CodeFile="IncidentReportList.ascx.cs" Inherits="RockWeb.Plugins.com_centralaz.DpsMatch.IncidentReportList" %>

<asp:UpdatePanel ID="upnlSettings" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlWorkflowList" CssClass="panel panel-block" runat="server">

            <div class="panel-heading">
                <h1 class="panel-title"><asp:Literal ID="lHeadingIcon" runat="server" ><i class="fa fa-list"></i></asp:Literal> <asp:Literal ID="lGridTitle" runat="server" Text="Workflows" /></h1>
            </div>
            <div class="panel-body">

	            <Rock:ModalAlert ID="mdGridWarning" runat="server" />

                <div class="grid grid-panel">
            	    <Rock:GridFilter ID="gfWorkflows" runat="server">
                        <asp:PlaceHolder ID="phAttributeFilters" runat="server" />
	                </Rock:GridFilter>
	                <Rock:Grid ID="gWorkflows" runat="server" AllowSorting="true" DisplayType="Full">
	                    <Columns>
                            <Rock:SelectField />
	                        <Rock:RockBoundField DataField="WorkflowId" HeaderText="Id" SortExpression="WorkflowId" />
	                    </Columns>
    	            </Rock:Grid>
                </div>

            </div> 

            

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
