using System.Windows; // Required for MessageBox

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class DialogService : IDialogService
    {
        public DialogResult ShowMessageBox(string message, string caption, DialogButton button, DialogImage icon)
        {
            MessageBoxButton wpfButton = button switch
            {
                DialogButton.OK => MessageBoxButton.OK,
                DialogButton.OKCancel => MessageBoxButton.OKCancel,
                DialogButton.YesNo => MessageBoxButton.YesNo,
                DialogButton.YesNoCancel => MessageBoxButton.YesNoCancel,
                _ => MessageBoxButton.OK
            };

            MessageBoxImage wpfIcon = icon switch
            {
                DialogImage.Error => MessageBoxImage.Error,
                DialogImage.Warning => MessageBoxImage.Warning,
                DialogImage.Information => MessageBoxImage.Information,
                DialogImage.Question => MessageBoxImage.Question,
                DialogImage.None => MessageBoxImage.None,
                _ => MessageBoxImage.None
            };

            MessageBoxResult result = MessageBox.Show(Application.Current?.MainWindow, message, caption, wpfButton, wpfIcon);

            return result switch
            {
                MessageBoxResult.OK => DialogResult.OK,
                MessageBoxResult.Cancel => DialogResult.Cancel,
                MessageBoxResult.Yes => DialogResult.Yes,
                MessageBoxResult.No => DialogResult.No,
                _ => DialogResult.None
            };
        }

        public void ShowError(string message, string caption)
        {
            ShowMessageBox(message, caption, DialogButton.OK, DialogImage.Error);
        }

        public void ShowWarning(string message, string caption)
        {
            ShowMessageBox(message, caption, DialogButton.OK, DialogImage.Warning);
        }

        public void ShowInfo(string message, string caption)
        {
            ShowMessageBox(message, caption, DialogButton.OK, DialogImage.Information);
        }

        public bool ShowConfirmation(string message, string caption)
        {
            return ShowMessageBox(message, caption, DialogButton.YesNo, DialogImage.Warning) == DialogResult.Yes;
        }
    }
}
