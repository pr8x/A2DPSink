using CommandLine;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace A2DPSink;

class Program
{
    public class Options
    {
        [Option('d', "device", Required = true, HelpText = "Name of the Bluetooth A2DP sink device to connect to.")]
        public required string DeviceName { get; set; }
    }

    static Task Main(string[] args) =>
        Parser
            .Default
            .ParseArguments<Options>(args)
            .WithParsedAsync(opts => Run(opts));

    private static async Task Run(Options opts)
    {
        for(;;)
        {
            Console.Write("Scanning for devices");

            DeviceInformation? device;

            while ((device = await FindPairedDevice(opts.DeviceName)) is null)
            {
                Console.Write(".");

                await Task.Delay(5000);
            }

            Console.WriteLine($"\nDevice {device.Name} found. [Id: {device.Id}]");

            Console.WriteLine($"Connecting to device...");

            try
            {
                await ConnectToDevice(device);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");

                await Task.Delay(5000);

                continue;
            }
        }
    }

    private static async Task<DeviceInformation?> FindPairedDevice(string deviceName)
    {
        var deviceSelector = AudioPlaybackConnection.GetDeviceSelector();
        var devices = await DeviceInformation.FindAllAsync(deviceSelector);

        return devices.FirstOrDefault(device =>
            device.Name.Equals(deviceName, StringComparison.InvariantCultureIgnoreCase));
    }

    private static async Task ConnectToDevice(DeviceInformation device)
    {
        var connection = AudioPlaybackConnection.TryCreateFromId(device.Id) ??
            throw new InvalidOperationException($"Failed to create AudioPlaybackConnection");

        var promise = new TaskCompletionSource();

        connection.StateChanged += Connection_StateChanged;

        await connection.StartAsync();

        var result = await connection.OpenAsync();

        if (result.Status != AudioPlaybackConnectionOpenResultStatus.Success)
        {
            throw new InvalidOperationException($"Failed to open AudioPlaybackConnection: {result.Status}");
        }

        Console.WriteLine($"Successfully connected.");

        void Connection_StateChanged(AudioPlaybackConnection sender, object args)
        {
            if (sender.State == AudioPlaybackConnectionState.Closed)
            {
                promise.SetResult();

                connection.StateChanged -= Connection_StateChanged;
            }
        }

        await promise.Task;
    }

}

