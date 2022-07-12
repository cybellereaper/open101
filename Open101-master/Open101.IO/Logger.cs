// From https://github.com/healingbrew/DragonLib/
/*
MIT License

Copyright (c) 2019 Naomi Ahmed

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static DragonLib.IO.ConsoleSwatch;

namespace DragonLib.IO
{
    public static class Logger
    {
        public static bool ShowTime = true;

#if DEBUG
        public static bool ShowDebug = true;
#else
        public static bool ShowDebug;
#endif

        public static bool Enabled = true;
        public static bool UseColor = true;

        public static void Log4Bit(ConsoleColor color, bool newLine, TextWriter writer, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (UseColor) {
                Console.ForegroundColor = color;
            }
            
            string output = message;

            if (arg.Length > 0) {
                output = string.Format(message, arg);
            }

            if (!string.IsNullOrWhiteSpace(category)) {
                output = $"[{category}] {output}";
            }

            if (ShowTime) {
                output = $"{DateTime.Now.ToLocalTime().ToLongTimeString()} {output}";
            }

            writer.Write(output);
            
            if (UseColor) {
                Console.ForegroundColor = ConsoleColor.Gray; // erm, reset
            }

            if (newLine) {
                writer.WriteLine();
            }
        }

        private static void Log24Bit(ConsoleColor color, string category, string message, params object[] arg) {
            Log24Bit(color, true, Console.Out, category, message, arg);
        }

        public static void Log24Bit(ConsoleColor color, bool newline, TextWriter writer, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(color, newline, writer, category, message, arg);
                return;
            }

            Log24Bit(color.AsDOSColor().AsXTermColor().ToForeground(), null, newline, writer, category, message, arg);
        }

        private static void Log24Bit(DOSColor color, string category, string message, params object[] arg) {
            Log24Bit(color, true, Console.Out, category, message, arg);
        }

        public static void Log24Bit(DOSColor color, bool newline, TextWriter writer, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(color.AsConsoleColor(), newline, writer, category, message, arg);
                return;
            }

            Log24Bit(color.AsXTermColor().ToForeground(), null, newline, writer, category, message, arg);
        }

        private static void Log24Bit(XTermColor color, string category, string message, params object[] arg) {
            Log24Bit(color, true, Console.Out, category, message, arg);
        }

        public static void Log24Bit(XTermColor color, bool newline, TextWriter writer, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(ConsoleColor.Gray, newline, writer, category, message, arg);
                return;
            }

            Log24Bit(color.ToForeground(), null, newline, writer, category, message, arg);
        }

        private static void Log24Bit(string foreground, string background, bool newLine, string category, string message, params object[] arg) {
            Log24Bit(foreground, background, newLine, Console.Out, category, message, arg);
        }

        public static void Log24Bit(string foreground, string background, bool newLine, TextWriter writer, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(ConsoleColor.Gray, newLine, writer, category, message, arg);
                return;
            }

            if (UseColor && !string.IsNullOrWhiteSpace(foreground)) {
                writer.Write(foreground);
            }

            if (UseColor && !string.IsNullOrWhiteSpace(background)) {
                writer.Write(background);
            }

            string output = message;

            if (arg.Length > 0) {
                output = string.Format(message, arg);
            }

            if (!string.IsNullOrWhiteSpace(category)) {
                output = $"[{category}] {output}";
            }

            if (ShowTime) {
                output = $"{DateTime.Now.ToLocalTime().ToLongTimeString()} {output}";
            }

            writer.Write(output);

            if (UseColor && (!string.IsNullOrWhiteSpace(foreground) || !string.IsNullOrWhiteSpace(background))) {
                writer.Write(ColorReset);
            }

            if (newLine) {
                writer.WriteLine();
            }
        }

        public static void Log(ConsoleColor color, bool newline, bool stderr, string category, string message, params object[] arg) {
            Log24Bit(color, newline, stderr ? Console.Error : Console.Out, category, message, arg);
        }

        public static void Success(string category, string message, params object[] arg) {
            Log(ConsoleColor.Green, true, false, category, message, arg);
        }

        public static void Info(string category, string message, params object[] arg) {
            Log(ConsoleColor.White, true, false, category, message, arg);
        }

        public static void Debug(string category, string message, params object[] arg) {
            if (!ShowDebug) return;
            Log(ConsoleColor.DarkGray, true, false, category, message, arg);
        }

        public static void Warn(string category, string message, params object[] arg) {
            Log(ConsoleColor.DarkYellow, true, false, category, message, arg);
        }

        public static void Error(string category, string message, params object[] arg) {
            Log(ConsoleColor.Red, true, true, category, message, arg);
        }
        
        public static string ReadLine(TextWriter writer, bool @private)
        {
            var builder = new StringBuilder();
            ConsoleKeyInfo ch;
            while ((ch = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (ch.Key == ConsoleKey.Backspace) // backspace
                {
                    if (builder.Length > 0)
                    {
                        if (!@private)
                        {
                            writer.Write(ch.KeyChar);
                            writer.Write(" ");
                            writer.Write(ch.KeyChar);
                        }

                        builder.Remove(builder.Length - 1, 1);
                    }
                    else
                    {
                        Console.Beep();
                    }
                }
                else
                {
                    builder.Append(ch.KeyChar);

                    if (!@private) writer.Write(ch.KeyChar);
                }
            }

            writer.WriteLine();
            return builder.ToString();
        }

        private static string LastMessage;
        
        private static void Fill<T>(T[] array, T value)
        {
            Fill(array, value, 0, array.Length);
        }

        private static void Fill<T>(T[] array, T value, int start, int length)
        {
            for (int i = 0; i < length; ++i) array[i + start] = value;
        }

        public static void LogProgress(string message, string pre, string post, double value, XTermColor messageColor, XTermColor preColor, XTermColor postColor, XTermColor brickColor, XTermColor processColor, bool showProgressValue, XTermColor processValueColor)
        {
            if (Console.IsOutputRedirected) return;

            var width = Console.WindowWidth;
            var empty = new char[width];
            Fill(empty, ' ');
            if (message != LastMessage)
            {
                Console.Out.Write(empty);
                Console.CursorLeft = 0;
                LastMessage = message;
                Log24Bit(messageColor, true, Console.Out, null, message);
            }

            Console.Out.Write(empty);
            Console.CursorLeft = 0;
            var remaining = width - pre.Length - post.Length - 4;
            Log24Bit(preColor, false, Console.Out, null, pre);
            if (remaining > 0)
            {
                Log24Bit(brickColor, false, Console.Out, null, " [");
                empty = new char[remaining];
                Fill(empty, ' ');
                Fill(empty, '=', 0, (int) Math.Round(remaining * Math.Min(value, 1)));
                Log24Bit(processColor, false, Console.Out, null, string.Join("", empty));
                Log24Bit(brickColor, false, Console.Out, null, "] ");

                if (showProgressValue && remaining > 6)
                {
                    var valueText = (Math.Min(value, 1) * 100).ToString(CultureInfo.InvariantCulture).Split('.')[0] + "%";
                    Console.CursorLeft = pre.Length + 2 + (int) Math.Floor(remaining / 2.0d - valueText.Length / 2.0d);
                    Log24Bit(processValueColor, false, Console.Out, null, valueText);
                    Console.CursorLeft = width - post.Length;
                }
            }

            Log24Bit(postColor, false, Console.Out, null, post);
            Console.CursorLeft = Console.WindowWidth - 1;
            Console.CursorTop -= 1;
        }

        public static void FlushProgress()
        {
            Console.CursorTop += 1;
            Console.WriteLine();
        }

        [DebuggerHidden]
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public static bool Assert(bool condition, string message = null, params string[] detail)
        {
            if (condition) return false;
            var trace = new StackTrace(1, true);
            var frame = trace.GetFrame(0);

            Log24Bit(XTermColor.Purple, true, Console.Error, "ASSERT", $"Assertion failed at {(frame?.ToString()?.Trim() ?? "unknown location")}");

            if (message != null)
                Log24Bit(XTermColor.Purple, true, Console.Error, null, "\t -> " + message);

            if (!(detail?.Length > 0)) return true;
            foreach (var line in detail) Log24Bit(XTermColor.Purple, true, Console.Error, null, "\t -> " + line);

            return true;
        }

        [DebuggerHidden]
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public static void Trace()
        {
            if (!ShowDebug) return;
            var trace = new StackTrace(1, true);

            Log24Bit(XTermColor.HotPink3, true, Console.Error, "TRACE", trace.ToString().Trim());
        }
    }
}

namespace DragonLib.IO
{
    public static class ConsoleSwatch
    {
        public const string ColorReset = "\x1b[0m";
        
        public enum DOSColor
        {
            Black = 0,
            DarkBlue = 1,
            DarkGreen = 2,
            DarkCyan = 3,
            DarkRed = 4,
            DarkMagenta = 5,
            DarkYellow = 6,
            Gray = 7,
            DarkGray = 8,
            Blue = 9,
            Green = 10,
            Cyan = 11,
            Red = 12,
            Magenta = 13,
            Yellow = 14,
            White = 15
        }

        public enum XTermColor : byte
        {
            Black = 0,
            Maroon,
            Green,
            Olive,
            Navy,
            Purple,
            Teal,
            Silver,
            Grey,
            Red,
            Lime,
            Yellow,
            Blue,
            Fuchsia,
            Aqua,
            White,
            Grey1,
            NavyBlue,
            DarkBlue,
            Blue1,
            Blue2,
            Blue3,
            DarkGreen,
            DeepSkyBlue,
            DeepSkyBlue1,
            DeepSkyBlue2,
            DodgerBlue,
            DodgerBlue1,
            Green1,
            SpringGreen,
            Turquoise,
            DeepSkyBlue3,
            DeepSkyBlue4,
            DodgerBlue2,
            Green2,
            SpringGreen1,
            DarkCyan,
            LightSeaGreen,
            DeepSkyBlue5,
            DeepSkyBlue6,
            Green3,
            SpringGreen2,
            SpringGreen3,
            Cyan,
            DarkTurquoise,
            Turquoise1,
            Green4,
            SpringGreen4,
            SpringGreen5,
            MediumSpringGreen,
            Cyan1,
            Cyan2,
            DarkRed,
            DeepPink,
            Purple1,
            Purple2,
            Purple3,
            BlueViolet,
            Orange,
            Grey2,
            MediumPurple,
            SlateBlue,
            SlateBlue1,
            RoyalBlue,
            Chartreuse,
            DarkSeaGreen,
            PaleTurquoise,
            SteelBlue,
            SteelBlue1,
            CornflowerBlue,
            Chartreuse1,
            DarkSeaGreen1,
            CadetBlue,
            CadetBlue1,
            SkyBlue,
            SteelBlue2,
            Chartreuse2,
            PaleGreen,
            SeaGreen,
            Aquamarine,
            MediumTurquoise,
            SteelBlue3,
            Chartreuse3,
            SeaGreen1,
            SeaGreen2,
            SeaGreen3,
            Aquamarine1,
            DarkSlateGray,
            DarkRed1,
            DeepPink1,
            DarkMagenta,
            DarkMagenta1,
            DarkViolet,
            Purple4,
            Orange1,
            LightPink,
            Plum,
            MediumPurple1,
            MediumPurple2,
            SlateBlue2,
            Yellow1,
            Wheat,
            Grey3,
            LightSlateGrey,
            MediumPurple3,
            LightSlateBlue,
            Yellow2,
            DarkOliveGreen,
            DarkSeaGreen2,
            LightSkyBlue,
            LightSkyBlue1,
            SkyBlue1,
            Chartreuse4,
            DarkOliveGreen1,
            PaleGreen1,
            DarkSeaGreen3,
            DarkSlateGray1,
            SkyBlue2,
            Chartreuse5,
            LightGreen,
            LightGreen1,
            PaleGreen2,
            Aquamarine2,
            DarkSlateGray2,
            Red1,
            DeepPink2,
            MediumVioletRed,
            Magenta,
            DarkViolet1,
            Purple5,
            DarkOrange,
            IndianRed,
            HotPink,
            MediumOrchid,
            MediumOrchid1,
            MediumPurple4,
            DarkGoldenrod,
            LightSalmon,
            RosyBrown,
            Grey4,
            MediumPurple5,
            MediumPurple6,
            Gold,
            DarkKhaki,
            NavajoWhite,
            Grey5,
            LightSteelBlue,
            LightSteelBlue1,
            Yellow3,
            DarkOliveGreen2,
            DarkSeaGreen4,
            DarkSeaGreen5,
            LightCyan,
            LightSkyBlue2,
            GreenYellow,
            DarkOliveGreen3,
            PaleGreen3,
            DarkSeaGreen6,
            DarkSeaGreen7,
            PaleTurquoise1,
            Red2,
            DeepPink3,
            DeepPink4,
            Magenta1,
            Magenta2,
            Magenta3,
            DarkOrange1,
            IndianRed1,
            HotPink1,
            HotPink2,
            Orchid,
            MediumOrchid2,
            Orange2,
            LightSalmon1,
            LightPink1,
            Pink,
            Plum1,
            Violet,
            Gold1,
            LightGoldenrod,
            Tan,
            MistyRose,
            Thistle,
            Plum2,
            Yellow4,
            Khaki,
            LightGoldenrod1,
            LightYellow,
            Grey6,
            LightSteelBlue2,
            Yellow5,
            DarkOliveGreen4,
            DarkOliveGreen5,
            DarkSeaGreen8,
            Honeydew,
            LightCyan1,
            Red3,
            DeepPink5,
            DeepPink6,
            DeepPink7,
            Magenta4,
            Magenta5,
            OrangeRed,
            IndianRed2,
            IndianRed3,
            HotPink3,
            HotPink4,
            MediumOrchid3,
            DarkOrange2,
            Salmon,
            LightCoral,
            PaleVioletRed,
            Orchid1,
            Orchid2,
            Orange3,
            SandyBrown,
            LightSalmon2,
            LightPink2,
            Pink1,
            Plum3,
            Gold2,
            LightGoldenrod2,
            LightGoldenrod3,
            NavajoWhite1,
            MistyRose1,
            Thistle1,
            Yellow6,
            LightGoldenrod4,
            Khaki1,
            Wheat1,
            Cornsilk,
            Grey7,
            Grey8,
            Grey9,
            Grey10,
            Grey11,
            Grey12,
            Grey13,
            Grey14,
            Grey15,
            Grey16,
            Grey17,
            Grey18,
            Grey19,
            Grey20,
            Grey21,
            Grey22,
            Grey23,
            Grey24,
            Grey25,
            Grey26,
            Grey27,
            Grey28,
            Grey29,
            Grey30,
            Grey31
        }

        public const string COLOR_RESET = "\x1b[0m";
        private const int STD_OUTPUT_HANDLE = -11;
        private const int ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;

        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        public static bool IsVTEnabled { get; private set; }
        public static bool IsVTCapable { get; private set; } = Environment.OSVersion.Version.Major >= 6;

        public static ConsoleColor AsConsoleColor(this DOSColor color) => (ConsoleColor) color;

        public static DOSColor AsDOSColor(this ConsoleColor color) => (DOSColor) color;

        public static XTermColor AsXTermColor(this DOSColor color) {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (color) {
                case DOSColor.DarkGray:
                    return XTermColor.Grey;
                case DOSColor.DarkYellow:
                    return XTermColor.Yellow;
                case DOSColor.Yellow:
                    return XTermColor.LightYellow;
                case DOSColor.Gray:
                    return XTermColor.LightSlateGrey;
                default:
                    return Enum.TryParse(color.ToString(), out XTermColor col) ? col : XTermColor.DarkSlateGray;
            }
        }

        public static string ToForeground(this XTermColor color) => $"\x1b[38;5;{(byte) color}m";

        public static string ToBackground(this XTermColor color) => $"\x1b[48;5;{(byte) color}m";

        public static string ToForeground(this Color color) => $"\x1b[38;2;{color.R};{color.G};{color.B}m";

        public static string ToBackground(this Color color) => $"\x1b[48;2;{color.R};{color.G};{color.B}m";

        public static bool EnableVT()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return true; // always on with unix.

            if (IsVTEnabled) return true;

            if (!IsVTCapable) return false;

            unsafe
            {
                var hOut = GetStdHandle(STD_OUTPUT_HANDLE);
                if (hOut == InvalidHandleValue)
                {
                    IsVTCapable = false;
                    return false;
                }

                var dwMode = 0;
                if (!GetConsoleMode(hOut, &dwMode))
                {
                    IsVTCapable = false;
                    return false;
                }

                dwMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                if (!SetConsoleMode(hOut, dwMode))
                {
                    IsVTCapable = false;
                    return false;
                }

                IsVTEnabled = true;
                return true;
            }
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("Kernel32.dll")]
        private static extern unsafe bool GetConsoleMode(IntPtr hConsoleHandle, int* lpMode);

        [DllImport("Kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);
    }
}