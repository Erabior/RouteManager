using Railloader;
using RouteManager.Railloader.Util;
using RouteManager.v2.dataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Builder;

namespace RouteManager.Railloader
{
    public class RMRailloader : SingletonPluginBase<RMRailloader>, IModTabHandler
    {
        private readonly IModDefinition self;
        private readonly IModdingContext moddingContext;
        private readonly IUIHelper uiHelper;
        public static RouteManager RMInstance { get; private set; }
        public static RailloaderLogger logger = new RailloaderLogger();

        public static RailloaderSettingsManager settingsManager = new RailloaderSettingsManager();
        public static settings settings;
        public static SettingsData settingsData;
        public RMRailloader(IModDefinition self, IModdingContext moddingContext, IUIHelper uiHelper)
        {
            this.self = self;
            this.moddingContext = moddingContext;
            this.uiHelper = uiHelper;
            settings = moddingContext.LoadSettingsData<settings>(self.Id) ?? new settings();
            RMInstance = new RouteManager(logger, settingsManager, "Railloader");
            settingsData = RouteManager.Settings;
            RMInstance.Start();
        }

        public void ModTabDidOpen(UIPanelBuilder builder)
        {
            List<string> ddlogstate = Enum.GetNames(typeof(v2.Logging.LogLevel)).ToList();
            builder.AddField("Current Log Level", builder.AddDropdown(ddlogstate, ddlogstate.FindIndex((string s) => s == settings.currentLogLevel.ToString()), (int i) => { settings.currentLogLevel = (v2.Logging.LogLevel)i; settings.Update(); }));
            builder.AddField("Minimum Diesel Quantity", builder.AddInputFieldValidated(settings.minDieselQuantity.ToString(), (string f) => { settings.minDieselQuantity = float.Parse(f); settings.Update(); }, "[0123456789,]"));
            builder.AddField("Minimum Water Quantity", builder.AddInputFieldValidated(settings.minWaterQuantity.ToString(), (string f) => { settings.minWaterQuantity = float.Parse(f); settings.Update(); }, "[0123456789,]"));
            builder.AddField("Minimum Coal Quantity", builder.AddInputFieldValidated(settings.minCoalQuantity.ToString(), (string f) => { settings.minCoalQuantity = float.Parse(f); settings.Update(); }, "[0123456789,]"));
            builder.AddField("Experimental UI", builder.AddToggle(() => settings.experimentalUI, (bool b) => { settings.experimentalUI = b; settings.Update(); }));
            builder.AddField("Show Timestamp", builder.AddToggle(() => settings.showTimestamp, (bool b) => { settings.showTimestamp = b; settings.Update(); }));
            builder.AddField("Show Daystamp", builder.AddToggle(() => settings.showDaystamp, (bool b) => { settings.showDaystamp = b; settings.Update(); }));
            builder.AddField("Show Arrival Message", builder.AddToggle(() => settings.showArrivalMessage, (bool b) => { settings.showArrivalMessage = b; settings.Update(); }));
            builder.AddField("Show Departure Message", builder.AddToggle(() => settings.showDepartureMessage, (bool b) => { settings.showDepartureMessage = b; settings.Update(); }));
            builder.AddField("Wait Until Full", builder.AddToggle(() => settings.waitUntilFull, (bool b) => { settings.waitUntilFull = b; settings.Update(); }));
        }

        public void ModTabDidClose()
        {
            this.moddingContext.SaveSettingsData<settings>(this.self.Id, settings);
        }
    }
}
