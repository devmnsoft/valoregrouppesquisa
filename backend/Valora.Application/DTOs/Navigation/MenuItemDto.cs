namespace Valora.Application.DTOs;

public sealed record MenuItemDto(string Code,string Label,string Url,string Icon,int Order,IReadOnlyList<MenuItemDto> Children);
