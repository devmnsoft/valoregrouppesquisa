namespace Valora.Application.Contracts;
public interface IMenuService { Task<IReadOnlyList<Valora.Application.DTOs.MenuItemDto>> GetMenuAsync(Guid userId,Guid? organizationId); }
