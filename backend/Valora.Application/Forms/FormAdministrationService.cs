namespace Valora.Application.Forms;

public sealed class FormAdministrationService(IFormAdministrationRepository repository) : IFormAdministrationService {
    private static readonly HashSet<string> ItemTypes = ["section", "question", "option"];

    public Task<FormListResponse> ListAsync(Guid organizationId, FormListQuery query, CancellationToken cancellationToken) {
        var status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim().ToLowerInvariant();
        if (status is not null && status is not ("draft" or "published" or "archived"))
            throw new ArgumentException("Status de formulário inválido.", nameof(query));
        return repository.ListAsync(RequireOrganization(organizationId), query with {
            Search = NullIfWhiteSpace(query.Search), Category = NullIfWhiteSpace(query.Category), Status = status,
            Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100)
        }, cancellationToken);
    }

    public Task<FormDetailResponse?> GetAsync(Guid organizationId, Guid formId, CancellationToken cancellationToken) =>
        repository.GetAsync(RequireOrganization(organizationId), formId, cancellationToken);

    public async Task<FormPublicationReviewResponse?> ReviewPublicationAsync(Guid organizationId, Guid formId, CancellationToken cancellationToken) {
        var detail = await repository.GetAsync(RequireOrganization(organizationId), formId, cancellationToken);
        if (detail is null || detail.CurrentDraftVersionId is null) return null;
        var questions = detail.Sections.SelectMany(section => section.Questions).ToList();
        var respondable = questions.Where(question => !IsInformational(question.Type)).ToList();
        var blockers = new List<PublicationReviewIssue>();
        var warnings = new List<PublicationReviewIssue>();
        if (string.IsNullOrWhiteSpace(detail.Name)) blockers.Add(new("FORM_NAME", "Informe o nome do formulário.", "form", detail.Id));
        if (string.IsNullOrWhiteSpace(detail.Category)) blockers.Add(new("FORM_CATEGORY", "Selecione a categoria do formulário.", "form", detail.Id));
        if (detail.Sections.Count == 0) blockers.Add(new("NO_SECTIONS", "Adicione ao menos uma seção ativa.", "form", detail.Id));
        if (respondable.Count == 0) blockers.Add(new("NO_RESPONDABLE_QUESTIONS", "Adicione ao menos uma pergunta respondível em uma seção ativa.", "form", detail.Id));
        foreach (var question in respondable) {
            if (string.IsNullOrWhiteSpace(question.Title)) blockers.Add(new("QUESTION_TITLE", "Informe o título da pergunta.", "question", question.Id));
            if (question.Type is "single_choice" or "multiple_choice" && question.Options.Count == 0)
                blockers.Add(new("QUESTION_OPTIONS", "Adicione ao menos uma opção de resposta.", "question", question.Id));
            if (string.IsNullOrWhiteSpace(question.DimensionCode)) warnings.Add(new("QUESTION_DIMENSION", "Vincule a pergunta a uma dimensão para incluí-la na leitura metodológica.", "question", question.Id));
            if (question.Weight == 0) warnings.Add(new("QUESTION_WEIGHT", "Esta pergunta tem peso zero e não contribuirá para a pontuação.", "question", question.Id));
        }
        return new(detail.Id, detail.CurrentDraftVersionId.Value, detail.SelectedVersionNumber, detail.Sections.Count,
            respondable.Count, questions.Count - respondable.Count, respondable.Count(question => !string.IsNullOrWhiteSpace(question.DimensionCode)), blockers, warnings);
    }

    public Task<IReadOnlyList<FormDimensionCatalogItem>> ListDimensionsAsync(Guid organizationId, Guid formId, CancellationToken cancellationToken) =>
        repository.ListDimensionsAsync(RequireOrganization(organizationId), formId, cancellationToken);

    public Task<FormDetailResponse> CreateAsync(Guid organizationId, Guid userId, CreateFormRequest request, CancellationToken cancellationToken) {
        RequireUser(userId);
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Informe o nome do formulário.", nameof(request));
        ValidateTextLengths(request.Name, request.Description, request.Category);
        if (request.EstimatedMinutes is < 1 or > 480) throw new ArgumentException("O tempo estimado deve estar entre 1 e 480 minutos.", nameof(request));
        return repository.CreateAsync(RequireOrganization(organizationId), userId, request with {
            Name = request.Name.Trim(), Description = request.Description?.Trim(), Category = request.Category?.Trim()
        }, cancellationToken);
    }

    public Task<FormDetailResponse?> UpdateAsync(Guid organizationId, Guid formId, UpdateFormRequest request, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Informe o nome do formulário.", nameof(request));
        ValidateTextLengths(request.Name, request.Description, request.Category);
        if (request.EstimatedMinutes is < 1 or > 480) throw new ArgumentException("O tempo estimado deve estar entre 1 e 480 minutos.", nameof(request));
        return repository.UpdateAsync(RequireOrganization(organizationId), formId, request with {
            Name = request.Name.Trim(), Description = request.Description?.Trim(), Category = request.Category?.Trim()
        }, cancellationToken);
    }

    public Task<bool> ArchiveAsync(Guid organizationId, Guid formId, Guid userId, ArchiveFormRequest request, CancellationToken cancellationToken) =>
        repository.ArchiveAsync(RequireOrganization(organizationId), formId, RequireUser(userId), request, cancellationToken);

    public Task<FormVersionResponse?> PublishAsync(Guid organizationId, Guid formId, Guid userId, PublishFormVersionRequest request, CancellationToken cancellationToken) =>
        repository.PublishVersionAsync(RequireOrganization(organizationId), formId, RequireUser(userId), request, cancellationToken);

    public Task<ReorderFormItemResponse?> ReorderAsync(Guid organizationId, Guid formId, ReorderFormItemRequest request, CancellationToken cancellationToken) {
        if (!ItemTypes.Contains(request.ItemType)) throw new ArgumentException("Tipo de item inválido.", nameof(request));
        if (request.NewPosition < 0) throw new ArgumentException("A posição deve ser positiva.", nameof(request));
        return repository.ReorderAsync(RequireOrganization(organizationId), formId, request, cancellationToken);
    }

    public Task<FormSectionResponse?> CreateSectionAsync(Guid organizationId, Guid formId, Guid userId,
        CreateFormSectionRequest request, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(request.Title)) throw new ArgumentException("Informe o título da seção.", nameof(request));
        if (request.Position < 0) throw new ArgumentException("A posição deve ser positiva.", nameof(request));
        return repository.CreateSectionAsync(RequireOrganization(organizationId), formId, RequireUser(userId),
            request with { Title = request.Title.Trim() }, cancellationToken);
    }

    public Task<FormSectionResponse?> UpdateSectionAsync(Guid organizationId, Guid formId, Guid sectionId, Guid userId,
        UpdateFormSectionRequest request, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(request.Title)) throw new ArgumentException("Informe o título da seção.", nameof(request));
        return repository.UpdateSectionAsync(RequireOrganization(organizationId), formId, sectionId, RequireUser(userId),
            request with { Title = request.Title.Trim() }, cancellationToken);
    }

    public Task<bool> DeleteSectionAsync(Guid organizationId, Guid formId, Guid sectionId, Guid userId,
        DeleteFormSectionRequest request, CancellationToken cancellationToken) =>
        repository.DeleteSectionAsync(RequireOrganization(organizationId), formId, sectionId, RequireUser(userId), request, cancellationToken);

    public Task<QuestionResponse?> CreateQuestionAsync(Guid organizationId, Guid formId, Guid userId,
        CreateQuestionRequest request, CancellationToken cancellationToken) {
        ValidateQuestion(request.Code, request.Type, request.Title, request.Weight, request.Settings);
        return repository.CreateQuestionAsync(RequireOrganization(organizationId), formId, RequireUser(userId), request with {
            Code = request.Code.Trim().ToLowerInvariant(),
            Title = request.Title.Trim()
        }, cancellationToken);
    }

    public Task<QuestionResponse?> UpdateQuestionAsync(Guid organizationId, Guid formId, Guid questionId, Guid userId,
        UpdateQuestionRequest request, CancellationToken cancellationToken) {
        ValidateQuestion(request.Code, request.Type, request.Title, request.Weight, request.Settings);
        return repository.UpdateQuestionAsync(RequireOrganization(organizationId), formId, questionId, RequireUser(userId), request with {
            Code = request.Code.Trim().ToLowerInvariant(),
            Title = request.Title.Trim()
        }, cancellationToken);
    }

    public Task<bool> DeleteQuestionAsync(Guid organizationId, Guid formId, Guid questionId, Guid userId,
        DeleteQuestionRequest request, CancellationToken cancellationToken) =>
        repository.DeleteQuestionAsync(RequireOrganization(organizationId), formId, questionId, RequireUser(userId), request, cancellationToken);

    public Task<QuestionOptionResponse?> CreateOptionAsync(Guid organizationId, Guid formId, Guid questionId, Guid userId,
        CreateQuestionOptionRequest request, CancellationToken cancellationToken) {
        ValidateOption(request.Label, request.Value, request.Score, request.Position);
        return repository.CreateOptionAsync(RequireOrganization(organizationId), formId, questionId, RequireUser(userId), request with {
            Label = request.Label.Trim(),
            Value = request.Value.Trim()
        }, cancellationToken);
    }

    public Task<QuestionOptionResponse?> UpdateOptionAsync(Guid organizationId, Guid formId, Guid optionId, Guid userId,
        UpdateQuestionOptionRequest request, CancellationToken cancellationToken) {
        ValidateOption(request.Label, request.Value, request.Score, 0);
        return repository.UpdateOptionAsync(RequireOrganization(organizationId), formId, optionId, RequireUser(userId), request with {
            Label = request.Label.Trim(),
            Value = request.Value.Trim()
        }, cancellationToken);
    }

    public Task<bool> DeleteOptionAsync(Guid organizationId, Guid formId, Guid optionId, Guid userId,
        DeleteQuestionOptionRequest request, CancellationToken cancellationToken) =>
        repository.DeleteOptionAsync(RequireOrganization(organizationId), formId, optionId, RequireUser(userId), request, cancellationToken);

    public Task<FormDetailResponse?> DuplicateAsync(Guid organizationId, Guid formId, Guid userId,
        DuplicateFormRequest request, CancellationToken cancellationToken) =>
        repository.DuplicateAsync(RequireOrganization(organizationId), formId, RequireUser(userId), request, cancellationToken);

    public Task<FormDetailResponse?> CreateDraftVersionAsync(Guid organizationId, Guid formId, Guid userId,
        CreateFormVersionRequest request, CancellationToken cancellationToken) =>
        repository.CreateDraftVersionAsync(RequireOrganization(organizationId), formId, RequireUser(userId), request, cancellationToken);

    public Task<FormDetailResponse?> PreviewAsync(Guid organizationId, Guid formId, CancellationToken cancellationToken) =>
        repository.PreviewAsync(RequireOrganization(organizationId), formId, cancellationToken);

    private static void ValidateQuestion(string code, string type, string title, decimal weight, string? settings) {
        string[] allowedTypes = ["likert_1_5", "single_choice", "multiple_choice", "short_text", "long_text", "heading", "description", "separator"];
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Código e título da pergunta são obrigatórios.");
        if (!allowedTypes.Contains(type, StringComparer.Ordinal)) throw new ArgumentException("Tipo de pergunta inválido.");
        if (weight < 0) throw new ArgumentException("O peso da pergunta não pode ser negativo.");
        if (!string.IsNullOrWhiteSpace(settings))
            try { System.Text.Json.JsonDocument.Parse(settings); }
            catch (System.Text.Json.JsonException) { throw new ArgumentException("As configurações da pergunta devem conter JSON válido."); }
    }

    private static void ValidateOption(string label, string value, decimal? score, int position) {
        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Rótulo e valor da opção são obrigatórios.");
        if (score is < 1 or > 5) throw new ArgumentException("A pontuação da opção deve estar entre 1 e 5.");
        if (position < 0) throw new ArgumentException("A posição deve ser positiva.");
    }

    private static void ValidateTextLengths(string name, string? description, string? category) {
        if (name.Trim().Length < 3) throw new ArgumentException("O nome deve ter pelo menos 3 caracteres.");
        if (name.Trim().Length > 160) throw new ArgumentException("O nome deve ter no máximo 160 caracteres.");
        if (description?.Length > 1000) throw new ArgumentException("A descrição deve ter no máximo 1000 caracteres.");
        if (category?.Length > 80) throw new ArgumentException("A categoria deve ter no máximo 80 caracteres.");
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsInformational(string type) => type is "heading" or "description" or "separator";

    private static Guid RequireOrganization(Guid organizationId) => organizationId != Guid.Empty
        ? organizationId
        : throw new UnauthorizedAccessException("O contexto da organização é obrigatório.");

    private static Guid RequireUser(Guid userId) => userId != Guid.Empty
        ? userId
        : throw new UnauthorizedAccessException("Não foi possível identificar o usuário responsável pela operação.");
}
