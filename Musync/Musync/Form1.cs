using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Blynclight;
using System.Threading;

namespace Musync
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var color = BlyncHelper.LightColor.Red;
            var light = new BlynclightController();
            var numDevices = light.InitBlyncDevices();
            for (int i = 0; i < numDevices; i++)
            {
                var sleepTime = 1000;
                light.TurnOnRedLight(i);
                Thread.Sleep(sleepTime);
                BlyncHelper.Pulse(light,color, i, 25);
                
            }
        }
    }
}
