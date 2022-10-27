using System;
//using System.Drawing;
//using System.Drawing.Printing;
//using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
//using ChemSW.Core;


namespace NbtPrintLib
{
    public class RawPrinterHelperUnicode
    {
        // Structure and API declarations:
        [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
        public class DOCINFOA
        {
            [MarshalAs( UnmanagedType.LPStr )]
            public string pDocName;
            [MarshalAs( UnmanagedType.LPStr )]
            public string pOutputFile;
            [MarshalAs( UnmanagedType.LPStr )]
            public string pDataType;
        }
        [DllImport( "winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall )]
        public static extern bool OpenPrinter( [MarshalAs( UnmanagedType.LPStr )] string szPrinter, out IntPtr hPrinter, IntPtr pd );

        [DllImport( "winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall )]
        public static extern bool ClosePrinter( IntPtr hPrinter );

        [DllImport( "winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall )]
        public static extern bool StartDocPrinter( IntPtr hPrinter, Int32 level, [In, MarshalAs( UnmanagedType.LPStruct )] DOCINFOA di );

        [DllImport( "winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall )]
        public static extern bool EndDocPrinter( IntPtr hPrinter );

        [DllImport( "winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall )]
        public static extern bool StartPagePrinter( IntPtr hPrinter );

        [DllImport( "winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall )]
        public static extern bool EndPagePrinter( IntPtr hPrinter );

        [DllImport( "winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall )]
        public static extern bool WritePrinter( IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten );

        // SendBytesToPrinter()
        // When the function is given a printer name and an unmanaged array
        // of bytes, the function sends those bytes to the print queue.
        // Returns true on success, false on failure.
        public static bool SendBytesToPrinter( string szPrinterName, IntPtr pBytes, Int32 dwCount )
        {
            Int32 dwError = 0, dwWritten = 0;
            IntPtr hPrinter = new IntPtr( 0 );
            DOCINFOA di = new DOCINFOA();
            bool bSuccess = false; // Assume failure unless you specifically succeed.

            di.pDocName = "CSW RAW Document";
            di.pDataType = "RAW";

            // Open the printer.
            if( OpenPrinter( szPrinterName.Normalize(), out hPrinter, IntPtr.Zero ) )
            {
                // Start a document.
                if( StartDocPrinter( hPrinter, 1, di ) )
                {
                    // Start a page.
                    if( StartPagePrinter( hPrinter ) )
                    {
                        // Write your bytes.
                        bSuccess = WritePrinter( hPrinter, pBytes, dwCount, out dwWritten );
                        EndPagePrinter( hPrinter );
                    }
                    EndDocPrinter( hPrinter );
                }
                ClosePrinter( hPrinter );
            }
            // If you did not succeed, GetLastError may give more information
            // about why not.
            if( bSuccess == false )
            {
                dwError = Marshal.GetLastWin32Error();
            }
            return bSuccess;
        }
        public static bool SendStringToPrinter( string szPrinterName, string szString )
        {
            Encoding unicode = Encoding.Unicode;
            byte[] encodedString = unicode.GetBytes(szString);


            //unmanaged code pointer required for the function call
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(encodedString.Length);
            try
            {
                Marshal.Copy(encodedString, 0, unmanagedPointer, encodedString.Length);
                // Call unmanaged code
                SendBytesToPrinter(szPrinterName, unmanagedPointer, encodedString.Length);
            }
            finally
            {
                //unmanaged pointer must be explicitly released to prevent memory leak
                Marshal.FreeHGlobal(unmanagedPointer);
            }
            return true;
        }
    }
}