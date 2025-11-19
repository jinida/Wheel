using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.ProjectSpecifications
{
    /// <summary>
    /// Specification to find projects belonging to a specific dataset
    /// </summary>
    public class ProjectByDatasetSpecification : CompositeSpecification<Project>
    {
        private readonly int _datasetId;

        public ProjectByDatasetSpecification(int datasetId)
        {
            _datasetId = datasetId;
        }

        public override Expression<Func<Project, bool>> ToExpression()
        {
            return project => project.DatasetId == _datasetId;
        }
    }
}
