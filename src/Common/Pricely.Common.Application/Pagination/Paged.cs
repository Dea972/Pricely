using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricely.Common.Application.Pagination;
public static class Paged
{
    public static Func<int, int, Task<PagedList<T>>> Of<T>(Func<int, int, Task<PagedList<T>>> f) where T: class => f;
}
