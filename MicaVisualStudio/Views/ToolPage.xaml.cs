using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using MicaVisualStudio.ViewModels;

namespace MicaVisualStudio.Views
{
    /// <summary>
    /// Interaction logic for ToolPage.xaml
    /// </summary>
    public partial class ToolPage : UserControl
    {
        public ToolPage()
        {
            InitializeComponent();
            DataContext = VS.GetRequiredService<SToolkitServiceProvider<MicaVisualStudioPackage>, IServiceProvider>()
                            .GetRequiredService<OptionPageViewModel>();
        }
    }
}
