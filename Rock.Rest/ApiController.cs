﻿// <copyright>
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
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
#if !IS_NET_CORE
using System.Web.Http;
#endif
using System.Web.Http.OData;

#if IS_NET_CORE
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FromUriAttribute = Microsoft.AspNetCore.Mvc.FromQueryAttribute;
using FromBodyAttribute = Microsoft.AspNetCore.Mvc.FromBodyAttribute;
#endif

using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Security;

namespace Rock.Rest
{
    /// <summary>
    /// ApiController for Rock REST Entity endpoints
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ApiController<T> : ApiControllerBase
        where T : Rock.Data.Entity<T>, new()
    {
        /// <summary>
        /// Gets or sets the service.
        /// </summary>
        /// <value>
        /// The service.
        /// </value>
        protected Service<T> Service
        {
            get { return _service; }
            set { _service = value; }
        }

        private Service<T> _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiController{T}"/> class.
        /// </summary>
        /// <param name="service">The service.</param>
        public ApiController( Service<T> service )
        {
            Service = service;

            // Turn off proxy creation by default so that when querying objects through rest, EF does not automatically navigate all child properties for requested objects
            // When adding, updating, or deleting objects, the proxy should be enabled to properly track relationships that should or shouldn't be updated
            SetProxyCreation( false );
        }

        /// <summary>
        /// Queryable GET endpoint
        /// </summary>
        /// <returns></returns>
        [Authenticate, Secured]
        [EnableQuery]
        public virtual IQueryable<T> Get()
        {
            var result = Service.Queryable().AsNoTracking();
            return result;
        }

        /// <summary>
        /// GET endpoint to get a single record 
        /// </summary>
        /// <param name="id">The Id of the record</param>
        /// <returns></returns>
        /// <exception cref="HttpResponseException"></exception>
        [Authenticate, Secured]
        [ActionName( "GetById" )]
        public virtual T GetById( int id )
        {
            T model;
            if ( !Service.TryGet( id, out model ) )
            {
                throw new HttpResponseException( HttpStatusCode.NotFound );
            }

            return model;
        }

        /// <summary>
        /// GET endpoint to get a single record 
        /// </summary>
        /// <param name="key">The Id of the record</param>
        /// <returns></returns>
        /// <exception cref="HttpResponseException"></exception>
        [Authenticate, Secured]
        [EnableQuery]
#if IS_NET_CORE
        // EFTODO: Duplicate route.. not sure how this one is supposed to work? Probably need an action constraint.
        [HttpOptions]
#endif
        public virtual T Get( [FromODataUri] int key )
        {
            T model;
            if ( !Service.TryGet( key, out model ) )
            {
                throw new HttpResponseException( HttpStatusCode.NotFound );
            }

            return model;
        }

        /// <summary>
        /// Gets records that have a particular attribute value.
        /// Example: api/People/GetByAttributeValue?attributeKey=FirstVisit&amp;value=2012-12-15
        /// </summary>
        /// <param name="attributeId">The attribute identifier.</param>
        /// <param name="attributeKey">The attribute key.</param>
        /// <param name="value">The value.</param>
        /// <param name="caseSensitive">if set to <c>true</c> [case sensitive].</param>
        /// <returns></returns>
        /// <exception cref="HttpResponseException">
        /// </exception>
        [Authenticate, Secured]
        [ActionName( "GetByAttributeValue" )]
        [EnableQuery]
#if IS_NET_CORE
        public virtual IActionResult GetByAttributeValue( [FromUri]int? attributeId = null, [FromUri]string attributeKey = null, [FromUri]string value = null, [FromUri]bool caseSensitive = false )
#else
        public virtual IQueryable<T> GetByAttributeValue( [FromUri]int? attributeId = null, [FromUri]string attributeKey = null, [FromUri]string value = null, [FromUri]bool caseSensitive = false )
#endif
        {
            // Value is always required
            if ( value.IsNullOrWhiteSpace() )
            {
#if IS_NET_CORE
                return BadRequest( "The value param is required" );
#else
                var errorResponse = ControllerContext.Request.CreateErrorResponse( HttpStatusCode.BadRequest, "The value param is required" );
                throw new HttpResponseException( errorResponse );
#endif
            }

            // Either key or id is required, but not both
            var queryByKey = !attributeKey.IsNullOrWhiteSpace();
            var queryById = attributeId.HasValue;

            if ( queryByKey == queryById )
            {
#if IS_NET_CORE
                return BadRequest( "Either attributeKey or attributeId must be specified, but not both" );
#else
                var errorResponse = ControllerContext.Request.CreateErrorResponse( HttpStatusCode.BadRequest, "Either attributeKey or attributeId must be specified, but not both" );
                throw new HttpResponseException( errorResponse );
#endif
            }

            // Query for the models that have the value for the attribute
            var rockContext = Service.Context as RockContext;
            var query = Service.Queryable().AsNoTracking();
            var valueComparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if ( queryById )
            {
                query = query.WhereAttributeValue( rockContext,
                    a => a.AttributeId == attributeId && a.Value.Equals( value, valueComparison ) );
            }
            else
            {
                query = query.WhereAttributeValue( rockContext,
                    a => a.Attribute.Key.Equals( attributeKey, StringComparison.OrdinalIgnoreCase ) && a.Value.Equals( value, valueComparison ) );
            }

#if IS_NET_CORE
            return Ok( query );
#else
            return query;
#endif
        }

        /// <summary>
        /// POST endpoint. Use this to INSERT a new record
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="HttpResponseException"></exception>
        [Authenticate, Secured]
#if IS_NET_CORE
        public virtual IActionResult Post( [FromBody]T value )
#else
        public virtual HttpResponseMessage Post( [FromBody]T value )
#endif
        {
            if ( value == null )
            {
#if IS_NET_CORE
                return BadRequest( ModelState );
#else
                throw new HttpResponseException( HttpStatusCode.BadRequest );
#endif
            }

            SetProxyCreation( true );

            CheckCanEdit( value );

            Service.Add( value );

            if ( !value.IsValid )
            {
#if IS_NET_CORE
                return BadRequest( string.Join( ",", value.ValidationResults.Select( r => r.ErrorMessage ).ToArray() ) );
#else
                return ControllerContext.Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    string.Join( ",", value.ValidationResults.Select( r => r.ErrorMessage ).ToArray() ) );
#endif
            }

#if IS_NET_CORE
            EnsureHttpContextHasCurrentPerson();
#else
            if ( !System.Web.HttpContext.Current.Items.Contains( "CurrentPerson" ) )
            {
                System.Web.HttpContext.Current.Items.Add( "CurrentPerson", GetPerson() );
            }
#endif

            Service.Context.SaveChanges();

#if IS_NET_CORE
            var response = StatusCode( ( int ) HttpStatusCode.Created, value.Id );
#else
            var response = ControllerContext.Request.CreateResponse( HttpStatusCode.Created, value.Id );
#endif

            //// TODO set response.Headers.Location as per REST POST convention
            // response.Headers.Location = new Uri( Request.RequestUri, "/api/pages/" + page.Id.ToString() );
            return response;
        }

