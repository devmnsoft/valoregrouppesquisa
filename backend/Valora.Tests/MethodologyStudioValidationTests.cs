using System.ComponentModel.DataAnnotations;
using Valora.Application.Methodology;

namespace Valora.Tests;

public sealed class MethodologyStudioValidationTests
{
    [Fact]
    public void Evaluative_question_requires_methodological_mapping_and_positive_weight()
    {
        var request = new CreateQuestionBankItemRequest("Q1", "Como a prática é evidenciada?", "scale_1_5", 0, [], [], true);
        Assert.Throws<ValidationException>(() => MethodologyValidationService.EnsureQuestion(request));
    }

    [Fact]
    public void Valid_question_is_accepted()
    {
        var request = new CreateQuestionBankItemRequest("Q1", "Como a prática é evidenciada?", "scale_1_5", 1, [Guid.NewGuid()], [], true);
        MethodologyValidationService.EnsureQuestion(request);
    }

    [Fact]
    public void Concept_requires_dimension_and_evidence()
    {
        var request = new CreateConceptRequest("culture", "Cultura", [], "");
        Assert.Throws<ValidationException>(() => MethodologyValidationService.EnsureConcept(request));
    }

    [Fact]
    public void Template_requires_sections_and_versioned_scoring_rule()
    {
        var request = new CreateDiagnosticTemplateRequest("BASE", "Diagnóstico base", [Guid.NewGuid()], null);
        Assert.Throws<ValidationException>(() => MethodologyValidationService.EnsureTemplate(request));
    }
}
