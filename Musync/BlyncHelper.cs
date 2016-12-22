using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blynclight;
using System.Threading;
using System.Diagnostics;

namespace Musync
{
    /// <summary>
    /// Representation of possible Blynclight color values
    /// </summary>
    public enum LyncColor
    {
        White = 0, Yellow, Green, Cyan, Blue, Magenta, Red
    }

    /// <summary>
    ///  Wrapper class for BlynclightController to extend the functionality of the Blynclight, 
    ///  particularly BLYNCUSB10 and BLYNCUSB17/20 models
    /// </summary>
    public class BlyncHelper
    {
        /// <summary>
        /// Controller for this instance
        /// </summary>
        private readonly BlynclightController controller;
        
        /// <summary>
        /// Number of connected Blynclights
        /// </summary>
        private readonly int numDevices;

        /// <summary>
        /// Current color of device at index 0
        /// </summary>
        private LyncColor color;

        private double lastPulseTime;
        private int minPulseDt = 300;

        /// <summary>
        /// Creates a Blynchelper instance
        /// </summary>
        /// <param name="controller">BlynclightController wrapped by this instance</param>
        public BlyncHelper(BlynclightController controller)
        {
            this.controller = controller;
            this.numDevices = controller.InitBlyncDevices();
            this.lastPulseTime = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
        }


        /// <summary>
        /// Controller for this instance
        /// </summary>
        public BlynclightController Controller
        {
            get { return this.controller; }
        }

        public int NumDevices
        {
            get { return this.numDevices; }
        }

        public LyncColor Color
        {
            get { return this.color;    }
            set { this.SetColor(value); }
        }
        
        /// <summary>
        /// Creates a pulse effect on the desired Blynclight
        /// </summary>
        /// <param name="color">Current color of the light during pulse</param>
        /// <param name="length">Total length of the pulse in milliseconds</param>
        public void Pulse(LyncColor color, int length = 25)
        {
            if (length < 0) return;

            var now = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
            double dt = now - this.lastPulseTime;

            if (dt < this.minPulseDt) return;

            this.lastPulseTime = now;

            this.controller.ResetLight(0);
            Thread.Sleep(length);
            this.SetColor(color);
        }

        public void Pulse()
        {
            this.Pulse(this.color);
        }

        public void SetColorAll(LyncColor color)
        {
            for (int i = 0; i < this.NumDevices; i++)
            {
                this.SetColor(color, i);
            }
        }

        public void TurnOff()
        {
            for (int i =  0; i < this.NumDevices; i++)
            {
                this.controller.ResetLight(i);
            }
        }


        /// <summary>
        /// Sets the color of the desired Blynclight
        /// </summary>
        /// <param name="color">Color to be set on Blynclight</param>
        /// <param name="index">Index of the targeted device</param>
        public void SetColor(LyncColor color, int index = 0)
        {
            switch(color)
            {
                case (LyncColor.White):
                    controller.TurnOnWhiteLight(index);
                    break;
                case (LyncColor.Red):
                    controller.TurnOnRedLight(index);
                    break;
                case (LyncColor.Yellow):
                    controller.TurnOnYellowLight(index);
                    break;
                case (LyncColor.Green):
                    controller.TurnOnGreenLight(index);
                    break;
                case (LyncColor.Cyan):
                    controller.TurnOnCyanLight(index);
                    break;
                case (LyncColor.Blue):
                    controller.TurnOnBlueLight(index);
                    break;                
                case (LyncColor.Magenta):
                    controller.TurnOnMagentaLight(index);
                    break;
            }

            this.color = color;
        }
    }
}
