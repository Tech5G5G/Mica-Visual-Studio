using System.Reflection;

namespace MicaVisualStudio.Options.Controls
{
    /// <summary>
    /// Interaction logic for WindowOptions.xaml
    /// </summary>
    public partial class WindowOptions : GroupBox
    {
        /// <summary>
        /// Gets or sets the name of the type of window represented by this <see cref="WindowOptions"/>.
        /// </summary>
        public string WindowName
        {
            get => (string)GetValue(WindowNameProperty);
            set => SetValue(WindowNameProperty, value);
        }
        /// <summary>
        /// Identifies the <see cref="WindowName"/> dependency property.
        /// </summary>
        public static DependencyProperty WindowNameProperty { get; } =
            DependencyProperty.Register(nameof(WindowName), typeof(string), typeof(WindowOptions), new(defaultValue: "Windows"));

        /// <summary>
        /// Gets or sets whether to show a <see cref="CheckBox"/> at the top of this <see cref="WindowOptions"/> for toggling separate options.
        /// </summary>
        public bool ShowCheck
        {
            get => (bool)GetValue(ShowCheckProperty);
            set => SetValue(ShowCheckProperty, value);
        }
        /// <summary>
        /// Identifies the <see cref="ShowCheck"/> dependency property.
        /// </summary>
        public static DependencyProperty ShowCheckProperty { get; } =
            DependencyProperty.Register(nameof(ShowCheck), typeof(bool), typeof(WindowOptions), new(defaultValue: true));

        /// <summary>
        /// Gets or sets the name of the property to modify in the specified model when setting the backdrop type.
        /// </summary>
        public string BackdropSource { get; set; }

        /// <summary>
        /// Gets or sets the name of the property to modify in the specified model when setting the corner preference.
        /// </summary>
        public string CornerPreferenceSource { get; set; }

        /// <summary>
        /// Gets or sets the name of the property to modify in the specified model when setting the theme.
        /// </summary>
        public string ThemeSource { get; set; }

        /// <summary>
        /// Gets or sets the name of the property to modify in the specified model when enabling or disabling separate options.
        /// </summary>
        public string EnabledSource { get; set; }

        private bool optionModelSet = false;

        /// <summary>
        /// Initializes a new instance of <see cref="WindowOptions"/>.
        /// </summary>
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

        /// <summary>
        /// Specifies the <see cref="BaseOptionModel{T}"/> to modify when changing settings.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="BaseOptionModel{T}"/>.</typeparam>
        /// <param name="model">A <see cref="BaseOptionModel{T}"/> whose properties are to be modified.</param>
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

        /// <summary>
        /// Binds <paramref name="box"/>'s <see cref="Selector.SelectedIndex"/> to the specified <paramref name="propertyName"/> on <paramref name="model"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="BaseOptionModel{T}"/>.</typeparam>
        /// <param name="box">A <see cref="ComboBox"/> to bind.</param>
        /// <param name="propertyName">The name of the property to bind to.</param>
        /// <param name="model">A <see cref="BaseOptionModel{T}"/> to use as the binding source.</param>
        public static void InitializeComboBox<T>(ComboBox box, string propertyName, T model)
            where T : BaseOptionModel<T>, new()
        {
            if (propertyName is null)
                return;

            Type type = typeof(T);
            var instanceProp = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            var prop = type.GetProperty(propertyName);

            UpdateIndex();
            box.SelectionChanged += SelectionChanged;

            type.GetEvent("Saved", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                .AddEventHandler(target: null, new Action<T>(m =>
                {
                    box.SelectionChanged -= SelectionChanged;
                    UpdateIndex();
                    box.SelectionChanged += SelectionChanged;
                }));

            void UpdateIndex() => box.SelectedIndex = (int)prop.GetValue(model);

            void SelectionChanged(object sender, RoutedEventArgs args)
            {
                var instance = (T)instanceProp.GetValue(obj: null);
                prop.SetValue(instance, box.SelectedIndex);
                instance.Save();
            }
        }

        /// <summary>
        /// Binds <paramref name="box"/>'s <see cref="ToggleButton.IsChecked"/> to the specified <paramref name="propertyName"/> on <paramref name="model"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="BaseOptionModel{T}"/>.</typeparam>
        /// <param name="box">A <see cref="CheckBox"/> to bind.</param>
        /// <param name="propertyName">The name of the property to bind to.</param>
        /// <param name="model">A <see cref="BaseOptionModel{T}"/> to use as the binding source.</param>
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
