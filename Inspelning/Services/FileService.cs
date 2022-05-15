using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.Toolkit.Uwp.Helpers;
using Unity;
using static Inspelning.Recorder.Utils.Constants;

namespace Inspelning.Recorder.Services
{
    public class FileService : IFileService
    {
        public FileService() : this(App.Container.Resolve<SettingService>())
        {
        }

        private FileService(ISettingService settingService)
        {
        }

        public StorageFolder CacheFolder()
        {
            return ApplicationData.Current.LocalCacheFolder;
        }

        public StorageFolder TemporaryFolder()
        {
            return ApplicationData.Current.TemporaryFolder;
        }

        public async Task<ulong> FreeSpaceRemaining()
        {
            const string freeSpaceKey = "System.FreeSpace";
            var retrieveProperties = await ApplicationData.Current.LocalFolder.Properties.RetrievePropertiesAsync(new[] { freeSpaceKey });
            return (ulong)retrieveProperties[freeSpaceKey];
        }

        public async Task CopyFilesAsync()
        {
            var sourceStorageFolder = CacheFolder();
            var destinationStorageFolder = SaveFolder(await LibraryAsync(KnownLibraryId.Videos));

            var storageFiles = await sourceStorageFolder.GetFilesAsync();
            foreach (var storageFile in storageFiles)
            {
                var sourceFileName = $"{storageFile.DisplayName}{FileExtensionVideo}";
                var destinationFileName = $"{storageFile.DisplayName}{FileExtensionVideo}";

                await CopyFilesAsync(sourceStorageFolder, storageFile.Name, destinationStorageFolder, destinationFileName);

                var isSameSize = await VerifyFileSizeAsync(sourceStorageFolder, storageFile.Name, destinationStorageFolder,
                    destinationFileName);
                if (!isSameSize)
                {
                    continue;
                }

                await storageFile.DeleteAsync(StorageDeleteOption.PermanentDelete);

                if (!await sourceStorageFolder.FileExistsAsync(sourceFileName))
                {
                    continue;
                }

                var sourceFile = await sourceStorageFolder.GetFileAsync(sourceFileName);
                await sourceFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }

        private static async Task<bool> VerifyFileSizeAsync(IStorageFolder storageFolderFile1,
            string fileName1, IStorageFolder storageFolderFile2, string fileName2)
        {
            var file1 = await storageFolderFile1.GetFileAsync(fileName1);
            var file2 = await storageFolderFile2.GetFileAsync(fileName2);

            var basicProperties1 = await file1.GetBasicPropertiesAsync();
            var basicProperties2 = await file2.GetBasicPropertiesAsync();

            return basicProperties1.Size == basicProperties2.Size;
        }

        private async Task<bool> VerifyChecksumAsync(IStorageFolder storageFolderFile1,
            string fileName1, IStorageFolder storageFolderFile2, string fileName2)
        {
            var checksum1 = await ChecksumAsync(storageFolderFile1, fileName1);
            var checksum2 = await ChecksumAsync(storageFolderFile2, fileName2);

            return checksum1 == checksum2;
        }

        private async Task CopyFilesAsync(IStorageFolder sourceStorageFolder, string sourceFileName,
            IStorageFolder destinationStorageFolder,
            string destinationFileName)
        {
            var sourceFile = await sourceStorageFolder.GetFileAsync(sourceFileName);
            _ = await sourceFile.CopyAsync(destinationStorageFolder, destinationFileName,
                NameCollisionOption.GenerateUniqueName);
        }

        private static StorageFolder SaveFolder(StorageLibrary storageLibrary)
        {
            return storageLibrary.SaveFolder;
        }

        private static async Task<StorageLibrary> LibraryAsync(KnownLibraryId knownLibraryId)
        {
            return await StorageLibrary.GetLibraryAsync(knownLibraryId);
        }

        private static async Task<string> ChecksumAsync(IStorageFolder storageFolder, string fileName)
        {
            using var md5 = MD5.Create();
            using var stream = await storageFolder.OpenStreamForReadAsync(fileName);
            var checksum = md5.ComputeHash(stream);
            return BitConverter.ToString(checksum).Replace("-", string.Empty).ToLower();
        }
    }
}