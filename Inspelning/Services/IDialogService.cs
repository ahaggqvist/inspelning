namespace Inspelning.Recorder.Services
{
    public interface IDialogService
    {
        void DisplayDialog(string content, string buttonText, string title = "Det har uppstått ett fel");
    }
}