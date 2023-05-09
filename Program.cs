
using System.Net;
using System.Text;


namespace WordCountWebServer
{
    class Program
    {
        static readonly object cacheLock = new object();
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(ProcessRequest, context);
            }
        }

        static void ProcessRequest(object state)
        {
            HttpListenerContext context = (HttpListenerContext)state;
            string requestUrl = context.Request.Url.LocalPath;
            Console.WriteLine("Zahtev primljen: " + requestUrl);

            string odgovor = "";

            lock (cacheLock)
            {
                if (cache.ContainsKey(requestUrl))
                {
                    odgovor = cache[requestUrl];
                    Console.WriteLine("Odgovor iz cache-a");
                }
            }

            if (odgovor == "")
            {
                string putanjaF = "C:\\Users\\Stole\\source\\repos\\Sistemsko projekat 1" + requestUrl.Replace('/', '\\');
                if (!File.Exists(putanjaF))
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                    Console.WriteLine("Fajl nije pronadjen: " + putanjaF);
                    return;
                }

                string sadrzajFajla = File.ReadAllText(putanjaF);
                string[] words = sadrzajFajla.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                int i = 0;
                foreach (string word in words)
                {
                    if (word.Length > 5 && char.IsUpper(word[0]))
                    {
                        i++;
                    }
                }
                odgovor = "Broj reci sa velikim pocetnim slovom koje su duze  od 5 karaktera je: " + i;
                lock (cacheLock)
                {
                    cache[requestUrl] = odgovor;
                    Console.WriteLine("Odgovor kesiran");
                }
            }

            byte[] buffer = Encoding.UTF8.GetBytes(odgovor);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
            Console.WriteLine("Zahtev obradjen");
        }
    }
}
