using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.DatasetEvents
{
    /// <summary>
    /// Raised when an image is added to a dataset
    /// </summary>
    public class ImageAddedToDatasetEvent : IDomainEvent
    {
        public Dataset Dataset { get; }
        public Image Image { get; }
        public DateTime OccurredOn { get; }

        public ImageAddedToDatasetEvent(Dataset dataset, Image image)
        {
            Dataset = dataset;
            Image = image;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
