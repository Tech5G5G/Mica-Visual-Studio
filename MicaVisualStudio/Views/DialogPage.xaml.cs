using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using MicaVisualStudio.ViewModels;

namespace MicaVisualStudio.Views
{
    /// <summary>
    /// Interaction logic for DialogPage.xaml
    /// </summary>
    public partial class DialogPage : UserControl
    {
        public DialogPage()
        {
            InitializeComponent();
            DataContext = VS.GetRequiredService<SToolkitServiceProvider<MicaVisualStudioPackage>, IServiceProvider>()
                            .GetRequiredService<OptionPageViewModel>();
        }
    }
}
