namespace WheelApp.Domain.Specifications
{
    /// <summary>
    /// Extension methods for ISpecification to enable fluent composition
    /// </summary>
    public static class SpecificationExtensions
    {
        /// <summary>
        /// Combines two specifications with AND logic
        /// </summary>
        public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
        {
            return new AndSpecification<T>(left, right);
        }

        /// <summary>
        /// Combines two specifications with OR logic
        /// </summary>
        public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right)
        {
            return new OrSpecification<T>(left, right);
        }

        /// <summary>
        /// Negates a specification with NOT logic
        /// </summary>
        public static ISpecification<T> Not<T>(this ISpecification<T> specification)
        {
            return new NotSpecification<T>(specification);
        }
    }
}
