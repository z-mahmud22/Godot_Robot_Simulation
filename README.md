# Godot_URDF_Robot_Simulation
This project provides a guideline on how to extract joint-link information from an `.urdf` file from ROS to load a robot model in Godot 4.x. 

## Requirements
* Download and install the latest [Godot](https://godotengine.org/) version. Please make sure to download the "Godot Engine - .NET" version as it supports C#. 
* Download and install [.NET SDK](https://dotnet.microsoft.com/en-us/download) which is required for .NET development.
* Download and install [VS Code](https://code.visualstudio.com/) and install these extensions: [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp), [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit), [.NET Install Tool](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.vscode-dotnet-runtime). Optionally, you can also install [godot-tools](https://marketplace.visualstudio.com/items?itemName=geequlim.godot-tools).

## Project Setup
* In Godot, navigate to `Editor > Text Editor > External`. Turn on "Use External Editor" and set `Exec Path` to your VS Code executable file `Code.exe`. 
* Navigate to `Editor > Text Editor > Editor`. Select "Visual Studio Code" at the dropdown menu for External Editor and set `Custom Exec Path` to your VS Code executable file `Code.exe`.
* In `.vscode/launch.json`, set "program" to the path of your Godot executable file `Godot_v4.3-stable_mono_win64.exe`.
