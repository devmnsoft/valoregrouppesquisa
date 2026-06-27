namespace Valora.Application.ReadModels;
public sealed record QuestionPublicReadModel(Guid Id,Guid FormId,Guid? DimensionId,string Text,string Type,bool Required,decimal MaxScore,int DisplayOrder);
