using System;

namespace LuminaColor {
    public struct RgbMultiplier {
        public double R;
        public double G;
        public double B;

        public RgbMultiplier(double r, double g, double b) {
            R = r;
            G = g;
            B = b;
        }
    }

    public static class ColorEngine {
        // Reference 6500K raw components used for normalization
        private static readonly RgbMultiplier D65Raw = GetRawKelvinRgb(6500);

        /// <summary>
        /// Calculates normalized RGB multipliers (where 6500K == 1.0, 1.0, 1.0)
        /// using the Planckian blackbody locus approximation.
        /// </summary>
        public static RgbMultiplier KelvinToRgbMultipliers(int kelvin) {
            if (kelvin < 1000) kelvin = 1000;
            if (kelvin > 12000) kelvin = 12000;

            RgbMultiplier raw = GetRawKelvinRgb(kelvin);

            // Normalize so 6500K is pure unattenuated white (1.0, 1.0, 1.0)
            double r = raw.R / D65Raw.R;
            double g = raw.G / D65Raw.G;
            double b = raw.B / D65Raw.B;

            // Clamp max to 1.0 to prevent clipping/white blowout
            double max = Math.Max(r, Math.Max(g, b));
            if (max > 1.0) {
                r /= max;
                g /= max;
                b /= max;
            }

            return new RgbMultiplier(r, g, b);
        }

        /// <summary>
        /// Raw Tanner Helland algorithm for blackbody Kelvin color
        /// </summary>
        public static RgbMultiplier GetRawKelvinRgb(int kelvin) {
            double temp = kelvin / 100.0;
            double red, green, blue;

            // Red calculation
            if (temp <= 66.0) {
                red = 255.0;
            } else {
                red = temp - 60.0;
                red = 329.698727446 * Math.Pow(red, -0.1332047592);
                if (red < 0) red = 0;
                if (red > 255) red = 255;
            }

            // Green calculation
            if (temp <= 66.0) {
                green = temp;
                green = 99.4708025861 * Math.Log(green) - 161.1195681661;
                if (green < 0) green = 0;
                if (green > 255) green = 255;
            } else {
                green = temp - 60.0;
                green = 288.1221695283 * Math.Pow(green, -0.0755148492);
                if (green < 0) green = 0;
                if (green > 255) green = 255;
            }

            // Blue calculation
            if (temp >= 66.0) {
                blue = 255.0;
            } else if (temp <= 19.0) {
                blue = 0.0;
            } else {
                blue = temp - 10.0;
                blue = 138.5177312231 * Math.Log(blue) - 305.0447927307;
                if (blue < 0) blue = 0;
                if (blue > 255) blue = 255;
            }

            return new RgbMultiplier(red, green, blue);
        }

