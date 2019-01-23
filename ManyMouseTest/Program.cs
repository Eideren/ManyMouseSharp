using System;
using ManyMouseSharp;
using static ManyMouseSharp.ManyMouse;
using static System.Console;

namespace ManyMouseTest
{
    static class Program
    {
        static bool stopPolling = false;
        
        static void Main()
        {
            CancelKeyPress += OnCancelKeyPress;
            string finalMessage;
            try
            {
                WriteLine( "Starting up ManyMouse" );
                int result = Init();
                WriteLine( $"{AmountOfMiceDetected} mice on {DriverName}" );
                for( uint i = 0; i < result; i++ )
                    WriteLine( $"\t{DeviceName( i )}" );
                
                WriteLine( "Starting to poll, cancel(ctrl+c) to stop." );
                while( !stopPolling )
                {
                    while( PollEvent( out ManyMouseEvent mme ) > 0 )
                        WriteLine( mme );
                }
                
                Quit();
                finalMessage = "Done";
            }
            catch( Exception e )
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine( e );
                ResetColor();
                finalMessage = "Error";
            }
            WriteLine( $"{finalMessage}, press any key to exit..." );
            Read();
        }

        static void OnCancelKeyPress( object sender, ConsoleCancelEventArgs args )
        {
            stopPolling = true;
            args.Cancel = true;
            CancelKeyPress -= OnCancelKeyPress;
        }
    }
}
