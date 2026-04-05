using System;
using System.IO.Ports;
using System.Net;
using System.Text;
using System.Collections.Specialized;
using System.Web;

class Program
{
    static SerialPort serialPort;

    static void Main(string[] args)
    {
        // SETTINGS 
        string portName = "COM1"; // The first half of the com0com pair
        int baudRate = 9600;
        string url = "http://localhost:8080/";

        // SETUP SERIAL PORT
        serialPort = new SerialPort(portName, baudRate);

        try
        {
            serialPort.Open();
            Console.WriteLine($"[SERIAL] Connected to {portName} successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERIAL ERROR] Could not open {portName}: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // SETUP HTTP SERVER 
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(url);

        try
        {
            listener.Start();
            Console.WriteLine($"[HTTP] Server is live at {url}");
            Console.WriteLine($"[TEST URL] {url}send?cmd=PolyComp");
            Console.WriteLine("----------------------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HTTP ERROR] Could not start server: {ex.Message}");
            return;
        }

        //MAIN LOOP 
        while (true)
        {
            try
            {
                // Wait for a request
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                string responseString = "";

                // Only handle requests to the "/send" path
                if (request.Url.AbsolutePath.ToLower() == "/send")
                {
                    // Parse the ?cmd= part of the URL
                    NameValueCollection query = HttpUtility.ParseQueryString(request.Url.Query);
                    string cmd = query["cmd"];

                    if (!string.IsNullOrEmpty(cmd))
                    {
                        // SEND TO SERIAL PORT
                        serialPort.WriteLine(cmd);

                        Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] Sent: {cmd}");
                        responseString = $"OK: Sent '{cmd}' to {portName}";
                    }
                    else
                    {
                        responseString = "Error: Missing 'cmd' parameter in URL.";
                    }
                }
                else
                {
                    responseString = "Welcome. Use /send?cmd=YourCommand to talk to Serial.";
                }

                // Send the response back to the browser/Postman
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RUNTIME ERROR] {ex.Message}");
            }
        }
    }
}
