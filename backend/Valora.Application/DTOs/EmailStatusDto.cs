namespace Valora.Application.DTOs;

public sealed record EmailStatusDto(int Queued,int Processing,int Sent,int Failed,int DeadLetter,bool DevelopmentOutbox);
