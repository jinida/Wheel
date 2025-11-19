using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.DatasetEvents
{
    /// <summary>
    /// Raised when an image is removed from a dataset
    /// </summary>
    public class ImageRemovedFromDatasetEvent : IDomainEvent
    {
        public Dataset Dataset { get; }
        public int ImageId { get; }
        public DateTime OccurredOn { get; }

        public ImageRemovedFromDatasetEvent(Dataset dataset, int imageId)
        {
            Dataset = dataset;
            ImageId = imageId;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
