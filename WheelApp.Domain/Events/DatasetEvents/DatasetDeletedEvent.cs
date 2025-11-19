namespace WheelApp.Domain.Events.DatasetEvents
{
    /// <summary>
    /// Raised when a dataset is deleted
    /// </summary>
    public class DatasetDeletedEvent : IDomainEvent
    {
        public int DatasetId { get; }
        public string DatasetName { get; }
        public DateTime OccurredOn { get; }

        public DatasetDeletedEvent(int datasetId, string datasetName)
        {
            DatasetId = datasetId;
            DatasetName = datasetName;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
