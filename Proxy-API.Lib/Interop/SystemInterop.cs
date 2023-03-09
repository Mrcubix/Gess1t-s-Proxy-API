using System.Runtime.InteropServices;
using OpenTabletDriver.Plugin;

namespace Proxy_API.Lib.Interop
{
    /// <summary>
    /// Provides information about the current platform. <br/>
    /// Shamelessly stolen from OpenTabletDriver.
    /// </summary>
    public class SystemInterop
    {
        protected SystemInterop()
        {
        }

        public static PluginPlatform CurrentPlatform
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return PluginPlatform.Windows;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return PluginPlatform.Linux;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return PluginPlatform.MacOS;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                    return PluginPlatform.FreeBSD;
                else
                    return 0;
            }
        }
    }
}