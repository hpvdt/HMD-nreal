﻿namespace HMD.Scripts.Util
{
    using System;
    public class Frac
    {
        private double _nominator;
        private double _denominator;

        public Frac(double nominator, double denominator)
        {
            _nominator = nominator;
            _denominator = denominator;
        }

        public double ToDouble()
        {
            return _nominator / _denominator;
        }
        public double ToExp()
        {
            return Math.Log(ToDouble(), 2d);
        }

        public static Frac FromDouble(double d)
        {
            // Multiply the aspect ratio by 100 to produce a whole number
            var wholeNumber = (int)(d * gcdBase);

            // Find the GCD of the whole number and 100
            var gcd = GCD(wholeNumber, gcdBase);

            // Divide the whole number and 100 by the GCD to reduce the fraction to its lowest terms
            var numerator = wholeNumber / gcd;
            var denominator = gcdBase / gcd;

            return new Frac(numerator, denominator);
        }

        private static int gcdBase = 128;

        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                var t = b;
                b = a % b;
                a = t;
            }

            return a;
        }

        public static Frac FromExp(double d)
        {
            return FromDouble(Math.Pow(2d, d));
        }

        public override string ToString()
        {
            return $"{_nominator}/{_denominator}";
        }
        public string ToRatioText()
        {
            return $"{_nominator}:{_denominator}";
        }

        public static Frac FromRatioText(string text)
        {
            var split = text.Split(':');
            var a = double.Parse(split[0]);
            var b = double.Parse(split[1]);
            return new Frac(a, b);
        }

        // generated by Copilot
        public static Frac operator +(Frac a, Frac b)
        {
            return new Frac(a._nominator * b._denominator + b._nominator * a._denominator,
                a._denominator * b._denominator);
        }

        public static Frac operator -(Frac a, Frac b)
        {
            return new Frac(a._nominator * b._denominator - b._nominator * a._denominator,
                a._denominator * b._denominator);
        }

        public static Frac operator *(Frac a, Frac b)
        {
            return new Frac(a._nominator * b._nominator, a._denominator * b._denominator);
        }

        public static Frac operator /(Frac a, Frac b)
        {
            return new Frac(a._nominator * b._denominator, a._denominator * b._nominator);
        }

        public static Frac operator -(Frac a)
        {
            return new Frac(-a._nominator, a._denominator);
        }


    }
}
