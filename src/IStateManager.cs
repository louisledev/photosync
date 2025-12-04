namespace PhotoSync
{
    /// <summary>
    /// Interface for managing processed photo state in Azure Table Storage
    /// </summary>
    public interface IStateManager
    {
        /// <summary>
        /// Load all processed file IDs from storage
        /// </summary>
        Task<HashSet<string>> LoadProcessedFilesAsync();

        /// <summary>
        /// Save processed file IDs to storage
        /// </summary>
        Task SaveProcessedFilesAsync(IEnumerable<string> allProcessedFiles);

        /// <summary>
        /// Clean up old records from storage
        /// </summary>
        /// <param name="daysToKeep">Number of days to keep records (default: 365)</param>
        Task CleanupOldRecordsAsync(int daysToKeep = 365);
    }
}
