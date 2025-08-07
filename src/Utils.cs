
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace MeshTunnel {
    static class Utils {

        // ########################################################################################################################
        // Supporto alla validazione della configurazione
        // ########################################################################################################################

        public static T? GetConfigValue<T>(Dictionary<string, object> config, string key, bool optional = false) {
            if (!config.TryGetValue(key, out var value)) {
                if (optional) return default;
                throw new Exception($"Missing required configuration key: {key}");
            }

            if (value is T typedValue) {
                return typedValue;
            }

            throw new Exception($"Invalid type for configuration key {key}. Expected {typeof(T).Name}, got {value.GetType().Name}");
        }

        public static void ValidateMapping(Dictionary<string, object> mapping) {

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

        public static long GetLongValue(object value) {
            return value switch {
                long l => l,
                int i => i,
                JsonElement e when e.TryGetInt64(out var num) => num,
                _ => throw new Exception($"Cannot convert value to long: {value}")
            };
        }

        // ########################################################################################################################
        // Supporto alla gestione dei segnali
        // ########################################################################################################################

        public enum CtrlType {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        public delegate bool ConsoleCtrlHandler(CtrlType sig);

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);
        public static void catchSignals() {

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {

                PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx => {
                    Console.WriteLine("Signal received (SIGINT)");
                    MeshTunnel.cts.Cancel();
                    ctx.Cancel = true;
                });

                PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => {
                    Console.WriteLine("Signal received (SIGTERM)");
                    MeshTunnel.cts.Cancel();
                    ctx.Cancel = true;
                });

            } else {

                SetConsoleCtrlHandler(ctrlType => {
                    switch (ctrlType) {
                        case CtrlType.CTRL_C_EVENT:
                        case CtrlType.CTRL_BREAK_EVENT:

                            Console.WriteLine($"Signal received ({ctrlType})");
                            MeshTunnel.cts.Cancel();
                            return true;

                        case CtrlType.CTRL_CLOSE_EVENT:
                        case CtrlType.CTRL_LOGOFF_EVENT:
                        case CtrlType.CTRL_SHUTDOWN_EVENT:

                            Console.WriteLine($"Signal received ({ctrlType})");
                            MeshTunnel.cts.Cancel();
                            while (MeshTunnel.mainThreadRunning) Thread.Sleep(1000);
                            Console.WriteLine("Application terminated");
                            return true;

                        default:
                            return false;
                    }
                }, true);
            }
        }
    }
}
