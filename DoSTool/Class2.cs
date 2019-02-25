namespace DoSTool
{
    public static class AttackVariables
    {
        public static byte[] BufferSize { get; set; }
        public static string IP { get; set; }
        public static string Mac { get; set; }
        public static bool ArpAttack { get; set; }
        public static bool IcmpAttack { get; set; }
        public static bool UdpAttack { get; set; }
        public static ushort PortNumber { get; set; }
    }
}
