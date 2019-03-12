using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using Rock.Model;
using Rock.Reporting;

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
    public partial class MatrixFieldType
    {
        public const string ATTRIBUTE_MATRIX_TEMPLATE = "attributematrixtemplate";
    }

    public partial class GroupTypeGroupFieldType
    {
        public static readonly string CONFIG_GROUP_PICKER_LABEL = "groupPickerLabel";
    }

    public partial class GroupAndRoleFieldType
    {
        public static readonly string CONFIG_GROUP_AND_ROLE_PICKER_LABEL = "groupAndRolePickerLabel";
    }

    public partial class KeyValueListFieldType
    {
        public List<KeyValuePair<string, object>> GetValuesFromString( object ignored, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            return new List<KeyValuePair<string, object>>();
        }
    }
}
