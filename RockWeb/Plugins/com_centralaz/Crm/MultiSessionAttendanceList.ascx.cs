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
using System.Data;

namespace RockWeb.Plugins.com_centralaz.Crm
{
    /// <summary>
    /// A block for rapidly adding taking 'attribute attendance' (that is, attendance that
    /// is recorded on a set of datetime attributes). The block lists all people
    /// registered for the configured event item who have not yet completed attendance
    /// for all three attributes in the set.
    /// </summary>
    [DisplayName( "Multi-Session Attendance List" )]
    [Category( "com_centralaz > CRM" )]
    [Description( "Block used to record attendance (via a set of DateTime attributes) for a multi-sessioned event.  The block lists all people registered for the configured event item who have not yet completed attendance for all attributes in the set." )]
    [LinkedPage( "Person Profile Page", "Page used for viewing a person's profile. If set a view profile button will show for each group member.", false, "", "", 2, "PersonProfilePage" )]
    [EventItemField( "Event Item", "The event item used to populate the list of people" )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Session Attributes", "", true, true, "" )]
    [WorkflowTypeField( "Workflow Types", "The workflow types to be fired whenever someone completes all the sessions.", true )]
    public partial class MultiSessionAttendanceList : RockBlock
    {
        #region Properties

        /// <summary>
        /// Gets or sets the attributecache.
        /// </summary>
        /// <value>
        /// The AttributeCache objects for the Date attributes representing when the person attended the sessions.
        /// </value>
        public List<AttributeCache> AttributeCache { get; set; }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            AttributeCache = ViewState["AttributeCache"] as List<AttributeCache>;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gList.GridRebind += gList_GridRebind;

            gList.PersonIdField = "Id";
            gList.DataKeyNames = new string[] { "Id" };
            gList.RowDataBound += gList_RowDataBound;
            gList.Actions.ShowCommunicate = true;
            gList.Actions.ShowBulkUpdate = false;
            gList.Actions.ShowMergePerson = false;

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

            nbMessage.Visible = false;

            if ( !Page.IsPostBack )
            {
                if ( CheckForAttributes() )
                {
                    ShowDetail();
                }
                else
                {
                    ShowConfigurationError();
                }
            }
        }

        /// <summary>
        /// Shows the configuration error message.
        /// </summary>
        private void ShowConfigurationError()
        {
            pnlPersonList.Visible = false;
            pnlNotification.Visible = true;
            nbConfigError.Text = "Please configure the block settings by selecting at least one attribute.";
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["AttributeCache"] = AttributeCache;

            return base.SaveViewState();
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
            AttributeCache = null;

            if ( CheckForAttributes() )
            {
                ShowDetail();
            }
            else
            {
                ShowConfigurationError();
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the grid list.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gList_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the RowDataBound event of the grid list.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gList_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            // If a person has already attended a session, this code prevents the admin from unchecking the checkbox for it.
            PersonAttendanceDataRow personAttendanceDataRow = e.Row.DataItem as PersonAttendanceDataRow;
            if ( personAttendanceDataRow != null )
            {
                foreach ( var attribute in AttributeCache )
                {
                    CheckBox checkBox_Attended = e.Row.FindControl( string.Format( "checkBox_Attended_{0}", attribute.Id ) ) as CheckBox;
                    if ( checkBox_Attended != null && personAttendanceDataRow.AttendedSessions.Any( a => a == attribute.Id ) )
                    {
                        checkBox_Attended.Enabled = false;
                        checkBox_Attended.AddCssClass( "disabled" );
                        checkBox_Attended.Attributes.Add( "disabled", "true" );
                    }
                }
            }
        }

        /// <summary>
        /// Gives the settings display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void gfSettings_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case FilterSetting.DATE_RANGE:
                    {
                        e.Value = SlidingDateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }

                case FilterSetting.NOT_ATTENDED_SESSIONS:
                    {
                        List<string> allChecked = cblSessions.Items.Cast<ListItem>()
                          .Where( i => i.Selected )
                          .Select( i => "<br>"+i.Text )
                          .ToList();

                        e.Value = String.Join( ", ", allChecked );
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

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfSettings_ApplyFilterClick( object sender, EventArgs e )
        {
            gfSettings.SaveUserPreference( FilterSetting.DATE_RANGE, sdrpRegistrationDateRange.DelimitedValues );
            gfSettings.SaveUserPreference( FilterSetting.CAMPUS, cpCampus.SelectedValues.AsDelimited( ";" ) );

            // WE ARE PURPOSELY NOT saving what the user checked since it is a temporary selection they are making.
            gfSettings.SaveUserPreference( FilterSetting.NOT_ATTENDED_SESSIONS, "0" );

            BindGrid();
        }

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSave_Click( object sender, EventArgs e )
        {
            var date = RockDateTime.Now.Date;
            int changedPeople = 0;

            using ( var rockContext = new RockContext() )
            {
                PersonService personService = new PersonService( rockContext );
                AttributeService attributeService = new AttributeService( rockContext );
                EventItemService eventItemService = new EventItemService( rockContext );
                AttributeValueService attributeValueService = new AttributeValueService( rockContext );

                foreach ( GridViewRow row in gList.Rows )
                {
                    var checkCount = 0;
                    bool setDateValue = false;

                    // For each row, grab the valid person object associated with it
                    int personId = int.Parse( gList.DataKeys[row.RowIndex].Value.ToString() );
                    Person person = personService.Get( personId );
                    if ( person != null )
                    {
                        person.LoadAttributes();

                        // For each checked checkbox in the row, get the attribute key from the header text. Grab the person attribute
                        // value for that key. If empty, save the current date to that attribute value.
                        foreach ( var fieldCell in row.Cells.OfType<DataControlFieldCell>() )
                        {
                            CheckBoxEditableField checkBoxTemplateField = fieldCell.ContainingField as CheckBoxEditableField;
                            if ( checkBoxTemplateField != null )
                            {
                                CheckBox checkBox = fieldCell.Controls[0] as CheckBox;

                                if ( checkBox.Checked )
                                {
                                    checkCount++;

                                    string attributeKey = GetAttributeKey( checkBoxTemplateField.HeaderText );
                                    var attributeDate = person.GetAttributeValue( attributeKey );
                                    if ( attributeDate == null || attributeDate.AsDateTime() == null )
                                    {
                                        setDateValue = true;
                                        person.SetAttributeValue( attributeKey, date.ToString() );
                                        person.SaveAttributeValue( attributeKey, rockContext );
                                    }
                                }
                            }
                        }

                        // Fire any configured workflows if the person has completed all three sessions
                        if ( checkCount == AttributeCache.Count )
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

                        // increment the person change count
                        if ( setDateValue )
                        {
                            changedPeople++;
                        }
                    }
                }
            }

            nbMessage.Visible = true;
            nbMessage.Title = "Attendance Saved";
            nbMessage.Text = string.Format( "<p>updated attendance for {0} people</p>", changedPeople );

            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetail()
        {
            pnlPersonList.Visible = true;
            pnlNotification.Visible = false;

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
                    AddColumns();
                    BindGrid();
                }
                else
                {
                    pnlPersonList.Visible = false;
                    pnlNotification.Visible = true;
                    nbConfigError.Text = "Please set the event item block setting to a valid item.";
                }
            }
        }

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            var rockContext = new RockContext();

            cpCampus.DataSource = CampusCache.All();
            cpCampus.DataBind();
            cpCampus.SetValues( gfSettings.GetUserPreference( FilterSetting.CAMPUS ).SplitDelimitedValues().AsIntegerList() );

            sdrpRegistrationDateRange.DelimitedValues = gfSettings.GetUserPreference( FilterSetting.DATE_RANGE );

            // Bind the attributes that represent the sessions for the class.
            cblSessions.DataSource = AttributeCache;
            cblSessions.DataBind();
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

                // Generate Attribute Values Qry
                var attributeIds = AttributeCache.Select( a => a.Id ).ToList();
                var qryAttributeValues = attributeValueService.Queryable().Where( av =>
                          attributeIds.Contains( av.AttributeId ) &&
                          av.EntityId != null );

                // Grab Registrants
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

                    // Build nice neat PersonAttendanceDataRow qry
                    var qry = qryRegistrations
                        .SelectMany( r => r.Registrants )
                        .OrderBy( rr => rr.Person.LastName ).ThenBy( rr => rr.Person.FirstName )
                        .Select( rr => new PersonAttendanceDataRow
                        {
                            Id = rr.PersonId.Value,
                            FullName = rr.Person.FullName,
                            RegisteredDateTime = rr.Registration.CreatedDateTime.Value,
                            AttendedSessions = qryAttributeValues.Where( av => av.EntityId == rr.PersonId && av.ValueAsDateTime.HasValue ).Select( av => av.Id ).ToList()
                        } );

                    // Filter by Sessions (if the user is filtering by them)
                    List<int> allChecked = cblSessions.Items.Cast<ListItem>()
                              .Where( i => i.Selected )
                              .Select( i => int.Parse( i.Value ) )
                              .ToList();
                    if ( allChecked.Count() > 0 )
                    {
                        qry = qry.Where( p => allChecked.Any( a => p.AttendedSessions.Contains( a ) ) );
                    }

                    // Filter out registrants who completed all sessions
                    qry = qry.Where( p => p.AttendedSessions.Count < AttributeCache.Count );
                    var x = qry.ToList();

                    gList.EntityTypeId = EntityTypeCache.Get<Rock.Model.Person>().Id;
                    gList.SetLinqDataSource( qry );
                    gList.DataBind();
                }
            }
        }

        /// <summary>
        /// Launches the workflows.
        /// </summary>
        /// <param name="workflowService">The workflow service.</param>
        /// <param name="workflowTypeGuid">The workflow type unique identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="entity">The entity.</param>
        private void LaunchWorkflows( WorkflowService workflowService, Guid workflowTypeGuid, string name, object entity )
        {
            var workflowType = WorkflowTypeCache.Get( workflowTypeGuid );
            if ( workflowType != null )
            {
                var workflow = Workflow.Activate( workflowType, name );
                List<string> workflowErrors;
                workflowService.Process( workflow, entity, out workflowErrors );
            }
        }

        /// <summary>
        /// Gets the attribute key given an attribute name. NOTE: Specifically for the three AttributeCache properties
        /// the block uses
        /// </summary>
        /// <param name="columnText">The column text.</param>
        /// <returns></returns>
        private string GetAttributeKey( string columnText )
        {
            var attributeKey = string.Empty;

            foreach( var attribute in AttributeCache )
            {
                if ( columnText == attribute.Name )
                {
                    return attribute.Key;
                }
            }

            return attributeKey;
        }

        /// <summary>
        /// Returns a bool based on whether all three session date attributes are valid
        /// </summary>
        /// <returns></returns>
        private bool CheckForAttributes()
        {
            return AttributeCache != null && AttributeCache.Count > 0;
        }

        /// <summary>
        /// Adds the checkbox and person profile columns
        /// </summary>
        private void AddColumns()
        {
            var checkBoxEditableFields = gList.Columns.OfType<CheckBoxEditableField>().ToList();
            foreach ( var field in checkBoxEditableFields )
            {
                gList.Columns.Remove( field );
            }

            foreach ( var attribute in AttributeCache )
            {
                gList.Columns.Add( new CheckBoxEditableField { HeaderText = attribute.Name } );
            }

            // Add Link to Profile Page Column
            if ( !string.IsNullOrEmpty( GetAttributeValue( "PersonProfilePage" ) ) )
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
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Constant like string-key-settings that are tied to user saved filter preferences.
        /// </summary>
        public static class FilterSetting
        {
            public const string CAMPUS = "Campus";
            public const string DATE_RANGE = "Registration Date Range";
            public const string NOT_ATTENDED_SESSIONS = "Showing people who haven't completed session(s)";
        }

        /// <summary>
        /// Helper class that binds all the attribute data into a nice neat object
        /// </summary>
        protected class PersonAttendanceDataRow
        {
            public int Id { get; set; }

            public string FullName { get; set; }

            public DateTime RegisteredDateTime { get; set; }

            public List<int> AttendedSessions { get; set; }
        }
        #endregion
    }
}