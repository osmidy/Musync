using System;
using System.Numerics;
using System.Windows.Forms;
using Blynclight;
using NAudio.Wave;
using System.Threading;
using AForge.Math;
using System.Collections.Generic;

namespace Musync
{
    public partial class Form1 : Form
    {
        private BlyncHelper device;

        private List<Complex> data;
        
        private readonly FourierTransform.Direction fftDir = FourierTransform.Direction.Forward;

        private bool blyncOn = false;

        private readonly int fftLength = 16384;  //todo

        private const double SampleRate = 44100.0;

        // Loopback to read in system audio output
        private WasapiLoopbackCapture audioLoopback;
        
        private readonly Random blyncRand = new Random();

        public Form1()
        {
            InitializeComponent();
            data = new List<Complex>();
            InitMusync();
        }

        private void InitMusync()
        {
            // Init loopback and attach event handler
            this.audioLoopback = new WasapiLoopbackCapture();
            this.audioLoopback.DataAvailable += HandleFrame;

            // Init Blync light device
            this.device = new BlyncHelper(new BlynclightController());
            this.device.SetColor(LyncColor.Green);
        }

        private void HandleFrame(object sender, WaveInEventArgs e)
        {
            var length = e.BytesRecorded;
            var frame = new byte[length];
            Array.Copy(e.Buffer, frame, length);

            for (int i = 0; i < length; i++)
            {
                double b = frame[i];
                var complexData = new Complex(b, 0);
                this.data.Add(complexData);
            }

            if (this.data.Count >=  this.fftLength) ///uhm...TODO
            {
                // TODO: get first 'count' elts, remove from active list, and process (change null)
                var arr = new Complex[this.data.Count];
                this.data.CopyTo(arr);

                var complexData = new Complex[this.fftLength];
                Array.Copy(arr, complexData, this.fftLength);

                this.data.RemoveRange(0, this.fftLength);

                ThreadPool.QueueUserWorkItem(this.HandleFft, complexData);
            }
            
        }

        private void HandleFft(object input)
        {
            Complex[] data = (Complex[])input;
            int n = data.Length;
            if (n < 2)
            {
                return;
            }

            FourierTransform.FFT(data, this.fftDir);
            // TODO: use number of samples to find index of last 'bass' freq.
            int bassMidBoundary = (int) (500 * n / SampleRate);

            // Use bass to handle pulsing
            // TODO: save the bass threshold


            // Use mid and treble to choose color
            double maxMagnitude = 0;
            int maxFreqIndex = 0;
            for (int i = bassMidBoundary; i < n; i++)
            {
                var mag = data[i].Magnitude;
                if (mag > maxMagnitude)
                {
                    maxMagnitude = mag;
                    maxFreqIndex = i;
                }
            }
            double freq = maxFreqIndex * SampleRate / n;

            // TODO: map power to an appropriate color; make some funciton of mid and treble (difference?)
            this.device.SetColor(AudioReader.FreqToColor(freq));
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            this.blyncOn = !this.blyncOn;

            if (this.blyncOn)
            {
                this.audioLoopback.StartRecording();
                this.btnPlay.Text = "Stop";
            } else
            {
                this.audioLoopback.StopRecording();
                this.StopLight();
                this.btnPlay.Text = "Play";
            }
        }

        private void StopAudio()
        {
            this.Dispose();
        }
        

        private void StartLight(object state)
        {
            do
            {
                var color = (LyncColor)this.blyncRand.Next(7);
                this.device.SetColor(color);
                this.device.Pulse();
            } while (blyncOn);
        }

        private void StopLight()
        {
            this.device.TurnOff();
        }

        // TODO: Dispose on close
        private void Dispose()
        {
            this.audioLoopback.Dispose();
            this.StopLight();
        }
    }
}
