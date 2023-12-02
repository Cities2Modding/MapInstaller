# MapInstaller

**MapInstaller** is a mod that simplifies the process of adding and updating custom maps in Cities Skylines 2. It supports both manual installations and those done through the Thunderstore.io app.

## For Map Makers

MapInstaller is designed to assist you in deploying your maps easily to the community. Here's how to prepare your map for distribution:

### Preparing Your Map

1. **Folder Structure**:
    - Place your `.cok` and `.cok.cid` map files directly inside a folder named after your map within `BepInEx\plugins`. For example: `BepInEx\plugins\YourMapName\`.
    - Alternatively, you can create a subfolder named `Maps` inside your map's folder and place the map files there: `BepInEx\plugins\YourMapName\Maps\` (NOTE: App launchers often don't support subfolders or flatten the directory anyway).

2. **ZIP File Creation**:
    - If you prefer distributing your map as a ZIP file, ensure that the `Maps` folder is included in the ZIP. The structure within the ZIP should mirror the `BepInEx\plugins` directory structure mentioned above.

3. **Testing**:
    - Before distributing your map, test it with MapInstaller to ensure it installs and updates correctly.

### Distribution

- Upload your map to a mod sharing platform like [Thunderstore.io](https://thunderstore.io/) or any other preferred community distribution point.

## For End Users

### Installing Maps

If you're using the Thunderstore APP, installing maps is straightforward:

1. **Install MapInstaller**:
   - Download and install MapInstaller through the Thunderstore APP. This only needs to be done once.

2. **Install Maps**:
   - Find and install your desired map(s) through the Thunderstore APP. The maps will automatically be installed or updated by MapInstaller when you run the game.

### Manual Installation (Advanced Users)

For manual installation without the use of Thunderstore or other app managers:

1. **Download the Map**:
   - Obtain the map files and place them within the `BepInEx\plugins` directory, following the structure provided by the map maker.

2. **Run the Game**:
   - Start Cities Skylines 2 after placing the map files. MapInstaller will handle the rest.

## Uninstallation

MapInstaller does not currently support automatic uninstallation of maps. If you uninstall a map via the Thunderstore APP or other app managers, please manually remove the map files from the `BepInEx\plugins` directory to ensure they are not loaded by MapInstaller.

## Support and Feedback

For support or feedback, please create an issue on our [GitHub repository](https://github.com/Cities2Modding/MapInstaller), or join the conversation on the Cities2Modding discord (link on Thunderstore.io under Cities Skylines II).

## Acknowledgements

Thank you to the Cities Skylines modding community for your support, creativity, and enthusiasm. Happy building!
