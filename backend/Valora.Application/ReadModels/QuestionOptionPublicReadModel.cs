namespace Valora.Application.ReadModels;
public sealed record QuestionOptionPublicReadModel(Guid Id,Guid QuestionId,string Text,decimal? Score,int DisplayOrder);
