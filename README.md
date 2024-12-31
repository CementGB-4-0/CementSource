# CementSource

This is the source code for the Cement modding tool for Gang Beasts. **To install the latest release of Cement along with its dependency, MelonLoader, go to [the Cement website](https://cementgb.github.io)** and download the mod. Installation instructions are provided [in the documentation.]() (link pending)

## Contribute

> **NOTE:** The terms "folder" and "directory" are used interchangeably, but they mean the same thing.

1. **Ensure you have installed the latest 0.6.x release of MelonLoader separately already and run the game with it at least once.** MelonLoader does not come provided with this repository.
2. Clone this repository into an empty directory.
3. Create a file called `game_dir.txt` at the root of the cloned repository (the folder that contains `CementMod`) and write the path to your Gang Beasts folder (or Staging folder) in it.
4. Open the solution in your preferred C# IDE (VS Code, Visual Studio, Rider, etc) and run `dotnet restore` in the terminal. This should copy all needed references from your game directory and vanquish a bunch of missing reference errors.
5. **Make your changes.** Make sure to commit often, as too many breaking changes in one commit can cause problems. Read up on Git fundamentals to learn more about how to contribute seamlessly.
6. **Building & Testing**: Once you've made your changes, you need to make sure things work before you submit a PR. It should be as simple as going into the root of the repository again in a terminal and calling `dotnet build`. Build events should take the built files and put them in their needed places in your game directory for testing.
