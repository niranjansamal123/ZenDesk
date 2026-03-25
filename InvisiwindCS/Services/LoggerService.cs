using Serilog;
using Serilog.Events;

namespace InvisiwindCS.Services
{
    public static class LoggerService
    {
        public static void Initialize()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("invisiwind.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Invisiwind 2.0 started");
        }

        public static void Debug(string msg) => Log.Debug(msg);
        public static void Info(string msg) => Log.Information(msg);
        public static void Error(string msg) => Log.Error(msg);
        public static void Dispose() => Log.CloseAndFlush();
    }
}
