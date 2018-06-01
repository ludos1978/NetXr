# NetXR
NetXr is a library that has been developed for the resaerch project "Games in Concert" (https://www.zhdk.ch/forschungsprojekt/431325) .
Video: https://vimeo.com/258784072

## What does it do
It allows to use a input device independant (tested are Desktop, Gaze, Vive Controllers) usage. So you can join the same session using desktop and a VR Device.

## What is required
You will need to add the SteamVR Library. Optionally the Leap Motion hand tracking is supported (but not fully implemented).

## Licensing
https://creativecommons.org/licenses/by-nc-sa/4.0/
Different licensing options can be requested by contacting the developer.
reto.spoerri (at) zhdk.ch

## Basic usage (BasicSetup-Scene)
Download the repo
Add SteamVR library
Open the basic scene NetXR/Scenes/NetXr-BasicSetup
Choose the input device in the PlayerSettings GameObject
Press Play

Get another Computer do the same and join the by defining the ip address, you can use a Desktop-PC without VR-Glasses as well.

## Advanced example
Open the NetXr-Examples/02_Beta/LargeSample/Scenes/NetXr-Example-v2 Scene.
Choose the input device in the PlayerSettings GameObject.

Start it on the other computer in the same network, it should join automatically by using our AutoDiscovery system.

(Windows Firewall can be problematic)
