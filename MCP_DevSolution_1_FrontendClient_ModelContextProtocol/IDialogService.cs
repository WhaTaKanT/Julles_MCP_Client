namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    // Simple enum wrappers for MessageBox types to avoid direct System.Windows dependency in ViewModel interfaces
    public enum DialogButton
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel
    }

    public enum DialogImage
    {
        None,
        Error,
        Warning,
        Information,
        Question
    }

    public enum DialogResult
    {
        None,
        OK,
        Cancel,
        Yes,
        No
    }

    public interface IDialogService
    {
        DialogResult ShowMessageBox(string message, string caption, DialogButton button, DialogImage icon);
        void ShowError(string message, string caption);
        void ShowWarning(string message, string caption);
        void ShowInfo(string message, string caption);
        bool ShowConfirmation(string message, string caption); // Returns true for Yes, false for No
    }
}
