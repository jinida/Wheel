using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Services
{
    /// <summary>
    /// Domain service for project type operations
    /// </summary>
    public interface IProjectTypeService
    {
        /// <summary>
        /// Validates if a project type change is allowed
        /// </summary>
        Task<bool> CanChangeProjectType(Project project, ProjectType newType);

        /// <summary>
        /// Gets recommended project type based on dataset characteristics
        /// </summary>
        Task<ProjectType> RecommendProjectType(Dataset dataset);

        /// <summary>
        /// Validates if project configuration is compatible with project type
        /// </summary>
        Task<bool> ValidateProjectConfiguration(Project project);
    }
}