        /// <summary>
        /// PUT endpoint. Use this to UPDATE a record
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="HttpResponseException">
        /// </exception>
        [Authenticate, Secured]
#if IS_NET_CORE
        public virtual IActionResult Put( int id, [FromBody]T value )
#else
        public virtual void Put( int id, [FromBody]T value )
#endif
        {
            if ( value == null )
            {
#if IS_NET_CORE
                return BadRequest( ModelState );
#else
                throw new HttpResponseException( HttpStatusCode.BadRequest );
#endif
            }

            SetProxyCreation( true );

            T targetModel;
            if ( !Service.TryGet( id, out targetModel ) )
            {
                throw new HttpResponseException( HttpStatusCode.NotFound );
            }

            CheckCanEdit( targetModel );

            Service.SetValues( value, targetModel );

            if ( targetModel.IsValid )
            {
#if IS_NET_CORE
                EnsureHttpContextHasCurrentPerson();
#else
                if ( !System.Web.HttpContext.Current.Items.Contains( "CurrentPerson" ) )
                {
                    System.Web.HttpContext.Current.Items.Add( "CurrentPerson", GetPerson() );
                }
#endif

                Service.Context.SaveChanges();

#if IS_NET_CORE
                return Ok();
#endif
            }
            else
            {
#if IS_NET_CORE
                return BadRequest( string.Join( ",", targetModel.ValidationResults.Select( r => r.ErrorMessage ).ToArray() ) );
#else
                var response = ControllerContext.Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    string.Join( ",", targetModel.ValidationResults.Select( r => r.ErrorMessage ).ToArray() ) );
                throw new HttpResponseException( response );
#endif
            }
        }

        /// <summary>
        /// PATCH endpoint. Use this to update a subset of the properties of the record
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="values">The values.</param>
        /// <exception cref="HttpResponseException">
        /// </exception>
        [Authenticate, Secured]
