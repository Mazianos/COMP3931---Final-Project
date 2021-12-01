using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WaveAnalyzer
{
    public struct Complex
    {
        public double real;
        public double imag;

        public Complex(double real, double imag)
        {
            this.real = real;
            this.imag = imag;
        }

        public static Complex operator +(Complex a, Complex b)
        {
            return new Complex(a.real + b.real, a.imag + b.imag);
        }
    }

    public struct CosWave
    {
        public double amplitude;
        public double frequency;
        public double phaseShift;

        public CosWave(double amplitude, double frequency, double phaseShift)
        {
            this.amplitude = amplitude;
            this.frequency = frequency;
            this.phaseShift = phaseShift;
        }

        public double Calculate(int t, int N) => amplitude * Math.Cos((frequency * 2 * Math.PI * t / N) + phaseShift);
    }

    public struct ThreadData
    {
        public int first;
        public int second;
        public int N;
        public short[] s;

        public ThreadData(int first, int second, int N, short[] s)
        {
            this.first = first;
            this.second = second;
            this.N = N;
            this.s = s;
        }
    }

    public static class Fourier
    {
        private const int DECIMAL_PLACES = 3;
        private const int Threads = 5;
        private static Complex[] A;

        private static void DFTTask(object data)
        {
            ThreadData threadData = (ThreadData)data;
            int first = threadData.first;
            int second = threadData.second;
            int N = threadData.N;
            short[] s = threadData.s;

            for (int f = first; f < second; ++f)
            {
                for (int t = 0; t < N; t++)
                {
                    A[f].real += s[t] * Math.Cos(2 * Math.PI * t * f / N);
                    A[f].imag -= s[t] * Math.Sin(2 * Math.PI * t * f / N);
                }
            }
        }

        public static Complex[] DFT(short[] s, int N, int start)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            A = new Complex[N];

            int firstIndex = 0;
            int secondIndex = 0;

            Task[] tasks = new Task[Threads];

            for (int i = 0; i < tasks.Length; ++i)
            {
                secondIndex += N / tasks.Length;

                Task t = Task.Factory.StartNew(DFTTask, new ThreadData(firstIndex, secondIndex, N, s));
                tasks[i] = t;

                firstIndex = secondIndex;
            }

            Task.WaitAll(tasks);
            sw.Stop();
            Trace.WriteLine("DFT took " + sw.ElapsedMilliseconds + " ms.");

            return A;
        }

        public static double[] InverseDFT(Complex[] A, int N)
        {
            double[] s = new double[N];

            for (int t = 0; t < N; ++t)
            {
                for (int f = 0; f < N; ++f)
                {
                    s[t] += A[f].real * Math.Cos(2 * Math.PI * t * f / N);
                    s[t] -= A[f].imag * Math.Sin(2 * Math.PI * t * f / N);
                }
            }

            return s;
        }

        public static Complex[] HalfFourier(double[] s, int N)
        {
            Complex[] A = new Complex[N];

            for (int f = 0; f < N; ++f)
            {
                for (int t = 0; t < N; ++t)
                {
                    A[f].real += s[t] * Math.Cos(2 * Math.PI * t * f / N);
                }
            }

            return A;
        }

        public static void PrintDoubles(double[] s)
        {
            for (int i = 0; i < s.Length; ++i)
            {
                Trace.WriteLine("[" + i + "]\t" + Math.Round(s[i], DECIMAL_PLACES));
            }
        }

        public static void PrintShorts(short[] s)
        {
            for (int i = 0; i < s.Length; ++i)
            {
                Trace.WriteLine(s[i]);
            }
        }

        public static void PrintComplex(Complex[] A)
        {
            for (int i = 0; i < A.Length; ++i)
            {
                Trace.WriteLine("[" + i + "]\t" + Math.Round(A[i].real, DECIMAL_PLACES) + ",\t" + Math.Round(A[i].imag, DECIMAL_PLACES));
            }
        }

        public static double[] GetAmplitudes(Complex[] A)
        {
            double[] amplitudes = new double[A.Length];
            for (int i = 0; i < amplitudes.Length; ++i)
            {
                amplitudes[i] = Pythagoras(A[i].real, A[i].imag);
            }

            return amplitudes;
        }

        public static double Pythagoras(double x, double y)
        {
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
        }

        public static double[] GetSamples(CosWave[] cosWaves, int N)
        {
            double[] samples = new double[N];

            for (int t = 0; t < samples.Length; ++t)
            {
                for (int j = 0; j < cosWaves.Length; ++j)
                {
                    samples[t] += cosWaves[j].Calculate(t, N);
                }
            }

            return samples;
        }

        public static void DivideByN(Complex[] A, int N)
        {
            for (int i = 0; i < A.Length; ++i)
            {
                A[i].real /= N;
                A[i].imag /= N;
            }
        }

        public static double[] GetPhaseShifts(Complex[] A)
        {
            double[] phaseShifts = new double[A.Length];

            for (int i = 0; i < A.Length; ++i)
            {
                phaseShifts[i] = ToDegrees(Math.Atan2(A[i].imag, A[i].real));
            }

            return phaseShifts;
        }

        public static double ToDegrees(double radians) => radians * 180 / Math.PI;
    }
}
