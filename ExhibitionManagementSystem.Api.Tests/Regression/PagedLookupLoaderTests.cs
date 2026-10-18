using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class PagedLookupLoaderTests
{
    [Fact]
    public async Task LoadsEveryPageWhenTheServerCapsPageSize()
    {
        var calls = new List<int>();
        var result = await PagedLookupLoader.LoadAllAsync<int>((page, _) =>
        {
            calls.Add(page);
            return Task.FromResult(ServiceResult<PagedResultDto<int>>.Success(new()
            {
                PageNumber = page, PageSize = 37, TotalCount = 451,
                Items = Enumerable.Range(1, 451).Skip((page - 1) * 37).Take(37).ToList()
            }));
        });
        Assert.True(result.IsSuccess);
        Assert.Equal(Enumerable.Range(1, 451), result.Data!);
        Assert.Equal(Enumerable.Range(1, 13), calls);
    }

    [Fact]
    public async Task LaterPageFailureNeverReturnsAPartialLookup()
    {
        var result = await PagedLookupLoader.LoadAllAsync<int>((page, _) => Task.FromResult(page == 1
            ? ServiceResult<PagedResultDto<int>>.Success(new() { PageNumber = 1, PageSize = 1, TotalCount = 2, Items = [1] })
            : ServiceResult<PagedResultDto<int>>.Failure("Synthetic failure", "UNAVAILABLE")));
        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal("UNAVAILABLE", result.ErrorCode);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    public async Task RejectsInvalidOrIncompletePages(int number, int size)
    {
        var result = await PagedLookupLoader.LoadAllAsync<int>((_, _) => Task.FromResult(
            ServiceResult<PagedResultDto<int>>.Success(new() { PageNumber = number, PageSize = size, TotalCount = 2 })));
        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal("INCOMPLETE_LOOKUP", result.ErrorCode);
    }

    [Fact]
    public async Task EmptyLookupIsSuccessful()
    {
        var result = await PagedLookupLoader.LoadAllAsync<int>((page, size) => Task.FromResult(
            ServiceResult<PagedResultDto<int>>.Success(new() { PageNumber = page, PageSize = size })));
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }
}
