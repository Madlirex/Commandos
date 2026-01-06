using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using NAudio.Wave;

namespace Commandos.Commands;

public static class ImageCommands
{
    private static bool _videoRan = false;
    [Command("image", "photo", "displayphoto", "displayimage", "showimage", "loadimage")]
    public static void ShowImage(string path)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            Console.Clear();

            using Bitmap original = new Bitmap(path);

            int consoleWidth = Console.WindowWidth;
            int consoleHeight = Console.WindowHeight * 2;

            float scaleX = (float)original.Width / consoleWidth;
            float scaleY = (float)original.Height / consoleHeight;
            float scale = Math.Max(scaleX, scaleY);

            int targetWidth = (int)(original.Width / scale);
            int targetHeight = (int)(original.Height / scale);

            using Bitmap image = new Bitmap(original, targetWidth, targetHeight);

            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;

                for (int y = 0; y < image.Height - 1; y += 2)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        byte* top = ptr + y * data.Stride + x * 3;
                        byte* bottom = ptr + (y + 1) * data.Stride + x * 3;

                        Console.Write(
                            $"\x1b[48;2;{top[2]};{top[1]};{top[0]}m" +
                            $"\x1b[38;2;{bottom[2]};{bottom[1]};{bottom[0]}m▀"
                        );
                    }

                    Console.Write("\x1b[0m\n");
                }
            }

            image.UnlockBits(data);
        }
        else
        {
            Debug.Warning($"This command is only available on Windows 6.1 or later.\nYou are currently using: {RuntimeInformation.OSDescription}");
        }
    }
    
    static (int width, int height) GetVideoSize(string path)
    {
        var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{path}\"",
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        p.Start();
        string output = p.StandardError.ReadToEnd();
        p.WaitForExit();

        var match = Regex.Match(output, @"(\d+)x(\d+)");
        return (int.Parse(match.Groups[1].Value),
            int.Parse(match.Groups[2].Value));
    }
    
    [Command("video")]
    public static void PlayVideo(string path, int fps = -1)
    {
        if (_videoRan)
        {
            Debug.Warning("This command has already been used once. Due to internal design of Windows command prompt and console it can not be ran twice.\n" +
                          "Please reopen the application to be able to run this command again.");
            return;
        }

        if (!Debug.AskSmallRisk(
                "This command can be run only once per application instance.\nTo run it again you will have to restart the application.\nAre you sure you want to proceed? (Y/N)"))
            return;

        _videoRan = true;

        fps = fps == -1 ? (int)GetVideoFps(path) : fps;
        
        Console.Clear();
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        Console.Write("\x1b[?1049h"); // alternate screen
        Console.Write("\x1b[?25l");   // hide cursor

        var (vw, vh) = GetVideoSize(path);
        double aspect = (double)vw / vh;

        int maxWidth = Console.LargestWindowWidth;
        int maxHeight = Console.LargestWindowHeight;

        int width = Math.Min(120, maxWidth);
        int height = Math.Min((int)(width / aspect / 2), maxHeight);

        Console.SetWindowSize(width, height);
        Console.SetBufferSize(width, height);

        int consoleWidth = Console.WindowWidth;
        int consoleHeight = Console.WindowHeight * 2;

        // Video process
        var videoProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments =
                    $"-loglevel quiet -i \"{path}\" " +
                    $"-vf scale={consoleWidth}:{consoleHeight} " +
                    "-f rawvideo -pix_fmt rgb24 -",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        videoProcess.Start();

        int frameSize = consoleWidth * consoleHeight * 3;
        byte[] frameBuffer = new byte[frameSize];

        // Audio process
        int sampleRate = 44100;
        int channels = 2;
        int bytesPerSample = 2; // 16-bit PCM
        int frameSamples = (int)(sampleRate * (1.0 / fps));
        int frameBytes = frameSamples * channels * bytesPerSample;
        byte[] audioChunk = new byte[frameBytes];

        var audioProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments =
                    $"-loglevel quiet -i \"{path}\" -vn -f s16le -ac 2 -ar {sampleRate} -",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        audioProcess.Start();

        var waveFormat = new NAudio.Wave.WaveFormat(sampleRate, 16, channels);
        var audioBuffer = new NAudio.Wave.BufferedWaveProvider(waveFormat)
        {
            BufferLength = waveFormat.AverageBytesPerSecond * 2,
            DiscardOnBufferOverflow = true
        };
        var audioOut = new NAudio.Wave.WaveOutEvent { DesiredLatency = 100 };
        audioOut.Init(audioBuffer);
        audioOut.Play();

        var frameTime = TimeSpan.FromMilliseconds((double)(1000.0 / fps));

        while (true)
        {
            var sw = Stopwatch.StartNew();

            int read = videoProcess.StandardOutput.BaseStream.Read(frameBuffer, 0, frameBuffer.Length);
            if (read < frameBuffer.Length)
                break;

            // Render frame exactly like before
            RenderFrame(frameBuffer, consoleWidth, consoleHeight);

            // Feed audio for this frame
            int totalRead = 0;
            while (totalRead < frameBytes)
            {
                int r = audioProcess.StandardOutput.BaseStream.Read(audioChunk, totalRead, frameBytes - totalRead);
                if (r <= 0) break;
                totalRead += r;
            }
            if (totalRead > 0)
                audioBuffer.AddSamples(audioChunk, 0, totalRead);

            sw.Stop();
            var delay = frameTime - sw.Elapsed;
            if (delay > TimeSpan.Zero)
                Thread.Sleep(delay);
        }

        audioOut.Stop();
        audioProcess.Kill();
        videoProcess.Kill();

        Console.Write("\x1b[0m\x1b[?25h\x1b[?1049l");
        Console.CursorVisible = true;
        Console.ForegroundColor = Settings.ConsoleColors["user"];
        Console.BackgroundColor = Settings.ConsoleColors["background"];
    }

    static double GetVideoFps(string path)
    {
        var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{path}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        p.Start();
        string output = p.StandardError.ReadToEnd();
        p.WaitForExit();

        // Look for "fps" or "tbr" in the output
        // Example line: Stream #0:0(eng): Video: h264 ... 30 fps, ...
        var match = Regex.Match(output, @" (\d+(?:\.\d+)?) fps");
        if (match.Success && double.TryParse(match.Groups[1].Value, out double fps))
            return fps;

        // fallback: look for tbr
        match = Regex.Match(output, @" (\d+(?:\.\d+)?) tbr");
        if (match.Success && double.TryParse(match.Groups[1].Value, out double tbr))
            return tbr;

        // default fallback
        return 30.0;
    }
    
    static void RenderFrame(byte[] frame, int width, int height)
    {
        var sb = new StringBuilder(width * height * 4);
        sb.Append("\x1b[H");

        int lastFgR = -1, lastFgG = -1, lastFgB = -1;
        int lastBgR = -1, lastBgG = -1, lastBgB = -1;

        for (int y = 0; y < height - 1; y += 2)
        {
            for (int x = 0; x < width; x++)
            {
                int top = (y * width + x) * 3;
                int bot = ((y + 1) * width + x) * 3;

                int bgR = frame[top];
                int bgG = frame[top + 1];
                int bgB = frame[top + 2];

                int fgR = frame[bot];
                int fgG = frame[bot + 1];
                int fgB = frame[bot + 2];

                if (bgR != lastBgR || bgG != lastBgG || bgB != lastBgB)
                {
                    sb.Append("\x1b[48;2;")
                        .Append(bgR).Append(';')
                        .Append(bgG).Append(';')
                        .Append(bgB).Append('m');

                    lastBgR = bgR; lastBgG = bgG; lastBgB = bgB;
                }

                if (fgR != lastFgR || fgG != lastFgG || fgB != lastFgB)
                {
                    sb.Append("\x1b[38;2;")
                        .Append(fgR).Append(';')
                        .Append(fgG).Append(';')
                        .Append(fgB).Append('m');

                    lastFgR = fgR; lastFgG = fgG; lastFgB = fgB;
                }

                sb.Append('▀');
            }

            sb.Append('\n');

            lastFgR = lastBgR = -1;
        }

        sb.Append("\x1b[0m");
        Console.Write(sb);
    }

    static (Process proc, WaveOutEvent output) StartAudio(string path)
    {
        var audioProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments =
                    $"-loglevel quiet -i \"{path}\" " +
                    "-vn -f s16le -ac 2 -ar 44100 -",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };

        audioProcess.Start();

        var waveFormat = new WaveFormat(44100, 16, 2);
        var buffer = new BufferedWaveProvider(waveFormat)
        {
            BufferLength = waveFormat.AverageBytesPerSecond * 2,
            DiscardOnBufferOverflow = true
        };

        var output = new WaveOutEvent
        {
            DesiredLatency = 100
        };

        output.Init(buffer);
        output.Play();

        var audioThread = new Thread(() =>
        {
            byte[] chunk = new byte[4096];
            var stream = audioProcess.StandardOutput.BaseStream;

            while (true)
            {
                int read = stream.Read(chunk, 0, chunk.Length);
                if (read <= 0) break;
                buffer.AddSamples(chunk, 0, read);
            }
        })
        {
            IsBackground = true
        };

        audioThread.Start();

        return (audioProcess, output);
    }

}