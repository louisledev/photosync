using System.Threading.Tasks;

namespace PhotoSync
{
    /// <summary>
    /// Interface for photo synchronization service
    /// </summary>
    public interface IPhotoSyncService
    {
        /// <summary>
        /// Synchronize photos from the configured source OneDrive account to the destination OneDrive account
        /// Configuration is read from app settings: OneDriveSource and OneDriveDestination
        /// </summary>
        Task SyncPhotosAsync();
    }
}
