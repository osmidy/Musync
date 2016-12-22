using AForge.Math;
using Blynclight;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Musync
{
    public class Musync : ApplicationContext
    {
        /// <summary>
        /// System tray icon
        /// </summary>
        private NotifyIcon trayIcon;

        /* Menu Items */
        MenuItem toggleMenuItem;
        MenuItem nightlightMenuItem;
        MenuItem colorMenuItem;
        MenuItem reconnectMenuItem;
        MenuItem exitMenuItem;

        MenuItem lastCheckedColor;

        /// <summary>
        /// Nightlight mode state
        /// </summary>
        private bool nightlightOn;

        /// <summary>
        /// Running state of application
        /// </summary>
        private bool blyncOn;

        /// <summary>
        /// Blynclight device control
        /// </summary>
        private BlyncHelper device;

        ///<summary>
        /// Currently selected color, or -1 if automatic
        /// </summary>
        private LyncColor currentColor;

        /// <summary>
        /// Buffer for streamed-in audio
        /// </summary>
        private Queue<byte> data;

        /// <summary>
        /// Lock on data
        /// </summary>
        private readonly object dataLock = new object();

        /// <summary>
        /// FFT direction from time- to frequency- domain
        /// </summary>
        private readonly FourierTransform.Direction fftDir = FourierTransform.Direction.Forward;

        /// <summary>
        /// Max length of AForge.NET FFT
        /// </summary>
        private readonly int fftLength = 16384;

        /// <summary>
        /// Sample rate of audio stream
        /// </summary>
        private double sampleRate;

        /// <summary>
        /// Loopback to read in system audio output
        /// </summary>
        private WasapiLoopbackCapture audioLoopback;

        /// <summary>
        /// Access available audio devices
        /// </summary>
        private MMDeviceEnumerator deviceEnumerator;

        /// <summary>
        /// ID of current default audio device
        /// </summary>
        private string audioEndpointId;

        /// <summary>
        /// Instance of Random for such operations
        /// </summary>
        private readonly Random blyncRand = new Random();

        public Musync()
        {
            InitTray();
            InitMusync();
        }

        private void InitTray()
        {
            this.trayIcon = new NotifyIcon();
            this.trayIcon.Icon = new System.Drawing.Icon("content/logo.ico");

            toggleMenuItem = new MenuItem("Disable visualizer", ToggleVisualizer);
            nightlightMenuItem = new MenuItem("Nightlight mode", ToggleNightlight);
            colorMenuItem = new MenuItem("Set color", this.ColorSubMenuFactory());
            reconnectMenuItem = new MenuItem("Reset && reconnect", ResetMusync);
            exitMenuItem = new MenuItem("Exit", ExitMusync);

            this.trayIcon.ContextMenu = new ContextMenu(new MenuItem[]
            {
                toggleMenuItem, nightlightMenuItem, colorMenuItem, reconnectMenuItem, exitMenuItem
            });

            this.nightlightOn = false;
            this.trayIcon.Visible = true;
        }

        private void InitMusync()
        {
            // Track current default audio source
            this.deviceEnumerator = new MMDeviceEnumerator();
            this.audioEndpointId = this.deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;

            // Init buffer
            this.data = new Queue<byte>();

            // Init Blync Light device
            this.device = new BlyncHelper(new BlynclightController());
            this.device.SetColor(LyncColor.White);
            this.currentColor = LyncColor.Automatic;

            // Begin stream listener thread
            this.blyncOn = true;
            ThreadPool.QueueUserWorkItem(this.HandleFft);
        }

        private void HandleFrame(object sender, WaveInEventArgs e)
        {
            var n = e.BytesRecorded;

            // Average left and right channel amplitudes
            for (int i = 0; i < n; i += 2)
            {
                byte avg = ((byte)((int)e.Buffer[i] + (int)e.Buffer[i + 1]));
                avg >>= 2;
                lock (dataLock)
                {
                    this.data.Enqueue(avg);
                }
            }
            Thread.Sleep(25);
        }

        private void HandleFft(object dummy)
        {
            double avgPsdRatio = 0;
            int ratioCount = 0;

            int numSilentFrames = 0;
            bool silentStream = false;

            // Init loopback and attach new frame handler
            this.SetLoopbackSource();            

            while (this.blyncOn)
            {
                // Check for change in audio endpoint
                var id = this.deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
                if (!id.Equals(this.audioEndpointId))
                {
                    this.audioEndpointId = id;

                    // Reassign loopback
                    this.SetLoopbackSource();

                    Thread.Sleep(25);
                    continue;
                }

                if (this.data.Count < this.fftLength)
                {
                    Thread.Sleep(25);
                    continue;
                }

                int n = this.fftLength;
                Complex[] complexData = new Complex[n];

                lock (this.dataLock)
                {
                    for (int i = 0; i < this.fftLength; i++)
                    {
                        complexData[i] = new Complex(this.data.Dequeue(), 0);
                    }
                }

                FourierTransform.FFT(complexData, this.fftDir);

                int minBassIndex = FFTHelper.FreqToIndex(FFTHelper.MinBassFreq);
                int maxBassIndex = FFTHelper.FreqToIndex(FFTHelper.MaxBassFreq);
                double peakBassMag = 0;

                // Use bass to handle pulsing
                for (int i = minBassIndex; i < maxBassIndex; i++)
                {
                    double mag = complexData[i].Magnitude;


                    if (mag > peakBassMag)
                    {
                        peakBassMag = mag;
                    }
                }

                int bound1 = FFTHelper.FreqToIndex(FFTHelper.MaxBassFreq);
                int bound2 = FFTHelper.FreqToIndex(1000);
                int bound3 = FFTHelper.FreqToIndex(4000);

                double lowerPsd = 0;
                double upperPsd = 0;

                // Ratios of power spectral densities in different regions will determine cube color
                for (int i = bound1; i < bound2; i++)
                {
                    double mag = complexData[i].Magnitude;
                    lowerPsd += mag * mag;
                }
                for (int i = bound2; i < bound3; i++)
                {
                    double mag = complexData[i].Magnitude;
                    upperPsd += mag * mag;
                }

                double ratio;
                if (lowerPsd == 0)
                {
                    ratio = 0;
                }
                else
                {
                    ratio = upperPsd / lowerPsd / 1.2;
                }

                if (ratio == 0)
                {
                    numSilentFrames++;
                }

                // Was silent, but no longer is
                if (silentStream && ratio > 0)
                {
                    silentStream = false;
                    avgPsdRatio = ((avgPsdRatio * ratioCount) + ratio) / ++ratioCount;
                    numSilentFrames = 0;
                }
                // Now has extended period (~3 sec) of silence
                else if (numSilentFrames > 3 * this.fftLength / this.sampleRate)
                {
                    silentStream = true;
                    avgPsdRatio = 0;
                    ratioCount = 0;
                }
                // Still playing
                else
                {
                    avgPsdRatio = ((avgPsdRatio * ratioCount) + ratio) / ++ratioCount;
                }

                LyncColor newColor;
                if (!this.ColorAutomated())
                {
                    newColor = this.currentColor;
                }
                else if (silentStream)
                {
                    newColor = LyncColor.White;
                }
                else
                {
                    newColor = FFTHelper.FreqToColor(avgPsdRatio);
                }

                bool pulseBeat = FFTHelper.ShouldPulse(peakBassMag);
                if (pulseBeat)
                {
                    this.device.Pulse(newColor);
                }
                else
                {
                    this.device.Color = newColor;
                }

                Thread.Sleep(25);
            }
        }

        private void SetLoopbackSource()
        {
            if (this.audioLoopback != null)
            {
                this.audioLoopback.Dispose();
            }

            this.audioLoopback = new WasapiLoopbackCapture();
            this.audioLoopback.ShareMode = AudioClientShareMode.Shared;
            this.audioLoopback.DataAvailable += this.HandleFrame;
            this.audioLoopback.StartRecording();
            this.sampleRate = this.audioLoopback.WaveFormat.SampleRate;
            FFTHelper.SetSampleRate(this.sampleRate);
        }

        private MenuItem[] ColorSubMenuFactory()
        {
            var colorOpts = new string[] { "Automatic", "White", "Yellow", "Green", "Cyan", "Blue", "Magenta", "Red" };
            MenuItem[] items = new MenuItem[colorOpts.Length];

            for (int i = 0; i < colorOpts.Length; i++)
            {
                items[i] = new MenuItem(colorOpts[i], SetCubeColor);
            }

            items[0].Checked = true;
            this.lastCheckedColor = items[0];
            return items;
        }


        private void ToggleVisualizer(object s, EventArgs e)
        {
            this.blyncOn = !this.blyncOn;

            if (this.blyncOn)
            {
                if (this.nightlightOn)
                {
                    this.ToggleNightlight(s, e);
                }
                ThreadPool.QueueUserWorkItem(this.HandleFft);
                this.toggleMenuItem.Text = "Disable visualizer";
            }
            else
            {
                // Turn off listener stream to save memory
                this.audioLoopback.Dispose();
                this.data.Clear();

                this.StopLight();
                this.toggleMenuItem.Text = "Enable visualizer";
            }
        }

        private void ToggleNightlight(object s, EventArgs e)
        {
            this.nightlightOn = !this.nightlightOn;

            if (this.nightlightOn)
            {
                if (this.blyncOn)
                {
                    this.ToggleVisualizer(s, e);
                }

                ThreadPool.QueueUserWorkItem(CycleNightlight);
                this.nightlightMenuItem.Checked = true;

            }
            else
            {
                this.StopLight();
                this.nightlightMenuItem.Checked = false;
            }
        }

        private void CycleNightlight(object s)
        {
            while (this.nightlightOn)
            {
                if (this.ColorAutomated())
                {
                    LyncColor color = (LyncColor)this.blyncRand.Next(BlyncHelper.NumColors);
                    this.device.SetColor(color);
                }
                else
                {
                    this.device.SetColor(this.currentColor);
                }
                Thread.Sleep(30 * 1000);
            }
        }

        private void SetCubeColor(object sender, EventArgs e)
        {
            var item = sender as MenuItem;

            this.currentColor = (LyncColor) (item.Index - 1);

            // Override previous settings for nightlight mode
            if (this.nightlightOn && this.ColorAutomated())
            {
                LyncColor color = (LyncColor)this.blyncRand.Next(BlyncHelper.NumColors);
                this.device.SetColor(color);
            }
            else if (this.nightlightOn && !this.ColorAutomated())
            {
                this.device.SetColor(this.currentColor);
            }

            this.lastCheckedColor.Checked = false;
            item.Checked = true;
            this.lastCheckedColor = item;
        }

        private void ResetMusync(object s, EventArgs e)
        {
            this.DisposeMusync();
            this.InitMusync();
        }

        private void StopLight()
        {
            this.device.TurnOff();
        }

        private bool ColorAutomated()
        {
            return this.currentColor.Equals(LyncColor.Automatic);
        }

        private void DisposeMusync()
        {
            if (this.blyncOn) this.ToggleVisualizer(null, null);
            if (this.nightlightOn) this.ToggleNightlight(null, null);

            if (this.audioLoopback != null)
            {
                this.audioLoopback.Dispose();
                this.audioLoopback = null;
            }

            if (this.device != null)
            {
                this.StopLight();
                this.device = null;
            }

            this.data.Clear();
        }

        private void ExitMusync(object s, EventArgs e)
        {
            this.DisposeMusync();
            this.trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
