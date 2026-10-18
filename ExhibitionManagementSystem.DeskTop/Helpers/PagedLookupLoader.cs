using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.DeskTop.Helpers;

public static class PagedLookupLoader
{
    public static async Task<ServiceResult<IReadOnlyList<T>>> LoadAllAsync<T>(
        Func<int, int, Task<ServiceResult<PagedResultDto<T>>>> fetchPage, int pageSize = 100)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        var items = new List<T>();
        for (var pageNumber = 1; ; pageNumber++)
        {
            var response = await fetchPage(pageNumber, pageSize);
            if (!response.IsSuccess || response.Data == null)
                return ServiceResult<IReadOnlyList<T>>.Failure(response.ErrorMessage ?? "تعذر تحميل القائمة كاملة", response.ErrorCode);

            var page = response.Data;
            if (page.PageNumber != pageNumber || page.PageSize <= 0 || page.TotalCount < 0 ||
                page.Items.Count > page.PageSize || (page.Items.Count == 0 && items.Count < page.TotalCount))
                return ServiceResult<IReadOnlyList<T>>.Failure("استجابة التصفح غير مكتملة؛ أعد تحميل القائمة", "INCOMPLETE_LOOKUP");

            items.AddRange(page.Items);
            if (items.Count >= page.TotalCount)
                return ServiceResult<IReadOnlyList<T>>.Success(items);
        }
    }
}
