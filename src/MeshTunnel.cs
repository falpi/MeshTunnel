
using static MeshTunnel.Utils;

namespace MeshTunnel {

    static class MeshTunnel {

        // Variabili di lavoro
        public static bool mainThreadRunning = true;
        public static readonly CancellationTokenSource cts = new();

        private static MeshServer server = new MeshServer();
        private static Dictionary<string, object> serverConfig = new Dictionary<string, object>();

        private static List<MeshMapper> mappers = new List<MeshMapper>();
        private static List<Dictionary<string, object>> mappersConfig = new List<Dictionary<string, object>>();

        static async Task Main(string[] args) {

            // Cattura i segnali di interruzione
            catchSignals();

            // Legge configurazione tunnels
            readConfig(args[0]);

            // Genera logging
            Console.WriteLine("Application starting...");

            // Connette al relay server e crea i tunnel
            connectServer();
            createTunnels();

            // Genera logging
            Console.WriteLine("Application started");

            try {
                while ((server.connectionState == (int)WebSocketClient.ConnectionStates.Connected) &&
                      mappers.All(mapper => mapper.state == 1)) {

                    await Task.Delay(1000, cts.Token);
                }
            } catch (TaskCanceledException) {
                Console.WriteLine("Application stopping...");
            }

            // Disconnette dal relay server
            disconnectServer();

            // Genera logging
            Console.WriteLine("Application stopped");

            // Conclude main thread
            mainThreadRunning = false;
        }

        static void readConfig(string configPath) {

            // Estrazione dei parametri di configurazione
            Console.WriteLine("Read configuration...");

            try {
                // Leggi e deserializza il file di configurazione
                serverConfig = JsonHelper.Deserialize(File.ReadAllText(configPath));

                // Validazione della configurazione di base
                if (serverConfig == null)
                    throw new Exception("Invalid configuration file");

                // Estrai e valida i mappings
                if (!serverConfig.TryGetValue("mappings", out var mappingsObj) ||
                    !(mappingsObj is IEnumerable<object> mappingsList)) {
                    throw new Exception("Invalid or missing mappings in configuration");
                }

                // Converti i mappings in un formato più gestibile
                foreach (var mapping in mappingsList) {
                    if (mapping is Dictionary<string, object> mappingDict) {
                        ValidateMapping(mappingDict);
                        mappersConfig.Add(mappingDict);
                    }
                }

            } catch (Exception ex) {
                Console.WriteLine($"Configuration error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void connectServer() {

            // Gestisce la connessione al server
            Console.WriteLine("Server connecting...");

            // Estrai e valida i parametri di connessione
            string? hostname = GetConfigValue<string>(serverConfig, "hostname");
            string? username = GetConfigValue<string>(serverConfig, "username");
            string? password = GetConfigValue<string>(serverConfig, "password");

            // Prepara la url del serve gestendo l'eventuale presenza della login key
            Uri? serverurl = null;
            int keyIndex = hostname.IndexOf("?key=");
            if (keyIndex < 0) {
                serverurl = new Uri("wss://" + hostname + "/control.ashx");
            } else {
                serverurl = new Uri("wss://" + hostname.Substring(0, keyIndex) + "/control.ashx?key=" + hostname.Substring(keyIndex + 5));
            }

            // Inizia la connessione
            server.connect(serverurl, username, password, null, null);

            // Attende il completamento della connessione
            while (server.connectionState == (int)WebSocketClient.ConnectionStates.Connecting) {
                Thread.Sleep(100);
            }

            // Se si sono verificati errori esce
            if (server.connectionState != (int)WebSocketClient.ConnectionStates.Connected) {
                Console.WriteLine("Server connection error: " + server.disconnectCause + "," + server.disconnectMsg);
                Environment.Exit(1);
            }

            Console.WriteLine("Server connected");
        }

        static void disconnectServer() {
            Console.WriteLine("Disconnecting from server...");
            server.disconnect();
        }

        static void createTunnels() {

            Console.WriteLine("Starting tunnels...");

            // Estrai e valida i parametri di connessione
            string? hostname = GetConfigValue<string>(serverConfig, "hostname");

            try {

                // Avvia tutti i tunnel
                foreach (var mapping in mappersConfig!) {
                    var name = (string)mapping["name"];
                    var nodeName = (string)mapping["nodeName"];
                    var nodeId = (string)mapping["nodeId"];
                    var protocol = (int)GetLongValue(mapping["protocol"]);
                    var localPort = (int)GetLongValue(mapping["localPort"]);
                    var remotePort = (int)GetLongValue(mapping["remotePort"]);
                    var remoteIP = mapping.TryGetValue("remoteIP", out var rip) ? (string)rip : null;

                    // Prepara la url del mapper gestendo l'eventuale presenza della login key
                    string mapperurl;
                    int keyIndex = hostname.IndexOf("?key=");
                    if (keyIndex < 0) {
                        mapperurl = "wss://" + hostname + "/meshrelay.ashx?nodeid=" + nodeId;
                    } else {
                        mapperurl = "wss://" + hostname.Substring(0, keyIndex) + "/meshrelay.ashx?nodeid=" + nodeId + "&key=" + hostname.Substring(keyIndex + 5);
                    }

                    // Aggiusta la url del mapper in base al protocollo
                    if (protocol == 1) {
                        mapperurl += ("&tcpport=" + remotePort);
                        if (remoteIP != null) { mapperurl += "&tcpaddr=" + remoteIP; }
                    } else if (protocol == 2) {
                        mapperurl += ("&udpport=" + remotePort);
                        if (remoteIP != null) { mapperurl += "&udpaddr=" + remoteIP; }
                    }

                    if (remoteIP == null) remoteIP = "127.0.0.1";

                    Console.WriteLine($"Starting tunnel {name} for node {nodeName} (local:{localPort} -> remote:{remoteIP}:{remotePort})");

                    var mapper = new MeshMapper();
                    mapper.start(server, protocol, localPort, mapperurl, remotePort, remoteIP);

                    if (mapper.state != 1) {
                        throw new Exception($"Failed to start tunnel {name}");
                    }

                    mappers.Add(mapper);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Tunnel error: {ex.Message}");
            }
        }
    }
}