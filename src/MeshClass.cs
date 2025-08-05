
namespace MeshTunnel
{
    public class MeshClass
    {
        public string? name;
        public string? meshid;
        public string? desc;
        public string? relayid;
        public int type;
        public ulong rights;
        public Dictionary<string, ulong>? links;

        public override string? ToString() { return name; }
    }
}