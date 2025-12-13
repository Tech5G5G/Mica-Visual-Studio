using System.ComponentModel.Design;

namespace MicaVisualStudio
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class BackdropCommands
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int NoneCommandId = 0x0100,
                         MicaCommandId = 0x0105,
                         TabbedCommandId = 0x0110,
                         AcrylicCommandId = 0x0115,
                         GlassCommandId = 0x0120,
                         MoreCommandId = 0x0125;

        /// <summary>
        /// Gets the instance of <see cref="BackdropCommands"/>.
        /// </summary>
        public static BackdropCommands Instance { get; private set; }

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new("89c88597-b045-43a5-bc13-8d34202031e8");

        /// <summary>
        /// <see cref="OleMenuCommandService"/> that commands are added to.
        /// </summary>
        private readonly OleMenuCommandService commandService =
            VS.GetRequiredService<IMenuCommandService, OleMenuCommandService>();

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider => package;

        /// <summary>
        /// <see cref="AsyncPackage"/> that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private int selection = MicaCommandId; //Default to Mica

        /// <summary>
        /// Initializes a new instance of the <see cref="BackdropCommands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private BackdropCommands(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.commandService = commandService;

            selection = (BackdropType)General.Instance.Backdrop switch
            {
                BackdropType.None => NoneCommandId,
                BackdropType.Tabbed => TabbedCommandId,
                BackdropType.Acrylic => AcrylicCommandId,
                BackdropType.Glass => GlassCommandId,
                _ => MicaCommandId
            };

            foreach (var id in new int[] { NoneCommandId, MicaCommandId, TabbedCommandId, AcrylicCommandId, GlassCommandId })
                RegisterCommand(id);

            commandService.AddCommand(new(
                (s, e) => VS.Settings.OpenAsync<OptionsProvider.GeneralOptions>().Forget(),
                new(CommandSet, MoreCommandId)));
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            if (await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
                Instance = new BackdropCommands(
                    package,
                    commandService);
        }

        private void RegisterCommand(int commandId)
        {
            CommandID menuCommandID = new(CommandSet, commandId);

            OleMenuCommand menuItem = new(Execute, menuCommandID);
            menuItem.BeforeQueryStatus += BeforeQueryStatus;

            commandService.AddCommand(menuItem);
        }

        private void BeforeQueryStatus(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
                command.Checked = selection == command.CommandID.ID;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if ((sender as OleMenuCommand)?.CommandID.ID is not int commandId)
                return;

            selection = commandId;

            var general = General.Instance;
            general.Backdrop = (int)(commandId switch
            {
                NoneCommandId => BackdropType.None,
                TabbedCommandId => BackdropType.Tabbed,
                AcrylicCommandId => BackdropType.Acrylic,
                GlassCommandId => BackdropType.Glass,
                _ => BackdropType.Mica
            });
            general.Save();
        }
    }
}
