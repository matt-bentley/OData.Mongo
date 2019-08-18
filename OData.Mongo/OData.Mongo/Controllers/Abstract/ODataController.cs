using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MongoDB.Driver;
using System;
using System.Linq;

namespace OData.Mongo.Controllers.Abstract
{
    public class ODataController : ControllerBase
    {
        private const int DEFAULT_TOP = 3;
        private const string DEFAULT_ORDER_BY = "{ _id: 1 }";
        private const string ASCENDING_TOKEN = "1";
        private const string DESCENDING_TOKEN = "-1";

        protected IActionResult ODataResult<T>(IFindFluent<T, T> items)
        {
            var queryParameters = HttpContext.Request.Query;

            items = items.ApplyFilters(queryParameters)
                         .Limit(GetTop(queryParameters))
                         .Skip(GetSkip(queryParameters))
                         .Sort(GetSort<T>(queryParameters))
                         .ApplyProjection(queryParameters);

            return Ok(items.ToEnumerable());
        }

        private int GetTop(IQueryCollection queryParameters)
        {
            try
            {
                StringValues topString;
                if(queryParameters.TryGetValue("$top", out topString))
                {
                    var top = Convert.ToInt32(topString[0]);
                    if(top > DEFAULT_TOP || top < 1)
                    {
                        return DEFAULT_TOP;
                    }
                    else
                    {
                        return top;
                    }
                }
                else
                {
                    return DEFAULT_TOP;
                }
            }
            catch
            {
                return DEFAULT_TOP;
            }
        }

        private int GetSkip(IQueryCollection queryParameters)
        {
            try
            {
                StringValues skipString;
                if (queryParameters.TryGetValue("$skip", out skipString))
                {
                    var skip = Convert.ToInt32(skipString[0]);
                    if (skip < 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return skip;
                    }
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        private SortDefinition<T> GetSort<T>(IQueryCollection queryParameters)
        {
            try
            {
                StringValues orderByString;
                bool ascending = true;
                if (queryParameters.TryGetValue("$orderby", out orderByString))
                {
                    var orderBy = orderByString[0];
                    if(orderBy.IndexOf(" desc", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        ascending = false;
                    }

                    var cutoff = orderBy.IndexOf(' ', StringComparison.OrdinalIgnoreCase);
                    if(cutoff > -1)
                    {
                        orderBy = orderBy.Substring(0, cutoff);
                    }
                    string orderByToken = ascending ? ASCENDING_TOKEN : DESCENDING_TOKEN;
                    orderBy = "{ \"" + orderBy + "\": "+ orderByToken + " }";
                    return (SortDefinition<T>)orderBy;
                }
                else
                {
                    return (SortDefinition<T>)DEFAULT_ORDER_BY;
                }
            }
            catch
            {
                return (SortDefinition<T>)DEFAULT_ORDER_BY;
            }
        }
    }

    internal static class IFindFluentExtensions
    {
        public static IFindFluent<T, T> ApplyProjection<T>(this IFindFluent<T, T> items, IQueryCollection queryParameters)
        {
            try
            {
                StringValues selectString;
                if (queryParameters.TryGetValue("$select", out selectString))
                {
                    string selects = selectString[0];
                    ProjectionDefinition<T> project;
                    if (selects.IndexOf(" ", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        selects = selects.Replace(" ", String.Empty);
                    }
                    if (selects.IndexOf(",", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        var selectArray = selects.Split(',');
                        project = Builders<T>.Projection.Include(selectArray[0]);
                        for(int i = 1; i < selectArray.Length; i++)
                        {
                            project = project.Include(selectArray[i]);
                        }
                    }
                    else
                    {
                        project = Builders<T>.Projection.Include(selects);
                    }
                    var projected = items.Project<T>(project);
                    return projected;
                }
                else
                {
                    return items;
                }
            }
            catch
            {
                return items;
            }      
        }

        public static IFindFluent<T, T> ApplyFilters<T>(this IFindFluent<T, T> items, IQueryCollection queryParameters)
        {
            try
            {
                StringValues filterString;
                if (queryParameters.TryGetValue("$filter", out filterString))
                {
                    string filter = filterString[0];
                    var filterIndex = filter.IndexOf(" eq ", StringComparison.OrdinalIgnoreCase);
                    if (filterIndex > -1)
                    {
                        var filterProperty = filter.Substring(0, filterIndex);
                        int start = filter.IndexOf("'", StringComparison.OrdinalIgnoreCase);
                        int end = filter.LastIndexOf("'", StringComparison.OrdinalIgnoreCase);
                        if(end > start)
                        {
                            var filterValue = filter.Substring(start + 1, end - start - 1);
                            var currentFilter = items.Filter;
                            var newFilters = Builders<T>.Filter.And(currentFilter, Builders<T>.Filter.Eq(filterProperty, filterValue));
                            items.Filter = newFilters;
                        }
                    }
                }
            }
            catch
            {
                // error adding filters
            }
            return items;
        }
    }
}
