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
    ///  Representation of a particulary Blynclight device. Helper functions
    ///  are included to extend the functionality of the Blynclight, 
    ///  particularly BLYNCUSB10 and BLYNCUSB17/20 models
    /// </summary>
    public class BlyncHelper
    {
        public enum LightColor
        {
            Red, Green, Blue, Cyan, Magenta, Yellow, Orange, White
        }
        /// <summary>
        /// Creates a pulse effect on the desired Blynclight
        /// </summary>
        /// <param name="controller">The controller for connected devices</param>
        /// <param name="color">Current color of the light during pulse</param>
        /// <param name="index">Index of the targeted device</param>
        /// <param name="length">Total length of the pulse in milliseconds</param>
        public static void Pulse(BlynclightController controller, LightColor color, int index, int speed)
        {
            var sleepTime = speed / 2;

            controller.ResetLight(index);
            Thread.Sleep(sleepTime);
            SetColor(controller, color, index);
        }

        public static void SetColor(BlynclightController controller, LightColor color, int index)
        {
            switch(color)
            {
                case (LightColor.Red):
                    controller.TurnOnRedLight(index);
                    break;
                case (LightColor.Green):
                    controller.TurnOnGreenLight(index);
                    break;
                case (LightColor.Blue):
                    controller.TurnOnBlueLight(index);
                    break;
                case (LightColor.Cyan):
                    controller.TurnOnCyanLight(index);
                    break;
                case (LightColor.Magenta):
                    controller.TurnOnMagentaLight(index);
                    break;
                case (LightColor.Yellow):
                    controller.TurnOnYellowLight(index);
                    break;
                case (LightColor.Orange):
                    controller.TurnOnOrangeLight(index);
                    break;
                case (LightColor.White):
                    controller.TurnOnWhiteLight(index);
                    break;
            }
        }
    }
}
