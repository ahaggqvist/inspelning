using System.Threading.Tasks;
using Windows.Storage;

namespace Inspelning.Recorder.Services
{
    public interface IFileService
    {
        StorageFolder CacheFolder();

        StorageFolder TemporaryFolder();

        Task<ulong> FreeSpaceRemaining();

        Task CopyFilesAsync();
    }
}