#if IS_NET_CORE
        public virtual IActionResult Patch( int id, [FromBody]Dictionary<string, object> values )
#else
        public virtual void Patch( int id, [FromBody]Dictionary<string, object> values )
#endif
        {
            // Check that something was sent in the body
            if ( values == null || !values.Keys.Any() )
            {
#if IS_NET_CORE
                return BadRequest( "No values were sent in the body" );
#else
                var response = ControllerContext.Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest, "No values were sent in the body" );
                throw new HttpResponseException( response );
#endif
            }
            else if ( values.ContainsKey( "Id" ) )
            {
#if IS_NET_CORE
                return BadRequest( "Cannot set Id" );
#else
                var response = ControllerContext.Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest, "Cannot set Id" );
                throw new HttpResponseException( response );
#endif
            }

            SetProxyCreation( true );

            T targetModel;
            if ( !Service.TryGet( id, out targetModel ) )
            {
                throw new HttpResponseException( HttpStatusCode.NotFound );
            }

            CheckCanEdit( targetModel );
            var type = targetModel.GetType();
            var properties = type.GetProperties().ToList();

            // Same functionality as Service.SetValues but for a subset of properties
            foreach ( var key in values.Keys )
            {
                if ( properties.Any( p => p.Name.Equals( key ) ) )
                {
                    var property = type.GetProperty( key, BindingFlags.Public | BindingFlags.Instance );
                    var propertyType = Nullable.GetUnderlyingType( property.PropertyType ) ?? property.PropertyType;
                    var currentValue = values[key];

                    if ( property != null )
                    {
                        if ( property.GetValue( targetModel ) == currentValue )
                        {
                            continue;
                        }
                        else if ( property.CanWrite )
                        {
                            if ( currentValue == null )
                            {
                                // No need to parse anything
                                property.SetValue( targetModel, null );
                            }
                            else if ( propertyType == typeof( int ) || propertyType == typeof( int? ) || propertyType.IsEnum )
                            {
                                // By default, objects that hold integer values, hold int64, so coerce to int32
                                try
                                {
                                    var int32 = Convert.ToInt32( currentValue );
                                    property.SetValue( targetModel, int32 );
                                }
                                catch ( OverflowException )
                                {
#if IS_NET_CORE
                                    return BadRequest( $"Cannot cast {key} to int32" );
#else
                                    var response = ControllerContext.Request.CreateErrorResponse(
                                        HttpStatusCode.BadRequest,
                                        string.Format( "Cannot cast {0} to int32", key ) );
                                    throw new HttpResponseException( response );
#endif
                                }
                            }
                            else
                            {
                                var castedValue = Convert.ChangeType( currentValue, propertyType );
                                property.SetValue( targetModel, castedValue );
                            }
                        }
                        else
                        {
#if IS_NET_CORE
                            return BadRequest( $"Cannot write {key}" );
#else
                            var response = ControllerContext.Request.CreateErrorResponse(
                                HttpStatusCode.BadRequest,
                                string.Format( "Cannot write {0}", key ) );
                            throw new HttpResponseException( response );
#endif
                        }
                    }
                    else
                    {
#if IS_NET_CORE
                        return BadRequest( $"Cannot find property {key}" );
#else
                        // This shouldn't happen because we are checking that the property exists.
                        // Just to make sure reflection doesn't fail
                        var response = ControllerContext.Request.CreateErrorResponse(
                            HttpStatusCode.BadRequest,
                            string.Format( "Cannot find property {0}", key ) );
                        throw new HttpResponseException( response );
#endif
                    }
                }
                else
                {
#if IS_NET_CORE
                    return BadRequest( $"{type.BaseType.Name} does not have attribute {key}" );
#else
                    var response = ControllerContext.Request.CreateErrorResponse(
                        HttpStatusCode.BadRequest,
                        string.Format( "{0} does not have attribute {1}", type.BaseType.Name, key ) );
                    throw new HttpResponseException( response );
#endif
                }
            }

            // Verify model is valid before saving
            if ( targetModel.IsValid )
            {
#if IS_NET_CORE
                EnsureHttpContextHasCurrentPerson();
#else
                if ( !System.Web.HttpContext.Current.Items.Contains( "CurrentPerson" ) )
                {
                    System.Web.HttpContext.Current.Items.Add( "CurrentPerson", GetPerson() );
                }
#endif

                Service.Context.SaveChanges();

#if IS_NET_CORE
                return Ok();
#endif
            }
            else
            {
#if IS_NET_CORE
                return BadRequest( string.Join( ",", targetModel.ValidationResults.Select( r => r.ErrorMessage ).ToArray() ) );
#else
                var response = ControllerContext.Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    string.Join( ",", targetModel.ValidationResults.Select( r => r.ErrorMessage ).ToArray() ) );
                throw new HttpResponseException( response );
