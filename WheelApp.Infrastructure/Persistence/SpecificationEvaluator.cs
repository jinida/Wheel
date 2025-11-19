using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Common;
using WheelApp.Domain.Specifications;

namespace WheelApp.Infrastructure.Persistence;

/// <summary>
/// Helper class to apply ISpecification to IQueryable
/// </summary>
public static class SpecificationEvaluator
{
    /// <summary>
    /// Applies the specification to the query
    /// </summary>
    public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecification<T> specification) where T : Entity
    {
        var query = inputQuery;

        // Apply the criteria expression from specification
        var expression = specification.ToExpression();
        if (expression != null)
        {
            query = query.Where(expression);
        }

        // Apply includes
        query = specification.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply string-based includes
        query = specification.IncludeStrings
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply paging
        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }
}
