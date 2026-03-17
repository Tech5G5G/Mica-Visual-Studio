using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using MicaVisualStudio.UI.ViewModels;

namespace MicaVisualStudio.UI.Views
{
    /// <summary>
    /// Interaction logic for ToolPage.xaml
    /// </summary>
    public sealed partial class ToolPage : UserControl
    {
        public ToolPage()
        {
            InitializeComponent();
            DataContext = VS.GetRequiredService<SToolkitServiceProvider<MicaVisualStudioPackage>, IServiceProvider>()
                            .GetRequiredService<OptionPageViewModel>();
        }
    }
}
