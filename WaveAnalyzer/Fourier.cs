using System;
using System.Diagnostics;

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

    public static class Fourier
    {
        private const int DECIMAL_PLACES = 3;

        public static Complex[] DFT(double[] s, int N)
        {
            Complex[] A = new Complex[N];

            for (int f = 0; f < N; ++f)
            {
                for (int t = 0; t < N; ++t)
                {
                    A[f].real += s[t] * Math.Cos(2 * Math.PI * t * f / N);
                    A[f].imag -= s[t] * Math.Sin(2 * Math.PI * t * f / N);
                }
            }

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

        public static void PrintComplex(Complex[] A)
        {
            for (int i = 0; i < A.Length; ++i)
            {
                Trace.WriteLine("[" + i + "]\t" + Math.Round(A[i].real, DECIMAL_PLACES) + ",\t" + Math.Round(A[i].imag, DECIMAL_PLACES));
            }
        }

        public static double[] GetAmplitudes(Complex[] A)
        {
            double[] amplitudes = new double[(A.Length / 2) + 1];
            for (int i = 0; i < (A.Length / 2) + 1; ++i)
            {
                amplitudes[i] = Pythagoras(A[i].real, A[i].imag) * 2;
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
