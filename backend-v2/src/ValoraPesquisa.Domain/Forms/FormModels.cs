namespace ValoraPesquisa.Domain.Forms;
public enum FormFieldType { Text, Textarea, Number, Money, Date, Datetime, Boolean, Select, Multiselect, User, Sector, Client, Product, Order, FinancialTitle, DynamicRecord, File, Photo, Signature, Gps, Barcode, Qrcode, Rating, Checklist, Json, Relation }
public enum FormRuleType { Visibility, Required, Calculation, DynamicOptions, RegexValidation }
public enum FormVersionStatus { Draft, Published, Archived }
public sealed record FormDefinition(Guid Id, Guid TenantId, string Name, string? Description, bool IsPublished);
public sealed record FormVersion(Guid Id, Guid FormDefinitionId, int VersionNumber, FormVersionStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt);
public sealed record FormSection(Guid Id, Guid FormVersionId, string Title, int DisplayOrder, string? Description);
public sealed record FormField(Guid Id, Guid SectionId, string Name, string Label, FormFieldType Type, int DisplayOrder, bool Required, string? Mask, string? Regex, string? DefaultValue, string? HelpText, string? OptionsJson, string? VisibilityJson, string? CalculationJson);
public sealed record FormRule(Guid Id, Guid FormVersionId, FormRuleType Type, string ExpressionJson, string ActionJson);
public sealed record FormResponse(Guid Id, Guid TenantId, Guid FormVersionId, string EntityType, Guid? EntityId, Guid CreatedBy, DateTimeOffset CreatedAt);
public sealed record FormResponseField(Guid Id, Guid ResponseId, Guid FieldId, string? ValueJson);
public sealed record FormTemplate(Guid Id, Guid TenantId, string Name, string TemplateJson);
