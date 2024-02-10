# MapInstaller

**MapInstaller** is a utility mod that simplifies the process of adding and updating custom maps in Cities Skylines 2. It is designed to work seamlessly with manual installations as well as installations done through mod managers like Thunderstore.io app and rModMan.

## For End Users

### Installing Maps with Mod Managers

Using Thunderstore APP or rModMan, installing maps is a breeze:

1. **Install MapInstaller**:
   - Obtain MapInstaller via your chosen mod manager. A one-time installation is required.

2. **Install Maps**:
   - Select and install your maps from the mod manager. MapInstaller will automatically install or update the maps when you start the game.

## For Map Makers

MapInstaller helps you distribute your maps to the community with ease. Follow these steps to prepare your map for distribution:

### Preparing Your Map

1. **Folder Structure**:
   - Store your `.cok` and `.cok.cid` map files inside a folder named after your map within `BepInEx\plugins`, e.g., `BepInEx\plugins\YourMapName\`.
   - You may also create a `Maps` subfolder within your map's folder to place the files: `BepInEx\plugins\YourMapName\Maps\`. Note that some app launchers might not support subfolders.

2. **ZIP File Creation**:
   - If distributing as a ZIP file, include a `Maps` folder. The ZIP's structure should reflect the `BepInEx\plugins` directory setup.

3. **Testing**:
   - Test your map with MapInstaller to confirm it installs and updates properly.

### Distribution

- Upload your map to mod sharing platforms such as [Thunderstore.io](https://thunderstore.io/) or other community hubs.

### Manual Installation (Advanced Users)

For installations without mod managers:

1. **Download the Map**:
   - Acquire the map files and place them in the `BepInEx\plugins` directory, adhering to the structure specified by the map maker.

2. **Run the Game**:
   - Open Cities Skylines 2. MapInstaller will take over from here.

### Mod Manager Considerations

MapInstaller checks both `BepInEx\plugins` and the mod manager's directory for maps. If you have maps installed through multiple mod managers or different versions, conflicts may occur. To avoid issues:

- Regularly clear out the cache or remove mods from unused mod managers.
- Ensure that maps installed through different mod managers are the same version.

## Uninstallation

MapInstaller does not handle automatic uninstallation. If you remove a map via a mod manager, manually delete the map files from the `BepInEx\plugins` directory to prevent MapInstaller from loading them.

## Support and Feedback

For support or to share feedback, please open an issue on our [GitHub repository](https://github.com/Cities2Modding/MapInstaller) or connect on the Cities2Modding discord (find the link on Thunderstore.io under Cities Skylines II).

## Acknowledgements

A heartfelt thanks to the Cities Skylines modding community for your continuous support and innovation. Enjoy streamlining your city-building experience with MapInstaller! Thank you to Gerbal (https://github.com/gerbal) for Linux bug fixes.