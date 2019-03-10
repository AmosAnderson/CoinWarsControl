using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoinWarsControl
{
    class Program
    {
        static Queue<VideoType> workQueue = new Queue<VideoType>();

        static Process player = new Process();

        static bool playingPromo = false;

        static readonly string positiveLocation = "positive";

        static List<string> positiveVideos = new List<string>();

        static readonly string negativeLocation = "negative";

        static List<string> negativeVideos = new List<string>();

        static readonly string promoLocation = "promo";

        static List<string> promoVideos = new List<string>();

        static Random rand = new Random();

        static void Main(string[] args)
        {
            try
            {

                positiveVideos = GetVideoNames(positiveLocation);

                negativeVideos = GetVideoNames(negativeLocation);

                promoVideos = GetVideoNames(promoLocation);

                SetupPlayer();

                StartPromo();

                SerialPort port = new SerialPort(@"/dev/ttyACM0")
                {
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One
                };

                port.Open();




                while (true)
                {
                    if (port.BytesToRead > 1)
                    {
                        string coin = port.ReadTo("#");

                        if (!String.IsNullOrEmpty(coin))
                        {
                            AddWorkToQueue(coin);
                        }
                    }

                    KeepPlayerGoing();

                    Thread.Sleep(300); // keep it from consuming all of the cpu time
                }
            }
            catch (Exception E)
            {
                Console.WriteLine(E);
            }
        }

        static void SetupPlayer()
        {
            string playerName = "omxplayer";

            player.StartInfo.UseShellExecute = false;
            player.StartInfo.RedirectStandardError = true;
            player.StartInfo.RedirectStandardInput = true;
            player.StartInfo.RedirectStandardOutput = true;
            player.StartInfo.FileName = playerName;

        }

        static void KeepPlayerGoing()
        {
            //Console.WriteLine("Keeping player going.");
            // check to see if omx player has stopped and start it again if so
            if (!player.HasExited && !playingPromo)
            {
                Console.WriteLine("Player is running and it's not a promo.");
                return;
            }

            bool hasWork = workQueue.Count() > 0;

            if (hasWork)
            {
                Console.WriteLine("Work found, queueing new work.");
                StopPromo();

                VideoType work = workQueue.Dequeue();

                PlayVideo(work);
            }
            else
            {
                if (player.HasExited)
                    StartPromo();
            }


        }

        static void AddWorkToQueue(string coin)
        {
            var coinValue = Int32.Parse(coin);
            if (coinValue == 9999)
                return;

            Console.WriteLine($"Queueing up work {coinValue}");

            if (coinValue == 1)
                workQueue.Enqueue(VideoType.Positive);
            else
                workQueue.Enqueue(VideoType.Negative);
        }

        static void PlayVideo(VideoType type)
        {
            Console.WriteLine($"Attempting to play {type} video.");
            string fileName = String.Empty;

            int count = 0;
            int random = 0;
            switch (type)
            {
                case VideoType.Positive:
                    count = positiveVideos.Count();
                    if (count <= 0)
                        break;
                    random = rand.Next(count);
                    fileName = positiveVideos[random];
                    break;
                case VideoType.Negative:
                    count = negativeVideos.Count();
                    if (count <= 0)
                        break;
                    random = rand.Next(count);
                    fileName = negativeVideos[random];
                    break;
                case VideoType.Promo:
                    count = promoVideos.Count();
                    if (count <= 0)
                        break;
                    random = rand.Next(count);
                    fileName = promoVideos[random];
                    break;
                default:
                    return;
            }

            Console.WriteLine($"Number picked {random}, count {count}.");
            Console.WriteLine($"Filename {fileName}");

            player.StartInfo.Arguments = fileName;

            if (!String.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("Playing file...");
                player.Start();
            }


        }

        static List<string> GetVideoNames(string directory)
        {
            var videos = new List<string>();

            string videoDirectory = Path.Combine(Directory.GetCurrentDirectory(), directory);

            Console.WriteLine($"Getting videos from {videoDirectory}");

            videos.AddRange(Directory.GetFiles(videoDirectory));

            Console.WriteLine($"Video Names Loaded Into {directory}");
            foreach (var i in videos)
            {
                Console.WriteLine(i);
            }

            return videos;
        }

        static void StartPromo()
        {
            Console.WriteLine("Staring promo video.");
            playingPromo = true;

            PlayVideo(VideoType.Promo);
        }

        static void StopPromo()
        {
            Console.WriteLine("Stopping promo video");
            if (player.HasExited)
                return;

            player.StandardInput.Write("q");

            player.WaitForExit();

            playingPromo = false;
        }

    }

    enum VideoType
    {
        Positive = 1,
        Negative = 2,
        Promo = 3
    }
}

