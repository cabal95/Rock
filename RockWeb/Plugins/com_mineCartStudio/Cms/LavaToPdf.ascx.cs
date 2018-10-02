// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI.Controls;
using Rock.Security;
using EvoPdf;
using System.Web;

namespace RockWeb.Plugins.com_mineCartStudio.Cms
{
    /// <summary>
    /// Block that syncs selected people to an exchange server.
    /// </summary>
    [DisplayName( "Lava To PDF" )]
    [Category( "Mine Cart Studio > CMS" )]
    [Description( "Block that renders a Lava template as PDF." )]

    [LavaCommandsField( "Enabled Lava Commands", "The Lava commands that should be enabled for this HTML block.", false, order: 0 )]
    [DecimalField("Document Width", "The width of the document in inches.", true, 8.5, order: 1)]
    [DecimalField( "Document Height", "The height of the document in inches.", true, 11, order: 2 )]
    [DecimalField( "Page Top Margin", "The height of top margin of the page in inches.", false, .5, order: 3 )]
    [DecimalField( "Page Bottom Margin", "The height of bottom margin of the page in inches.", false, .5, order: 4 )]
    [TextField("Document Name", "Lava template for the document name.", true, "document.pdf", order: 5)]
    [CodeEditorField( "Document Template", "The Lava to use for the document.", CodeEditorMode.Html, CodeEditorTheme.Rock, 500, true, @"<html>
    <head>
        <style>
            body {
                font-family: Arial, Helvetica, sans-serif;
                font-size: 14px;
                margin: .5in;
            }
        </style>
    </head>
    <body>
        <h1 style=""font-size: 52px; text-align: center; margin-top: 220px;"">Hello World!</h1>
    </body>
</html>", order: 6 )]
    [BooleanField( "Open Inline", "Determines if the document should be opened in the browser or downloaded as a file.", true, "", 7 )]
    [IntegerField( "Document Render Delay", "The amount of time in seconds to wait before generating the PDF. This allows time for scripts and external resources to load.", false, 0, order: 8)]
    [BooleanField( "Auto Create Bookmarks", "Determines if PDF bookmarks should be generated from h1..h6.", false, "", 9 )]
    [CustomRadioListField( "Page Mode", "Determines what should be displayed when the file opens.", "0^Normal,1^Show Bookmarks,2^Show Thumbnails", true, "0", "", 10 )]
    [BooleanField( "Table Header Repeat Enabled", "Determines table headers should be repeated across page breaks.", true, "", 11 )]
    [TextField( "Document Background Color", "The hex color code (e.g. #ffffff) of the document.", false, "", "", 12 )]
    [BooleanField( "Avoid Image Breaks", "Keeps images from being split across page breaks.", true, "", 13 )]
    [BooleanField( "Images Scaling Enabled", "This option enables the converter to scale the images used in HTML down to their display size in HTML before rendering them in PDF. This can lead to smaller memory consumption during conversion and to a smaller PDF document size but the images quality in PDF can be lower.", true, "", 14 )]
    [BooleanField( "Enable Debug", "Show lava merge fields.", false, "", 15 )]

    [BooleanField("Enable Header", "Displays a document header based on the template provided.", false, "Header", 0)]
    [CodeEditorField( "Header Template", "The Lava to use for the header (should be a full HTML document).", CodeEditorMode.Html, CodeEditorTheme.Rock, 250, false, @"", order: 2, Category = "Header" )]
    [DecimalField( "Header Height", "The header height in inches.", true, 1, "Header", 2)]
    [DecimalField( "Header Spacing", "The amount of space between the header and the document body in inches.", true, .25, "Header", 3 )]
    [TextField( "Header Background Color", "The hex color code color (e.g. #ffffff) for the header background.", false, "", "Header", 4)]

    [BooleanField( "Enable Footer", "Displays a document footer based on the template provided.", false, "Footer", 5 )]
    [CodeEditorField( "Footer Template", "The Lava to use for the footer (should be a full HTML document). Use &amp;p; for the current page and &amp;P; for the total number of pages.", CodeEditorMode.Html, CodeEditorTheme.Rock, 250, false, @"", order: 6, Category = "Footer" )]
    [DecimalField( "Footer Height", "The header height in inches.", true, 1, "Footer", 7 )]
    [DecimalField( "Footer Spacing", "The amount of space between the header and the document body in inches.", true, .25, "Footer", 8 )]
    [TextField( "Footer Background Color", "The hex color code color (e.g. #ffffff) for the header background.", false, "", "Footer", 9 )]

    public partial class LavaToPdf : Rock.Web.UI.RockBlock
    {

        #region Fields

        private readonly string _licensekey = "G5WGlIGElIWGgZSCmoSUh4WahYaajY2NjZSE"; // this key is licensed to be only be used by mine cart plugins
        private readonly string BOOKS_CACHE_KEY = "RockBooks";

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:Init" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;

            Render();
        }

        /// <summary>
        /// Raises the <see cref="E:Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            Render();
        }

        #endregion


        #region Methods

        private void Render()
        {
            lMessages.Text = string.Empty;

            try {

                var enableDebug = GetAttributeValue( "EnableDebug" ).AsBoolean();
                var documentTemplate = GetAttributeValue( "DocumentTemplate" );
                var openInline = GetAttributeValue( "OpenInline" ).AsBoolean();
                var documentWidth = GetAttributeValue( "DocumentWidth" ).AsDecimal() * 72; // 72 points in an inch
                var documentHeight = GetAttributeValue( "DocumentHeight" ).AsDecimal() * 72; // 72 points in an inch
                var pageTopMargin = GetAttributeValue( "PageTopMargin" ).AsDecimal() * 72; // 72 points in an inch
                var pageBottomMargin = GetAttributeValue( "PageBottomMargin" ).AsDecimal() * 72; // 72 points in an inch
                var documentName = GetAttributeValue( "DocumentName" );
                var documentRenderDelay = GetAttributeValue( "DocumentRenderDelay" ).AsInteger();
                var autoGenerateBookmarks = GetAttributeValue( "AutoCreateBookmarks" ).AsBoolean();
                var pageMode = GetAttributeValue( "PageMode" ).AsInteger();
                var tableHeaderRepeatEnabled = GetAttributeValue( "TableHeaderRepeatEnabled" ).AsBoolean();
                var avoidImageBreaks = GetAttributeValue( "AvoidImageBreaks" ).AsBoolean();

                var documentBackgroundColor = GetAttributeValue( "DocumentBackgroundColor" );
                var imagesScalingEnabled = GetAttributeValue( "ImagesScalingEnabled" ).AsBoolean();

                var enabledLavaCommands = GetAttributeValue( "EnabledLavaCommands" );

                var headerEnabled = GetAttributeValue( "EnableHeader" ).AsBoolean();
                var headerHeight = GetAttributeValue( "HeaderHeight" ).AsDecimal() * 72;
                var headerSpacing = GetAttributeValue( "HeaderSpacing" ).AsDecimal() * 72;
                var headerTemplate = GetAttributeValue( "HeaderTemplate" );
                var headerBackgroundColor = GetAttributeValue( "HeaderBackgroundColor" );

                var footerEnabled = GetAttributeValue( "EnableFooter" ).AsBoolean();
                var footerHeight = GetAttributeValue( "FooterHeight" ).AsDecimal() * 72;
                var footerSpacing = GetAttributeValue( "FooterSpacing" ).AsDecimal() * 72;
                var footerTemplate = GetAttributeValue( "FooterTemplate" );
                var footerBackgroundColor = GetAttributeValue( "FooterBackgroundColor" );

                var baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd( '/' ) + "/"; ;

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

                // if in debug render the document to the screen
                if ( enableDebug )
                {
                    ifDocumentPreview.Visible = true;
                    lMessages.Text = string.Empty;
                    ifDocumentPreview.Attributes["srcdoc"] = documentTemplate.ResolveMergeFields( mergeFields, enabledLavaCommands );
                    lDebug.Text = mergeFields.lavaDebugInfo();
                    return;
                }

                // if the user has administrative access show them a link to the pdf to view it (so you can still access the block settings)
                if ( IsUserAuthorized( Authorization.ADMINISTRATE ) && this.PageParameter( "ViewDocument" ).AsBoolean() == false )
                {
                    ifDocumentPreview.Visible = false;
                    lDebug.Text = string.Empty;

                    var uriBuilder = new UriBuilder( Request.Url );
                    var query = HttpUtility.ParseQueryString( uriBuilder.Query );
                    query["ViewDocument"] = "true";
                    uriBuilder.Query = query.ToString();

                    lMessages.Text = string.Format( @"<div class=""alert alert-warning"">If you did not have administrative access <a href=""{0}""> you would be shown this PDF.</a></div>", uriBuilder.ToString() );
                    return;
                }

                // process the pdf

                // prepare the pdf document
                HtmlToPdfConverter htmlToPdfConverter = new HtmlToPdfConverter();
                htmlToPdfConverter.LicenseKey = _licensekey;
                htmlToPdfConverter.PdfDocumentOptions.PdfPageSize = new PdfPageSize( (float)documentWidth, (float)documentHeight );

                if (documentWidth > documentHeight )
                {
                    htmlToPdfConverter.PdfDocumentOptions.PdfPageOrientation = PdfPageOrientation.Landscape;
                }
                else
                {
                    htmlToPdfConverter.PdfDocumentOptions.PdfPageOrientation = PdfPageOrientation.Portrait;
                }

                htmlToPdfConverter.ConversionDelay = documentRenderDelay;
                htmlToPdfConverter.PdfBookmarkOptions.AutoBookmarksEnabled = autoGenerateBookmarks;
                htmlToPdfConverter.PdfViewerPreferences.PageMode = (ViewerPageMode)pageMode;
                htmlToPdfConverter.PdfDocumentOptions.TableHeaderRepeatEnabled = tableHeaderRepeatEnabled;
                htmlToPdfConverter.PdfDocumentOptions.AvoidImageBreak = avoidImageBreaks;
                htmlToPdfConverter.PdfDocumentOptions.BottomMargin = (float)pageBottomMargin;
                htmlToPdfConverter.PdfDocumentOptions.TopMargin = (float)pageTopMargin;
                

                if ( !string.IsNullOrWhiteSpace( documentBackgroundColor ) )
                {
                    htmlToPdfConverter.PdfDocumentOptions.BackColor = System.Drawing.ColorTranslator.FromHtml( documentBackgroundColor );
                }

                // Set images scaling before rendering to PDF
                // This option enables the converter to scale the images used in HTML down to their display size in HTML before rendering them in PDF. 
                // This can lead to smaller memory consumption during conversion and to a smaller PDF document size but the images quality in PDF can be lower
                htmlToPdfConverter.PdfDocumentOptions.ImagesScalingEnabled = imagesScalingEnabled;

                htmlToPdfConverter.PdfBookmarkOptions.AutoBookmarksEnabled = true;

                // create output buffer
                var content = documentTemplate.ResolveMergeFields( mergeFields, enabledLavaCommands );

                // header info
                if ( headerEnabled )
                {
                    htmlToPdfConverter.PdfDocumentOptions.ShowHeader = true;
                    htmlToPdfConverter.PdfDocumentOptions.TopSpacing = (float)headerSpacing;
                    htmlToPdfConverter.PdfHeaderOptions.HeaderHeight = (float)headerHeight;
                    if ( !string.IsNullOrWhiteSpace( headerBackgroundColor ) )
                    {
                        htmlToPdfConverter.PdfHeaderOptions.HeaderBackColor = System.Drawing.ColorTranslator.FromHtml( headerBackgroundColor );
                    }
                    HtmlToPdfVariableElement header = new HtmlToPdfVariableElement( headerTemplate.ResolveMergeFields( mergeFields, enabledLavaCommands ), baseUrl );
                    header.FitHeight = true;
                    htmlToPdfConverter.PdfHeaderOptions.AddElement( header );
                }

                // footer info
                if ( footerEnabled )
                {
                    htmlToPdfConverter.PdfDocumentOptions.ShowFooter = true;
                    htmlToPdfConverter.PdfDocumentOptions.BottomSpacing = (float)footerSpacing;
                    htmlToPdfConverter.PdfFooterOptions.FooterHeight = (float)footerHeight;
                    if ( !string.IsNullOrWhiteSpace( footerBackgroundColor ) )
                    {
                        htmlToPdfConverter.PdfFooterOptions.FooterBackColor = System.Drawing.ColorTranslator.FromHtml( footerBackgroundColor );
                    }
                    HtmlToPdfVariableElement footer = new HtmlToPdfVariableElement( footerTemplate.ResolveMergeFields( mergeFields, enabledLavaCommands ), baseUrl );
                    footer.FitHeight = true;
                    htmlToPdfConverter.PdfFooterOptions.AddElement( footer );
                }

                byte[] outPdfBuffer = htmlToPdfConverter.ConvertHtml( content, baseUrl );

                Response.AddHeader( "Content-Type", "application/pdf" );

                // instruct the browser to open the pdf file as an attachment or inline
                Response.AddHeader( "Content-Disposition", String.Format( "{0}; filename={1}; size={2}",
                    openInline ? "inline" : "attachment", documentName.ResolveMergeFields( mergeFields, enabledLavaCommands ), outPdfBuffer.Length.ToString() ) );

                // write the pdf document buffer to http response
                Response.BinaryWrite( outPdfBuffer );

                // end the http response and stop the current page processing
                Response.End();

                // clear buffer
                outPdfBuffer = null;
            }
            catch(Exception ex )
            {
                lMessages.Text = string.Format( @"<div class=""alert alert-danger"">An error occurred processing the document: {0}.", ex.Message );
            }
        }

        #endregion
        
    }
}