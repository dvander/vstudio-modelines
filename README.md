vstudio-modelines
=================

# Usage
Visual Studio add-in to use VIM modelines for per-file indentation rules. Only Visual Studio 2012 has been tested. Does not conflict with VsVim.

If any of the first 3 lines of a file contains:
   vim: set

Then the rest of the line is scanned for any occurrence of:
   ts=<n>    - Sets TabSize property.
   sw=<n>    - Sets IndentSize property.
   et        - Sets InsertTabs to false.
   noet      - Sets InsertTabs to true.

The line must end in a ':' as vim requires. Example:

  vim: set ts=4 sw=4 tw=99 noet :

If you change a modeline, just re-focus the file (click in the solution explorer, click back) to refresh.

# Installation

* Extract the contents of the zip file anywhere.
* In Visual Studio, go to Tools -> Options -> Environment -> Add-in Security.
* Check "Allow Add-in components to load".
* Click "Add", and add the folder that contains the VSModelines.dll and associated files.
* Click OK.
* Go to Tools -> Add-in Manager.
* The vstudio-modelines add-in should be listed. Make sure it is checked/enabled, and click OK.
* The file that is currently open may be need to be re-opened before it is recognized.

# Credits

The source code to this add-in was originally based on exTabSettings[1].

[1] http://code.google.com/p/extabsettings/
