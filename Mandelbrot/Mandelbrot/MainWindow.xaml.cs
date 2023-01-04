using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mandelbrot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // These change the constant for Mandelbrot and Julia sets
        private static double IterateConstantValReal = -0.8;
        private static double IterateConstantValComplex = +0.153;
        private bool isAnimating = false;
        private static bool isMandelbrot = true;

        WriteableBitmap bmp = new WriteableBitmap(
               (int)SystemParameters.PrimaryScreenWidth,
               (int)SystemParameters.PrimaryScreenHeight,
               96,
               96,
               PixelFormats.Bgr24,
               null);
        GradientColorGenerator colors = new GradientColorGenerator();
        int depth = 100;
        double step = 0.002;
        double centerX = -0.5;
        double centerY = 0;

        public MainWindow()
        {
            InitializeComponent();
            Render();
            img.ImageSource = bmp;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var p = e.GetPosition(this);
            centerX += (p.X - bmp.PixelWidth / 2) * step;
            centerY += (p.Y - bmp.PixelHeight / 2) * step;
            Render();

            base.OnMouseDown(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                depth = (int)(e.Delta > 0 ? 1.1 * depth : 0.9 * depth);
            }
            else
            {
                step *= e.Delta > 0 ? 0.9 : 1.1;
            }

            Render();

            base.OnMouseWheel(e);
        }

        private void Render()
        {
            GenerateImage(
                bmp,
                centerX - (bmp.PixelWidth / 2) * step,
                centerY - (bmp.PixelHeight / 2) * step,
                step,
                depth,
                colors.GeneratePalette(depth));
        }

        public static void GenerateImage(WriteableBitmap bmp, double startX, double startY, double step, int depth, int[] palette)
        {
            long buffer = 0;
            var stride = 0;
            var width = 0;
            var height = 0;

            bmp.Dispatcher.Invoke(() =>
            {
                bmp.Lock();
                buffer = (long)bmp.BackBuffer;
                stride = bmp.BackBufferStride;
                width = bmp.PixelWidth;
                height = bmp.PixelHeight;
            });

            unsafe
            {
                Parallel.For(0, height, j =>
                {
                    Parallel.For(0, width, i =>
                    {
                        var pixel = buffer + j * stride + i * 3;
                        *(int*)pixel = palette[Iterate(
                            startX + i * step,
                            startY + j * step,
                            palette.Length)];
                    });
                });
            }

            bmp.Dispatcher.Invoke(() =>
            {
                bmp.AddDirtyRect(new Int32Rect(0, 0, width, height));
                bmp.Unlock();
            });
        }

        static int Iterate(double x, double y, int limit)
        {
            var x0 = x;
            var y0 = y;
            // Make x0 = 0 and y0 = y for Mandelbrot set.  Otherwise variables for Julia set are set with x0 and y0
            if(isMandelbrot == false)
            {
                x0 = IterateConstantValReal;//-0.8;// x;
                y0 = IterateConstantValComplex;// + .156;// y;
            }

            for (var i = 0; i < limit; i++)
            {
                if (x * x + y * y >= 4)
                    return i;

                var zx = x * x - y * y + x0;
                y = 2 * x * y + y0;
                x = zx;
            }

            return 0;
        }

        class GradientColorGenerator
        {
            private readonly IList<dynamic> _stops = new List<dynamic>
            {
                new {v=0.000f, r=0.000f, g=0.000f, b=0.000f },
                new {v=0.160f, r=0.125f, g=0.420f, b=0.796f },
                new {v=0.420f, r=0.930f, g=1.000f, b=1.000f },
                new {v=0.642f, r=1.000f, g=0.666f, b=0.000f },
                new {v=1.000f, r=0.000f, g=0.000f, b=0.000f },
            };

            public int[] GeneratePalette(int depth)
            {
                var x = new int[depth];
                for (int i = 0; i < depth; i++)
                {
                    x[i] = GetColor(i / (float)depth);
                }
                return x;
            }

            private int GetColor(float p)
            {
                if (p > 1) p = 1;
                for (int i = 0; i < _stops.Count; i++)
                {
                    if (_stops[i].v <= p && _stops[i + 1].v >= p)
                    {
                        var s0 = _stops[i];
                        var s1 = _stops[i + 1];
                        var pos = (p - s0.v) / (s1.v - s0.v);
                        var rpos = 1 - pos;
                        var r = rpos * s0.r + pos * s1.r;
                        var g = rpos * s0.g + pos * s1.g;
                        var b = rpos * s0.b + pos * s1.b;
                        return
                            Convert.ToByte(r * 255) << 16 |
                            Convert.ToByte(g * 255) << 8 |
                            Convert.ToByte(b * 255);
                    }
                }

                return 0;
            }
        }

        private void AnimateButton_Click(object sender, RoutedEventArgs e)
        {
            isMandelbrot = false;
            // Toggle the Animation
            if(isAnimating == true)
            {
                Dispatcher.Invoke(() => AnimateButton.Content = "Start Julia Animation");
                isAnimating = false;
            } else
            {
                Dispatcher.Invoke(() => AnimateButton.Content = "Stop Julia Animation");
                isAnimating = true;
            }

            if(isAnimating == true)
            {
                Thread thread = new Thread(UpdateConstantValues);
                thread.Start();
            }
        }

        private void MandelbrotButton_Click(object sender, RoutedEventArgs e)
        {
            isAnimating = false;
            isMandelbrot = true;

 //           // clear the slider values
 //           IterateConstantValReal = 0;
 //           IterateConstantValComplex = 0;

//            Dispatcher.Invoke(() => slValueReal.Value = IterateConstantValReal);
//            Dispatcher.Invoke(() => slValueComplex.Value = IterateConstantValComplex);

            Dispatcher.Invoke(() => AnimateButton.Content = "Start Animation");
            Dispatcher.Invoke(() => Render());
        }

        private void UpdateConstantValues()
        {
            double iterate_value_step = 0.02;
            double current_step = iterate_value_step;
            int animate_speed = 50;  // milliseconds
            while(isAnimating == true)
            {
                if (isMandelbrot == true)
                    break;

                IterateConstantValReal = Math.Sin(current_step);
                Dispatcher.Invoke(() => slValueReal.Value = IterateConstantValReal);
//                IterateConstantValComplex = Math.Sin(current_step);


                Dispatcher.Invoke(() => Render());

                Thread.Sleep(TimeSpan.FromMilliseconds(animate_speed));
                current_step += iterate_value_step;  
            }
        }

        private void slValueReal_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            isMandelbrot = false;
            IterateConstantValReal = slValueReal.Value;

            Render();
        }

        private void slValueComplex_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            isMandelbrot = false;
            IterateConstantValComplex = slValueComplex.Value;

            Render();
        }
    }
}
