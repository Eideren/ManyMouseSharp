namespace ManyMouseSharp
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    
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
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        internal delegate int ManyMouse_Init_Type();
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        internal delegate int ManyMouse_Quit_Type();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        internal delegate int ManyMouse_PollEvent_Type(ref ManyMouseEvent mouseEvent);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        internal delegate IntPtr ManyMouse_DriverName_Type();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        internal delegate IntPtr ManyMouse_DeviceName_Type(uint index);

        internal static ManyMouse_Init_Type ManyMouse_Init;
        internal static ManyMouse_Quit_Type ManyMouse_Quit;
        internal static ManyMouse_PollEvent_Type ManyMouse_PollEvent;
        internal static ManyMouse_DriverName_Type ManyMouse_DriverName;
        internal static ManyMouse_DeviceName_Type ManyMouse_DeviceName;

        const string libDir = "lib";
        const string dllName = "libManyMouse";
        
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
                if( !File.Exists( Path.Combine( managedLibDir, source ) ) )
                {
                    // If path doesn't exist, try to find it locally to this lib's path instead of this + lib/
                    source = source.Remove( libDir.Length + 1 );
                }
                
                IntPtr pDll;
                IntPtr ptrManyMouse_Init;
                IntPtr ptrManyMouse_Quit;
                IntPtr ptrManyMouse_PollEvent;
                IntPtr ptrManyMouse_DriverName;
                IntPtr ptrManyMouse_DeviceName;
                
                switch( Environment.OSVersion.Platform )
                {
                    case PlatformID.Unix:
                        pDll = LibraryOperations.dlopen(source, LibraryOperations.RTLD_NOW);
                        ptrManyMouse_Init = LibraryOperations.dlsym(pDll, "ManyMouse_Init");
                        ptrManyMouse_Quit = LibraryOperations.dlsym(pDll, "ManyMouse_Quit");
                        ptrManyMouse_PollEvent = LibraryOperations.dlsym(pDll, "ManyMouse_PollEvent");
                        ptrManyMouse_DriverName = LibraryOperations.dlsym(pDll, "ManyMouse_DriverName");
                        ptrManyMouse_DeviceName = LibraryOperations.dlsym(pDll, "ManyMouse_DeviceName");
                        break;
                    case PlatformID.Xbox:
                    case PlatformID.WinCE:
                    case PlatformID.Win32NT:
                    case PlatformID.Win32Windows:
                    case PlatformID.Win32S:
                        pDll = LibraryOperations.LoadLibrary(source);
                        ptrManyMouse_Init = LibraryOperations.GetProcAddress(pDll, "ManyMouse_Init");
                        ptrManyMouse_Quit = LibraryOperations.GetProcAddress(pDll, "ManyMouse_Quit");
                        ptrManyMouse_PollEvent = LibraryOperations.GetProcAddress(pDll, "ManyMouse_PollEvent");
                        ptrManyMouse_DriverName = LibraryOperations.GetProcAddress(pDll, "ManyMouse_DriverName");
                        ptrManyMouse_DeviceName = LibraryOperations.GetProcAddress(pDll, "ManyMouse_DeviceName");
                        break;
                    case PlatformID.MacOSX:
                    default:
                        throw new NotImplementedException( $"ManyMouse dll loading for {Environment.OSVersion.Platform} not implemented." );
                }
                
                ManyMouse_Init = Marshal.GetDelegateForFunctionPointer<ManyMouse_Init_Type>( ptrManyMouse_Init );
                ManyMouse_Quit = Marshal.GetDelegateForFunctionPointer<ManyMouse_Quit_Type>( ptrManyMouse_Quit);
                ManyMouse_PollEvent = Marshal.GetDelegateForFunctionPointer<ManyMouse_PollEvent_Type>( ptrManyMouse_PollEvent);
                ManyMouse_DriverName = Marshal.GetDelegateForFunctionPointer<ManyMouse_DriverName_Type>( ptrManyMouse_DriverName);
                ManyMouse_DeviceName = Marshal.GetDelegateForFunctionPointer<ManyMouse_DeviceName_Type>( ptrManyMouse_DeviceName);
            }
        }
    }
    
    internal static class LibraryOperations
    {
        // Linux

        public const int RTLD_NOW = 2; // for dlopen's flags 

        [DllImport("dl", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr dlopen(string filename, int flags);

        [DllImport("dl", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("dl", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr dlclose();

        // Windows

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool FreeLibrary(IntPtr hModule);
    }
}