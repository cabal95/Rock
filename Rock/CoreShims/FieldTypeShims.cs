namespace Rock.Field.Types
{
    public partial class AccountFieldType : Rock.Field.FieldType
    {
    }
    public partial class AccountsFieldType : Rock.Field.FieldType
    {
    }
    public partial class AddressFieldType : Rock.Field.FieldType
    {
    }
    public partial class AssessmentTypesFieldType : Rock.Field.FieldType
    {
    }
    public partial class AssetFieldType : Rock.Field.FieldType
    {
    }
    public partial class AssetStorageProviderFieldType : Rock.Field.FieldType
    {
    }
    public partial class AttributeFieldType : Rock.Field.FieldType
    {
    }
    public partial class AudioFileFieldType : Rock.Field.FieldType
    {
    }
    public partial class AudioUrlFieldType : Rock.Field.FieldType
    {
    }
    public partial class BackgroundCheckFieldType : Rock.Field.FieldType
    {
    }
    public partial class BadgesFieldType : Rock.Field.FieldType
    {
    }
    public partial class BenevolenceRequestFieldType : Rock.Field.FieldType
    {
    }
    public partial class BinaryFileFieldType : Rock.Field.FieldType
    {
    }
    public partial class BinaryFileTypeFieldType : Rock.Field.FieldType
    {
    }
    public partial class BinaryFileTypesFieldType : Rock.Field.FieldType
    {
    }
    public partial class BooleanFieldType : Rock.Field.FieldType
    {
        public static class ConfigurationKey
        {
            public const string TrueText = "truetext";
            public const string FalseText = "falsetext";
            public const string BooleanControlType = "BooleanControlType";
        }
        public enum BooleanControlType
        {
            /// <summary>
            /// Use a RockDropDown with TrueText and FalseTest as the options
            /// </summary>
            DropDown,

            /// <summary>
            /// Use a RockCheckBox
            /// </summary>
            Checkbox,

            /// <summary>
            /// Use a Rock:Toggle with TrueText and FalseTest as the buttons text
            /// </summary>
            Toggle
        }
    }
    public partial class CampusesFieldType : Rock.Field.FieldType
    {
    }
    public partial class CampusFieldType : Rock.Field.FieldType
    {
    }
    public partial class CategoriesFieldType : Rock.Field.FieldType
    {
    }
    public partial class CategoryFieldType : Rock.Field.FieldType
    {
    }
    public partial class CheckListFieldType : Rock.Field.FieldType
    {
    }
    public partial class CodeEditorFieldType : Rock.Field.FieldType
    {
    }
    public partial class ColorFieldType : Rock.Field.FieldType
    {
    }
    public partial class CommunicationPreferenceFieldType : Rock.Field.FieldType
    {
    }
    public partial class CommunicationTemplateFieldType : Rock.Field.FieldType
    {
    }
    public partial class ComparisonFieldType : Rock.Field.FieldType
    {
    }
    public partial class ComponentFieldType : Rock.Field.FieldType
    {
    }
    public partial class ComponentsFieldType : Rock.Field.FieldType
    {
    }
    public partial class ConditionalScaleFieldType : Rock.Field.FieldType
    {
    }
    public partial class ConnectionActivityTypeFieldType : Rock.Field.FieldType
    {
    }
    public partial class ConnectionOpportunityFieldType : Rock.Field.FieldType
    {
    }
    public partial class ConnectionRequestFieldType : Rock.Field.FieldType
    {
    }
    public partial class ConnectionStateFieldType : Rock.Field.FieldType
    {
    }
    public partial class ConnectionStatusFieldType : Rock.Field.FieldType
    {
    }
    public partial class ConnectionTypeFieldType : Rock.Field.FieldType
    {
    }
    public partial class ConnectionTypesFieldType : Rock.Field.FieldType
    {
    }
    public partial class ContentChannelFieldType : Rock.Field.FieldType
    {
    }
    public partial class ContentChannelItemFieldType : Rock.Field.FieldType
    {
        public static readonly string CONTENT_CHANNEL_KEY = "contentchannel";
    }
    public partial class ContentChannelTypeFieldType : Rock.Field.FieldType
    {
    }
    public partial class ContentChannelTypesFieldType : Rock.Field.FieldType
    {
    }
    public partial class CurrencyFieldType : Rock.Field.FieldType
    {
    }
    public partial class DataViewFieldType : Rock.Field.FieldType
    {
    }
    public partial class DataViewsFieldType : Rock.Field.FieldType
    {
    }
    public partial class DateFieldType : Rock.Field.FieldType
    {
        public enum DatePickerControlType
        {
            /// <summary>
            /// The date picker
            /// </summary>
            DatePicker,
            
            /// <summary>
            /// The date parts picker
            /// </summary>
            DatePartsPicker
        }
    }
    public partial class DateRangeFieldType : Rock.Field.FieldType
    {
    }
    public partial class DateTimeFieldType : Rock.Field.FieldType
    {
    }
    public partial class DayOfWeekFieldType : Rock.Field.FieldType
    {
    }
    public partial class DaysOfWeekFieldType : Rock.Field.FieldType
    {
    }
    public partial class DecimalFieldType : Rock.Field.FieldType
    {
    }
    public partial class DecimalRangeFieldType : Rock.Field.FieldType
    {
    }
    public partial class DefinedTypeFieldType : Rock.Field.FieldType
    {
    }
    public partial class DefinedValueFieldType : Rock.Field.FieldType
    {
    }
    public partial class DefinedValueRangeFieldType : Rock.Field.FieldType
    {
    }
    public partial class EmailFieldType : Rock.Field.FieldType
    {
    }
    public partial class EncryptedTextFieldType : Rock.Field.FieldType
    {
    }
    public partial class EntityFieldType : Rock.Field.FieldType
    {
    }
    public partial class EntityTypeFieldType : Rock.Field.FieldType
    {
    }
    public partial class EnumFieldType<T> : Rock.Field.FieldType
    {
    }
    public partial class EventCalendarFieldType : Rock.Field.FieldType
    {
    }
    public partial class EventItemFieldType : Rock.Field.FieldType
    {
    }
    public partial class FileFieldType : Rock.Field.FieldType
    {
    }
    public partial class FinancialGatewayFieldType : Rock.Field.FieldType
    {
    }
    public partial class GenderFieldType : Rock.Field.FieldType
    {
    }
    public partial class GroupAndRoleFieldType : Rock.Field.FieldType
    {
        public static readonly string CONFIG_GROUP_AND_ROLE_PICKER_LABEL = "groupAndRolePickerLabel";
    }
    public partial class GroupFieldType : Rock.Field.FieldType
    {
    }
    public partial class GroupLocationTypeFieldType : Rock.Field.FieldType
    {
    }
    public partial class GroupMemberFieldType : Rock.Field.FieldType
    {
    }
    public partial class GroupRoleFieldType : Rock.Field.FieldType
    {
    }
    public partial class GroupTypeFieldType : Rock.Field.FieldType
    {
    }
    public partial class GroupTypeGroupFieldType : Rock.Field.FieldType
    {
        public static readonly string CONFIG_GROUP_PICKER_LABEL = "groupPickerLabel";
    }
    public partial class GroupTypesFieldType : Rock.Field.FieldType
    {
    }
    public partial class HtmlFieldType : Rock.Field.FieldType
    {
    }
    public partial class ImageFieldType : Rock.Field.FieldType
    {
    }
    public partial class IntegerFieldType : Rock.Field.FieldType
    {
    }
    public partial class IntegerRangeFieldType : Rock.Field.FieldType
    {
    }
    public partial class InteractionChannelFieldType : Rock.Field.FieldType
    {
    }
    public partial class InteractionChannelsFieldType : Rock.Field.FieldType
    {
    }
    public partial class KeyValueListFieldType : Rock.Field.FieldType
    {
    }
    public partial class LabelFieldType : Rock.Field.FieldType
    {
    }
    public partial class LavaCommandsFieldType : Rock.Field.FieldType
    {
    }
    public partial class LavaFieldType : Rock.Field.FieldType
    {
    }
    public partial class LocationFieldType : Rock.Field.FieldType
    {
    }
    public partial class MarkdownFieldType : Rock.Field.FieldType
    {
    }
    public partial class MatrixFieldType : Rock.Field.FieldType
    {
        public const string ATTRIBUTE_MATRIX_TEMPLATE = "attributematrixtemplate";
    }
    public partial class MemoFieldType : Rock.Field.FieldType
    {
    }
    public partial class MergeTemplateFieldType : Rock.Field.FieldType
    {
    }
    public partial class MetricCategoriesFieldType : Rock.Field.FieldType
    {
    }
    public partial class MonthDayFieldType : Rock.Field.FieldType
    {
    }
    public partial class NoteTypeFieldType : Rock.Field.FieldType
    {
    }
    public partial class NoteTypesFieldType : Rock.Field.FieldType
    {
    }
    public partial class PageReferenceFieldType : Rock.Field.FieldType
    {
    }
    public partial class PersistedDatasetFieldType : Rock.Field.FieldType
    {
    }
    public partial class PersonFieldType : Rock.Field.FieldType
    {
    }
    public partial class PhoneNumberFieldType : Rock.Field.FieldType
    {
    }
    public partial class RangeSliderFieldType : Rock.Field.FieldType
    {
    }
    public partial class RatingFieldType : Rock.Field.FieldType
    {
    }
    public partial class RegistrationInstanceFieldType : Rock.Field.FieldType
    {
        public static readonly string REGISTRATION_TEMPLATE_KEY = "registrationtemplate";
    }
    public partial class RegistrationTemplateFieldType : Rock.Field.FieldType
    {
    }
    public partial class RegistrationTemplatesFieldType : Rock.Field.FieldType
    {
    }
    public partial class RegistryEntryFieldType : Rock.Field.FieldType
    {
    }
    public partial class RemoteAuthsFieldType : Rock.Field.FieldType
    {
    }
    public partial class ReportFieldType : Rock.Field.FieldType
    {
    }
    public partial class ScheduleFieldType : Rock.Field.FieldType
    {
    }
    public partial class SchedulesFieldType : Rock.Field.FieldType
    {
    }
    public partial class SecurityRoleFieldType : Rock.Field.FieldType
    {
    }
    public partial class SelectMultiFieldType : Rock.Field.FieldType
    {
    }
    public partial class SelectSingleFieldType : Rock.Field.FieldType
    {
    }
    public partial class SiteFieldType : Rock.Field.FieldType
    {
    }
    public partial class SlidingDateRangeFieldType : Rock.Field.FieldType
    {
    }
    public partial class SocialMediaAccountFieldType : Rock.Field.FieldType
    {
    }
    public partial class SSNFieldType : Rock.Field.FieldType
    {
    }
    public partial class StepProgramFieldType : Rock.Field.FieldType
    {
    }
    public partial class StepProgramStepStatusFieldType : Rock.Field.FieldType
    {
        public static class ConfigKey
        {
            /// <summary>
            /// The default step program unique identifier
            /// </summary>
            public const string DefaultStepProgramGuid = "DefaultStepProgramGuid";
        }
    }
    public partial class StepProgramStepTypeFieldType : Rock.Field.FieldType
    {
        public static class ConfigKey
        {
            /// <summary>
            /// The default step program unique identifier
            /// </summary>
            public const string DefaultStepProgramGuid = "DefaultStepProgramGuid";
        }
    }
    public partial class StreakTypeFieldType : Rock.Field.FieldType
    {
    }
    public partial class SystemEmailFieldType : Rock.Field.FieldType
    {
    }
    public partial class TextFieldType : Rock.Field.FieldType
    {
    }
    public partial class TimeFieldType : Rock.Field.FieldType
    {
    }
    public partial class TimeZoneFieldType : Rock.Field.FieldType
    {
    }
    public partial class UrlLinkFieldType : Rock.Field.FieldType
    {
    }
    public partial class ValueFilterFieldType : Rock.Field.FieldType
    {
        public const string HIDE_FILTER_MODE = "hidefiltermode";
        public const string COMPARISON_TYPES = "comparisontypes";
    }
    public partial class ValueListFieldType : Rock.Field.FieldType
    {
    }
    public partial class VideoFileFieldType : Rock.Field.FieldType
    {
    }
    public partial class VideoUrlFieldType : Rock.Field.FieldType
    {
    }
    public partial class WorkflowActivityFieldType : Rock.Field.FieldType
    {
    }
    public partial class WorkflowAttributeFieldType : Rock.Field.FieldType
    {
    }
    public partial class WorkflowFieldType : Rock.Field.FieldType
    {
    }
    public partial class WorkflowTextOrAttributeFieldType : Rock.Field.FieldType
    {
    }
    public partial class WorkflowTypeFieldType : Rock.Field.FieldType
    {
    }
    public partial class WorkflowTypesFieldType : Rock.Field.FieldType
    {
    }
    public partial class SelectFromListFieldType : Rock.Field.FieldType
    {
    }
}
