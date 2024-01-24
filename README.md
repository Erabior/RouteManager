


# RouteManager
This mod is automates passenger service only. This mod has no control of switches and all switches must therefore be prealigned for the correct path. 

**Core Features**

* Pilots consist from station to station stopping at each selected stop.
* Configurable alerts to low consumables
    * Coal
    * Water
    * Fuel
* Automatic Bell and Whistle on station approach
* Support for BepLnEx and Unity Mod Manger

***

**V2 Improvements**

* Updated UI
    * Expanded Car Inspector window for easier scrolling of the station list.
* Configurable Max Speed
    * Speed slider now applies while in route mode to control the maximum speed.
* Option to pause control for refueling
	* Vastly improved refueling support with the new pause feature.
* Improved Station Loading / Unloading
* Improved Station routing 
    * Cochran / Alarka Depo are known to still be problematic. Further improvement coming in 2.1

***

**Latest Release**

https://github.com/Erabior/RouteManager/releases/latest

# Installation

There are two methods of installation, depending on the mod manager you use:

**BepInEx Method**
1. Download the latest version 'RouteManager.BepInEx': https://github.com/Erabior/RouteManager/releases/latest

2. Download the latest version of BepInEx: https://github.com/BepInEx/BepInEx/releases Make sure to grab the correct zip for your system (x64/x86). If you are unsure if you need x64 or x86 you can find that in ThisPC -> (Right Click) -> Properties

3. Open your game directory (Steam Library right-click on Railroader -> properties -> installed files -> Browse)
![image](https://github.com/Erabior/RouteManager/assets/7718625/0b75293a-9092-4cb1-a7cc-7125cf09f799)

4. OPEN (DO NOT unzip) the BepInEx zip file

5. Drag all files in the BepInEx .zip into your install directory
![image](https://github.com/Erabior/RouteManager/assets/7718625/4eec8c87-4a12-4d99-9cc5-a255ebdd16d5)

6. Finish installation of BepInEx by running the game once.

7. Close the game and copy 'RouteManager.BepInEx.dll' into the plugins folder that was generated after launching the game.(Railroader/BepInEx/plugins)
![image](https://github.com/Erabior/RouteManager/assets/7718625/d8719272-514b-4b7d-96f4-f765bb751eca)

8. Prosper


**Unity Mod Manager Method**
1. Download the latest version 'RouteManager.UMM': https://github.com/Erabior/RouteManager/releases/latest

2. Download the latest version of Unity Mod Manager: https://www.nexusmods.com/site/mods/21 and unzip to a folder (e.g. your desktop)

3. Run 'UnityModManager.exe'

4. Select 'Railroader' from the dropdown list and click 'Install'

5. Click on the 'Mods' tab

6. Either drag 'RouteManager.UMM' zip file into the UMM Installer window OR click 'Install Mod' and select the 'RouteManager.UMM' zip file

7. Prosper

***

**Usage:**

1. Select a locomotive (preferably with coaches coupled)

2. Go to the 'Orders' Panel

3. Select 'Road' mode

4. Select all of the stations you want your train to stop at.

5. Click 'Enable Route Mode'

# Configuration

**BepInEx Users**

The mod contains a .ini file that allows for some customization of the mod's behavior. Currently there are the following options are supported:

| Section| Option | Description | Type | Example Value | Accepted Values
|--|--|--|--|--|--|
| Core | LogLevel  | Configures internal diagnostic log levels | string | Debug | Trace, Verbose, Debug, Informational, Warning, Error
| Alerts | WaterLevel| Minimum Fuel Quantity (Gallons) to depart station / warn en-route | Float | 500 | value >= 0
| Alerts | CoalLevel|  Minimum Fuel Quantity (Tons) to depart station / warn  en-route  | Float | 0.5 | value  >= 0
| Alerts | DieselLevel|  Minimum Fuel Quantity (Gallons) to depart station / warn  en-route  | Float | 100 | value  >= 0
| Alerts | ShowTimestamp| Console Messages show timestamp | Bool | False | True / False
| Alerts | ShowDaystamp|  Console Messages show daystamp  | Bool | False | True / False
| Alerts | ShowArrivalMessage|  Console alerts on train arival at station  | Bool | False | True / False
| Alerts | ShowDepartureMessage|  Console alerts on train departure from station  | Bool | False | True / False
| Dev | NewInterface  | Allows access do DEV features | Bool | False |

***

**Unity Mod Manager Users**

Configuration can be done via the in-game Unity Mod Manager interface.
1. Open the in-game interface (default shortcut is Ctrl-F10 if you have closed the window)
2. Click the options icon next to 'Dispatcher'
![image](https://i.imgur.com/2Vw1cjW.png)

Settings changes take effect immediately, but will only save for next time if the 'Save' button at the bottom of the window is clicked.
***
**Notes:**

- Dev features are unsupported and listed only for completeness of documentation.

# Building From Source

Note: This assumes you have some fundamental understanding of how Github & visual studio work and so is intended more as a quick start than a full guide.
 1. Download and install Visual Studio community edition. Not visual Studio Code.
 2. Clone / download this repository to your computer.
 3. Open the Project solution.
 4. If your game install is not in the default location (`C:\Program Files (x86)\Steam\steamapps\common\Railroader`) follow these steps to add the correct references
     1. Create a user solution file `Directory.Build.targets` in the root of the project (where 'RouteManager.sln' is). There is an example file you can copy
     2. Update the `GamePath` property so it points to the base of your Railroader install
 5. Build the solution
	* If you are using BepInEx, 'RouteManager.BepInEx.dll' and 'config.ini' will be copied to the 'BepInEx\plugins' folder
	* If you are using Unity Mod Manager, 'RouteManager.UMM.dll' and 'info.json' will be copied to the 'Mods\RouteManager' folder
	
Note: If the build config is set to 'Release' zip archives containing the required files will be created for both mod loader types in the solution's 'Release' folder


***





