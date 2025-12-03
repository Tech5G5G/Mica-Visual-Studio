![Mica Visual Studio showcase image](https://github.com/user-attachments/assets/84393113-b591-46fd-9472-dd197f869326)

## Overview

Mica Visual Studio provides four materials to apply as your window backdrop:

1. Mica
2. Tabbed/Mica Alt
3. Acrylic
4. Glass

Official documentation of these materials can be found here (excluding Glass): [Materials in Windows (learn.microsoft.com)](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/materials)

## Additional features

Mica Visual Studio features the ability to set Visual Studio's corner preferences. This allows you to customize the the corners of a window. You can choose from three options:

- Round
- Round small
- Square

Mica Visual Studio allows you to select where and what is applied to different windows. You can apply a specific backdrop and corner preference to the main window and another backdrop and corner preference to tool windows.

## Issues

Mica Visual Studio may contain up to three main issues, being:

- Crashes at startup
- A washed out appearance (depending on your Visual Studio theme)
- [Duplicate caption buttons](https://github.com/Tech5G5G/Mica-Visual-Studio/issues/1)

If you ever experience the first issue, try running Visual Studio in safe mode: `devenv /SafeMode`  
and Mica Visual Studio will be disabled. This will allow you to uninstall Mica Visual Studio with having to repair/reinstall Visual Studio.

If you are facing the second issue, try applying Tabbed as your backdrop instead of Mica.  
For the final issue (as linked earlier) there is an issue open on GitHub. If you have any ideas on a fix, feel free to reply.

If at any point in your usage of Mica Visual Studio you experience a bug, please [open an issue on GitHub](https://github.com/Tech5G5G/Mica-Visual-Studio/issues).
