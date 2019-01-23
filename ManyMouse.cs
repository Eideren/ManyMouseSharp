namespace ManyMouseSharp
{
	using System;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Threading;

	public class AlreadyInitException : Exception { }

	public class SystemNotSupportedException : Exception
	{
		public SystemNotSupportedException() : base( "This OS is not supported, refer to ManyMouse's documentation for more information" )
		{
		}
	}
	public class WrongCallingThreadException : Exception {
		public WrongCallingThreadException( string message ) : base( message )
		{
		}
	}

	public static class ManyMouse
	{
		public static string DriverName{ get; private set; } = string.Empty;

		/// <summary>
		/// ManyMouse is not thread safe, this wrapper will throw if the caller isn't the one who ran Init()
		/// </summary>
		public static bool IgnoreThreadSafety{ get; private set; } = false;

		/// <summary>
		/// The thread that called Init(), null if it hasn't been called yet.
		/// </summary>
		public static Thread CallingThread{ get; private set; } = null;

		public static int AmountOfMiceDetected{ get; private set; } = 0;

		/// <summary>
		/// Initialize the system and returns the amount of mice found.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="AlreadyInitException">Will throw if Init() called while already init.</exception>
		public static int Init()
		{
			if( CallingThread != null )
				throw new AlreadyInitException();
			
			int result = Native.ManyMouse_Init();
			CallingThread = Thread.CurrentThread;
			AmountOfMiceDetected = result < 0 ? 0 : result;
			
			{ // Fetch driver name
				var utf8P = Native.ManyMouse_DriverName();
				int length = 0;
				while( Marshal.ReadByte( utf8P, length ) != 0 ) ++length;
				if( length > 0 )
				{
					byte[] buffer = new byte[ length - 1 ];
					Marshal.Copy( utf8P, buffer, 0, buffer.Length );
					DriverName = Encoding.UTF8.GetString( buffer );
				}
			} // Fetch driver name

			return result;
		}

		public static void Quit()
		{
			if( CallingThread == null )
				return;
			
			ThrowIfWrongCaller();
			Native.ManyMouse_Quit();
			CallingThread = null;
		}

		/// <summary>
		/// As ManyMouse's documentation mention, this causes a Quit() followed by an Init(),
		/// re-plugged mice don't keep the same device index.
		/// </summary>
		public static void RedetectMice()
		{
			Quit();
			Init();
		}

		/// <summary>
		/// Call in a while > 0 loop to fetch all buffered mouse events.
		/// </summary>
		public static int PollEvent( out ManyMouseEvent mouseEvent )
		{
			ThrowIfWrongCaller();
			mouseEvent = new ManyMouseEvent();
			return Native.ManyMouse_PollEvent( ref mouseEvent );
		}
		
		/// <summary>
		/// Returns the human readable name of the pointer device, not guaranteed to be localized, uniquely identifiable nor constant.
		/// </summary>
		public static string DeviceName( uint index )
		{
			ThrowIfWrongCaller();
			return Marshal.PtrToStringAnsi( Native.ManyMouse_DeviceName( index ) );
		}
		
		static void ThrowIfWrongCaller()
		{
			if( IgnoreThreadSafety == false && Thread.CurrentThread != CallingThread )
			{
				string message = $"Thread used to call this function(thread id:{Thread.CurrentThread.ManagedThreadId} name:{Thread.CurrentThread.Name}) is not the same used to Init ManyMouse(id:{CallingThread.ManagedThreadId} name:{CallingThread.Name})";
				throw new WrongCallingThreadException( message );
			}
		}
	}
}