using WheelApp.Domain.Services;

namespace WheelApp.Infrastructure.Services
{
    /// <summary>
    /// Implementation of role split service for random train/validation/test splits
    /// </summary>
    public class RoleSplitService : IRoleSplitService
    {
        /// <summary>
        /// Performs random split of images into Train/Validation/Test sets
        /// </summary>
        public Dictionary<int, int> PerformRandomSplit(
            List<int> imageIds,
            double trainRatio = 0.8,
            double validationRatio = 0.2,
            double testRatio = 0.0)
        {
            // Validate input
            if (imageIds == null || !imageIds.Any())
                throw new ArgumentException("Image IDs list cannot be null or empty", nameof(imageIds));

            // Validate ratios sum to 1.0 (with small tolerance for floating point errors)
            var totalRatio = trainRatio + validationRatio + testRatio;
            if (Math.Abs(totalRatio - 1.0) > 0.001)
                throw new ArgumentException(
                    $"Ratios must sum to 1.0. Current sum: {totalRatio:F3}",
                    nameof(trainRatio));

            // Validate individual ratios are between 0 and 1
            if (trainRatio < 0 || trainRatio > 1)
                throw new ArgumentOutOfRangeException(nameof(trainRatio), "Train ratio must be between 0 and 1");
            if (validationRatio < 0 || validationRatio > 1)
                throw new ArgumentOutOfRangeException(nameof(validationRatio), "Validation ratio must be between 0 and 1");
            if (testRatio < 0 || testRatio > 1)
                throw new ArgumentOutOfRangeException(nameof(testRatio), "Test ratio must be between 0 and 1");

            // Shuffle the image IDs randomly
            var random = new Random();
            var shuffledIds = imageIds.OrderBy(_ => random.Next()).ToList();

            // Calculate split counts
            var trainCount = (int)Math.Round(shuffledIds.Count * trainRatio);
            var validationCount = (int)Math.Round(shuffledIds.Count * validationRatio);
            // Test count is the remainder to ensure all images are assigned
            var testCount = shuffledIds.Count - trainCount - validationCount;

            // Adjust if rounding caused issues
            if (testCount < 0)
            {
                if (trainCount > 0)
                    trainCount--;
                else if (validationCount > 0)
                    validationCount--;
                testCount = shuffledIds.Count - trainCount - validationCount;
            }

            // Assign roles
            var result = new Dictionary<int, int>();

            for (int i = 0; i < shuffledIds.Count; i++)
            {
                int roleType;
                if (i < trainCount)
                    roleType = 0; // Train
                else if (i < trainCount + validationCount)
                    roleType = 1; // Validation
                else
                    roleType = 2; // Test

                result[shuffledIds[i]] = roleType;
            }

            return result;
        }
    }
}
