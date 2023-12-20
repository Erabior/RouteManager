# RouteManager
This mod is VERY barebones. The purpose of this mod is to allow people to automate passenger service. This mod does not automate throwing of switches for the passenger train. You must make sure the switches are thrown properly for the passenger train for the time being. The mod currently does not support automating freight.  More features will be added in the future to expand the capabilities fo this mod, but for right now this mod only automates passanger your service.


Installation:
1)Download the latest version of BepInEx: https://github.com/BepInEx/BepInEx/releases Make sure to grab the correct zip for your system (x64/x86)
2)Open your game directory (Steam Library right click on Railroader -> properties -> installed files -> Browse)
3)Drag all files in the BepInEx .zip into your install directory
4)Inside Railroader/BepInEx create a new folder called "plugins"
5)Put RouteManager.dll in the plugins folder
6)Prosper

Usage:
Select a locomotive (preferably with coaches coupled)
Go to the 'Orders' Panel
Select 'Road' mode
(NOTE! before continuing just know htat currently the train will go to the first station you select reguardless of if you select additional stations on the way)
Select any station you want your train to stop at. The order in which you select stations does not matter as the train will go in the order that stations apear on the line.
The Route Manager Logic will take over and stop your train at every station you have selected. No need to worry about selecting passengers in the coaches either. The Route Manager will take care of that as well!
Make sure to keep track of the coal and water remaining in your locomotive as there is currently no check for low fuel/water
