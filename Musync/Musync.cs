using System;
using System.Numerics;
using System.Windows.Forms;
using Blynclight;
using NAudio.Wave;
using System.Threading;
using System.Diagnostics;
using AForge.Math;
using System.Collections.Generic;
using NAudio.CoreAudioApi;

namespace Musync
{
    public partial class Musync : Form
    {
        private BlyncHelper device;

        private Queue<byte> data;
        private readonly object dataLock = new object();

        private readonly FourierTransform.Direction fftDir = FourierTransform.Direction.Forward;

        private bool blyncOn = false;

        private readonly int fftLength = 16384;

        private double sampleRate;

        // Loopback to read in system audio output
        private WasapiLoopbackCapture audioLoopback;

        private MMDeviceEnumerator deviceEnumerator;

        private string audioEndpointId;

        public Musync()
        {
            InitializeComponent();
            InitMusync();
            this.btnPlay_Click(null, null);
        }

        private void InitMusync()
        {
            // Init loopback and attach event handler
            this.SetLoopbackSource();

            // Track current default audio source
            this.deviceEnumerator = new MMDeviceEnumerator();
            this.audioEndpointId = this.deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;

            // Init FFT parameters
            this.data = new Queue<byte>();
            this.sampleRate = this.audioLoopback.WaveFormat.SampleRate;
            FFTHelper.SetSampleRate(this.sampleRate);

            // Init Blync light device
            this.device = new BlyncHelper(new BlynclightController());
            this.device.SetColor(LyncColor.White);

            // Form closing event listener
            this.FormClosing += Musync_FormClosing;
        }

        private void Musync_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.DisposeMusync();
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

                for (int i = 0; i < this.fftLength; i++)
                {
                    lock (dataLock)
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
                    ratio = upperPsd / lowerPsd / 1.2 ;
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
                if (silentStream)
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
            if (this.audioLoopback != null) { this.audioLoopback.Dispose(); }
            this.audioLoopback = new WasapiLoopbackCapture();
            this.audioLoopback.ShareMode = AudioClientShareMode.Shared;
            this.audioLoopback.DataAvailable += this.HandleFrame;
            this.audioLoopback.StartRecording();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            this.blyncOn = !this.blyncOn;

            if (this.blyncOn)
            {
                this.InitMusync();
                this.btnPlay.Text = "Stop";

                ThreadPool.QueueUserWorkItem(this.HandleFft);
            }
            else
            {
                this.DisposeMusync();
                this.btnPlay.Text = "Play";
            }
        }

        private void StopLight()
        {
            this.device.TurnOff();
        }

        private void DisposeMusync()
        {
            if (this.audioLoopback != null)
            {
                this.audioLoopback.Dispose();
            }

            if (this.device != null)
            {
                this.StopLight();
                this.device = null;
            }

            this.data.Clear();
        }
    }
}
