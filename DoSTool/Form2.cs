using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using static DoSTool.HelperFunctions;

namespace DoSTool
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            Location = new Point(ActiveForm.Location.X, ActiveForm.Location.Y + 300);
            backgroundWorker1.WorkerSupportsCancellation = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy)
                return;

            backgroundWorker1.RunWorkerAsync();
        }

        private void WriteLine(string text)
        {
            Invoke((MethodInvoker)delegate
            {
                richTextBox1.AppendText(text + "\n");
            });
        }

        private void WriteStats(Packet packet)
        {
            Invoke((MethodInvoker)delegate
            {
                richTextBox1.AppendText("Source: " + packet.Ethernet.IpV4.Source.ToString() +
                    " MAC: " + packet.Ethernet.Source +
                    " Length: " + packet.Length +
                    "\n");
            });
        }

        private bool TryParseSimpleRange(string ipRange, string ip)
        {
            try
            {
                string[] ipParts = ipRange.Split('-');
                string[] ipPart = ip.Split('.');

                byte[] beginIP = new byte[4];
                byte[] endIP = new byte[4];
                byte[] ipToCheck = new byte[4];

                int checksum = 0;

                for (int i = 0; i < 4; i++)
                {
                    string[] rangeParts = ipParts[0].Split('.');
                    string[] rangeParts2 = ipParts[1].Split('.');
                    beginIP[i] = byte.Parse(rangeParts[0]);
                    endIP[i] = byte.Parse(rangeParts2[0]);
                    ipToCheck[i] = byte.Parse(ipPart[0]);
                    if (beginIP[0] <= endIP[0] && endIP[0] >= beginIP[0] && checksum == 0)
                        checksum++;
                    if (beginIP[i]+ beginIP[i+1] <= endIP[i]+ endIP[i+1] && endIP[i]+ endIP[i+1] >= beginIP[i]+ beginIP[i+1])
                        checksum++;
                    if (checksum == 3)
                        break;
                }
                if (checksum == 3)
                {
                    for (int ii = 0; ii < 4; ii++)
                    {
                        if (ipToCheck[ii] >= beginIP[ii] && ipToCheck[ii] <= endIP[ii] && checksum == 3)
                            checksum++;
                        if (ipToCheck[ii] + ipToCheck[ii + 1] >= beginIP[ii] + beginIP[ii + 1] && ipToCheck[ii] + ipToCheck[ii + 1] <= endIP[ii] + endIP[ii + 1])
                            checksum++;
                        if (checksum == 6)
                            return true;
                    }
                }
            }
            catch (Exception)
            {
                //WriteLine(x.Message.ToString());
            }
            return false;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            using (PacketCommunicator communicator = DeviceManager.SelectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1))
            {
                Packet packet;
                do
                {
                    if (backgroundWorker1.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out packet);
                    switch (result)
                    {
                        case PacketCommunicatorReceiveResult.Timeout:
                            continue;
                        case PacketCommunicatorReceiveResult.Ok:
                            Invoke((MethodInvoker)delegate
                            {
                                string sourceIp = packet.Ethernet.IpV4.Source.ToString();
                                if (TryParseSimpleRange(textBox1.Text, sourceIp))
                                    WriteStats(packet);
                            });
                            break;
                        default:
                            throw new InvalidOperationException("The result " + result + " should never be reached here");
                    }

                }
                while (DeviceManager.SelectedDevice != null);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        public void MousePressed(object sender, MouseEventArgs e)
        {
            if (e == null)
                return;

            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, WM_NCLBUTTONDOWN, (UIntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            ScrollToBottom(richTextBox1);
        }
    }
}
