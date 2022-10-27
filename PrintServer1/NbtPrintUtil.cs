
using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
//using ChemSW.Core;


namespace NbtPrintLib
{
    public static class NbtPrintUtil
    {

        public static bool PrintLabel( string aPrinterName, string LabelData, ref string statusInfo, ref string errMsg, string LabelType = "EPL Label" )
        {
            bool Ret = true;
            errMsg = string.Empty;
            bool isZPL = false;
            if (LabelType.ToLower().Equals("zpl label")) isZPL = true;
            else if (LabelType.ToLower().Equals("epl label")) isZPL = false;
            else //detect
            {
                if (LabelData.Length > 0)
                {
                    if (LabelData.Substring(0, 1) == "^") { 
                        isZPL = true;
                        statusInfo += " detected ZPL;";
                    }
                    else {
                        statusInfo += " detected EPL;";
                    }
                }
            }

            if ( LabelData != string.Empty )
            {
                string HexStarter = "<HEX>";
                string HexEnder = "</HEX>";
                if( LabelData.Contains( HexStarter ) )
                {
                    // We have to print it as byte[], not string

                    // Convert to a set of byte[]'s
                    Collection<byte[]> PartsOfLabel = new Collection<byte[]>();
                    string currentLabelData = LabelData;

                    while( currentLabelData.Contains( HexStarter ) )
                    {
                        Int32 hexstart = currentLabelData.IndexOf( HexStarter );
                        Int32 hexend = currentLabelData.IndexOf( HexEnder );
                        string prestr = currentLabelData.Substring( 0, hexstart );
                        string hexstr = currentLabelData.Substring( hexstart + HexStarter.Length, hexend - hexstart - HexEnder.Length + 1 );

                        //PartsOfLabel.Add( CswTools.StringToByteArray( prestr, isZPL ? EncodingTypeEnum.Unicode : EncodingTypeEnum.ASCII ) );
                        if (isZPL)
                        {
                            Encoding unicode = Encoding.Unicode;
                            PartsOfLabel.Add(unicode.GetBytes(prestr));
                        }
                        else
                        {
                            PartsOfLabel.Add(Encoding.ASCII.GetBytes(prestr));
                        }

                        PartsOfLabel.Add( Convert.FromBase64String( hexstr ) );

                        currentLabelData = currentLabelData.Substring( hexend + HexEnder.Length + 1 );
                    }

                    //PartsOfLabel.Add( CswTools.StringToByteArray( currentLabelData, isZPL ? EncodingTypeEnum.Unicode : EncodingTypeEnum.ASCII) );
                    if (isZPL)
                    {
                        Encoding unicode = Encoding.Unicode;
                        PartsOfLabel.Add(unicode.GetBytes(currentLabelData));
                    }
                    else
                    {
                        PartsOfLabel.Add(Encoding.ASCII.GetBytes(currentLabelData));
                    }

                    // Concatenate all parts into a single byte[]
                    Int32 newLen = 0;
                    foreach( byte[] part in PartsOfLabel )
                    {
                        newLen += part.Length;
                    }
                    byte[] entireLabel = new byte[newLen];
                    Int32 currentOffset = 0;
                    foreach( byte[] part in PartsOfLabel )
                    {
                        System.Buffer.BlockCopy( part, 0, entireLabel, currentOffset, part.Length );
                        currentOffset += part.Length;
                    }

                    //unmanaged code pointer required for the function call
                    IntPtr unmanagedPointer = Marshal.AllocHGlobal( entireLabel.Length );
                    try
                    {
                        Marshal.Copy( entireLabel, 0, unmanagedPointer, entireLabel.Length );
                        // Call unmanaged code

                        bool success = isZPL ? 
                            RawPrinterHelperUnicode.SendBytesToPrinter(aPrinterName, unmanagedPointer, entireLabel.Length) :
                            RawPrinterHelperAnsi.SendBytesToPrinter(aPrinterName, unmanagedPointer, entireLabel.Length);
                        if ( success )
                        {
                            statusInfo += " Printed;";
                        }
                        else
                        {
                            Ret = false;
                            errMsg = "Label printing error on client.";
                            statusInfo += " Error printing;" ;
                        }
                    }
                    finally
                    {
                        //unmanaged pointer must be explicitly released to prevent memory leak
                        Marshal.FreeHGlobal( unmanagedPointer );
                    }
                }
                else
                {
                    bool success = isZPL ?
                        RawPrinterHelperUnicode.SendStringToPrinter(aPrinterName, LabelData) :
                        RawPrinterHelperAnsi.SendStringToPrinter(aPrinterName, LabelData);
                    if ( success )
                    {
                        statusInfo += " Printed;";
                    }
                    else
                    {
                        Ret = false;
                        errMsg = "Label printing error on client.";
                        statusInfo += " Error printing;";
                    }

                }
            } // if( LabelData != string.Empty )
            else
            {
                statusInfo = "No label content.";
            }
            return Ret;
        } // _printLabel()
    }
}
