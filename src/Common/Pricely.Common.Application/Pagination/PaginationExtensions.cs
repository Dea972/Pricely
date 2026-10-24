using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricely.Common.Application.Pagination;
public static class PaginationExtensions
{
    public static async Task<List<T>> b<T>(
        this List<T> items,
        Func<int, int, Task<List<T>>> pageFetcher,
        int pageSize = 100)
    {
        int pageIndex = 0;

        while (true)
        {
            List<T> page = await pageFetcher(pageIndex, pageSize);
            if (page == null || page.Count == 0)
            {
                break;
            }

            items.AddRange(page);

            if (page.Count < pageSize)
            {
                break;

            }

            pageIndex++;
        }

        return items;
    }
}
