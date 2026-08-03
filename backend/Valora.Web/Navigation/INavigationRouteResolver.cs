namespace Valora.Web.Navigation;

public interface INavigationRouteResolver
{
    string? Resolve(NavigationDestination destination);
}
