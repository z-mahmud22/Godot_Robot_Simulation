# Godot_URDF_Robot_Simulation
This project provides a guideline for extracting joint-link information from an ROS `.urdf` file to load a robot model in Godot 4.x. I have used the G1 Humanoid Robot from [Unitree](https://www.unitree.com/g1) for this project. The robot description file `.urdf` and the corresponding mesh files can be accessed from their [GitHub page](https://github.com/unitreerobotics/unitree_ros). For this project, I'm using the 29 DoF model.  

## Requirements
* Download and install the latest [Godot](https://godotengine.org/) version. Make sure to download the "Godot Engine - .NET" version as it supports C#. 
* Download and install [.NET SDK](https://dotnet.microsoft.com/en-us/download) which is required for .NET development.
* (Optional) Download and install [VS Code](https://code.visualstudio.com/) and install these extensions: [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp), [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit), [.NET Install Tool](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.vscode-dotnet-runtime), [godot-tools](https://marketplace.visualstudio.com/items?itemName=geequlim.godot-tools).

## Project Setup
* Clone this repo and extract all the files. 
* Import the project into Godot and click `Edit` to open the project.
* Navigate to `Project > Tool > C#` and then select `Create C# Solution`.
* Press `Alt + B` or click the `Build Project`( :hammer: ) button to build the project.
* Finally, press `F5` or click the `Run Project` ( :arrow_forward: ) butoon to run the project. 

**Optional:** If you'd like to develop with VS Code and use it as an external editor, then follow these steps:  
* In Godot, navigate to `Editor > Text Editor > External`. Turn on `Use External Editor` and set `Exec Path` to your VS Code executable file `Code.exe`. 
* Navigate to `Editor > Text Editor > Editor`. Select "Visual Studio Code" at the dropdown menu for External Editor and set `Custom Exec Path` to your VS Code executable file, i.e `Code.exe`.
* In `.vscode/launch.json`, set "program" to the path of your Godot executable file, i.e `Godot_v4.x-stable_mono_win64.exe`.
