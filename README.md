
# RouteManager
This mod is VERY barebones. The purpose of this mod is to allow people to automate passenger service. This mod does not automate the throwing of switches for the passenger train. You must ensure the switches are thrown properly for the passenger train for the time being. The mod currently does not support automating freight.  More features will be added in the future to expand the capabilities of this mod, but for right now this mod only automates passenger service.

***

**Latest Release**

https://github.com/Erabior/RouteManager/releases/latest

***

**Installation:**

1)Download the latest version of BepInEx: https://github.com/BepInEx/BepInEx/releases Make sure to grab the correct zip for your system (x64/x86). If you are unsure if you need x64 or x86 you can find that in ThisPC -> (Right Click) -> Properties

2)Open your game directory (Steam Library right-click on Railroader -> properties -> installed files -> Browse)
![image](https://github.com/Erabior/RouteManager/assets/7718625/0b75293a-9092-4cb1-a7cc-7125cf09f799)

3)OPEN (DO NOT unzip) the BepInEx zip file

4)Drag all files in the BepInEx .zip into your install directory
![image](https://github.com/Erabior/RouteManager/assets/7718625/4eec8c87-4a12-4d99-9cc5-a255ebdd16d5)

5)Finish installation of BepInEx by running the game once.

6)Close the game and copy RouteManager.dll into the plugins folder that was generated after launching the game.(Railroader/BepInEx/plugins) 
![image](https://github.com/Erabior/RouteManager/assets/7718625/d8719272-514b-4b7d-96f4-f765bb751eca)

7)Prosper

***

**Usage:**

1. Select a locomotive (preferably with coaches coupled)
    
2. Go to the 'Orders' Panel
    
3. Select 'Road' mode
    
4. Select all of the stations you want your train to stop at. Note: Although you may only see a single station, it is indeed a scroll-able list. (Hopefully will be addressed i a future release.)
    
5. Click 'Enable Route Mode'

***

**Notes:**
1. The Route Manager Logic will take over and stop your train at every station you have selected—no need to worry about selecting passengers in the coaches either. The Route Manager will take care of that as well!

2. ~~Make sure to keep track of the coal and water remaining in your locomotive as there is currently no check for low fuel/water~~
Basic alerting of low consumables has been implemented as of v1.0.2.0

***

**Building From Source:**
Note: This assumes you have some fundamental understanding of how Github & visual studio work and so is intended more as a quick start than a full guide. 
 1. Download and install Visual Studio community edition. Not visual Studio Code.
 2. Clone / download this repository to your computer. 
 3. Open the Project solution.
 4. Add / update references to the following DLL's as necessary. If the references do not load correctly you will get build errors. 
 
| DLL | Source Location |
|--|--|
| 0Harmony.dll | (Railroader\BepInEx\core\) |
| BepInEx.dll | (Railroader\BepInEx\core\) |
| Assembly-CSharp.dll | (Railroader\Railroader_Data\Managed\) |
| Core.dll | (Railroader\Railroader_Data\Managed\) |
| Definition.dll | (Railroader\Railroader_Data\Managed\) |
| KeyValue.Runtime.dll | (Railroader\Railroader_Data\Managed\) |
| Serilog.dll | (Railroader\Railroader_Data\Managed\) |
| Unity.InputSystem.dll | (Railroader\Railroader_Data\Managed\) |
| Unity.InputSystem.ForUI.dll | (Railroader\Railroader_Data\Managed\) |
| UnityEngine.dll | (Railroader\Railroader_Data\Managed\) |
| UnityEngine.CoreModule.dll | (Railroader\Railroader_Data\Managed\) |
| UnityEngine.InputModule.dll | (Railroader\Railroader_Data\Managed\) |
| UnityEngine.UI.dll | (Railroader\Railroader_Data\Managed\) |
 5. Build the solution 
 6. Copy the RouteManager.DLL to plugin folder in your RailRoader install directory (Railroader/BepInEx/plugins)

***





