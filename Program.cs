
using System.Net;
using System.Text;
using System.IO;
using System.Threading;


namespace BrojacReciWebServer
{
    class Program
    {
        static readonly object cacheLock = new object();
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5050/");
            listener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(ObradaZahteva, context);
            }
        }

        static void ObradaZahteva(object state)
        {
            HttpListenerContext context = (HttpListenerContext)state;
            string zahtevUrl = context.Request.Url.LocalPath;
            Console.WriteLine("Zahtev primljen: " + zahtevUrl);

            string odgovor = "";

            lock (cacheLock)
            {
                if (cache.ContainsKey(zahtevUrl))
                {
                    odgovor = cache[zahtevUrl];
                    Console.WriteLine("Odgovor iz cache-a");
                }
            }

            if (odgovor == "")
            {

                string absolutePath = Path.GetFullPath(".");
               
                string directoryPath = Path.GetDirectoryName(absolutePath);
              
                string parentDirectoryPath = Directory.GetParent(directoryPath).ToString();
                
                string rootFolder = Directory.GetParent(parentDirectoryPath).ToString();

                string filename = context.Request.Url.Segments.Last();

                string putanjaF1 = Directory.GetFiles(rootFolder, filename, SearchOption.AllDirectories).FirstOrDefault();
            
                string putanjaF = Path.GetDirectoryName(putanjaF1) + zahtevUrl.Replace('/', '\\');
 
                if (!File.Exists(putanjaF))
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                    Console.WriteLine("Fajl nije pronadjen: " + putanjaF);
                    return;
                }

                string sadrzajFajla = File.ReadAllText(putanjaF);
                string[] words = sadrzajFajla.Split(new char[] { ' ', '\t', '\n', '\r',',','.','!','?' }, StringSplitOptions.RemoveEmptyEntries);
                int i = 0;
                foreach (string word in words)
                {
                    if (word.Length > 5 && char.IsUpper(word[0]))
                    {
                        Console.WriteLine(word);
                        i++;
                    }
                }
                odgovor = "Broj reci sa velikim pocetnim slovom koje su duze  od 5 karaktera je: " + i;
                lock (cacheLock)
                {
                    cache[zahtevUrl] = odgovor;
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
