namespace ManyMouseSharp
{
    using System;
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
        const string dllLocation = "libManyMouse";

        [ DllImport( dllLocation ) ]
        internal static extern int ManyMouse_Init();
        [ DllImport( dllLocation ) ]
        internal static extern void ManyMouse_Quit();
        [ DllImport( dllLocation ) ]
        internal static extern int ManyMouse_PollEvent( ref ManyMouseEvent mouseEvent );

        /// <summary> UTF8 </summary>
        [ DllImport( dllLocation ) ]
        internal static extern IntPtr ManyMouse_DriverName();

        /// <summary> ANSI </summary>
        [ DllImport( dllLocation, CharSet = CharSet.Ansi ) ]
        internal static extern IntPtr ManyMouse_DeviceName( uint index );
    }
}