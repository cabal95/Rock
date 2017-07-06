using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Rock;

namespace Rock.Web.UI.Controls
{
    [ToolboxData( "<{0}:CollapsiblePanel runat=server></{0}:CollapsablePanel" )]
    [ParseChildren(true)]
    [PersistChildren(false)]
    public class CollapsiblePanel : Control
    {
        public string Title { get; set; }
        public string TitleIcon { get; set; }
        public string CssClass { get; set; }

        public string HeaderCssClass { get; set; }
        public string BodyCssClass { get; set; }
        public string FooterCssClass { get; set; }

        [PersistenceMode(PersistenceMode.InnerProperty)]
        [TemplateInstance(TemplateInstance.Single)]
        public ITemplate Buttons { get; set; }

        [PersistenceMode( PersistenceMode.InnerProperty )]
        [TemplateInstance( TemplateInstance.Single )]
        public ITemplate Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                if ( !DesignMode && _body != null )
                {
                    CreateContents();
                }
            }
        }
        private ITemplate _body;

        [PersistenceMode( PersistenceMode.InnerProperty )]
        [TemplateInstance( TemplateInstance.Single )]
        public ITemplate Footer { get; set; }

        private HtmlGenericControl _toggleButton;
        private Panel _bodyCollapseControl;
        private HtmlGenericControl _headerTitle;

        /// <summary>
        /// Generate the contents. This is called early in the life of the control before other properties
        /// are available, so we need to store some of these controls in private variables. We can then
        /// set the actual content later in the life cycle.
        /// </summary>
        private void CreateContents()
        {
            if ( DesignMode )
            {
                this.Controls.Clear();
            }

            //
            // Setup the heading div.
            //
            var headerControl = new Panel();
            headerControl.Attributes.Add( "class", string.Format( "panel-heading clearfix clickable {0}", HeaderCssClass ?? string.Empty ) );
            this.Controls.Add( headerControl );

            //
            // Setup the title with optional css icon.
            //
            _headerTitle = new HtmlGenericControl( "h3" );
            _headerTitle.Attributes.Add( "class", "panel-title pull-left" );
            headerControl.Controls.Add( _headerTitle );

            //
            // Setup the buttons span to contain the collapse button.
            //
            var actions = new HtmlGenericControl( "span" );
            actions.Attributes.Add( "class", "pull-right" );
            headerControl.Controls.Add( actions );
            if ( Buttons != null )
            {
                Buttons.InstantiateIn( actions );
            }
            _toggleButton = new HtmlGenericControl( "a" );
            _toggleButton.Attributes.Add( "class", "btn btn-xs btn-link js-collapsiblepanel-chevron" );
            _toggleButton.Attributes.Add( "href", "#" );
            actions.Controls.Add( _toggleButton );

            //
            // Setup the collapsible div to contain the real body.
            //
            _bodyCollapseControl = new Panel();
            this.Controls.Add( _bodyCollapseControl );

            //
            // Setup the real body.
            //
            var bodyControl = new Panel();
            bodyControl.Attributes.Add( "class", string.Format( "panel-body {0}", BodyCssClass ?? string.Empty ) );
            _bodyCollapseControl.Controls.Add( bodyControl );
            if ( this.Body != null )
            {
                Body.InstantiateIn( bodyControl );
            }

            //
            // Setup the footer content if there is any.
            //
            if ( this.Footer != null )
            {
                var footerControl = new Panel();
                footerControl.AddCssClass( "panel-footer" );
                footerControl.AddCssClass( FooterCssClass ?? string.Empty );
                this.Controls.Add( footerControl );
                Footer.InstantiateIn( footerControl );
            }
        }

        public override void RenderControl( HtmlTextWriter writer )
        {
            //
            // Snag the preference key to use and the default state of the panel.
            //
            string prefKey = string.Empty;
            string state = "true";
            if ( this.RockBlock() != null )
            {
                prefKey = string.Format( "block-{0}-collapsible-{1}", this.RockBlock().BlockId, this.ID );
                state = Rock.Model.PersonService.GetUserPreference( this.RockBlock().CurrentPerson, prefKey );
            }

            //
            // Set the title now that we have the properties available.
            //
            if ( !string.IsNullOrWhiteSpace( TitleIcon ) )
            {
                _headerTitle.InnerHtml = string.Format( "<i class=\"{0}\"></i> {1}", TitleIcon, Title.EncodeHtml() );
            }
            else
            {
                _headerTitle.InnerHtml = Title.EncodeHtml();
            }

            //
            // Set the default state of the toggle button and the panel.
            //
            _toggleButton.InnerHtml = string.Format( "<i class=\"fa {0}\"></i>", state == "false" ? "fa-chevron-down" : "fa-chevron-up" );
            _bodyCollapseControl.Attributes.Add( "class", string.Format( "panel-collapse {0}", state == "false" ? "collapse" : string.Empty ) );

            writer.AddAttribute( HtmlTextWriterAttribute.Id, this.ClientID );
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "js-collapsible-panel " + CssClass ?? "panel panel-default" );
            writer.AddAttribute( "data-preference-key", prefKey );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            RenderChildren( writer );
            writer.RenderEndTag();

            //
            // Setup the script for expanding and collapsing the panel.
            //
            var script = string.Format( @"
    (function () {{
        $('.js-collapsible-panel > .panel-heading,.js-collapsible-panel > .panel-heading .js-collapsiblepanel-chevron').click(function (e) {{
            e.stopImmediatePropagation();
            e.preventDefault();

            var $p = $(this).closest('.js-collapsible-panel');
            var $t = $p.children('.panel-collapse');
            var $c = $p.find('> .panel-heading .js-collapsiblepanel-chevron > i');
            var state = '';

            $t.slideToggle();
            $c.toggleClass('fa-chevron-down').toggleClass('fa-chevron-up');
            state = $c.hasClass

            $.post('{0}', {{ key: $p.data('preference-key'), value: $c.hasClass('fa-chevron-up') }});
        }});
    }})();
", this.RockBlock().ResolveClientUrl( "~/api/UserPreference" ) );
            ScriptManager.RegisterStartupScript( this, GetType(), "CollapsablePanelInit", script, true );

        }
    }
}
