using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Media; // Required for playing system sounds
using System;
using System.Media;
using System.IO;

namespace WeightMaster.Services
{
    public class ConsoleSound
    {



        public static void PlayReset()
        {
            try
            {
                // Get the directory where the executable is located
                string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // Combine the executable directory path with the sound file name
                string soundFilePath = Path.Combine(executableDirectory, "beep-02.wav");

                // Create a SoundPlayer instance
                SoundPlayer player = new SoundPlayer(soundFilePath);

                // Play the sound asynchronously
                player.Play(); // Use `PlaySync()` if you want to block until the sound finishes
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
            }
        }
        public static void PlayStable()
        {
            try
            {
                // Get the directory where the executable is located
                string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // Combine the executable directory path with the sound file name
                string soundFilePath = Path.Combine(executableDirectory, "beep-04.wav");

                // Create a SoundPlayer instance
                SoundPlayer player = new SoundPlayer(soundFilePath);

                // Play the sound asynchronously
                player.Play(); // Use `PlaySync()` if you want to block until the sound finishes
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
            }
        }




    }
}
