using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Forms;

namespace Valora.Infrastructure.Repositories;

public sealed class FormAdministrationRepository(IDbConnectionFactory connections, IDbTransactionFactory transactions) : IFormAdministrationRepository
{
    public async Task<IReadOnlyList<FormListItemResponse>> ListAsync(Guid organizationId, FormListQuery query, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT f.id AS "Id",
                   COALESCE(f.name, '') AS "Name",
                   COALESCE(f.description, '') AS "Description",
                   COALESCE(f.category, '') AS "Category",
                   COALESCE(f.estimated_minutes, 0)::int AS "EstimatedMinutes",
                   COALESCE(f.status, 'draft') AS "Status",
                   COALESCE(fv.version_number, 0)::int AS "VersionNumber",
                   COALESCE(stats.sections, 0)::int AS "Sections",
                   COALESCE(stats.questions, 0)::int AS "Questions",
                   COALESCE(stats.dimensions, 0)::int AS "Dimensions",
                   COALESCE(f.updated_at, f.created_at, now()) AS "UpdatedAt",
                   COALESCE(f.version, 0)::bigint AS "Version"
              FROM valorapesquisa.forms f
              LEFT JOIN valorapesquisa.form_versions fv ON fv.id = COALESCE(f.current_draft_version_id, f.latest_published_version_id)
              LEFT JOIN LATERAL (
                  SELECT COUNT(DISTINCT s.id)::int AS sections,
                         COUNT(q.id)::int AS questions,
                         COUNT(DISTINCT q.dimension_code)::int AS dimensions
                    FROM valorapesquisa.form_section_versions s
                    LEFT JOIN valorapesquisa.question_versions q ON q.section_id = s.id AND q.deleted_at IS NULL
                   WHERE s.form_version_id = fv.id AND s.deleted_at IS NULL
              ) stats ON true
             WHERE f.organization_id = @organizationId AND f.deleted_at IS NULL
               AND (@search IS NULL OR f.name ILIKE '%' || @search || '%')
               AND (@status IS NULL OR f.status = @status)
               AND (@category IS NULL OR f.category = @category)
             ORDER BY COALESCE(f.updated_at, f.created_at) DESC
             OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            """;
        using var connection = connections.Create();
        var command = new CommandDefinition(sql, new { organizationId, search = NullIfEmpty(query.Search), status = NullIfEmpty(query.Status), category = NullIfEmpty(query.Category), offset = (query.Page - 1) * query.PageSize, query.PageSize }, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<FormListItemResponse>(command)).AsList();
    }

    public async Task<FormDetailResponse?> GetAsync(Guid organizationId, Guid formId, CancellationToken cancellationToken)
    {
        const string formSql = """
            SELECT f.id AS "Id",
                   f.organization_id AS "OrganizationId",
                   f.name AS "Name",
                   f.description AS "Description",
                   f.category AS "Category",
                   COALESCE(f.estimated_minutes, 0)::int AS "EstimatedMinutes",
                   f.status AS "Status",
                   f.current_draft_version_id AS "CurrentDraftVersionId",
                   f.latest_published_version_id AS "LatestPublishedVersionId",
                   COALESCE(f.version, 0)::bigint AS "Version",
                   fv.version::int AS "DraftVersion"
              FROM valorapesquisa.forms f
              LEFT JOIN valorapesquisa.form_versions fv ON fv.id = f.current_draft_version_id
             WHERE f.id = @formId
               AND f.organization_id = @organizationId
               AND f.deleted_at IS NULL
             LIMIT 1;
            """;
        using var connection = connections.Create();
        var row = await connection.QuerySingleOrDefaultAsync<FormRow>(new CommandDefinition(formSql, new { organizationId, formId }, cancellationToken: cancellationToken));
        if (row is null) return null;
        var sections = row.CurrentDraftVersionId is null
            ? []
            : await LoadSectionsAsync(connection, row.CurrentDraftVersionId.Value, cancellationToken);
        return new(row.Id, row.OrganizationId, row.Name, row.Description, row.Category, row.EstimatedMinutes, row.Status, row.CurrentDraftVersionId, row.LatestPublishedVersionId, row.Version, row.DraftVersion, sections);
    }

    public async Task<FormDetailResponse> CreateAsync(Guid organizationId, Guid userId, CreateFormRequest request, CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        var formId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        const string sql = """
            INSERT INTO valorapesquisa.forms
                (id, code, organization_id, name, description, category, estimated_minutes, status,
                 current_draft_version_id, created_by_user_id, created_at, updated_at, version)
            VALUES
                (@formId, @code, @organizationId, @name, @description, @category, @estimatedMinutes, 'draft',
                 @versionId, @userId, now(), now(), 1);

            INSERT INTO valorapesquisa.form_versions
                (id, organization_id, form_id, version, version_number, status, is_immutable, maximum_score, max_score, created_at, updated_at)
            VALUES (@versionId, @organizationId, @formId, 1, 1, 'draft', false, 0, 0, now(), now());
            """;
        await unit.Connection.ExecuteAsync(new CommandDefinition(sql, new { formId, versionId, code = $"org-{organizationId:N}-{formId:N}", organizationId, userId, request.Name, request.Description, request.Category, request.EstimatedMinutes }, unit.Transaction, cancellationToken: cancellationToken));
        await unit.CommitAsync();
        return (await GetAsync(organizationId, formId, cancellationToken))!;
    }

    public async Task<FormDetailResponse?> UpdateAsync(Guid organizationId, Guid formId, UpdateFormRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE valorapesquisa.forms
               SET name = @name, description = @description, category = @category,
                   estimated_minutes = @estimatedMinutes, updated_at = now(), version = version + 1
             WHERE id = @formId AND organization_id = @organizationId AND deleted_at IS NULL
               AND status = 'draft' AND version = @expectedVersion;
            """;
        using var connection = connections.Create();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new { organizationId, formId, request.Name, request.Description, request.Category, request.EstimatedMinutes, request.ExpectedVersion }, cancellationToken: cancellationToken));
        return affected == 1 ? await GetAsync(organizationId, formId, cancellationToken) : null;
    }

    public async Task<bool> ArchiveAsync(Guid organizationId, Guid formId, ArchiveFormRequest request, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE valorapesquisa.forms SET status='archived', updated_at=now(), version=version+1 WHERE id=@formId AND organization_id=@organizationId AND deleted_at IS NULL AND status<>'archived' AND version=@expectedVersion;";
        using var connection = connections.Create();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { organizationId, formId, request.ExpectedVersion }, cancellationToken: cancellationToken)) == 1;
    }

    public async Task<FormVersionResponse?> PublishVersionAsync(Guid organizationId, Guid formId, Guid userId, PublishFormVersionRequest request, CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        const string validationSql = """
            SELECT fv.id AS "Id", fv.form_id AS "FormId", fv.version_number::int AS "VersionNumber",
                   fv.status AS "Status", fv.maximum_score::int AS "MaximumScore",
                   fv.published_at AS "PublishedAt", fv.version::bigint AS "Version"
              FROM valorapesquisa.form_versions fv
              JOIN valorapesquisa.forms f ON f.id=fv.form_id AND f.current_draft_version_id=fv.id
             WHERE f.id=@formId AND f.organization_id=@organizationId AND f.deleted_at IS NULL
               AND fv.status='draft' AND fv.version=@expectedVersion
               AND EXISTS (SELECT 1 FROM valorapesquisa.form_section_versions s WHERE s.form_version_id=fv.id AND s.deleted_at IS NULL)
               AND EXISTS (SELECT 1 FROM valorapesquisa.question_versions q JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id WHERE s.form_version_id=fv.id AND q.deleted_at IS NULL);
            """;
        var version = await unit.Connection.QuerySingleOrDefaultAsync<FormVersionResponse>(new CommandDefinition(validationSql, new { organizationId, formId, request.ExpectedVersion }, unit.Transaction, cancellationToken: cancellationToken));
        if (version is null) return null;
        const string publishSql = """
            UPDATE valorapesquisa.form_versions
               SET status='published', is_immutable=true, published_at=now(), published_by_user_id=@userId,
                   maximum_score=(SELECT COALESCE(SUM(CASE WHEN q.type='likert_1_5' THEN 5*q.weight ELSE COALESCE(o.score,0) END),0)::int FROM valorapesquisa.question_versions q LEFT JOIN LATERAL (SELECT MAX(score) score FROM valorapesquisa.question_option_versions x WHERE x.question_id=q.id AND x.deleted_at IS NULL) o ON true JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id WHERE s.form_version_id=@versionId AND q.deleted_at IS NULL),
                   updated_at=now(), version=version+1
             WHERE id=@versionId;
            UPDATE valorapesquisa.forms SET status='published', latest_published_version_id=@versionId,
                   current_draft_version_id=NULL, updated_at=now(), version=version+1
             WHERE id=@formId AND organization_id=@organizationId;
            """;
        await unit.Connection.ExecuteAsync(new CommandDefinition(publishSql, new { organizationId, formId, versionId = version.Id, userId }, unit.Transaction, cancellationToken: cancellationToken));
        await unit.CommitAsync();
        return version with { Status = "published", PublishedAt = DateTimeOffset.UtcNow, Version = version.Version + 1 };
    }

    public async Task<ReorderFormItemResponse?> ReorderAsync(Guid organizationId, Guid formId, ReorderFormItemRequest request, CancellationToken cancellationToken)
    {
        var table = request.ItemType switch { "section" => "form_section_versions", "question" => "question_versions", "option" => "question_option_versions", _ => throw new ArgumentOutOfRangeException(nameof(request)) };
        var containerColumn = request.ItemType switch { "section" => "form_version_id", "question" => "section_id", _ => "question_id" };
        await using var unit = await transactions.BeginAsync(cancellationToken);
        var ownershipSql = $"SELECT 1 FROM valorapesquisa.{table} i JOIN valorapesquisa.form_versions fv ON fv.id={(request.ItemType == "section" ? "i.form_version_id" : request.ItemType == "question" ? "(SELECT s.form_version_id FROM valorapesquisa.form_section_versions s WHERE s.id=i.section_id)" : "(SELECT s.form_version_id FROM valorapesquisa.question_versions q JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id WHERE q.id=i.question_id)")} JOIN valorapesquisa.forms f ON f.id=fv.form_id WHERE i.id=@itemId AND f.id=@formId AND f.organization_id=@organizationId AND fv.status='draft' AND i.version=@expectedVersion;";
        if (await unit.Connection.ExecuteScalarAsync<int?>(new CommandDefinition(ownershipSql, new { organizationId, formId, request.ItemId, request.ExpectedVersion }, unit.Transaction, cancellationToken: cancellationToken)) is null) return null;
        var containerId = request.TargetContainerId ?? request.SourceContainerId;
        var sql = $"UPDATE valorapesquisa.{table} SET position=position+1 WHERE {containerColumn}=@containerId AND deleted_at IS NULL AND position>=@newPosition AND id<>@itemId; UPDATE valorapesquisa.{table} SET {containerColumn}=@containerId,position=@newPosition,version=version+1,updated_at=now() WHERE id=@itemId; SELECT id FROM valorapesquisa.{table} WHERE {containerColumn}=@containerId AND deleted_at IS NULL ORDER BY position,id;";
        var order = (await unit.Connection.QueryAsync<Guid>(new CommandDefinition(sql, new { request.ItemId, containerId, request.NewPosition }, unit.Transaction, cancellationToken: cancellationToken))).AsList();
        await unit.CommitAsync();
        return new(request.ItemId, request.ItemType, containerId, request.NewPosition, request.ExpectedVersion + 1, order);
    }

    private static async Task<IReadOnlyList<FormSectionResponse>> LoadSectionsAsync(System.Data.IDbConnection connection, Guid versionId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id AS "Id", form_version_id AS "FormVersionId", title AS "Title",
                   description AS "Description", position::int AS "Position", version::bigint AS "Version"
              FROM valorapesquisa.form_section_versions
             WHERE form_version_id=@versionId AND deleted_at IS NULL
             ORDER BY position,id;
            SELECT q.id AS "Id", q.section_id AS "SectionId", q.code AS "Code", q.type AS "Type",
                   q.title AS "Title", q.description AS "Description", q.required AS "Required",
                   q.dimension_code AS "DimensionCode", q.weight AS "Weight", q.position::int AS "Position",
                   q.settings::text AS "Settings", q.version::bigint AS "Version"
              FROM valorapesquisa.question_versions q
              JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id
             WHERE s.form_version_id=@versionId AND s.deleted_at IS NULL AND q.deleted_at IS NULL
             ORDER BY q.section_id,q.position,q.id;
            SELECT o.id AS "Id", o.question_id AS "QuestionId", o.label AS "Label", o.value AS "Value",
                   o.score AS "Score", o.position::int AS "Position", o.version::bigint AS "Version"
              FROM valorapesquisa.question_option_versions o
              JOIN valorapesquisa.question_versions q ON q.id=o.question_id
              JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id
             WHERE s.form_version_id=@versionId AND s.deleted_at IS NULL
               AND q.deleted_at IS NULL AND o.deleted_at IS NULL
             ORDER BY o.question_id,o.position,o.id;
            """;
        using var grid = await connection.QueryMultipleAsync(new CommandDefinition(sql, new { versionId }, cancellationToken: cancellationToken));
        var sections = (await grid.ReadAsync<SectionRow>()).AsList();
        var questions = (await grid.ReadAsync<QuestionRow>()).AsList();
        var options = (await grid.ReadAsync<OptionRow>()).AsList();
        var optionsByQuestion = options.ToLookup(option => option.QuestionId);
        var questionsBySection = questions
            .Select(question => new QuestionResponse(question.Id, question.SectionId, question.Code, question.Type,
                question.Title, question.Description, question.Required, question.DimensionCode, question.Weight,
                question.Position, question.Settings, question.Version,
                optionsByQuestion[question.Id].Select(option => new QuestionOptionResponse(option.Id, option.QuestionId,
                    option.Label, option.Value, option.Score, option.Position, option.Version)).ToList()))
            .ToLookup(question => question.SectionId);
        return sections.Select(section => new FormSectionResponse(section.Id, section.FormVersionId, section.Title,
            section.Description, section.Position, section.Version, questionsBySection[section.Id].ToList())).ToList();
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private sealed class FormRow
    {
        public Guid Id { get; init; }
        public Guid OrganizationId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Category { get; init; }
        public int EstimatedMinutes { get; init; }
        public string Status { get; init; } = string.Empty;
        public Guid? CurrentDraftVersionId { get; init; }
        public Guid? LatestPublishedVersionId { get; init; }
        public long Version { get; init; }
        public int? DraftVersion { get; init; }
    }

    private sealed class SectionRow
    {
        public Guid Id { get; init; }
        public Guid FormVersionId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int Position { get; init; }
        public long Version { get; init; }
    }

    private sealed class QuestionRow
    {
        public Guid Id { get; init; }
        public Guid SectionId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool Required { get; init; }
        public string? DimensionCode { get; init; }
        public decimal Weight { get; init; }
        public int Position { get; init; }
        public string Settings { get; init; } = "{}";
        public long Version { get; init; }
    }

    private sealed class OptionRow
    {
        public Guid Id { get; init; }
        public Guid QuestionId { get; init; }
        public string Label { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public decimal? Score { get; init; }
        public int Position { get; init; }
        public long Version { get; init; }
    }
}
