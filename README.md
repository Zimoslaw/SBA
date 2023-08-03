# EZ Backapp
## Program for creating backup of your files (for Windows)
Simple .NET WPF project in C# for practicing purposes. Program lets you choose folders to copy and destination folder. Folder's URLs can be saved and then they're loaded on program startup. Program lets you choose if you want to overwrite existing files. There is option to compare hashes of original and copied files. Overwriting and hash checking are settings and they're saved and are loaded on program startup. Program has logging feature - logs every event into a file (**logs.txt**). Events are also displayed in info TextBlock.

## Manual
### Startup
On strartup you can see a dialog "Error when loading paths". It means that configuration file (**paths.ini**) is incrorect or some URLs saved in that file are invalid. It can also pop up if there is no paths saved.
You have 3 options to choose:
1. Do not load the configuration
 - Program starts with no folders set.
2. Load only correct
 - Load only valid URLs.
3. Exit program

### Settings
Settings can be found in menu bar: **File -> Settings**.
There are two settings:
1. Replace files in destination directory
 - If checked, program replaces file with the same filename in destination directory.
2. Compare hash (SHA256) of original and copied file
 - If checked, program compares copied file's hash with original file's hash (checks if file are identical). If they are not identical, program copies that file again. If file are still not identical, that fact is logged and backup process goes on.

Settings are aplied and saved if you click **"Ok"**

Settings are saved in **config.ini** file

### Backup
To backup your files, you have to choose destination directory. You can do it by clicking **"..."** button in "Backup directory" section or by clicking **Backup -> Choose destination directory...**
You also have to choose folders to backup. You select folder by clicking **"..."** button in "Directories to copy:" section or **Backup -> Add new directory to backup...**
After selecting you have to add that folder to the list by clicking "Add" button. You can add up to **64** folders to backup.
Start backup by clicking **"Backup" button**. Progress bar will inform you about progress, and message will be displayed after backup is done. If there were some errors in the proccess, they are displayed in Info section

### Saving your folders
You can save chosen folders by clicking **"Save" button** or **File -> Save sources and destination**
