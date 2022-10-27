using System;
using System.Net;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;
using NbtPrintLib;
using System.Web;

/*
public static class GdiWrapper
{

    public static IntPtr printerDC;
    public static Int32 PASSTHROUGH = 19; // https://winappdbg.sourceforge.net/doc/latest/reference/winappdbg.win32.gdi32-module.html

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);
    [DllImport("gdi32.dll")]
    public static extern IntPtr DeleteDC(IntPtr hDC);
    [DllImport("gdi32.dll")]
    public static extern int ExtEscape(IntPtr hdc, int nEscape, int cbInput,string lpszInData, int cbOutput, IntPtr lpszOutData);
}
*/

public class HttpServer
{
    public int Port=10069;
    public string PrinterName="no_printer_name";

    private HttpListener _listener;

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://127.0.0.1:" + Port.ToString() + "/");
        _listener.Start();
        Receive();
    }

    public void Stop()
    {
        _listener.Stop();
    }

    private void Receive()
    {
        _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
    }

    private void ListenerCallback(IAsyncResult result)
    {
        if (_listener.IsListening)
        {
            var context = _listener.EndGetContext(result);
            var request = context.Request;
            var printerName = "DavidPrn1";

            // do something with the request
            Console.WriteLine($"{request.HttpMethod} {request.Url}");

            if (request.HasEntityBody)
            {
                
                var body = request.InputStream;
                var encoding = request.ContentEncoding;
                var reader = new StreamReader(body, encoding);
                if (request.ContentType != null)
                {
                    Console.WriteLine("Client data content type {0}", request.ContentType);
                }
                Console.WriteLine("Client data content length {0}", request.ContentLength64);

                Console.WriteLine("Start of data:");
                string s = HttpUtility.UrlDecode(reader.ReadToEnd());
                if (s.IndexOf("labeldata=") > -1)
                {
                    s = s.Replace("labeldata=", "");
                }
                else s = "";
                Console.WriteLine(s);
                Console.WriteLine("End of data:");
                reader.Close();
                body.Close();


                if (s.Length > 0)
                {
                    //test send to printer
                    foreach (string printer in PrinterSettings.InstalledPrinters)
                    {
                        if (printer == PrinterName)
                        {
                            Console.WriteLine("printer_found=" + printer);
                            Console.WriteLine("printlabel(s)");
                            /*
                            //using gdi does not work
                            GdiWrapper.printerDC = GdiWrapper.CreateDC("WINSPOOL", printer, "",IntPtr.Zero);
                            if (GdiWrapper.printerDC != IntPtr.Zero)
                            {
                                Console.WriteLine("CreateDC()=success");
                            }
                            else
                            {
                                Console.WriteLine("CreateDC()=failed");
                            }

                            string abuff = "line1\nline2\nline3\n";

                            try {
                                int res = GdiWrapper.ExtEscape(GdiWrapper.printerDC, GdiWrapper.PASSTHROUGH, abuff.Length, abuff, 0, IntPtr.Zero);
                                Console.WriteLine("ExtEscape() returned=" + res.ToString());

                            }
                            finally {
                                GdiWrapper.DeleteDC(GdiWrapper.printerDC);
                            }
                            */

                        }
                    }
                }

            }
            var response = context.Response;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "text/plain";
            response.OutputStream.Write(Encoding.ASCII.GetBytes("Printed the label!"), 0, 0);
            response.OutputStream.Close();

            Receive();
        }
    }
}

class Program
{
    private static bool _keepRunning = true;

    static int Main(string[] args)
    {
        Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Program._keepRunning = false;
        };

        var def_port = 10061;
        var def_prn = "LabelPrinter";

        for(int i = 0; i < args.Length; ++i)
        {
            //port arg
            var ip = args[i].IndexOf("port=");
            if (ip > -1)
            {
                def_port = Convert.ToInt32(args[i].Substring(ip + 5));
            }
            //printer (name) arg
            ip = args[i].IndexOf("printer=");
            if (ip > -1)
            {
                def_prn = args[i].Substring(ip + 8);
            }

            Console.WriteLine("arg[" + i.ToString() + "] is " + args[i]);
        }

        Console.WriteLine("Starting HTTP listener...");

        var httpServer = new HttpServer();
        httpServer.Port = def_port;
        httpServer.PrinterName = def_prn;

        httpServer.Start();

        while (Program._keepRunning) { }

        httpServer.Stop();

        Console.WriteLine("Exiting gracefully...");
        return (0);
    }
}
