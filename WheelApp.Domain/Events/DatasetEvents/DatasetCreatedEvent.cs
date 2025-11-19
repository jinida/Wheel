using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.DatasetEvents
{
    /// <summary>
    /// Raised when a dataset is created
    /// </summary>
    public class DatasetCreatedEvent : IDomainEvent
    {
        public Dataset Dataset { get; }
        public DateTime OccurredOn { get; }

        public DatasetCreatedEvent(Dataset dataset)
        {
            Dataset = dataset;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
