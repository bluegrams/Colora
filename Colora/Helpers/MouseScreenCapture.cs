﻿using System;
using System.Windows.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;

namespace Colora.Helpers
{
    class MouseScreenCapture
    {
        private DispatcherTimer timer;
        private Bitmap screenBmp;
        private int captureSize;

        public MouseScreenCapture(int ms = 50)
        {
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, ms);
            timer.Tick += new EventHandler(timer_Tick);
            CaptureSize = 100;
            CaptureBitmap = new Bitmap(100, 100);
        }

        public event EventHandler CaptureTick;

        public bool IsCapturing => timer.IsEnabled;

        /// <summary>
        /// The real pixel size captured which will be zoomed to 100px.
        /// </summary>
        public int CaptureSize
        {
            get => captureSize;
            set
            {
                captureSize = value;
                screenBmp?.Dispose();
                screenBmp = null;
            }
        }

        /// <summary>
        /// The number of pixels in each direction to include for the sampled color.
        /// </summary>
        public byte ColorSampleWindow { get; set; }

        public Point MouseScreenPosition => new Point(Control.MousePosition.X, Control.MousePosition.Y);

        /// <summary>
        /// The captured area from screen.
        /// </summary>
        public Bitmap CaptureBitmap { get; private set; }

        public BitmapImage CaptureBitmapImage
        {
            get
            {
                BitmapImage bmpImage = new BitmapImage();
                using (MemoryStream memory = new MemoryStream())
                {
                    CaptureBitmap.Save(memory, ImageFormat.Bmp);
                    memory.Position = 0;
                    bmpImage.BeginInit();
                    bmpImage.StreamSource = memory;
                    bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                    bmpImage.EndInit();
                }
                return bmpImage;
            }
        }

        public Color PointerPixelColor { get; private set; }

        public void StartCapturing()
        {
            timer.Start();
        }

        public void StopCapturing()
        {
            timer.Stop();
        }

        private void updateScreenBmp()
        {
            if (screenBmp == null)
            {
                screenBmp = new Bitmap(CaptureSize, CaptureSize);
            }
            int shalf = (int)Math.Floor(CaptureSize / (double)2);
            using (Graphics g = Graphics.FromImage(screenBmp))
            {
                g.CopyFromScreen(Control.MousePosition.X - shalf, Control.MousePosition.Y - shalf,
                    0, 0, new Size(CaptureSize, CaptureSize));
            }
        }

        // this method is *very* slow for large windows
        private Color sampleColor()
        {
            int r = 0, g = 0, b = 0, t = 0;
            int center = CaptureSize / 2;
            for (int x = -ColorSampleWindow; x <= ColorSampleWindow; x++)
            {
                for (int y = -ColorSampleWindow; y <= ColorSampleWindow; y++)
                {
                    var col = screenBmp.GetPixel(center + x, center + y);
                    r += col.R;
                    g += col.G;
                    b += col.B;
                    t++;
                }
            }
            return Color.FromRgb((byte)(r / t), (byte)(g / t), (byte)(b / t));
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            updateScreenBmp();
            using (Graphics g = Graphics.FromImage(CaptureBitmap))
            {
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(screenBmp, new Rectangle(0, 0, 100, 100),
                    new Rectangle(0, 0, screenBmp.Width, screenBmp.Height), GraphicsUnit.Pixel);
            }
            PointerPixelColor = sampleColor();
            CaptureTick?.Invoke(this, new EventArgs());
        }
    }
}
