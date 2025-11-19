using System.Linq.Expressions;

namespace WheelApp.Domain.Specifications
{
    /// <summary>
    /// Specification pattern contract for encapsulating query logic
    /// </summary>
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>>? ToExpression();
        bool IsSatisfiedBy(T entity);

        // Eager loading
        List<Expression<Func<T, object>>> Includes { get; }
        List<string> IncludeStrings { get; }

        // Sorting
        Expression<Func<T, object>>? OrderBy { get; }
        Expression<Func<T, object>>? OrderByDescending { get; }

        // Paging
        int? Skip { get; }
        int? Take { get; }
    }
}
