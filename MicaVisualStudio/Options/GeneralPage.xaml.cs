using System.Windows;
using System.Windows.Controls;
using Community.VisualStudio.Toolkit;

namespace MicaVisualStudio.Options
{
    /// <summary>
    /// Interaction logic for GeneralWpf.xaml
    /// </summary>
    public partial class GeneralPage : UserControl
    {
        public GeneralPage()
        {
            InitializeComponent();

            var general = General.Instance;

            InitializeComboBox(backdrop, general, nameof(General.Backdrop));
            InitializeComboBox(cornerPreference, general, nameof(General.CornerPreference));

            InitializeComboBox(toolBackdrop, general, nameof(General.ToolBackdrop)); 
            InitializeComboBox(toolCornerPreference, general, nameof(General.ToolCornerPreference));

            toolWindowsGrid.Visibility = (toolWindows.IsChecked = general.ToolWindows) == true ? Visibility.Visible : Visibility.Collapsed;
            toolWindows.Click += (s, e) =>
            {
                toolWindowsGrid.Visibility = (General.Instance.ToolWindows = toolWindows.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
                General.Instance.Save();    
            };
        }

        private void InitializeComboBox<T>(ComboBox box, T model, string propertyName) where T : BaseOptionModel<T>, new()
        {
            var prop = model.GetType().GetProperty(propertyName);
            box.SelectedIndex = (int)prop.GetValue(model);
            box.SelectionChanged += (s, e) =>
            {
                prop.SetValue(General.Instance, box.SelectedIndex);
                General.Instance.Save();
            };
        }
    }
}
