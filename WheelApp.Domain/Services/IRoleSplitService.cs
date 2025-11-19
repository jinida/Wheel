using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Services
{
    /// <summary>
    /// Domain service for splitting images into Train/Validation/Test sets
    /// </summary>
    public interface IRoleSplitService
    {
        /// <summary>
        /// Performs random split of images into Train/Validation/Test sets
        /// </summary>
        /// <param name="imageIds">Image IDs to split</param>
        /// <param name="trainRatio">Training set ratio (default 0.7)</param>
        /// <param name="validationRatio">Validation set ratio (default 0.2)</param>
        /// <param name="testRatio">Test set ratio (default 0.1)</param>
        /// <returns>Dictionary mapping ImageId to RoleType value (1=Train, 2=Validation, 3=Test)</returns>
        Dictionary<int, int> PerformRandomSplit(
            List<int> imageIds,
            double trainRatio = 0.7,
            double validationRatio = 0.2,
            double testRatio = 0.1);
    }
}
