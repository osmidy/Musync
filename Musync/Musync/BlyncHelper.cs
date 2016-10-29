using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blynclight;
using System.Threading;

namespace Musync
{
    /// <summary>
    /// Representation of possible Blynclight color values
    /// </summary>
    public enum LyncColor
    {
        White = 0, Yellow, Cyan, Green, Magenta, Blue, Red
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

        /// <summary>
        /// Creates a Blynchelper instance
        /// </summary>
        /// <param name="controller">BlynclightController wrapped by this instance</param>
        public BlyncHelper(BlynclightController controller)
        {
            this.controller = controller;
            this.numDevices = controller.InitBlyncDevices();
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
        
        /// <summary>
        /// Creates a pulse effect on the desired Blynclight
        /// </summary>
        /// <param name="color">Current color of the light during pulse</param>
        /// <param name="length">Total length of the pulse in seconds</param>
        public void Pulse(double length)
        {
            int blinkLength = (int) (length * 1000);
            if (blinkLength < 0)
            {
                return;
            }

            int pauseLength = 0;

            this.controller.ResetLight(0);
            this.SetColor(this.color);
            Thread.Sleep(blinkLength);
            // Thread.Sleep(pauseLength);
        }

        public void Pulse()
        {
            this.Pulse(.4);
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
        public void SetColor(LyncColor color, int index)
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
                default:
                    // Should never get here
                    break;
            }

            this.color = color;
        }

        public void SetColor(LyncColor color)
        {
            this.SetColor(color, 0);
        }
    }
}
