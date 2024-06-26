﻿using GalaSoft.MvvmLight.Messaging;
using Game.Messages;
using Game.State;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Railloader;
using Serilog;
using Serilog.Debugging;
using System.Linq;
using System.Net.Http;
using TweaksAndThings.Commands;
using UI.Builder;
using UI.CarInspector;

namespace TweaksAndThings
{
    public class TweaksAndThings : SingletonPluginBase<TweaksAndThings>, IUpdateHandler, IModTabHandler
    {
        private HttpClient client;
        internal HttpClient Client
        {
            get
            {
                if (client == null)
                    client = new HttpClient();

                return client;
            }
        }
        internal Settings? settings { get; private set; } = null;
        readonly ILogger logger = Log.ForContext<TweaksAndThings>();
        IModdingContext moddingContext { get; set; }
        IModDefinition modDefinition { get; set; }

        static TweaksAndThings()
        {
            Log.Information("Hello! Static Constructor was called!");
        }

        public TweaksAndThings(IModdingContext moddingContext, IModDefinition self)
        {
            this.modDefinition = self;
            
            this.moddingContext = moddingContext;

            logger.Information("Hello! Constructor was called for {modId}/{modVersion}!", self.Id, self.Version);

            //moddingContext.RegisterConsoleCommand(new EchoCommand());

            settings = moddingContext.LoadSettingsData<Settings>(self.Id);
        }

        public override void OnEnable()
        {
            logger.Information("OnEnable() was called!");
            var harmony = new Harmony(modDefinition.Id);
            harmony.PatchCategory(modDefinition.Id.Replace(".",string.Empty));
        }

        public override void OnDisable()
        {
            var harmony = new Harmony(modDefinition.Id);
            harmony.UnpatchAll(modDefinition.Id);
            Messenger.Default.Unregister(this);
        }

        public void Update()
        {
            logger.Verbose("UPDATE()");
        }

        public void ModTabDidOpen(UIPanelBuilder builder)
        {
            logger.Information("Daytime!");
            //WebhookUISection(ref builder);
            //builder.AddExpandingVerticalSpacer();
            WebhooksListUISection(ref builder);
            builder.AddExpandingVerticalSpacer();
            HandbrakesAndAnglecocksUISection(ref builder);
        }

        private void HandbrakesAndAnglecocksUISection(ref UIPanelBuilder builder)
        {
            builder.AddSection("Tag Callout Handbrake and Air System Helper", delegate (UIPanelBuilder builder)
            {
                builder.AddField(
                    "Enable Tag Updates",
                    builder.AddToggle(
                        () => settings?.HandBrakeAndAirTagModifiers ?? false,
                        delegate (bool enabled)
                        {
                            if (settings == null) settings = new() { WebhookSettingsList = new[] { new WebhookSettings() }.ToList() };
                            settings.HandBrakeAndAirTagModifiers = enabled;
                            builder.Rebuild();
                        }
                    )
                ).Tooltip("Enable Tag Updates", $"Will add {TextSprites.CycleWaybills} to the car tag title having Air System issues. Also prepends {TextSprites.HandbrakeWheel} if there is a handbrake set.\n\nHolding Shift while tags are displayed only shows tag titles that have issues.");
            });
        }

        private void WebhooksListUISection(ref UIPanelBuilder builder)
        {
            builder.AddSection("Webhooks List", delegate (UIPanelBuilder builder)
            {
                for (int i = 1; i <= settings.WebhookSettingsList.Count; i++)
                {
                    int z = i - 1;
                    builder.AddSection($"Webhook {i}", delegate (UIPanelBuilder builder)
                    {
                        builder.AddField(
                            "Webhook Enabled",
                            builder.AddToggle(
                                () => settings?.WebhookSettingsList[z]?.WebhookEnabled ?? false,
                                delegate (bool enabled)
                                {
                                    if (settings == null) settings = new() { WebhookSettingsList = new[] { new WebhookSettings() }.ToList() };
                                    settings.WebhookSettingsList[z].WebhookEnabled = enabled;
                                    settings.AddAnotherRow();
                                    builder.Rebuild();
                                }
                            )
                        ).Tooltip("Webhook Enabled", "Will parse the console messages and transmit to a Discord webhook.");

                        builder.AddField(
                            "Reporting Mark",
                            builder.HStack(delegate (UIPanelBuilder field)
                            {
                                field.AddInputField(
                                    settings?.WebhookSettingsList[z]?.RailroadMark,
                                    delegate (string railroadMark)
                                    {
                                        if (settings == null) settings = new() { WebhookSettingsList = new[] { new WebhookSettings() }.ToList() };
                                        settings.WebhookSettingsList[z].RailroadMark = railroadMark;
                                        settings.AddAnotherRow();
                                        builder.Rebuild();
                                    }, characterLimit: GameStorage.ReportingMarkMaxLength).FlexibleWidth();
                            })
                        ).Tooltip("Reporting Mark", "Reporting mark of the company this Discord webhook applies to..");

                        builder.AddField(
                            "Webhook Url",
                            builder.HStack(delegate (UIPanelBuilder field)
                            {
                                field.AddInputField(
                                    settings?.WebhookSettingsList[z]?.WebhookUrl,
                                    delegate (string webhookUrl)
                                    {
                                        if (settings == null) settings = new() { WebhookSettingsList = new[] { new WebhookSettings() }.ToList() };
                                        settings.WebhookSettingsList[z].WebhookUrl = webhookUrl;
                                        settings.AddAnotherRow();
                                        builder.Rebuild();
                                    }).FlexibleWidth();
                            })
                        ).Tooltip("Webhook Url", "Url of Discord webhook to publish messages to.");
                    });
                }
            });
        }

        public void ModTabDidClose()
        {
            logger.Information("Nighttime...");
            this.moddingContext.SaveSettingsData(this.modDefinition.Id, settings ?? new());
        }
    }
}
