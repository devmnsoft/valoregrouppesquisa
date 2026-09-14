using Valora.Application.ActionCenter;
using Valora.Application.Workspace;

namespace Valora.Tests;

public sealed class ActionCenterPagingTests
{
    [Theory]
    [InlineData("in_progress", "Em execução")]
    [InlineData("blocked", "Bloqueada")]
    [InlineData("completed", "Concluída")]
    [InlineData("critical", "Crítica")]
    public void Action_labels_are_shared_between_views(string code, string expected)
    {
        Assert.Equal(expected, ActionStatuses.Label(code));
    }

    [Theory]
    [InlineData(-4, 500, 1, 50)]
    [InlineData(2, 25, 2, 25)]
    public void Item_query_normalizes_invalid_page_parameters(int page, int size, int expectedPage, int expectedSize)
    {
        var query = new ActionItemListQuery(Page: page, PageSize: size);

        Assert.Equal(expectedPage, query.ValidPage);
        Assert.Equal(expectedSize, query.ValidPageSize);
    }

    [Theory]
    [InlineData(0, 1, 1, false, false)]
    [InlineData(21, 1, 3, false, true)]
    [InlineData(21, 2, 3, true, true)]
    [InlineData(21, 3, 3, true, false)]
    [InlineData(21, 999, 3, true, false)]
    public void Page_result_exposes_accessible_navigation(int total, int page, int pages, bool previous, bool next)
    {
        var result = new PageResult<int>(Array.Empty<int>(), page, 10, total);

        Assert.Equal(pages, result.TotalPages);
        Assert.Equal(previous, result.HasPrevious);
        Assert.Equal(next, result.HasNext);
    }
}
