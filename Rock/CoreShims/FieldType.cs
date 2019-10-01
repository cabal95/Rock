using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using Rock.Model;
using Rock.Reporting;
using Rock.Web.UI.Controls;

namespace Rock.Field
{
    public class FieldType : IFieldType
    {
        public virtual ComparisonType FilterComparisonType => ComparisonHelper.BinaryFilterComparisonTypes;

        public string AttributeValueFieldName => "Value";

        public Type AttributeValueFieldType => typeof( string );

        public event EventHandler QualifierUpdated;

        public Expression AttributeFilterExpression( Dictionary<string, ConfigurationValue> configurationValues, List<string> filterValues, ParameterExpression parameterExpression )
        {
            throw new NotImplementedException();
        }

        public string FormatFilterValues( Dictionary<string, ConfigurationValue> configurationValues, List<string> filterValues )
        {
            throw new NotImplementedException();
        }

        public string FormatValue( object parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            return value;
        }

        public string FormatValue( object parentControl, int? entityTypeId, int? entityId, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            return value;
        }

        public string FormatValueAsHtml( object parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed = false )
        {
            return value;
        }

        public string FormatValueAsHtml( object parentControl, int? entityTypeId, int? entityId, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed = false )
        {
            return value;
        }

        public bool HasFilterControl()
        {
            return false;
        }

        public bool IsComparedToValue( List<string> filterValues, string value )
        {
            return false;
        }

        public bool IsEqualToValue( List<string> filterValues, string value )
        {
            return false;
        }

        public bool IsSensitive()
        {
            return false;
        }

        public Expression PropertyFilterExpression( Dictionary<string, ConfigurationValue> configurationValues, List<string> filterValues, Expression parameterExpression, string propertyName, Type propertyType )
        {
            throw new NotImplementedException();
        }

        public object SortValue( object parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues )
        {
            return value;
        }

        public object ValueAsFieldType( object parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues )
        {
            return value;
        }
    }
}

namespace Rock.Field.Types
{
    public partial class KeyValueListFieldType
    {
        public List<KeyValuePair<string, object>> GetValuesFromString( object ignored, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            return new List<KeyValuePair<string, object>>();
        }
    }

    public partial class SSNFieldType
    {
        /// <summary>
        /// Unencrypts and strips any non-numeric characters from value.
        /// </summary>
        /// <param name="encryptedValue">The encrypted value.</param>
        /// <returns></returns>
        public static string UnencryptAndClean( string encryptedValue )
        {
            if ( encryptedValue.IsNotNullOrWhiteSpace() )
            {
                string ssn = Rock.Security.Encryption.DecryptString( encryptedValue );
                if ( !string.IsNullOrEmpty( ssn ) )
                {
                    return ssn.AsNumeric(); ;
                }
            }

            return string.Empty;
        }
    }

    public partial class StepProgramStepTypeFieldType
    {
        /// <summary>
        /// Gets the models from the delimited values.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="stepProgramGuid">The step program unique identifier.</param>
        /// <param name="stepTypeGuid">The step type unique identifier.</param>
        public static void ParseDelimitedGuids( string value, out Guid? stepProgramGuid, out Guid? stepTypeGuid )
        {
            var parts = ( value ?? string.Empty ).Split( '|' );

            if ( parts.Length == 1 )
            {
                // If there is only one guid, assume it is the type
                stepProgramGuid = null;
                stepTypeGuid = parts[0].AsGuidOrNull();
                return;
            }

            stepProgramGuid = parts.Length > 0 ? parts[0].AsGuidOrNull() : null;
            stepTypeGuid = parts.Length > 1 ? parts[1].AsGuidOrNull() : null;
        }
    }

    public partial class StepProgramStepStatusFieldType
    {
        /// <summary>
        /// Gets the models from the delimited values.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="stepProgramGuid">The step program unique identifier.</param>
        /// <param name="stepStatusGuid">The step status unique identifier.</param>
        public static void ParseDelimitedGuids( string value, out Guid? stepProgramGuid, out Guid? stepStatusGuid )
        {
            var parts = ( value ?? string.Empty ).Split( '|' );

            if ( parts.Length == 1 )
            {
                // If there is only one guid, assume it is the status
                stepProgramGuid = null;
                stepStatusGuid = parts[0].AsGuidOrNull();
                return;
            }

            stepProgramGuid = parts.Length > 0 ? parts[0].AsGuidOrNull() : null;
            stepStatusGuid = parts.Length > 1 ? parts[1].AsGuidOrNull() : null;
        }
    }

    public partial class ValueFilterFieldType
    {
        /// <summary>
        /// Gets the filter object that can be used to evaluate an object against the filter.
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="value">The attribute value.</param>
        /// <returns>A CompoundFilter object that can be used to evaluate the truth of the filter.</returns>
        public static FilterExpression GetFilterExpression( Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
            return FilterExpression.FromJsonOrNull( value );
        }
    }
}