#endif
            }
        }

        /// <summary>
        /// DELETE endpoint. To delete the record
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <exception cref="HttpResponseException"></exception>
        [Authenticate, Secured]
        public virtual void Delete( int id )
        {
            SetProxyCreation( true );

            T model;
            if ( !Service.TryGet( id, out model ) )
            {
                throw new HttpResponseException( HttpStatusCode.NotFound );
            }

            CheckCanEdit( model );

            Service.Delete( model );
            Service.Context.SaveChanges();
        }

        /// <summary>
        /// Gets a list of objects represented by the selected data view
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [ActionName( "DataView" )]
        [EnableQuery]
        public IQueryable<T> GetDataView( int id )
        {
            var dataView = new DataViewService( new RockContext() ).Get( id );

            // since DataViews can be secured at the Dataview or Category level, specifically check for CanView
            CheckCanView( dataView, GetPerson() );

            SetProxyCreation( false );

            if ( dataView != null && dataView.EntityType.Name == typeof( T ).FullName )
            {
                var errorMessages = new List<string>();

                var paramExpression = Service.ParameterExpression;
                var whereExpression = dataView.GetExpression( Service, paramExpression, out errorMessages );

                if ( paramExpression != null )
                {
                    return Service.GetNoTracking( paramExpression, whereExpression );
                }
            }

            return null;
        }

