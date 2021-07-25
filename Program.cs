using System.Threading;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;

namespace Proxy_API
{
    public class Program : ITool
    {
        static private readonly ManualResetEvent resetEvent = new ManualResetEvent(false);
        SocketServer socketServer;
        HTTPServer httpServer;
        Server server;
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Initialize();
            resetEvent.WaitOne();
        }
        public bool Initialize()
        {
            _ = Task.Run(InitializeAsync);
            return true;
        }
        public async Task InitializeAsync()
        {
            socketServer = new SocketServer(7272);
            await socketServer.StartAsync();
            httpServer = new HTTPServer(27272, 7272);
            await httpServer.StartAsync();
            server = new Server("API", socketServer);
            await server.StartAsync();
        }
        public void Dispose()
        {
            socketServer.Dispose();
            socketServer = null;
            httpServer.Stop();
            httpServer = null;
            server = null;
        }
    }
}
