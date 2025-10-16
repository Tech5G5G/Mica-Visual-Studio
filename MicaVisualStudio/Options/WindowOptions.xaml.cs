using System.Reflection;

namespace MicaVisualStudio.Options
{
    /// <summary>
    /// Interaction logic for WindowOptions.xaml
    /// </summary>
    public partial class WindowOptions : GroupBox
    {
        public string WindowName
        {
            get => (string)GetValue(WindowNameProperty);
            set => SetValue(WindowNameProperty, value);
        }
        public static DependencyProperty WindowNameProperty { get; } =
            DependencyProperty.Register(nameof(WindowName), typeof(string), typeof(WindowOptions), new(defaultValue: "Windows"));

        public bool ShowCheck
        {
            get => (bool)GetValue(ShowCheckProperty);
            set => SetValue(ShowCheckProperty, value);
        }
        public static DependencyProperty ShowCheckProperty { get; } =
            DependencyProperty.Register(nameof(ShowCheck), typeof(bool), typeof(WindowOptions), new(defaultValue: true));

        public string BackdropSource { get; set; }

        public string CornerPreferenceSource { get; set; }

        public string ThemeSource { get; set; }

        public string EnabledSource { get; set; }

        private bool optionModelSet = false;

        public WindowOptions()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == WindowNameProperty)
                enabled.Content = $"Enable separate options for {WindowName.ToLower()}";
            else if (e.Property == ShowCheckProperty && !ShowCheck)
                enabled.IsChecked = true;
        }

        public void SetOptionModel<T>(T model)
            where T : BaseOptionModel<T>, new()
        {
            if (optionModelSet)
                return;

            optionModelSet = true;

            InitializeComboBox(backdrop, BackdropSource, model);
            InitializeComboBox(cornerPreference, CornerPreferenceSource, model);
            InitializeComboBox(theme, ThemeSource, model);

            InitializeCheckBox(enabled, EnabledSource, model);
        }

        public static void InitializeComboBox<T>(ComboBox box, string propertyName, T model)
            where T : BaseOptionModel<T>, new()
        {
            if (propertyName is null)
                return;

            Type type = typeof(T);
            var instanceProp = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            var prop = type.GetProperty(propertyName);

            box.SelectedIndex = (int)prop.GetValue(model);
            box.SelectionChanged += (s, e) =>
            {
                var instance = (T)instanceProp.GetValue(null);
                prop.SetValue(instance, box.SelectedIndex);
                instance.Save();
            };
        }

        public static void InitializeCheckBox<T>(CheckBox box, string propertyName, T model)
            where T : BaseOptionModel<T>, new()
        {
            if (propertyName is null)
                return;

            Type type = typeof(T);
            var instanceProp = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            var prop = type.GetProperty(propertyName);

            box.IsChecked = (bool)prop.GetValue(model);
            box.Click += (s, e) =>
            {
                var instance = (T)instanceProp.GetValue(null);
                prop.SetValue(instance, box.IsChecked);
                instance.Save();
            };
        }
    }
}
