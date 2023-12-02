using BepInEx;
using System.Reflection;
using System.Linq;

#if BEPINEX_V6
    using BepInEx.Unity.Mono;
#endif

namespace MapInstaller
{
    [BepInPlugin( MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION )]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake( )
        {
            Logger.LogInfo( "=================================================================" );
            Logger.LogInfo( MyPluginInfo.PLUGIN_NAME + " by Cities2Modding community." );
            Logger.LogInfo( "=================================================================" );
            Logger.LogInfo( "Reddit link: https://www.reddit.com/r/cities2modding/" );
            Logger.LogInfo( "Discord link: https://discord.gg/KGRNBbm5Fh" );
            Logger.LogInfo( "Our mods are officially distributed via Thunderstore.io and https://github.com/Cities2Modding" );
            Logger.LogInfo( "Example mod repository and modding info: https://github.com/optimus-code/Cities2Modding" );
            Logger.LogInfo( "Thanks to Captain_Of_Coit, 89pleasure, Rebecca, optimus-code and the Cites2Modding community!" );
            Logger.LogInfo( "=================================================================" );

            // Run the map installer
            new Installer( Logger ).Run( );
        }
    }
}
