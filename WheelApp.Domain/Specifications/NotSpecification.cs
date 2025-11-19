using System.Linq.Expressions;

namespace WheelApp.Domain.Specifications
{
    /// <summary>
    /// NOT logical specification
    /// </summary>
    public class NotSpecification<T> : CompositeSpecification<T>
    {
        private readonly ISpecification<T> _specification;

        public NotSpecification(ISpecification<T> specification)
        {
            _specification = specification;
        }

        public override Expression<Func<T, bool>>? ToExpression()
        {
            var expression = _specification.ToExpression();
            if (expression == null)
                return null;

            var parameter = expression.Parameters[0];
            var body = Expression.Not(expression.Body);

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
}
