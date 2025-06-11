using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic; // Required for List<string>
using System.Collections.ObjectModel; // Required for ObservableCollection
using System.Linq; // Required for .ToList()
//using System.Windows; // Required for MessageBox - already included by default project templates or via System.Windows.Controls
using System.Text.RegularExpressions; // Required for Regex validation
using System.Threading.Tasks; // Required for Task
using System.Globalization; // Required for CultureInfo
using System.Windows.Data; // Required for IValueConverter

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public partial class MainWindow : Window
    {
        // Specific log channel collections can remain here if they are not part of MainViewModel yet,
        // or if MainViewModel will expose them and these are just for XAML initialization.
        // For this refactor, ServerOutputMessages is handled by MainViewModel.
        // These are kept to ensure the XAML initializes ItemsSource for currently unmanaged logs.
        public ObservableCollection<string> AllLogMessages { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ChatLogMessages { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> CombatLogMessages { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SystemLogMessages { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> McpDebugLogMessages { get; set; } = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();

            // Set the main DataContext for the Window to the MainViewModel
            DataContext = new MainViewModel();

            // Initialize ItemsSource for log channels that might still be directly managed here
            // If MainViewModel exposes these collections, these lines are not needed as XAML bindings would handle it.
            AllLogOutputListView.ItemsSource = AllLogMessages;
            ChatLogOutputListView.ItemsSource = ChatLogMessages;
            CombatLogOutputListView.ItemsSource = CombatLogMessages;
            SystemLogOutputListView.ItemsSource = SystemLogMessages;
            McpDebugLogOutputListView.ItemsSource = McpDebugLogMessages;
        }

        private void CommandInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var mainVM = DataContext as MainViewModel;
            if (mainVM == null) return;

            if (e.Key == Key.Enter)
            {
                if (mainVM.SendCommand.CanExecute(null))
                {
                    mainVM.SendCommand.Execute(null);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                mainVM.NavigateCommandHistory(true);
                e.Handled = true;
                // Move cursor to end of text after history navigation
                // This is a view-specific concern, ideally handled with an attached behavior or custom TextBox if pure MVVM is desired.
                // For simplicity now, keeping it in code-behind.
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    textBox.Select(textBox.Text.Length, 0);
                }
            }
            else if (e.Key == Key.Down)
            {
                mainVM.NavigateCommandHistory(false);
                e.Handled = true;
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    textBox.Select(textBox.Text.Length, 0);
                }
            }
        }
        // All other methods (old event handlers, LoadInitialDataAsync, ScrollListViewToEnd, etc.) are removed.
        // Service instances and sub-ViewModel instances are now owned by MainViewModel.
        // Command history is now owned by MainViewModel.
        // ServerOutputMessages is now owned by MainViewModel and bound in XAML.
    }

    public class RoleToDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string role)
            {
                return culture.TextInfo.ToTitleCase(role); // Example: "user" -> "User"
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}