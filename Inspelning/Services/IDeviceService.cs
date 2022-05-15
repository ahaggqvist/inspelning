using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.UI.Xaml.Controls.Primitives;

namespace Inspelning.Recorder.Services
{
    public interface IDeviceService
    {
        Task PopulateResolutionUi(string deviceInterfaceId, MediaStreamType streamType, Selector comboBox,
            bool showFrameRate = true);

        Task PopulateDevicesUi(DeviceClass deviceClass, Selector comboBox);
        Task<DeviceInformation> FindDeviceAsync(string deviceId);
        Task<DeviceInformation> FindFirstCameraDeviceAsync();
        Task<DeviceInformation> FindFirstMicrophoneDeviceAsync();
    }
}