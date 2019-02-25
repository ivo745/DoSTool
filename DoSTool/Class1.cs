using System.Collections.Generic;
using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets.Ethernet;

namespace DoSTool
{
    public static class DeviceManager
    {
        public static PacketDevice SelectedDevice { get; set; }
        public static IList<LivePacketDevice> AllDevices = LivePacketDevice.AllLocalMachine;
        public static int DeviceId { get; set; }

        public static string GetDeviceMac()
        {
            MacAddress address = AllDevices[DeviceId].GetMacAddress();
            string addressString = address.ToString();
            return addressString;
        }

        public static string GetDeviceIP()
        {
            foreach (DeviceAddress address in SelectedDevice.Addresses)
            {
                if (address.Address != null && address.Address.Family.Equals(SocketAddressFamily.Internet))
                {
                    string MyString = address.Address.ToString();
                    char[] MyChar = { 'I', 'n', 't', 'e', 'r', 'n', 'e', 't', ' ' };
                    string NewString = MyString.TrimStart(MyChar);
                    return NewString;
                }
            }
            return null;
        }
    }
}
