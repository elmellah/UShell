# UShell: Command Line Interpreter for Unity

UShell is a tool that allows developers to interact with unity and the game at runtime by sending command lines to a shell.

It provides the user with a terminal to interact with the shell by typing command lines and see logs on the screen.

The user can easily create new commands by using the Convar and Cmd attributes to fields/properties/methods or create more sophisticated commands by implementing the ICommand interface.
Consoles can also be implemented and offer new ways for the user to interact with the shell.

## How to install as a Variant (recommended):
- right-click on the shell prefab > Create > Prefab Variant
- move the prefab variant to the location of your choice
- drag and drop the prefab variant into your scene(s)
- for interacting with the terminal, an event system must be present in the scene

## How to install (for older versions of Unity):
- drag and drop the shell prefab into your scene(s)
- for interacting with the terminal, an event system must be present in the scene

## How to test:
- hit play
- the default key to display the terminal is F1 (as a bind)
- if there is no bind, hit Shift + F1(by default) to display the terminal

## Check script execution order (Edit > Project Settings... > Script Execution Order)
Shell then Pipeline then Terminal. To receive early logs from other scripts, place the shell, the pipeline and the terminal at the top of the script execution order (done by default).

## Headless builds
When you run a headless build, the terminal is automatically destroy to save resources.

## Singleton
The shell follows the singleton pattern and is not destroyed on load. It means that only one shell can exists on a scene and that it is persistent through scene loads. All other instances will be destroyed (especially when you load a new scene)

## Changing the Terminal font
Only one font is provided with UShell (Courier Prime Code) to allow a very lightweight asset. You can add your own font(s) to your project and use it for the Terminal. It is recommended to use a monospaced font.

## Terminal limitations
- The Terminal can display a limited number of characters. Going above the limitation will display the following error: "ArgumentException: Mesh can not have more than 65000 vertices". Change the default value on the Terminal game object or use the ":maxchars" event at runtime to modify the maximum number of characters that can be displayed.
- Rich Text: ...

## Scripting Define Symbols (Edit > Project Settings... > Player > Other Settings)
- SHELL_EXTERNAL_SCRIPTS
