using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricely.Common.Application.Pagination;
public static class RefitPaginationExtensions
{
    public static async Task<List<TItem>> GetAllPagedAsync<TItem>(
        this Func<int, int, Task<PagedList<TItem>>> fetchPage,
        int pageSize = 100) where TItem : class
    {
        var results = new List<TItem>();
        int pageIndex = 0;

        while (true)
        {
            PagedList<TItem> result = await fetchPage(pageIndex, pageSize);
            if (result?.Items == null || result.Items.Count == 0)
            {
                break;
            }


            results.AddRange(result.Items);

            if (results.Count >= result.TotalCount || result.Items.Count < pageSize)
            {
                break;

            }

            pageIndex++;
        }

        return results;
    }
}
