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
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_centralaz.Widgets
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Multi-Session Attendance List" )]
    [Category( "com_centralaz > Widgets" )]
    [Description( "Block used to record attendance for a multi-sessioned event" )]
    [LinkedPage( "Person Profile Page", "Page used for viewing a person's profile. If set a view profile button will show for each group member.", false, "", "", 2, "PersonProfilePage" )]
    [EventItemField( "Event Item", "The event item used to populate the list of people" )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Attended First Session Attribute", "", false, false, "" )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Attended Second Session Attribute", "", false, false, "" )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Attended Third Session Attribute", "", false, false, "" )]
    [WorkflowTypeField( "Workflow Types", "The workflow types to be fired whenever someone completes all three sessions.", true )]
    public partial class MultiSessionAttendanceList : RockBlock
    {

        #region Properties

        public AttributeCache FirstAttributeCache { get; set; }
        public AttributeCache SecondAttributeCache { get; set; }
        public AttributeCache ThirdAttributeCache { get; set; }

        #endregion

        #region Base Control Methods

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            FirstAttributeCache = ViewState["FirstAttributeCache"] as AttributeCache;
            SecondAttributeCache = ViewState["SecondAttributeCache"] as AttributeCache;
            ThirdAttributeCache = ViewState["ThirdAttributeCache"] as AttributeCache;

            // Add Link to Profile Page Column
            if ( !string.IsNullOrEmpty( GetAttributeValue( "PersonProfilePage" ) ) )
            {
                AddPersonProfileLinkColumn();
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gList.GridRebind += gList_GridRebind;
            gList.DataKeyNames = new string[] { "Id" };
            gList.RowDataBound += gList_RowDataBound;
            gList.Actions.ShowCommunicate = true;

            gfSettings.ApplyFilterClick += gfSettings_ApplyFilterClick;
            gfSettings.DisplayFilterValue += gfSettings_DisplayFilterValue;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                if ( CheckForAttributes() )
                {
                    ShowDetail();
                }
                else
                {
                    pnlPersonList.Visible = false;
                    pnlNotification.Visible = true;
                    nbConfigError.Text = "Please set up all session attributes.";
                }
            }
        }

        protected override object SaveViewState()
        {
            ViewState["FirstAttributeCache"] = FirstAttributeCache;
            ViewState["SecondAttributeCache"] = SecondAttributeCache;
            ViewState["ThirdAttributeCache"] = ThirdAttributeCache;

            return base.SaveViewState();
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {

        }

        /// <summary>
        /// Handles the GridRebind event of the gPledges control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gList_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        protected void gList_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            Person person = e.Row.DataItem as Person;
            if ( person != null )
            {
                person.LoadAttributes();

                foreach ( var cell in e.Row.Cells.OfType<DataControlFieldCell>() )
                {
                    if ( cell.ContainingField is CheckBoxEditableField )
                    {
                        CheckBoxEditableField checkBoxEditableField = cell.ContainingField as CheckBoxEditableField;
                        if ( checkBoxEditableField != null )
                        {
                            string attributeKey = GetAttributeKey( checkBoxEditableField.HeaderText );

                            CheckBox checkBox = cell.Controls[0] as CheckBox;
                            if ( checkBox != null )
                            {
                                var attendedDate = person.GetAttributeValue( attributeKey );
                                if ( attendedDate.AsDateTime() != null )
                                {
                                    checkBox.Checked = true;
                                    checkBox.Enabled = false;
                                }
                                else
                                {
                                    checkBox.Checked = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected void gfSettings_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case FilterSetting.DATE_RANGE:
                    {
                        e.Value = SlidingDateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }
                case FilterSetting.CAMPUS:
                    {
                        var resolvedValues = new List<string>();

                        foreach ( string value in e.Value.Split( ';' ) )
                        {
                            var item = cpCampus.Items.FindByValue( value );
                            if ( item != null )
                            {
                                resolvedValues.Add( item.Text );
                            }
                        }

                        e.Value = resolvedValues.AsDelimited( ", " );
                        break;
                    }
                default:
                    {
                        e.Value = string.Empty;
                        break;
                    }
            }
        }

        protected void gfSettings_ApplyFilterClick( object sender, EventArgs e )
        {
            gfSettings.SaveUserPreference( FilterSetting.DATE_RANGE, sdrpRegistrationDateRange.DelimitedValues );
            gfSettings.SaveUserPreference( FilterSetting.CAMPUS, cpCampus.SelectedValues.AsDelimited( ";" ) );
            BindGrid();
        }

        protected void lbSave_Click( object sender, EventArgs e )
        {
            var date = RockDateTime.Now.Date;
            using ( var rockContext = new RockContext() )
            {
                PersonService personService = new PersonService( rockContext );
                AttributeService attributeService = new AttributeService( rockContext );
                EventItemService eventItemService = new EventItemService( rockContext );
                AttributeValueService attributeValueService = new AttributeValueService( rockContext );

                foreach ( GridViewRow row in gList.Rows )
                {
                    var checkCount = 0;
                    Person person = row.DataItem as Person;
                    person.LoadAttributes();

                    foreach ( var fieldCell in row.Cells.OfType<DataControlFieldCell>() )
                    {
                        CheckBoxEditableField checkBoxTemplateField = fieldCell.ContainingField as CheckBoxEditableField;
                        if ( checkBoxTemplateField != null )
                        {
                            string attributeKey = GetAttributeKey( checkBoxTemplateField.HeaderText );

                            CheckBox checkBox = fieldCell.Controls[0] as CheckBox;

                            if ( checkBox.Checked )
                            {
                                checkCount++;
                                var attributeDate = person.GetAttributeValue( attributeKey );
                                if ( attributeDate == null || attributeDate.AsDateTime() == null )
                                {
                                    person.SetAttributeValue( attributeKey, date.ToString() );
                                }
                            }
                        }
                    }

                    person.SaveAttributeValues();

                    // Fire any configured workflows if the person has completed all three sessions
                    if ( checkCount == 3 )
                    {
                        var workflowService = new WorkflowService( rockContext );

                        var workflows = GetAttributeValue( "WorkflowTypes" ).SplitDelimitedValues().AsGuidList();

                        if ( workflows.Any() )
                        {
                            foreach ( var workflowType in workflows )
                            {
                                LaunchWorkflows( workflowService, workflowType, person.FullName, person );
                            }
                        }
                    }
                }
            }
        }


        #endregion

        #region Methods

        private void ShowDetail()
        {
            using ( var rockContext = new RockContext() )
            {
                EventItemService eventItemService = new EventItemService( rockContext );
                var eventItem = eventItemService.Get( GetAttributeValue( "EventItem" ).AsGuid() );
                if ( eventItem != null )
                {
                    lTitle.Text = String.Format( "{0} Attendance", eventItem.Name );
                    pnlPersonList.Visible = true;
                    pnlNotification.Visible = false;
                    BindFilter();
                    BindGrid();
                }
                else
                {
                    pnlPersonList.Visible = false;
                    pnlNotification.Visible = true;
                    nbConfigError.Text = "Please set the Event Item block setting to a valid Event Item.";
                }
            }
        }

        private void BindFilter()
        {
            var rockContext = new RockContext();

            cpCampus.DataSource = CampusCache.All();
            cpCampus.DataBind();
            cpCampus.SetValues( gfSettings.GetUserPreference( FilterSetting.CAMPUS ).SplitDelimitedValues().AsIntegerList() );

            sdrpRegistrationDateRange.DelimitedValues = gfSettings.GetUserPreference( FilterSetting.DATE_RANGE );

            // Add Link to Profile Page Column
            if ( !string.IsNullOrEmpty( GetAttributeValue( "PersonProfilePage" ) ) )
            {
                AddPersonProfileLinkColumn();
            }
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            using ( var rockContext = new RockContext() )
            {
                PersonService personService = new PersonService( rockContext );
                EventItemService eventItemService = new EventItemService( rockContext );
                AttributeValueService attributeValueService = new AttributeValueService( rockContext );

                var attributeIds = new List<int>();
                attributeIds.Add( FirstAttributeCache.Id );
                attributeIds.Add( SecondAttributeCache.Id );
                attributeIds.Add( ThirdAttributeCache.Id );

                gList.ColumnsOfType<CheckBoxEditableField>().Where( a => a.ID == "cbfA" ).FirstOrDefault().HeaderText = FirstAttributeCache.Name;
                gList.ColumnsOfType<CheckBoxEditableField>().Where( a => a.ID == "cbfB" ).FirstOrDefault().HeaderText = SecondAttributeCache.Name;
                gList.ColumnsOfType<CheckBoxEditableField>().Where( a => a.ID == "cbfC" ).FirstOrDefault().HeaderText = ThirdAttributeCache.Name;

                var eventItem = eventItemService.Get( GetAttributeValue( "EventItem" ).AsGuid() );
                if ( eventItem != null )
                {
                    var qryEventItemOccurrences = eventItem.EventItemOccurrences.AsQueryable();

                    // Filter by Campus
                    List<int> campusIds = cpCampus.SelectedValuesAsInt;
                    if ( campusIds.Count > 0 )
                    {
                        qryEventItemOccurrences = qryEventItemOccurrences
                            .Where( r =>
                                r.Campus != null &&
                                campusIds.Contains( r.CampusId.Value ) );
                    }

                    var qryRegistrations = qryEventItemOccurrences
                        .SelectMany( eio => eio.Linkages )
                        .Select( l => l.RegistrationInstance )
                        .SelectMany( ri => ri.Registrations );

                    // Filter by Date Range
                    var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( sdrpRegistrationDateRange.DelimitedValues );

                    if ( dateRange.Start.HasValue )
                    {
                        qryRegistrations = qryRegistrations.Where( r =>
                            r.CreatedDateTime.HasValue &&
                            r.CreatedDateTime.Value >= dateRange.Start.Value );
                    }

                    if ( dateRange.End.HasValue )
                    {
                        qryRegistrations = qryRegistrations.Where( r =>
                            r.CreatedDateTime.HasValue &&
                            r.CreatedDateTime.Value < dateRange.End.Value );
                    }

                    var qryPersons = qryRegistrations
                        .SelectMany( r => r.Registrants )
                        .Select( rr => rr.Person );

                    // Filter out registrants who completed all sessions

                    var personIds = qryPersons.Select( p => p.Id ).ToList();

                    var completedPersonIds = attributeValueService.Queryable().Where( av =>
                             attributeIds.Contains( av.AttributeId ) &&
                             av.EntityId != null &&
                             personIds.Contains( av.EntityId.Value )
                        )
                        .GroupBy( av => av.EntityId.Value )
                        .Where( a => a.Count() >= 3 )
                        .Select( a => a.Key )
                        .ToList();

                    qryPersons = qryPersons.Where( p => !completedPersonIds.Contains( p.Id ) );


                    gList.SetLinqDataSource( qryPersons );
                    gList.DataBind();
                }
            }
        }

        private void LaunchWorkflows( WorkflowService workflowService, Guid workflowTypeGuid, string name, object entity )
        {
            var workflowType = WorkflowTypeCache.Read( workflowTypeGuid );
            if ( workflowType != null )
            {
                var workflow = Workflow.Activate( workflowType, name );
                List<string> workflowErrors;
                workflowService.Process( workflow, entity, out workflowErrors );
            }
        }

        private string GetAttributeKey( string columnText )
        {
            var attributeKey = string.Empty;

            if ( columnText == FirstAttributeCache.Name )
            {
                attributeKey = FirstAttributeCache.Key;
            }

            if ( columnText == SecondAttributeCache.Name )
            {
                attributeKey = SecondAttributeCache.Key;
            }

            if ( columnText == ThirdAttributeCache.Name )
            {
                attributeKey = ThirdAttributeCache.Key;
            }

            return attributeKey;
        }

        private bool CheckForAttributes()
        {
            bool allAttributesPresent = false;

            if ( FirstAttributeCache == null )
            {
                FirstAttributeCache = AttributeCache.Read( GetAttributeValue( "AttendedFirstSessionAttribute" ).AsGuid() );
            }

            if ( SecondAttributeCache == null )
            {
                SecondAttributeCache = AttributeCache.Read( GetAttributeValue( "AttendedSecondSessionAttribute" ).AsGuid() );
            }

            if ( ThirdAttributeCache == null )
            {
                ThirdAttributeCache = AttributeCache.Read( GetAttributeValue( "AttendedThirdSessionAttribute" ).AsGuid() );
            }

            if ( FirstAttributeCache != null && SecondAttributeCache != null && ThirdAttributeCache != null )
            {
                allAttributesPresent = true;
            }

            return allAttributesPresent;
        }

        private void AddPersonProfileLinkColumn()
        {
            HyperLinkField hlPersonProfileLink = new HyperLinkField();
            hlPersonProfileLink.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
            hlPersonProfileLink.HeaderStyle.CssClass = "grid-columncommand";
            hlPersonProfileLink.ItemStyle.CssClass = "grid-columncommand";
            hlPersonProfileLink.DataNavigateUrlFields = new string[1] { "Id" };
            hlPersonProfileLink.DataNavigateUrlFormatString = LinkedPageUrl( "PersonProfilePage", new Dictionary<string, string> { { "PersonId", "###" } } ).Replace( "###", "{0}" );
            hlPersonProfileLink.DataTextFormatString = "<div class='btn btn-default btn-sm'><i class='fa fa-user'></i></div>";
            hlPersonProfileLink.DataTextField = "Id";
            gList.Columns.Add( hlPersonProfileLink );
        }

        #endregion

        #region Filter's User Preference Setting Keys
        /// <summary>
        /// Constant like string-key-settings that are tied to user saved filter preferences.
        /// </summary>
        public static class FilterSetting
        {
            public const string CAMPUS = "Campus";
            public const string DATE_RANGE = "DateRange";
        }
        #endregion
    }
}