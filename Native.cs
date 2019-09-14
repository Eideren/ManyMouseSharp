namespace ManyMouseSharp
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    
    public enum ManyMouseEventType
    {
        MANYMOUSE_EVENT_ABSMOTION = 0,
        MANYMOUSE_EVENT_RELMOTION,
        MANYMOUSE_EVENT_BUTTON,
        MANYMOUSE_EVENT_SCROLL,
        MANYMOUSE_EVENT_DISCONNECT,
        MANYMOUSE_EVENT_MAX
    }

    public struct ManyMouseEvent
    {
        public ManyMouseEventType type;
        public uint device;
        public uint item;
        public int  value;
        public int  minval;
        public int  maxval;

        public override string ToString()
        {
            return$"{type} device:{device} item:{item} {minval}/{value}/{maxval}";
        }
    }
    
    internal static class Native
    {
        [ DllImport( importedLib ) ]
        internal static extern int ManyMouse_Init();
        [ DllImport( importedLib ) ]
        internal static extern void ManyMouse_Quit();
        [ DllImport( importedLib ) ]
        internal static extern int ManyMouse_PollEvent( ref ManyMouseEvent mouseEvent );

        /// <summary> UTF8 </summary>
        [ DllImport( importedLib ) ]
        internal static extern IntPtr ManyMouse_DriverName();

        /// <summary> ANSI </summary>
        [ DllImport( importedLib, CharSet = CharSet.Ansi ) ]
        internal static extern IntPtr ManyMouse_DeviceName( uint index );

        
        
        const string libDir      = "lib";
        const string selectedDir = "selected";
        const string dllName     = "libManyMouse";
        const string importedLib = libDir + "/" + selectedDir + "/" + dllName;
        static Native()
        {
            // Since DllImport's path is constant we'll manually select the right lib and
            // move it to the location that dotnet expects to avoid compiling for each platforms.
            {
                string extension;
                string osDir;
                string bitDir = Environment.Is64BitProcess ? "x64" : "x86";
    
                switch( Environment.OSVersion.Platform )
                {
                    case PlatformID.Unix: extension = "so"; osDir = "unix"; break;
                    case PlatformID.MacOSX: extension = "dylib"; osDir = "osx"; break;

                    case PlatformID.Xbox:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                    case PlatformID.Win32NT:
                    {
                        extension = "dll";
                        osDir = "win";
                        break;
                    }
                    default: return;
                }

                string managedLibDir = new FileInfo( Assembly.GetExecutingAssembly().Location ).DirectoryName;


                string source = Path.Combine( libDir, osDir, bitDir, $"{dllName}.{extension}" );
                string dest = $"{importedLib}.{extension}";
                // If path doesn't exist, try to find it locally to this lib's path instead of this + lib/
                if( !File.Exists( Path.Combine( managedLibDir, source ) ) )
                {
                    source = source.Remove( libDir.Length + 1 );
                    dest   = dest.Remove( libDir.Length   + 1 );
                }
                
                // Make them local to the current lib's folder
                source = Path.Combine( managedLibDir, source );
                dest = Path.Combine( managedLibDir, dest );
                
                // Create dir which will house our selected dll
                Directory.CreateDirectory( Path.Combine( managedLibDir, libDir, selectedDir ) );
                if( File.Exists( dest ) )
                {
                    var srcInfo = new FileInfo( source );
                    var destInfo = new FileInfo( dest );
                    // Files have an extremely small chance of not being equal, let's say they are equal
                    if( srcInfo.Length == destInfo.Length && srcInfo.LastWriteTimeUtc == destInfo.LastWriteTimeUtc )
                        return;
                }
                // Files aren't equal, copy over
                File.Copy( source, dest, true );
            }
        }
    }
}