#if !IS_NET_CORE
        // EFTODO: Duplicate routes not supported.

        /// <summary>
        /// Determines if the entity id is in the data view
        /// </summary>
        /// <param name="dataViewId">The data view identifier.</param>
        /// <param name="entityId">The entity identifier.</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [ActionName( "InDataView" )]
        [EnableQuery]
        [HttpGet]
        public bool InDataView( int dataViewId, int entityId )
        {
            var rockContext = new RockContext();

            var dataView = new DataViewService( rockContext ).Get( dataViewId );

            // since DataViews can be secured at the Dataview or Category level, specifically check for CanView
            CheckCanView( dataView, GetPerson() );

            if ( dataView != null && dataView.EntityType.Name == typeof( T ).FullName )
            {
                var errorMessages = new List<string>();
                var qryGroupsInDataView = dataView.GetQuery( null, rockContext, null, out errorMessages ) as IQueryable<T>;
                qryGroupsInDataView = qryGroupsInDataView.Where( d => d.Id == entityId );

                return qryGroupsInDataView.Any();
            }

            return false;
        }

        /// <summary>
        /// Launches a workflow. And optionally passes the entity with selected id as the entity for the workflow
        /// </summary>
        /// <param name="id">The Id of the entity to pass to workflow, if entity cannot be loaded workflow will still be launched but without passing an entity</param>
        /// <param name="workflowTypeGuid">The workflow type unique identifier.</param>
        /// <param name="workflowName">The Name of the workflow.</param>
        /// <param name="workflowAttributeValues">Optional list of workflow values to set.</param>
        [Authenticate, Secured]
        [ActionName( "LaunchWorkflow" )]
        [HttpPost]
        public void LaunchWorkflow( int id, Guid workflowTypeGuid, string workflowName, [FromBody] Dictionary<string, string> workflowAttributeValues )
        {
            T entity = null;
            if ( id > 0 )
            {
                entity = Get( id );
            }

            if ( entity != null )
            {
                entity.LaunchWorkflow( workflowTypeGuid, workflowName, workflowAttributeValues );
            }
            else
            {
                var transaction = new Rock.Transactions.LaunchWorkflowTransaction( workflowTypeGuid, workflowName );
                if ( workflowAttributeValues != null )
                {
                    transaction.WorkflowAttributeValues = workflowAttributeValues;
                }

                Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
            }
        }
#endif

        /// <summary>
        /// Launches a workflow. And optionally passes the entity with selected id as the entity for the workflow
        /// </summary>
        /// <param name="id">The Id of the entity to pass to workflow, if entity cannot be loaded workflow will still be launched but without passing an entity</param>
        /// <param name="workflowTypeId">The workflow type identifier.</param>
        /// <param name="workflowName">Name of the workflow.</param>
        /// <param name="workflowAttributeValues">Optional list of workflow values to set.</param>
        [Authenticate, Secured]
        [ActionName( "LaunchWorkflow" )]
        [HttpPost]
        public void LaunchWorkflow( int id, int workflowTypeId, string workflowName, [FromBody] Dictionary<string, string> workflowAttributeValues )
        {
            T entity = null;
            if ( id > 0 )
            {
                entity = Get( id );
            }

            if ( entity != null )
            {
                entity.LaunchWorkflow( workflowTypeId, workflowName, workflowAttributeValues );
            }
            else
            {
                var transaction = new Rock.Transactions.LaunchWorkflowTransaction( workflowTypeId, workflowName );
                if ( workflowAttributeValues != null )
                {
                    transaction.WorkflowAttributeValues = workflowAttributeValues;
                }

                Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
            }
        }

        /// <summary>
        /// Gets a query of the items that are followed by a specific person. For example, ~/api/Groups/FollowedItems
        /// would return a list of groups that the person is following. Either ?personId= or ?personAliasId= can be
        /// specified to indicate what person you want to see the followed items for.
        /// </summary>
        /// <param name="personId">The person identifier.</param>
        /// <param name="personAliasId">The person alias identifier.</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [ActionName( "FollowedItems" )]
        [EnableQuery]
        public IQueryable<T> GetFollowedItems( int? personId = null, int? personAliasId = null )
        {
            if ( !personId.HasValue )
            {
                if ( personAliasId.HasValue )
                {
                    personId = new PersonAliasService( this.Service.Context as RockContext ).GetPersonId( personAliasId.Value );
                }
            }

            if ( personId.HasValue )
            {
                return Service.GetFollowed( personId.Value );
            }

            throw new HttpResponseException( new HttpResponseMessage( HttpStatusCode.BadRequest ) { ReasonPhrase = "either personId or personAliasId must be specified"  } );
        }

        /// <summary>
        /// DELETE to delete the specified attribute value for the record
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="attributeKey">The attribute key.</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [HttpDelete]
#if IS_NET_CORE
        public virtual IActionResult DeleteAttributeValue( int id, string attributeKey )
#else
        public virtual HttpResponseMessage DeleteAttributeValue( int id, string attributeKey )
