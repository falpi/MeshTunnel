
using System.Text.Json;

namespace MeshTunnel {

    static class MeshTunnel {

        public static MeshServer? server;
        public static List<MeshMapper>? mappers;

        static void Main(string[] args) {

            // Prepara variabili di lavoro
            Uri? serverurl = null;
            string? hostname = null;
            string? username = null;
            string? password = null;
            string? certhash = null;
            Dictionary<string, object>? config;
            List<Dictionary<string, object>>? mappings = null;

            // Estrazione dei parametri di configurazione
            Console.WriteLine("Parse configuration...");

            try {
                // Leggi e deserializza il file di configurazione
                config = JsonHelper.Deserialize(File.ReadAllText(args[0]));

                // Validazione della configurazione di base
                if (config == null)
                    throw new Exception("Invalid configuration file");

                // Estrai e valida i parametri principali
                hostname = GetConfigValue<string>(config, "hostname");
                username = GetConfigValue<string>(config, "username");
                password = GetConfigValue<string>(config, "password");
                certhash = GetConfigValue<string>(config, "certhash", true);

                // Estrai e valida i mappings
                if (!config.TryGetValue("mappings", out var mappingsObj) ||
                    !(mappingsObj is IEnumerable<object> mappingsList)) {
                    throw new Exception("Invalid or missing mappings in configuration");
                }

                // Converti i mappings in un formato più gestibile
                mappings = new List<Dictionary<string, object>>();
                foreach (var mapping in mappingsList) {
                    if (mapping is Dictionary<string, object> mappingDict) {
                        ValidateMapping(mappingDict);
                        mappings.Add(mappingDict);
                    }
                }

                // Gestisce l'eventuale presenza della login key
                int keyIndex = hostname.IndexOf("?key=");
                if (keyIndex < 0) {
                    serverurl = new Uri("wss://" + hostname + "/control.ashx");
                } else {
                    serverurl = new Uri("wss://" + hostname.Substring(0, keyIndex) + "/control.ashx?key=" + hostname.Substring(keyIndex + 5));
                }

            } catch (Exception ex) {
                Console.WriteLine($"Configuration error: {ex.Message}");
                Environment.Exit(1);
            }

            // Gestisce la connessione al server
            Console.WriteLine("Server connecting...");

            server = new MeshServer();
            server.connect(serverurl, username, password, null, null);

            while (server.connectionState == (int)WebSocketClient.ConnectionStates.Connecting) {
                System.Threading.Thread.Sleep(100);
            }

            if (server.connectionState != (int)WebSocketClient.ConnectionStates.Connected) {
                Console.WriteLine("Server connection error: " + server.disconnectCause + "," + server.disconnectMsg);
                Environment.Exit(1);
            }

            Console.WriteLine("Server connected");
            Console.WriteLine("Starting tunnels...");

            // Inizializza la lista dei mapper
            mappers = new List<MeshMapper>();

            try {

                // Avvia tutti i tunnel
                foreach (var mapping in mappings!) {

                    var name = (string)mapping["name"];
                    var nodeName = (string)mapping["nodeName"];
                    var nodeId = (string)mapping["nodeId"];
                    var protocol = (int)GetLongValue(mapping["protocol"]);
                    var localPort = (int)GetLongValue(mapping["localPort"]);
                    var remotePort = (int)GetLongValue(mapping["remotePort"]);
                    var remoteIP = mapping.TryGetValue("remoteIP", out var rip) ? (string)rip : null;

                    string mapperurl;
                    int keyIndex = hostname.IndexOf("?key=");
                    if (keyIndex < 0) {
                        mapperurl = "wss://" + hostname + "/meshrelay.ashx?nodeid=" + nodeId;
                    } else {
                        mapperurl = "wss://" + hostname.Substring(0, keyIndex) + "/meshrelay.ashx?nodeid=" + nodeId + "&key=" + hostname.Substring(keyIndex + 5);
                    }

                    if (protocol == 1) {
                        mapperurl += ("&tcpport=" + remotePort);
                        if (remoteIP != null) { mapperurl += "&tcpaddr=" + remoteIP; }
                    } else if (protocol == 2) {
                        mapperurl += ("&udpport=" + remotePort);
                        if (remoteIP != null) { mapperurl += "&udpaddr=" + remoteIP; }
                    }

                    Console.WriteLine($"Starting tunnel {name} for node {nodeName} (local:{localPort} -> remote:{remoteIP}:{remotePort})");

                    var mapper = new MeshMapper();
                    mapper.start(server, protocol, localPort, mapperurl, remotePort, remoteIP);

                    if (mapper.state != 1) {
                        throw new Exception($"Failed to start tunnel {name}");
                    }

                    mappers.Add(mapper);
                }

                Console.WriteLine($"All {mappers.Count} tunnels activated successfully");

                // Mantieni attiva la connessione finché il server è connesso o ci sono tunnel attivi
                while (server.connectionState == (int)WebSocketClient.ConnectionStates.Connected &&
                       mappers.Any(m => m.state == 1)) {
                    System.Threading.Thread.Sleep(1000);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Tunnel error: {ex.Message}");
            } finally {
                // Ferma tutti i tunnel
                Console.WriteLine("Stopping all tunnels...");
                if (mappers != null) {
                    foreach (var mapper in mappers) {
                        try {
                            mapper.stop();
                        } catch (Exception ex) {
                            Console.WriteLine($"Error stopping tunnel: {ex.Message}");
                        }
                    }
                }

                // Disconnessione dal server
                Console.WriteLine("Disconnecting from server...");
                server?.disconnect();
            }
        }

        // Metodo helper per estrarre valori dalla configurazione con validazione
        private static T? GetConfigValue<T>(Dictionary<string, object> config, string key, bool optional = false) {
            if (!config.TryGetValue(key, out var value)) {
                if (optional) return default;
                throw new Exception($"Missing required configuration key: {key}");
            }

            if (value is T typedValue) {
                return typedValue;
            }

            throw new Exception($"Invalid type for configuration key {key}. Expected {typeof(T).Name}, got {value.GetType().Name}");
        }

        // Metodo per validare un singolo mapping
        private static void ValidateMapping(Dictionary<string, object> mapping) {
            var requiredFields = new Dictionary<string, Type> {
                { "nodeName", typeof(string) },
                { "name", typeof(string) },
                { "nodeId", typeof(string) },
                { "protocol", typeof(long) },
                { "localPort", typeof(long) },
                { "remotePort", typeof(long) }
            };

            foreach (var field in requiredFields) {
                if (!mapping.TryGetValue(field.Key, out var value)) {
                    throw new Exception($"Missing required field in mapping: {field.Key}");
                }

                if (field.Value == typeof(long)) {
                    if (value is not JsonElement jsonElement || !jsonElement.TryGetInt64(out _)) {
                        if (value is not long) {
                            throw new Exception($"Field '{field.Key}' must be a number");
                        }
                    }
                } else if (value.GetType() != field.Value) {
                    throw new Exception($"Field '{field.Key}' has wrong type. Expected {field.Value.Name}, got {value.GetType().Name}");
                }
            }

            // Validazione porte
            var localPort = GetLongValue(mapping["localPort"]);
            if (localPort <= 0 || localPort > 65535)
                throw new Exception($"Invalid localPort in mapping: {mapping["localPort"]}");

            var remotePort = GetLongValue(mapping["remotePort"]);
            if (remotePort <= 0 || remotePort > 65535)
                throw new Exception($"Invalid remotePort in mapping: {mapping["remotePort"]}");

            // Validazione remoteIP solo se presente
            if (mapping.TryGetValue("remoteIP", out var remoteIpObj)) {
                if (remoteIpObj is string remoteIp) {
                    if (!System.Net.IPAddress.TryParse(remoteIp, out _))
                        throw new Exception($"Invalid remoteIP format: {remoteIp}");
                } else {
                    throw new Exception("Field 'remoteIP' must be a string when present");
                }
            }
        }

        private static long GetLongValue(object value) {
            return value switch {
                long l => l,
                int i => i,
                JsonElement e when e.TryGetInt64(out var num) => num,
                _ => throw new Exception($"Cannot convert value to long: {value}")
            };
        }
    }
}