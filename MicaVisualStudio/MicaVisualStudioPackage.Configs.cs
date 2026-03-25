using System.Collections.Generic;
using MicaVisualStudio.Services.Resourcing;

namespace MicaVisualStudio;

public partial class MicaVisualStudioPackage
{
    private static readonly Dictionary<string, ResourceConfiguration> s_configs = new()
    {
        { "Background", new(translucent: true) },

        { "SolidBackgroundFillQuaternary", new(translucent: true) },

        // { "SolidBackgroundFillTertiary", ColorConfig.Default },
        // { "EnvironmentLayeredBackground", new(transparentOnGray: true, translucent: true, opacity: 0x7F) },

        { "EnvironmentBackground", new(translucent: true) },
        { "EnvironmentBackgroundGradient", ResourceConfiguration.Default },

        { "ActiveCaption", ResourceConfiguration.Layered },
        { "InactiveCaption", ResourceConfiguration.Layered },

        { "MainWindowActiveCaption", ResourceConfiguration.Default },
        { "MainWindowInactiveCaption", ResourceConfiguration.Default },

        { "ToolWindow", ResourceConfiguration.Default },
        { "ToolWindowGroup", ResourceConfiguration.Default },
        { "ToolWindowBackground", ResourceConfiguration.Default },
        { "ToolWindowFloatingFrame", ResourceConfiguration.Default },
        { "ToolWindowFloatingFrameInactive", ResourceConfiguration.Default },
        { "ToolWindowTabMouseOverBackgroundGradient", ResourceConfiguration.Layered },

        { "ToolWindowContentGrid", ResourceConfiguration.Layered },

        { "PopupBackground", ResourceConfiguration.Default },

        { "Default", ResourceConfiguration.Default },

        { "Window", ResourceConfiguration.Default },
        { "WindowPanel", new(translucent: true) },

        { "CommandBarGradient", ResourceConfiguration.Default },
        { "CommandBarGradientBegin", ResourceConfiguration.Default },

        { "CommandShelfHighlightGradient", ResourceConfiguration.Default },

        { "ListBox", ResourceConfiguration.Layered },
        { "ListItemBackgroundHover", new(transparentIfGray: false, translucent: true) },

        { "SelectedItemActive", ResourceConfiguration.Layered },
        { "SelectedItemInactive", ResourceConfiguration.Layered },

        { "Unfocused", ResourceConfiguration.Layered },

        { "Caption", ResourceConfiguration.Layered },

        { "TextBoxBackground", ResourceConfiguration.Layered },
        { "SearchBoxBackground", ResourceConfiguration.Layered },

        { "Button", ResourceConfiguration.Layered },
        { "ButtonFocused", ResourceConfiguration.Default },

        { "ComboBoxBackground", ResourceConfiguration.Layered },

        { "InfoBarBorder", ResourceConfiguration.Default },

        { "Page", ResourceConfiguration.Default },
        { "PageBackground", ResourceConfiguration.Default },

        { "BrandedUIBackground", ResourceConfiguration.Default },

        { "ScrollBarBackground", ResourceConfiguration.Layered },
        { "ScrollBarArrowBackground", ResourceConfiguration.Default },
        { "ScrollBarArrowDisabledBackground", ResourceConfiguration.Default },

        { "AutoHideResizeGrip", ResourceConfiguration.Default },
        { "AutoHideResizeGripDisabled", ResourceConfiguration.Default },

        { "Content", ResourceConfiguration.Default },
        { "ContentSelected", ResourceConfiguration.Layered },
        { "ContentMouseOver", ResourceConfiguration.Layered },
        { "ContentInactiveSelected", ResourceConfiguration.Layered },

        { "Container", ResourceConfiguration.Default },

        { "Wonderbar", ResourceConfiguration.Default },
        { "WonderbarMouseOver", ResourceConfiguration.Layered },
        { "WonderbarTreeInactiveSelected", ResourceConfiguration.Default },

        { "Details", ResourceConfiguration.Layered },

        { "BackgroundLowerRegion", ResourceConfiguration.Default },
        { "WizardBackgroundLowerRegion", ResourceConfiguration.Default }
    };
}