#endif
        {
            return SetAttributeValue( id, attributeKey, string.Empty );
        }

        /// <summary>
        /// POST an attribute value. Use this to set an attribute value for the record
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="attributeKey">The attribute key.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns></returns>
        /// <exception cref="HttpResponseException">
        /// </exception>
        /// <exception cref="HttpResponseMessage">
        /// </exception>
        [Authenticate, Secured]
        [HttpPost]
#if IS_NET_CORE
        public virtual IActionResult SetAttributeValue( int id, string attributeKey, string attributeValue )
#else
        public virtual HttpResponseMessage SetAttributeValue( int id, string attributeKey, string attributeValue )
#endif
        {
            T model;
            if ( !Service.TryGet( id, out model ) )
            {
                throw new HttpResponseException( HttpStatusCode.NotFound );
            }

            CheckCanEdit( model );

            IHasAttributes modelWithAttributes = model as IHasAttributes;
            if ( modelWithAttributes != null )
            {
                using ( var rockContext = new RockContext() )
                {
                    modelWithAttributes.LoadAttributes( rockContext );
                    Rock.Web.Cache.AttributeCache attributeCache = modelWithAttributes.Attributes.ContainsKey( attributeKey ) ? modelWithAttributes.Attributes[attributeKey] : null;

                    if ( attributeCache != null )
                    {
                        if ( !attributeCache.IsAuthorized( Rock.Security.Authorization.EDIT, this.GetPerson() ) )
                        {
                            throw new HttpResponseException( new HttpResponseMessage( HttpStatusCode.Forbidden ) { ReasonPhrase = string.Format( "Not authorized to edit {0} on {1}", modelWithAttributes.GetType().GetFriendlyTypeName(), attributeKey ) } );
                        }

                        Rock.Attribute.Helper.SaveAttributeValue( modelWithAttributes, attributeCache, attributeValue, rockContext );
#if IS_NET_CORE
                        return Accepted( modelWithAttributes.Id );
#else
                        var response = ControllerContext.Request.CreateResponse( HttpStatusCode.Accepted, modelWithAttributes.Id );
                        return response;
#endif
                    }
                    else
                    {
                        throw new HttpResponseException( new HttpResponseMessage( HttpStatusCode.BadRequest ) { ReasonPhrase = string.Format( "{0} does not have a {1} attribute", modelWithAttributes.GetType().GetFriendlyTypeName(), attributeKey ) } );
                    }
                }
            }
            else
            {
                throw new HttpResponseException( new HttpResponseMessage( HttpStatusCode.BadRequest ) { ReasonPhrase = "specified item does not have attributes" } );
            }
        }

        /// <summary>
        /// Sets the Context Cookie to the specified record. Use this to set the Campus Context, Group Context, etc
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        /// <exception cref="HttpResponseException">
        /// </exception>
        [Authenticate, Secured]
        [HttpPut, HttpOptions]
        [ActionName( "SetContext" )]
#if IS_NET_CORE
        public virtual IActionResult SetContext( int id )
#else
        public virtual HttpResponseMessage SetContext( int id )
