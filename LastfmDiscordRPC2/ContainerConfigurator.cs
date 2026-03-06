using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Autofac;
using DiscordRPC.Logging;
using LastfmDiscordRPC2.IO;
using LastfmDiscordRPC2.Logging;
using LastfmDiscordRPC2.Models.API;
using LastfmDiscordRPC2.Models.RPC;
using LastfmDiscordRPC2.Platform;
using LastfmDiscordRPC2.ViewModels;
using LastfmDiscordRPC2.ViewModels.Panes;
using LastfmDiscordRPC2.ViewModels.Setter;
using LastfmDiscordRPC2.Views;

namespace LastfmDiscordRPC2;

public static class ContainerConfigurator
{
    public static IContainer Configure()
    {
        ContainerBuilder builder = new ContainerBuilder();

        builder.RegisterType<MainViewModel>().AsSelf().SingleInstance();
        
        builder.RegisterAssemblyTypes(Assembly.Load(nameof(LastfmDiscordRPC2)))
            .Where(t => t.Namespace?.Contains("ViewModels.Panes") == true)
            .As<AbstractPaneViewModel>()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(Assembly.Load(nameof(LastfmDiscordRPC2)))
            .Where(t => t.Namespace?.Contains("ViewModels.Controls") == true)
            .AsSelf()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        builder.RegisterType<UIContext>().AsSelf().SingleInstance();

        builder.RegisterType<DialogWindow>().AsSelf().SingleInstance();
        
        builder.RegisterType<LoggingService>().AsSelf().SingleInstance();

        builder.RegisterType<ViewLogger>().As<IRPCLogger>().SingleInstance().WithParameter("level", LogLevel.Info);
        builder.RegisterType<TextLogger>().As<IRPCLogger>().SingleInstance().WithParameter("level", LogLevel.Warning);

        builder.RegisterType<LastfmAPIService>().AsSelf().SingleInstance();
        
        builder.RegisterType<SignatureAPIService>().As<ISignatureAPIService>().SingleInstance();
        // builder.RegisterType<SignatureLocalAPIService>().As<ISignatureAPIService>().SingleInstance();
        // builder.RegisterType< *YOUR CLASS HERE * >().As<ISecretKey>().SingleInstance();

        builder.RegisterType<DiscordClient>().As<IDiscordClient>().SingleInstance();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                string baseDir = AppContext.BaseDirectory;
                string windowsDll = Path.Combine(baseDir, "LastfmDiscordRPC2.Windows.dll");
                if (File.Exists(windowsDll))
                {
                    Assembly winAsm = Assembly.LoadFrom(windowsDll);
                    Type? providerType = winAsm.GetType("LastfmDiscordRPC2.Platform.Windows.WindowsPlaybackStateProvider");
                    if (providerType != null)
                        builder.RegisterType(providerType).As<IPlaybackStateProvider>().SingleInstance();
                    else
                        builder.RegisterType<DefaultPlaybackStateProvider>().As<IPlaybackStateProvider>().SingleInstance();
                }
                else
                    builder.RegisterType<DefaultPlaybackStateProvider>().As<IPlaybackStateProvider>().SingleInstance();
            }
            catch
            {
                builder.RegisterType<DefaultPlaybackStateProvider>().As<IPlaybackStateProvider>().SingleInstance();
            }
        }
        else
            builder.RegisterType<DefaultPlaybackStateProvider>().As<IPlaybackStateProvider>().SingleInstance();

        builder.RegisterType<PresenceService>().As<IPresenceService>().SingleInstance();

        builder.RegisterType<ViewModelSetter>().As<IViewModelSetter>().SingleInstance();

        builder.RegisterType<LogIOService>().AsSelf().SingleInstance();
        builder.RegisterType<SaveCfgIOService>().AsSelf().SingleInstance();
            
        return builder.Build();
    }
}