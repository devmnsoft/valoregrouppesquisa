using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Forms;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class FormAdministrationRepository(IDbConnectionFactory connections, IDbTransactionFactory transactions,
    IAuditRepository audit) : IFormAdministrationRepository
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
                   fv.row_version::bigint AS "DraftVersion"
              FROM valorapesquisa.forms f
              LEFT JOIN valorapesquisa.form_versions fv ON fv.id = COALESCE(f.current_draft_version_id, f.latest_published_version_id)
             WHERE f.id = @formId
               AND f.organization_id = @organizationId
               AND f.deleted_at IS NULL
             LIMIT 1;
            """;
        using var connection = connections.Create();
        var row = await connection.QuerySingleOrDefaultAsync<FormRow>(new CommandDefinition(formSql, new { organizationId, formId }, cancellationToken: cancellationToken));
        if (row is null) return null;
        var selectedVersionId = row.CurrentDraftVersionId ?? row.LatestPublishedVersionId;
        var sections = selectedVersionId is null
            ? []
            : await LoadSectionsAsync(connection, selectedVersionId.Value, cancellationToken);
        return new(row.Id, row.OrganizationId, row.Name, row.Description, row.Category, row.EstimatedMinutes, row.Status, row.CurrentDraftVersionId, row.LatestPublishedVersionId, row.Version, row.DraftVersion, sections);
    }

    public async Task<FormDetailResponse> CreateAsync(Guid organizationId, Guid userId, CreateFormRequest request, CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        var formId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        const string sql = """
            INSERT INTO valorapesquisa.forms
                (id, code, title, slug, form_key, organization_id, name, description, category, estimated_minutes, status,
                 current_draft_version_id, created_by_user_id, created_at, updated_at, version)
            VALUES
                (@formId, @code, @name, @code, @code, @organizationId, @name, @description, @category, @estimatedMinutes, 'draft',
                 @versionId, @userId, now(), now(), 1);

            INSERT INTO valorapesquisa.form_versions
                (id, organization_id, form_id, version, version_number, status, is_immutable, maximum_score, max_score, row_version, created_at, updated_at)
            VALUES (@versionId, @organizationId, @formId, 1, 1, 'draft', false, 0, 0, 1, now(), now());
            """;
        await unit.Connection.ExecuteAsync(new CommandDefinition(sql, new { formId, versionId, code = $"org-{organizationId:N}-{formId:N}", organizationId, userId, request.Name, request.Description, request.Category, request.EstimatedMinutes }, unit.Transaction, cancellationToken: cancellationToken));
        await AuditAsync(unit, organizationId, userId, "form.created", "form", formId, cancellationToken);
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
                   fv.published_at AS "PublishedAt", fv.row_version::bigint AS "Version"
              FROM valorapesquisa.form_versions fv
              JOIN valorapesquisa.forms f ON f.id=fv.form_id AND f.current_draft_version_id=fv.id
             WHERE f.id=@formId AND f.organization_id=@organizationId AND f.deleted_at IS NULL
               AND fv.status='draft' AND fv.row_version=@expectedVersion
               AND NULLIF(BTRIM(f.name),'') IS NOT NULL AND NULLIF(BTRIM(f.category),'') IS NOT NULL
               AND EXISTS (SELECT 1 FROM valorapesquisa.form_section_versions s WHERE s.form_version_id=fv.id AND s.deleted_at IS NULL)
               AND EXISTS (SELECT 1 FROM valorapesquisa.question_versions q JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id WHERE s.form_version_id=fv.id AND q.deleted_at IS NULL AND NULLIF(BTRIM(q.title),'') IS NOT NULL)
               AND NOT EXISTS (
                    SELECT 1 FROM valorapesquisa.question_versions q
                    JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id
                    WHERE s.form_version_id=fv.id AND q.deleted_at IS NULL
                      AND q.type IN ('single_choice','multiple_choice')
                      AND NOT EXISTS (SELECT 1 FROM valorapesquisa.question_option_versions o WHERE o.question_id=q.id AND o.deleted_at IS NULL AND NULLIF(BTRIM(o.label),'') IS NOT NULL)
               );
            """;
        var version = await unit.Connection.QuerySingleOrDefaultAsync<FormVersionResponse>(new CommandDefinition(validationSql, new { organizationId, formId, request.ExpectedVersion }, unit.Transaction, cancellationToken: cancellationToken));
        if (version is null) return null;
        const string publishSql = """
            UPDATE valorapesquisa.form_versions
               SET status='published', is_immutable=true, published_at=now(), published_by_user_id=@userId,
                   maximum_score=(SELECT COALESCE(SUM(CASE WHEN q.type='likert_1_5' THEN 5*q.weight ELSE COALESCE(o.score,0) END),0)::int FROM valorapesquisa.question_versions q LEFT JOIN LATERAL (SELECT MAX(score) score FROM valorapesquisa.question_option_versions x WHERE x.question_id=q.id AND x.deleted_at IS NULL) o ON true JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id WHERE s.form_version_id=@versionId AND q.deleted_at IS NULL),
                   updated_at=now(), row_version=row_version+1
             WHERE id=@versionId;
            UPDATE valorapesquisa.forms SET status='published', latest_published_version_id=@versionId,
                   current_draft_version_id=NULL, updated_at=now(), version=version+1
             WHERE id=@formId AND organization_id=@organizationId;
            """;
        await unit.Connection.ExecuteAsync(new CommandDefinition(publishSql, new { organizationId, formId, versionId = version.Id, userId }, unit.Transaction, cancellationToken: cancellationToken));
        await AuditAsync(unit, organizationId, userId, "form.published", "form", formId, cancellationToken);
        await unit.CommitAsync();
        return version with { Status = "published", PublishedAt = DateTimeOffset.UtcNow, Version = version.Version + 1 };
    }

    public async Task<ReorderFormItemResponse?> ReorderAsync(Guid organizationId, Guid formId, ReorderFormItemRequest request, CancellationToken cancellationToken)
    {
        var table = request.ItemType switch { "section" => "form_section_versions", "question" => "question_versions", "option" => "question_option_versions", _ => throw new ArgumentOutOfRangeException(nameof(request)) };
        var containerColumn = request.ItemType switch { "section" => "form_version_id", "question" => "section_id", _ => "question_id" };
        await using var unit = await transactions.BeginAsync(cancellationToken);
        var ownershipSql = $"SELECT fv.id FROM valorapesquisa.{table} i JOIN valorapesquisa.form_versions fv ON fv.id={(request.ItemType == "section" ? "i.form_version_id" : request.ItemType == "question" ? "(SELECT s.form_version_id FROM valorapesquisa.form_section_versions s WHERE s.id=i.section_id)" : "(SELECT s.form_version_id FROM valorapesquisa.question_versions q JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id WHERE q.id=i.question_id)")} JOIN valorapesquisa.forms f ON f.id=fv.form_id WHERE i.id=@itemId AND f.id=@formId AND f.organization_id=@organizationId AND f.current_draft_version_id=fv.id AND fv.status='draft' AND fv.row_version=@expectedVersion;";
        var draftVersionId = await unit.Connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(ownershipSql, new { organizationId, formId, request.ItemId, request.ExpectedVersion }, unit.Transaction, cancellationToken: cancellationToken));
        if (draftVersionId is null) return null;
        var containerId = request.TargetContainerId ?? request.SourceContainerId;
        if (containerId is null) return null;
        var targetIsValid = request.ItemType switch
        {
            "section" => containerId == draftVersionId,
            "question" => await unit.Connection.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM valorapesquisa.form_section_versions WHERE id=@containerId AND form_version_id=@draftVersionId AND deleted_at IS NULL)", new { containerId, draftVersionId }, unit.Transaction, cancellationToken: cancellationToken)),
            _ => await unit.Connection.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM valorapesquisa.question_versions q JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id WHERE q.id=@containerId AND s.form_version_id=@draftVersionId AND q.deleted_at IS NULL AND s.deleted_at IS NULL)", new { containerId, draftVersionId }, unit.Transaction, cancellationToken: cancellationToken))
        };
        if (!targetIsValid) return null;
        var sql = $"UPDATE valorapesquisa.{table} SET position=position+1 WHERE {containerColumn}=@containerId AND deleted_at IS NULL AND position>=@newPosition AND id<>@itemId; UPDATE valorapesquisa.{table} SET {containerColumn}=@containerId,position=@newPosition,version=version+1,updated_at=now() WHERE id=@itemId; SELECT id FROM valorapesquisa.{table} WHERE {containerColumn}=@containerId AND deleted_at IS NULL ORDER BY position,id;";
        var order = (await unit.Connection.QueryAsync<Guid>(new CommandDefinition(sql, new { request.ItemId, containerId, request.NewPosition }, unit.Transaction, cancellationToken: cancellationToken))).AsList();
        await unit.Connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.form_versions SET row_version=row_version+1,updated_at=now() WHERE id=@draftVersionId", new { draftVersionId }, unit.Transaction, cancellationToken: cancellationToken));
        await unit.CommitAsync();
        return new(request.ItemId, request.ItemType, containerId, request.NewPosition, request.ExpectedVersion + 1, order);
    }

    public async Task<FormSectionResponse?> CreateSectionAsync(Guid organizationId, Guid formId, Guid userId,
        CreateFormSectionRequest request, CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        const string sql = """
            WITH draft AS (
                UPDATE valorapesquisa.form_versions fv SET row_version=row_version+1,updated_at=now()
                 FROM valorapesquisa.forms f
                 WHERE f.id=@formId AND f.organization_id=@organizationId AND f.current_draft_version_id=fv.id
                   AND f.deleted_at IS NULL AND fv.status='draft' AND fv.row_version=@expectedVersion
                 RETURNING fv.id
            )
            INSERT INTO valorapesquisa.form_section_versions(id,organization_id,form_version_id,title,description,position,display_order,version)
            SELECT @id,@organizationId,draft.id,@title,@description,@position,@position,1 FROM draft
            RETURNING id AS "Id",form_version_id AS "FormVersionId",title AS "Title",description AS "Description",position::int AS "Position",version::bigint AS "Version";
            """;
        var id = Guid.NewGuid();
        var row = await unit.Connection.QuerySingleOrDefaultAsync<SectionRow>(new CommandDefinition(sql,
            new { id, organizationId, formId, request.Title, request.Description, request.Position, request.ExpectedVersion },
            unit.Transaction, cancellationToken: cancellationToken));
        if (row is null) return null;
        await AuditAsync(unit, organizationId, userId, "form.section.created", "form_section", id, cancellationToken);
        await unit.CommitAsync();
        return new(row.Id, row.FormVersionId, row.Title, row.Description, row.Position, row.Version, []);
    }

    public async Task<FormSectionResponse?> UpdateSectionAsync(Guid organizationId, Guid formId, Guid sectionId, Guid userId,
        UpdateFormSectionRequest request, CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        const string sql = """
            UPDATE valorapesquisa.form_section_versions s
               SET title=@title,description=@description,version=version+1,updated_at=now()
              FROM valorapesquisa.form_versions fv,valorapesquisa.forms f
             WHERE s.id=@sectionId AND s.form_version_id=fv.id AND fv.form_id=f.id
               AND f.id=@formId AND f.organization_id=@organizationId AND f.current_draft_version_id=fv.id
               AND fv.status='draft' AND s.deleted_at IS NULL AND s.version=@expectedVersion
            RETURNING s.id AS "Id",s.form_version_id AS "FormVersionId",s.title AS "Title",s.description AS "Description",s.position::int AS "Position",s.version::bigint AS "Version";
            """;
        var row = await unit.Connection.QuerySingleOrDefaultAsync<SectionRow>(new CommandDefinition(sql,
            new { organizationId, formId, sectionId, request.Title, request.Description, request.ExpectedVersion }, unit.Transaction, cancellationToken: cancellationToken));
        if (row is null) return null;
        await TouchDraftAsync(unit, row.FormVersionId, cancellationToken);
        await AuditAsync(unit, organizationId, userId, "form.section.updated", "form_section", sectionId, cancellationToken);
        await unit.CommitAsync();
        return new(row.Id, row.FormVersionId, row.Title, row.Description, row.Position, row.Version, []);
    }

    public Task<bool> DeleteSectionAsync(Guid organizationId, Guid formId, Guid sectionId, Guid userId,
        DeleteFormSectionRequest request, CancellationToken cancellationToken) => DeleteStructuralAsync(
            organizationId, formId, sectionId, userId, request.ExpectedVersion, "form_section_versions",
            "i.form_version_id", "form.section.archived", "form_section", cancellationToken);

    public async Task<QuestionResponse?> CreateQuestionAsync(Guid organizationId, Guid formId, Guid userId,
        CreateQuestionRequest request, CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        const string sql = """
            WITH draft AS (
                UPDATE valorapesquisa.form_versions fv SET row_version=row_version+1,updated_at=now()
                 FROM valorapesquisa.forms f,valorapesquisa.form_section_versions s
                 WHERE f.id=@formId AND f.organization_id=@organizationId AND f.current_draft_version_id=fv.id
                   AND s.id=@sectionId AND s.form_version_id=fv.id AND s.deleted_at IS NULL
                   AND fv.status='draft' AND fv.row_version=@expectedVersion
                 RETURNING fv.id
            )
            INSERT INTO valorapesquisa.question_versions(id,organization_id,section_id,code,type,title,description,required,dimension_code,weight,position,display_order,settings,version)
            SELECT @id,@organizationId,@sectionId,@code,@type,@title,@description,@required,@dimensionCode,@weight,@position,@position,CAST(@settings AS jsonb),1 FROM draft
            RETURNING id AS "Id",section_id AS "SectionId",code AS "Code",type AS "Type",title AS "Title",description AS "Description",required AS "Required",dimension_code AS "DimensionCode",weight AS "Weight",position::int AS "Position",settings::text AS "Settings",version::bigint AS "Version";
            """;
        var id = Guid.NewGuid();
        var row = await unit.Connection.QuerySingleOrDefaultAsync<QuestionRow>(new CommandDefinition(sql, new
        {
            id, organizationId, formId, request.SectionId, request.Code, request.Type, request.Title, request.Description,
            request.Required, request.DimensionCode, request.Weight, request.Position, settings = NullIfEmpty(request.Settings) ?? "{}",
            request.ExpectedVersion
        }, unit.Transaction, cancellationToken: cancellationToken));
        if (row is null) return null;
        await AuditAsync(unit, organizationId, userId, "form.question.created", "question", id, cancellationToken);
        await unit.CommitAsync();
        return ToQuestion(row, []);
    }

    public async Task<QuestionResponse?> UpdateQuestionAsync(Guid organizationId, Guid formId, Guid questionId, Guid userId,
        UpdateQuestionRequest request, CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        const string sql = """
            UPDATE valorapesquisa.question_versions q
               SET code=@code,type=@type,title=@title,description=@description,required=@required,
                   dimension_code=@dimensionCode,weight=@weight,settings=CAST(@settings AS jsonb),version=version+1,updated_at=now()
              FROM valorapesquisa.form_section_versions s,valorapesquisa.form_versions fv,valorapesquisa.forms f
             WHERE q.id=@questionId AND q.section_id=s.id AND s.form_version_id=fv.id AND fv.form_id=f.id
               AND f.id=@formId AND f.organization_id=@organizationId AND f.current_draft_version_id=fv.id
               AND fv.status='draft' AND q.deleted_at IS NULL AND q.version=@expectedVersion
            RETURNING q.id AS "Id",q.section_id AS "SectionId",q.code AS "Code",q.type AS "Type",q.title AS "Title",q.description AS "Description",q.required AS "Required",q.dimension_code AS "DimensionCode",q.weight AS "Weight",q.position::int AS "Position",q.settings::text AS "Settings",q.version::bigint AS "Version";
            """;
        var row = await unit.Connection.QuerySingleOrDefaultAsync<QuestionRow>(new CommandDefinition(sql, new
        {
            organizationId, formId, questionId, request.Code, request.Type, request.Title, request.Description,
            request.Required, request.DimensionCode, request.Weight, settings = NullIfEmpty(request.Settings) ?? "{}", request.ExpectedVersion
        }, unit.Transaction, cancellationToken: cancellationToken));
        if (row is null) return null;
        var draftId = await DraftIdForQuestionAsync(unit, questionId, cancellationToken);
        await TouchDraftAsync(unit, draftId, cancellationToken);
        await AuditAsync(unit, organizationId, userId, "form.question.updated", "question", questionId, cancellationToken);
        await unit.CommitAsync();
        return ToQuestion(row, []);
    }

    public Task<bool> DeleteQuestionAsync(Guid organizationId, Guid formId, Guid questionId, Guid userId,
        DeleteQuestionRequest request, CancellationToken cancellationToken) => DeleteStructuralAsync(
            organizationId, formId, questionId, userId, request.ExpectedVersion, "question_versions",
            "(SELECT s.form_version_id FROM valorapesquisa.form_section_versions s WHERE s.id=i.section_id)",
            "form.question.archived", "question", cancellationToken);

    public async Task<QuestionOptionResponse?> CreateOptionAsync(Guid organizationId, Guid formId, Guid questionId, Guid userId,
        CreateQuestionOptionRequest request, CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        const string sql = """
            WITH draft AS (
                UPDATE valorapesquisa.form_versions fv SET row_version=row_version+1,updated_at=now()
                 FROM valorapesquisa.forms f,valorapesquisa.form_section_versions s,valorapesquisa.question_versions q
                 WHERE f.id=@formId AND f.organization_id=@organizationId AND f.current_draft_version_id=fv.id
                   AND q.id=@questionId AND q.section_id=s.id AND s.form_version_id=fv.id
                   AND q.deleted_at IS NULL AND s.deleted_at IS NULL AND fv.status='draft' AND fv.row_version=@expectedVersion
                 RETURNING fv.id
            )
            INSERT INTO valorapesquisa.question_option_versions(id,organization_id,question_id,label,value,score,position,display_order,version)
            SELECT @id,@organizationId,@questionId,@label,@value,@score,@position,@position,1 FROM draft
            RETURNING id AS "Id",question_id AS "QuestionId",label AS "Label",value AS "Value",score AS "Score",position::int AS "Position",version::bigint AS "Version";
            """;
        var id = Guid.NewGuid();
        var row = await unit.Connection.QuerySingleOrDefaultAsync<OptionRow>(new CommandDefinition(sql,
            new { id, organizationId, formId, questionId, request.Label, request.Value, request.Score, request.Position, request.ExpectedVersion }, unit.Transaction, cancellationToken: cancellationToken));
        if (row is null) return null;
        await AuditAsync(unit, organizationId, userId, "form.option.created", "question_option", id, cancellationToken);
        await unit.CommitAsync();
        return ToOption(row);
    }

    public async Task<QuestionOptionResponse?> UpdateOptionAsync(Guid organizationId, Guid formId, Guid optionId, Guid userId,
        UpdateQuestionOptionRequest request, CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        const string sql = """
            UPDATE valorapesquisa.question_option_versions o
               SET label=@label,value=@value,score=@score,version=version+1,updated_at=now()
              FROM valorapesquisa.question_versions q,valorapesquisa.form_section_versions s,valorapesquisa.form_versions fv,valorapesquisa.forms f
             WHERE o.id=@optionId AND o.question_id=q.id AND q.section_id=s.id AND s.form_version_id=fv.id AND fv.form_id=f.id
               AND f.id=@formId AND f.organization_id=@organizationId AND f.current_draft_version_id=fv.id
               AND fv.status='draft' AND o.deleted_at IS NULL AND o.version=@expectedVersion
            RETURNING o.id AS "Id",o.question_id AS "QuestionId",o.label AS "Label",o.value AS "Value",o.score AS "Score",o.position::int AS "Position",o.version::bigint AS "Version";
            """;
        var row = await unit.Connection.QuerySingleOrDefaultAsync<OptionRow>(new CommandDefinition(sql,
            new { organizationId, formId, optionId, request.Label, request.Value, request.Score, request.ExpectedVersion }, unit.Transaction, cancellationToken: cancellationToken));
        if (row is null) return null;
        var draftId = await DraftIdForOptionAsync(unit, optionId, cancellationToken);
        await TouchDraftAsync(unit, draftId, cancellationToken);
        await AuditAsync(unit, organizationId, userId, "form.option.updated", "question_option", optionId, cancellationToken);
        await unit.CommitAsync();
        return ToOption(row);
    }

    public Task<bool> DeleteOptionAsync(Guid organizationId, Guid formId, Guid optionId, Guid userId,
        DeleteQuestionOptionRequest request, CancellationToken cancellationToken) => DeleteStructuralAsync(
            organizationId, formId, optionId, userId, request.ExpectedVersion, "question_option_versions",
            "(SELECT s.form_version_id FROM valorapesquisa.question_versions q JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id WHERE q.id=i.question_id)",
            "form.option.archived", "question_option", cancellationToken);

    public async Task<FormDetailResponse?> PreviewAsync(Guid organizationId, Guid formId, CancellationToken cancellationToken) =>
        await GetAsync(organizationId, formId, cancellationToken);

    public async Task<FormDetailResponse?> DuplicateAsync(Guid organizationId, Guid formId, Guid userId,
        DuplicateFormRequest request, CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        const string sourceSql = """
            SELECT f.id AS "Id",f.name AS "Name",f.description AS "Description",f.category AS "Category",
                   f.estimated_minutes::int AS "EstimatedMinutes",COALESCE(f.current_draft_version_id,f.latest_published_version_id) AS "SourceVersionId"
              FROM valorapesquisa.forms f
             WHERE f.id=@formId AND f.organization_id=@organizationId AND f.deleted_at IS NULL
               AND f.version=@expectedVersion AND COALESCE(f.current_draft_version_id,f.latest_published_version_id) IS NOT NULL;
            """;
        var source = await unit.Connection.QuerySingleOrDefaultAsync<CloneSourceRow>(new CommandDefinition(sourceSql,
            new { organizationId, formId, request.ExpectedVersion }, unit.Transaction, cancellationToken: cancellationToken));
        if (source is null) return null;
        var newFormId = Guid.NewGuid(); var newVersionId = Guid.NewGuid(); var code = $"org-{organizationId:N}-{newFormId:N}";
        const string insert = """
            INSERT INTO valorapesquisa.forms(id,code,title,slug,form_key,organization_id,name,description,category,estimated_minutes,status,current_draft_version_id,created_by_user_id,version,created_at,updated_at)
            VALUES(@newFormId,@code,@name,@code,@code,@organizationId,@name,@description,@category,@estimatedMinutes,'draft',@newVersionId,@userId,1,now(),now());
            INSERT INTO valorapesquisa.form_versions(id,organization_id,form_id,version,version_number,status,is_immutable,maximum_score,max_score,row_version,created_at,updated_at)
            VALUES(@newVersionId,@organizationId,@newFormId,1,1,'draft',false,0,0,1,now(),now());
            """;
        var name = string.IsNullOrWhiteSpace(request.Name) ? $"{source.Name} (cópia)" : request.Name.Trim();
        await unit.Connection.ExecuteAsync(new CommandDefinition(insert, new { newFormId, newVersionId, code, organizationId, userId, name, source.Description, source.Category, source.EstimatedMinutes }, unit.Transaction, cancellationToken: cancellationToken));
        await CloneVersionContentAsync(unit, organizationId, source.SourceVersionId, newVersionId, cancellationToken);
        await AuditAsync(unit, organizationId, userId, "form.duplicated", "form", newFormId, cancellationToken);
        await unit.CommitAsync();
        return await GetAsync(organizationId, newFormId, cancellationToken);
    }

    public async Task<FormDetailResponse?> CreateDraftVersionAsync(Guid organizationId, Guid formId, Guid userId,
        CreateFormVersionRequest request, CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        const string sourceSql = """
            SELECT f.latest_published_version_id AS "SourceVersionId",COALESCE(MAX(fv.version_number),0)::int+1 AS "NextVersion"
              FROM valorapesquisa.forms f JOIN valorapesquisa.form_versions fv ON fv.form_id=f.id AND fv.deleted_at IS NULL
             WHERE f.id=@formId AND f.organization_id=@organizationId AND f.deleted_at IS NULL
               AND f.current_draft_version_id IS NULL AND f.latest_published_version_id IS NOT NULL AND f.version=@expectedVersion
             GROUP BY f.latest_published_version_id;
            """;
        var source = await unit.Connection.QuerySingleOrDefaultAsync<DraftSourceRow>(new CommandDefinition(sourceSql,
            new { organizationId, formId, expectedVersion = request.ExpectedFormVersion }, unit.Transaction, cancellationToken: cancellationToken));
        if (source is null) return null;
        var versionId = Guid.NewGuid();
        const string insert = """
            INSERT INTO valorapesquisa.form_versions(id,organization_id,form_id,version,version_number,status,is_immutable,maximum_score,max_score,row_version,created_at,updated_at)
            VALUES(@versionId,@organizationId,@formId,@nextVersion,@nextVersion,'draft',false,0,0,1,now(),now());
            UPDATE valorapesquisa.forms SET current_draft_version_id=@versionId,status='draft',version=version+1,updated_at=now()
             WHERE id=@formId AND organization_id=@organizationId AND version=@expectedVersion;
            """;
        await unit.Connection.ExecuteAsync(new CommandDefinition(insert, new { versionId, organizationId, formId, nextVersion = source.NextVersion, expectedVersion = request.ExpectedFormVersion }, unit.Transaction, cancellationToken: cancellationToken));
        await CloneVersionContentAsync(unit, organizationId, source.SourceVersionId, versionId, cancellationToken);
        await AuditAsync(unit, organizationId, userId, "form.version.created", "form", formId, cancellationToken);
        await unit.CommitAsync();
        return await GetAsync(organizationId, formId, cancellationToken);
    }

    private async Task<bool> DeleteStructuralAsync(Guid organizationId, Guid formId, Guid itemId, Guid userId,
        long expectedVersion, string table, string versionExpression, string action, string entityType,
        CancellationToken cancellationToken)
    {
        await using var unit = await transactions.BeginAsync(cancellationToken);
        var sql = $"UPDATE valorapesquisa.{table} i SET deleted_at=now(),updated_at=now(),version=version+1 FROM valorapesquisa.form_versions fv,valorapesquisa.forms f WHERE i.id=@itemId AND fv.id={versionExpression} AND f.id=fv.form_id AND f.id=@formId AND f.organization_id=@organizationId AND f.current_draft_version_id=fv.id AND fv.status='draft' AND i.deleted_at IS NULL AND i.version=@expectedVersion RETURNING fv.id;";
        var draftId = await unit.Connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql,
            new { organizationId, formId, itemId, expectedVersion }, unit.Transaction, cancellationToken: cancellationToken));
        if (draftId is null) return false;
        await TouchDraftAsync(unit, draftId.Value, cancellationToken);
        await AuditAsync(unit, organizationId, userId, action, entityType, itemId, cancellationToken);
        await unit.CommitAsync();
        return true;
    }

    private static Task TouchDraftAsync(IUnitOfWork unit, Guid draftId, CancellationToken cancellationToken) =>
        unit.Connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.form_versions SET row_version=row_version+1,updated_at=now() WHERE id=@draftId AND status='draft'", new { draftId }, unit.Transaction, cancellationToken: cancellationToken));

    private static Task<Guid> DraftIdForQuestionAsync(IUnitOfWork unit, Guid questionId, CancellationToken cancellationToken) =>
        unit.Connection.ExecuteScalarAsync<Guid>(new CommandDefinition("SELECT s.form_version_id FROM valorapesquisa.question_versions q JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id WHERE q.id=@questionId", new { questionId }, unit.Transaction, cancellationToken: cancellationToken));

    private static Task<Guid> DraftIdForOptionAsync(IUnitOfWork unit, Guid optionId, CancellationToken cancellationToken) =>
        unit.Connection.ExecuteScalarAsync<Guid>(new CommandDefinition("SELECT s.form_version_id FROM valorapesquisa.question_option_versions o JOIN valorapesquisa.question_versions q ON q.id=o.question_id JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id WHERE o.id=@optionId", new { optionId }, unit.Transaction, cancellationToken: cancellationToken));

    private Task AuditAsync(IUnitOfWork unit, Guid organizationId, Guid userId, string action, string entityType,
        Guid entityId, CancellationToken cancellationToken) => audit.LogAsync(new AuditEntry(organizationId, userId,
            action, entityType, entityId.ToString(), "Operação estrutural de formulário concluída.", "{}"), unit.Transaction);

    private static QuestionResponse ToQuestion(QuestionRow row, IReadOnlyList<QuestionOptionResponse> options) =>
        new(row.Id, row.SectionId, row.Code, row.Type, row.Title, row.Description, row.Required,
            row.DimensionCode, row.Weight, row.Position, row.Settings, row.Version, options);

    private static QuestionOptionResponse ToOption(OptionRow row) =>
        new(row.Id, row.QuestionId, row.Label, row.Value, row.Score, row.Position, row.Version);

    private static async Task CloneVersionContentAsync(IUnitOfWork unit, Guid organizationId, Guid sourceVersionId,
        Guid targetVersionId, CancellationToken cancellationToken)
    {
        var sections = (await unit.Connection.QueryAsync<SectionRow>(new CommandDefinition(
            "SELECT id AS \"Id\",form_version_id AS \"FormVersionId\",title AS \"Title\",description AS \"Description\",position::int AS \"Position\",version::bigint AS \"Version\" FROM valorapesquisa.form_section_versions WHERE form_version_id=@sourceVersionId AND deleted_at IS NULL ORDER BY position,id",
            new { sourceVersionId }, unit.Transaction, cancellationToken: cancellationToken))).AsList();
        foreach (var section in sections)
        {
            var newSectionId = Guid.NewGuid();
            await unit.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.form_section_versions(id,organization_id,form_version_id,title,description,position,display_order,version) VALUES(@newSectionId,@organizationId,@targetVersionId,@Title,@Description,@Position,@Position,1)", new { newSectionId, organizationId, targetVersionId, section.Title, section.Description, section.Position }, unit.Transaction, cancellationToken: cancellationToken));
            var questions = (await unit.Connection.QueryAsync<QuestionRow>(new CommandDefinition(
                "SELECT id AS \"Id\",section_id AS \"SectionId\",code AS \"Code\",type AS \"Type\",title AS \"Title\",description AS \"Description\",required AS \"Required\",dimension_code AS \"DimensionCode\",weight AS \"Weight\",position::int AS \"Position\",settings::text AS \"Settings\",version::bigint AS \"Version\" FROM valorapesquisa.question_versions WHERE section_id=@sectionId AND deleted_at IS NULL ORDER BY position,id",
                new { sectionId = section.Id }, unit.Transaction, cancellationToken: cancellationToken))).AsList();
            foreach (var question in questions)
            {
                var newQuestionId = Guid.NewGuid();
                await unit.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.question_versions(id,organization_id,section_id,code,type,title,description,required,dimension_code,weight,position,display_order,settings,version) VALUES(@newQuestionId,@organizationId,@newSectionId,@Code,@Type,@Title,@Description,@Required,@DimensionCode,@Weight,@Position,@Position,CAST(@Settings AS jsonb),1)", new { newQuestionId, organizationId, newSectionId, question.Code, question.Type, question.Title, question.Description, question.Required, question.DimensionCode, question.Weight, question.Position, question.Settings }, unit.Transaction, cancellationToken: cancellationToken));
                var options = await unit.Connection.QueryAsync<OptionRow>(new CommandDefinition(
                    "SELECT id AS \"Id\",question_id AS \"QuestionId\",label AS \"Label\",value AS \"Value\",score AS \"Score\",position::int AS \"Position\",version::bigint AS \"Version\" FROM valorapesquisa.question_option_versions WHERE question_id=@questionId AND deleted_at IS NULL ORDER BY position,id",
                    new { questionId = question.Id }, unit.Transaction, cancellationToken: cancellationToken));
                foreach (var option in options)
                    await unit.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.question_option_versions(id,organization_id,question_id,label,value,score,position,display_order,version) VALUES(@id,@organizationId,@newQuestionId,@Label,@Value,@Score,@Position,@Position,1)", new { id = Guid.NewGuid(), organizationId, newQuestionId, option.Label, option.Value, option.Score, option.Position }, unit.Transaction, cancellationToken: cancellationToken));
            }
        }
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
        public long? DraftVersion { get; init; }
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

    private sealed record CloneSourceRow(Guid Id, string Name, string? Description, string? Category,
        int EstimatedMinutes, Guid SourceVersionId);

    private sealed record DraftSourceRow(Guid SourceVersionId, int NextVersion);
}