        /// <summary>
        /// Generates a 3x256 GDI Gamma Ramp with complete visual control:
        /// Warmth, RGB channels, Tint, Brightness, Contrast, Gamma, Noise Reduction,
        /// Clarity, Saturation, Vibrance, Shadow Lift, and Highlight Softening.
        /// </summary>
        public static ushort[] BuildGammaRamp(
            int kelvin,
            double redGain,
            double greenGain,
            double blueGain,
            double tint,            // -50 (magenta) to +50 (green)
            double brightness,      // 0.1 to 1.5
            double contrast,        // 0.5 to 1.5
            double gamma,           // 0.5 to 2.5
            double noiseReduction,  // 0.0 to 1.0 (Digital & Dark Noise Gate)
            double clarity,         // 0.0 to 1.0 (Micro-contrast & Edge Pop)
            double saturation,      // 0.0 to 2.0 (Color Saturation multiplier)
            double vibrance,        // 0.0 to 2.0 (Smart Midtone Vibrance)
            double shadowLift,      // -50 to +50 (Black level / shadow detail offset)
            double highlightRolloff) // 0.0 to 1.0 (Highlight softening roll-off)
        {
            ushort[] ramp = new ushort[3 * 256];
            RgbMultiplier warmth = KelvinToRgbMultipliers(kelvin);

            // Apply tint: positive increases green, negative increases red & blue (magenta)
            double tintFactor = tint / 100.0; // -0.5 to +0.5
            double effectiveGreenGain = greenGain * (1.0 + tintFactor);
            double effectiveRedGain = redGain * (1.0 - (tintFactor > 0 ? 0 : -tintFactor * 0.5));
            double effectiveBlueGain = blueGain * (1.0 - (tintFactor > 0 ? 0 : -tintFactor * 0.5));

            // White Point channel multipliers
            double rMult = effectiveRedGain * warmth.R * brightness;
            double gMult = effectiveGreenGain * warmth.G * brightness;
            double bMult = effectiveBlueGain * warmth.B * brightness;

            // Normalize so that the peak channel reaches 1.0 at maximum output, preventing premature highlight clipping
            double maxMult = Math.Max(1.0, Math.Max(rMult, Math.Max(gMult, bMult)));
            double rScale = rMult / maxMult;
            double gScale = gMult / maxMult;
            double bScale = bMult / maxMult;

            double invGamma = 1.0 / Math.Max(0.1, gamma);
            double shadowOffset = shadowLift / 100.0;

            for (int i = 0; i < 256; i++) {
                double x = i / 255.0;

                // 1. Shadow Lift / Black Level Adjustment (-0.5 to +0.5)
                // Lifts near-black to reveal crushed shadow detail without washing out midtones
                if (shadowOffset != 0.0) {
                    double liftFalloff = Math.Pow(1.0 - x, 2.2);
                    x = x + shadowOffset * liftFalloff;
                    if (x < 0.0) x = 0.0;
                    if (x > 1.0) x = 1.0;
                }

                // 2. Noise Reduction: Shadow & Dark Noise Floor Gate
                if (noiseReduction > 0.0) {
                    double noiseGate = 0.05 * noiseReduction;
                    if (x < noiseGate) {
                        double factor = x / noiseGate;
                        x = x * (factor * factor);
                    }
                }

                // 3. Smart Midtone Vibrance (Hermite Bell-Curve Midtone Expansion)
                // Preserves endpoints (x=0 and x=1) with zero slope alteration
                if (vibrance != 1.0) {
                    double bell = 16.0 * x * x * (1.0 - x) * (1.0 - x); // Peaks at 1.0 at x=0.5
                    double vibDelta = (vibrance - 1.0) * 0.35;
                    x = x + vibDelta * bell * (x - 0.5);
                    if (x < 0.0) x = 0.0;
                    if (x > 1.0) x = 1.0;
                }

                // 4. Color Saturation & Chroma Separation
                // Midtone-confined curve: strictly preserves linear slope at both 0.0 and 1.0 (no highlight clipping)
                if (saturation != 1.0) {
                    double satDelta = (saturation - 1.0) * 0.35;
                    double sDev = 4.0 * x * (1.0 - x) * (x - 0.5);
                    x = x + satDelta * sDev;
                    if (x < 0.0) x = 0.0;
                    if (x > 1.0) x = 1.0;
                }

                // 5. Clarity & Micro-Contrast
                // Bell-shaped midtone contrast: enhances details without flattening highlight slope near 1.0
                if (clarity > 0.0) {
                    double midBell = 16.0 * x * x * (1.0 - x) * (1.0 - x);
                    x = x + clarity * 0.20 * midBell * (x - 0.5);
                    if (x < 0.0) x = 0.0;
                    if (x > 1.0) x = 1.0;
                }

                // 6. Contrast adjustment with Soft Knee Shoulder
                // Prevents crushing light greys (240-254) into pure white (255) when contrast > 1.0
                double v = (x - 0.5) * contrast + 0.5;
                if (contrast > 1.0 && v > 0.82) {
                    double maxV = (1.0 - 0.5) * contrast + 0.5;
                    double t = (v - 0.82) / Math.Max(0.001, maxV - 0.82);
                    t = Math.Max(0.0, Math.Min(1.0, t));
                    v = 0.82 + 0.18 * (0.85 * t + 0.15 * t * t);
                } else {
                    if (v < 0.0) v = 0.0;
                    if (v > 1.0) v = 1.0;
                }

                // 7. Gamma curve response
                v = Math.Pow(v, invGamma);

                // 8. Highlight Softening / Roll-off (anti-glare; softens peak white without squashing light gray separation)
                if (highlightRolloff > 0.0 && v > 0.80) {
                    double t = (v - 0.80) / 0.20;
                    v = v - highlightRolloff * 0.06 * t * t;
                }

                // 9. Apply channel multipliers
                double rVal = v * rScale;
                double gVal = v * gScale;
                double bVal = v * bScale;

                // 10. Clamp and map to 16-bit ushort
                ramp[0 * 256 + i] = (ushort)(Math.Max(0.0, Math.Min(1.0, rVal)) * 65535.0);
                ramp[1 * 256 + i] = (ushort)(Math.Max(0.0, Math.Min(1.0, gVal)) * 65535.0);
                ramp[2 * 256 + i] = (ushort)(Math.Max(0.0, Math.Min(1.0, bVal)) * 65535.0);
            }

            return ramp;
        }

        /// <summary>
        /// Backward-compatible overload for 10 parameters
        /// </summary>
        public static ushort[] BuildGammaRamp(
            int kelvin,
            double redGain,
            double greenGain,
            double blueGain,
            double tint,
            double brightness,
            double contrast,
            double gamma,
            double noiseReduction,
            double clarity)
        {
            return BuildGammaRamp(kelvin, redGain, greenGain, blueGain, tint, brightness, contrast, gamma, noiseReduction, clarity, 1.0, 1.0, 0.0, 0.0);
        }

        /// <summary>
        /// Backward-compatible overload for 8 parameters
        /// </summary>
        public static ushort[] BuildGammaRamp(
            int kelvin,
            double redGain,
            double greenGain,
            double blueGain,
            double tint,
            double brightness,
            double contrast,
            double gamma)
        {
            return BuildGammaRamp(kelvin, redGain, greenGain, blueGain, tint, brightness, contrast, gamma, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0);
        }

        /// <summary>
        /// Builds standard linear identity ramp
        /// </summary>
        public static ushort[] BuildIdentityRamp() {
            ushort[] ramp = new ushort[3 * 256];
            for (int i = 0; i < 256; i++) {
                ushort val = (ushort)(i * 256 + (i * 256 / 255));
                ramp[0 * 256 + i] = val;
                ramp[1 * 256 + i] = val;
                ramp[2 * 256 + i] = val;
            }
            return ramp;
        }

        /// <summary>
        /// Returns friendly descriptive label for a given Kelvin temperature
        /// </summary>
        public static string GetKelvinDescription(int kelvin) {
            if (kelvin <= 1800) return "Candlelight (Ultra Warm)";
            if (kelvin <= 2400) return "Cozy Fireplace / Amber";
            if (kelvin <= 3000) return "Warm Sunset / Incandescent";
            if (kelvin <= 3800) return "Soft White / Halogen";
            if (kelvin <= 4800) return "Golden Hour / Neutral Warm";
            if (kelvin <= 5800) return "Horizon Daylight / Morning";
            if (kelvin <= 6700) return "Pure Daylight (D65 Neutral)";
            if (kelvin <= 7800) return "Overcast Sky / Cool";
            if (kelvin <= 9000) return "Shade / Arctic Blue";
            return "Clear Blue Sky (Deep Cool)";
        }
    }
}
