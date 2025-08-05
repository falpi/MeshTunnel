
namespace MeshTunnel
{
    public class NodeClass
    {
        public string? name;
        public int icon;
        public string? nodeid;
        public string? meshid;
        public string? host;
        public int agentid;
        public int agentcaps;
        public int conn;
        public int rdpport;
        public ulong rights;
        public int mtype;
        public MeshClass? mesh;
        public Dictionary<string, ulong>? links;
        public string[]? users;

        public override string? ToString() { return name; }

        public string getStateString()
        {
            string status = "";
            //if (mtype == 3) return Properties.Resources.Local;
            //if ((conn & 1) != 0) { if (status.Length > 0) { status += ", "; } status += Properties.Resources.Agent; }
            //if ((conn & 2) != 0) { if (status.Length > 0) { status += ", "; } status += Properties.Resources.CIRA; }
            //if ((conn & 4) != 0) { if (status.Length > 0) { status += ", "; } status += Properties.Resources.AMT; }
            //if ((conn & 8) != 0) { if (status.Length > 0) { status += ", "; } status += Properties.Resources.Relay; }
            //if ((conn & 16) != 0) { if (status.Length > 0) { status += ", "; } status += Properties.Resources.MQTT; }
            //if (status == "") { status = Properties.Resources.Offline; }
            return status;
        }
    }
}