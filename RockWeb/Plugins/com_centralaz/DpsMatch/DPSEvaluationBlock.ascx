<%@ Control Language="C#" AutoEventWireup="true" CodeFile="DPSEvaluationBlock.ascx.cs" Inherits="RockWeb.Plugins.com_centralaz.DpsMatch.DPSEvaluationBlock" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading">
            </div>
            <div class="panel-body">
                <asp:HiddenField ID="hfSelectedColumn" runat="server" />
                <div class="row">
                    <div class="col-md-6">
                        <Rock:FileUploader ID="fuOffenderFile" runat="server" Label="Upload Offender List" OnFileUploaded="fuOffenderFile_FileUploaded" />
                        <Rock:NotificationBox ID="nbUploadMessage" runat="server" Visible="false" />
                    </div>
                </div>
                <br />
                <div class="row">
                    <div class="col-md-12">

                        <div class="grid grid-panel">
                            <Rock:GridFilter ID="gfSettings" runat="server">
                                <Rock:RangeSlider ID="rsMatchThreshold" runat="server" Label="Match Threshold" Help="The minimum threshold percentage for matches to be shown." MinValue="60" MaxValue="90" />
                            </Rock:GridFilter>
                            <Rock:Grid ID="gValues" runat="server" AllowSorting="false" EmptyDataText="No Results" />
                        </div>
                    </div>
                </div>
                
                <br />
                <br />
                <div class="row">
                    <div class="col-md-12">
                        <Rock:NotificationBox ID="nbComplete" runat="server" Visible="false" />

                    </div>
                </div>
                <div class="actions pull-right">
                    <asp:LinkButton ID="lbReset" runat="server" Text="Reset Matches" CssClass="btn btn-xs" OnClick="lbReset_Click" />
                    <asp:LinkButton ID="lbNext" runat="server" Text="Next" CssClass="btn btn-primary" OnClick="lbNext_Click" />
                </div>


            </div>

        </div>
        <script>
            $(document).ready(function ()
            {
                $('[data-toggle="tooltip"]').tooltip();
            });
        </script>

    </ContentTemplate>
</asp:UpdatePanel>
