P4Connect - Perforce integration for Unity by Artificial Heart
http://artificialheartgames.com/P4Connect/

Description
-----------

P4Connect is the simplest Perforce integration tool for Unity yet. P4Connect will:

	- Allow you to perform all basic version control operations directly from inside Unity
	- Show you the list of pending changes
	- Let you enter submission notes and diff your files against the Depot
	- Automatically check out / move / add / delete files when you make changes in the project view
    - Check out project settings and Solution files
	
Additionally, P4Connect supports:
	- SSL connections
	- Perforce streams
    - UnityVS
	- Assembla workspaces

Release Notes
-------------
Version 2.3.1
- Fixed issue with Perforce Enable flag that wasn't sticking, and consequently triggering exceptions

Version 2.3
- Better handling of binary and mixed mode serialization.
- Pending changes window now displays the type of the files on the server (text or binary)
- Allow adding files with special characters in them to the depot (%, #, @, *)

Version 2.2.1
- Fixed freeze when attempting to revert a directory from the project view.
- Don't update the project view icons when the editor enters play mode.

Version 2.2
- Added Refresh And Commit contextual menus in Project View. Refresh will make sure the status icons are up to date, and Commit
  will open up the Pending Changes Window and Select only the files of the items you have selected (folder, etc...)
- Minor bug fix to the Log messages.

Version 2.1
- Avoid initial sln exception on startup due to Unity trying to modify the .sln file
- It is no longer necessary to manually set the project root, P4Connect will grab it automatically
- P4Connect will tell you when you try to manually perform an operation but that operation causes no change

Version 2.0
- UnityP4 is now P4Connect
- OSX Support!!!
- Properly warn when assets contain @ or # in their names
- auto check out on edit (make it an option)
- Check out / Revert / Etc... menu items
- Settings per-user / per-project
- Icons in project view
- Pending changes window
	- Allow sorting by name/dir/type/opType
	- Revert unchanged
	- Space to se/deselect
	- Double-click opens
	- Right click reveal menu
	- Multi selection
	- Arrow keys
	- Launch Diff tool
	- Show selected count
- Revert on Move/Add file
- Combined file operations for better performance and console output

Version 1.4
- Improved responsiveness when reloading assemblies
- Perforce settings window no longer white while on first checking connection

Version 1.3
- Fixed some SSL support issues

Version 1.2
- Support for SSL Connections
- Improved .meta file change detection. Hopefully this means fewer missed deletion, move or renames
- Cosmetic changes to the UI

Installation
------------

Note: P4Connect will do its best to hold your hand through the configuration process, only allowing you to enter the next configuration setting if the previous one was validated.

- Start by importing the P4Connect Package from the Asset Store.
- Open the Perforce Settings dialog window from Edit->Perforce Settings.
  You should see it complain about invalid server URI.
- Enter the address of your server. Click Verify when you're done.
  If you entered a correct server address, the Verify button should turn into Valid! and P4Connect should have enabled the next section, your username and password.
- Enter a username and password (leave password empty if your server doesn't need one).
- Enter the name of your Perforce workspace
- (optionally) Enter a Project Root.
  Use this when your Unity Project is NOT at the root of your workspace. Typically you want to leave this blank.

P4Connect should now tell you that all the settings are correct and that Perforce Integration is active. You are ready to go!

Advanced Options
----------------

P4Connect allows you to customize its behavior further with a few advanced options:
- Display Status Icon: Show P4V-style icons on your files to indicate their version control status
- Show File Paths in Console: If unchecked, P4Connect will strip the directories from the file names when telling you what it's doing
- Gray out invalid menu options: If checked, P4Connect will first look at the current state of the file and gray out options that do not apply/would do nothing
- Diff Tool Executable: The tool that P4Connect will launch when diffing a file against the current head revision on the depot
- Ask before checkout on edit: When double clicking to open a file, tells P4Connect whether or not it should auto-checkout the file or ask you first
- Include Solution Files/Project Files: By default P4Connect will check out project and solution files from the Perforce depot. If you did not add them to source control (Unity usually recreates these for you), then you can disable those options.
- Integrate with UnityVS: If you use UnityVS to integrate Unity with Visual Studio, you can make sure that P4Connect checks out the UnityVS project files anytime UnityVS needs to regenerate these.
- Periodically Refresh Status: If turned on, P4Connect will periodically poll the server to update the status of files (so for instance it can show up to date blue icons)

Thanks for using P4Connect!