#endif
        {
            Guid? guid = Service.GetGuid( id );
            if ( !guid.HasValue )
            {
                throw new HttpResponseException( HttpStatusCode.NotFound );
            }

            string cookieName = "Rock_Context";
            string typeName = typeof( T ).FullName;

            string identifier =
                typeName + "|" +
                id.ToString() + ">" +
                guid.ToString();
            string contextValue = Rock.Security.Encryption.EncryptString( identifier );

#if IS_NET_CORE
            var httpContext = HttpContext;
#else
            var httpContext = System.Web.HttpContext.Current;
#endif
            if ( httpContext == null )
            {
                throw new HttpResponseException( HttpStatusCode.BadRequest );
            }

#if IS_NET_CORE
            httpContext.Response.Cookies.Append( cookieName, contextValue, new Microsoft.AspNetCore.Http.CookieOptions
            {
                Expires = RockDateTime.Now.AddYears( 1 )
            } );
#else
            var contextCookie = httpContext.Request.Cookies[cookieName];
            if ( contextCookie == null )
            {
                contextCookie = new System.Web.HttpCookie( cookieName );
            }

            contextCookie.Values[typeName] = contextValue;
            contextCookie.Expires = RockDateTime.Now.AddYears( 1 );
            httpContext.Response.Cookies.Add( contextCookie );
#endif

#if IS_NET_CORE
            return Ok();
#else
            return ControllerContext.Request.CreateResponse( HttpStatusCode.OK );
#endif
        }

        /// <summary>
        /// Checks the can edit.
        /// </summary>
        /// <param name="entity">The entity.</param>
        protected virtual void CheckCanEdit( T entity )
        {
            if ( entity is ISecured )
            {
                CheckCanEdit( ( ISecured ) entity );
            }
        }

        /// <summary>
        /// Checks the can edit.
        /// </summary>
        /// <param name="securedModel">The secured model.</param>
        protected virtual void CheckCanEdit( ISecured securedModel )
        {
            CheckCanEdit( securedModel, GetPerson() );
        }

        /// <summary>
        /// Checks the can edit.
        /// </summary>
        /// <param name="securedModel">The secured model.</param>
        /// <param name="person">The person.</param>
        /// <exception cref="System.Web.Http.HttpResponseException">
        /// </exception>
        protected virtual void CheckCanEdit( ISecured securedModel, Person person )
        {
            if ( securedModel != null )
            {
                if ( IsProxy( securedModel ) )
                {
                    if ( !securedModel.IsAuthorized( Rock.Security.Authorization.EDIT, person ) )
                    {
                        throw new HttpResponseException( HttpStatusCode.Unauthorized );
                    }
                }
                else
                {
                    // Need to reload using service with a proxy enabled so that if model has custom
                    // parent authorities, those properties can be lazy-loaded and checked for authorization
                    SetProxyCreation( true );
                    ISecured reloadedModel = ( ISecured ) Service.Get( securedModel.Id );
                    if ( reloadedModel != null && !reloadedModel.IsAuthorized( Rock.Security.Authorization.EDIT, person ) )
                    {
                        throw new HttpResponseException( HttpStatusCode.Unauthorized );
                    }
                }
            }
        }

        /// <summary>
        /// Checks to see if the person is authorized to VIEW
        /// </summary>
        /// <param name="securedModel">The secured model.</param>
        /// <param name="person">The person.</param>
        /// <exception cref="System.Web.Http.HttpResponseException">
        /// </exception>
        protected virtual void CheckCanView( ISecured securedModel, Person person )
        {
            if ( securedModel != null )
            {
                if ( IsProxy( securedModel ) )
                {
                    if ( !securedModel.IsAuthorized( Rock.Security.Authorization.VIEW, person ) )
                    {
                        throw new HttpResponseException( HttpStatusCode.Unauthorized );
                    }
                }
                else
                {
                    // Need to reload using service with a proxy enabled so that if model has custom
                    // parent authorities, those properties can be lazy-loaded and checked for authorization
                    SetProxyCreation( true );
                    ISecured reloadedModel = ( ISecured ) Service.Get( securedModel.Id );
                    if ( reloadedModel != null && !reloadedModel.IsAuthorized( Rock.Security.Authorization.VIEW, person ) )
                    {
                        throw new HttpResponseException( HttpStatusCode.Unauthorized );
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [enable proxy creation].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable proxy creation]; otherwise, <c>false</c>.
        /// </value>
        protected void SetProxyCreation( bool enabled )
        {
#if IS_NET_CORE
            Service.Context.ChangeTracker.LazyLoadingEnabled = enabled;
#else
            Service.Context.Configuration.ProxyCreationEnabled = enabled;
#endif
        }

        /// <summary>
        /// Determines whether the specified type is proxy.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        protected bool IsProxy( object type )
        {
#if IS_NET_CORE
            return type.GetType().Namespace == "Castle.Proxies";
#else
            return type != null && System.Data.Entity.Core.Objects.ObjectContext.GetObjectType( type.GetType() ) != type.GetType();
#endif
        }
    }
}