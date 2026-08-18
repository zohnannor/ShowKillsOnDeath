using System;
using System.Security.Permissions;
using BepInEx;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace ShowKillsOnDeath;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class ShowKillsOnDeathMain : BaseUnityPlugin {
    public const string PLUGIN_GUID = "zohnannor.showkillsondeath";
    public const string PLUGIN_NAME = "Show Kills On Death";
    public const string PLUGIN_VERSION = "1.1.0";

    private bool initDone = false;
    public static ShowKillsOnDeathOptions Options;

    public void OnEnable() {
        On.RainWorld.OnModsInit += OnModsInit;
    }

    public void OnDisable() {
        On.RainWorld.OnModsInit -= OnModsInit;
        On.Menu.SleepAndDeathScreen.GetDataFromGame -= SleepAndDeathScreen_GetDataFromGame;
        On.Menu.PlayerResultBox.SymbolAndLabel.GrafUpdate -= SymbolAndLabel_GrafUpdate;
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self) {
        orig(self);
        if (initDone) {
            return;
        }

        Options = new ShowKillsOnDeathOptions();
        MachineConnector.SetRegisteredOI(PLUGIN_GUID, Options);

        On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
        On.Menu.PlayerResultBox.SymbolAndLabel.GrafUpdate += SymbolAndLabel_GrafUpdate;

        Logger.LogDebug($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded");
        initDone = true;
    }

    private void SleepAndDeathScreen_GetDataFromGame(
        On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig,
        SleepAndDeathScreen self,
        KarmaLadderScreen.SleepDeathScreenDataPackage package
    ) {
        orig(self, package);

        if (
            Options.Enabled.Value
                && self.IsAnyDeath
                && package.sessionRecord != null
                && package.sessionRecord.kills.Count > 0
        ) {
            if (self.killsDisplay == null && self.pages.Count > 0) {
                self.killsDisplay = new SleepScreenKills(
                    self,
                    self.pages[0],
                    new Vector2(self.LeftHandButtonsPosXAdd, 728f),
                    package.sessionRecord.kills
                );
                self.pages[0].subObjects.Add(self.killsDisplay);
                self.killsDisplay.started = true;
            }
        }
    }

    private void SymbolAndLabel_GrafUpdate(
        On.Menu.PlayerResultBox.SymbolAndLabel.orig_GrafUpdate orig,
        PlayerResultBox.SymbolAndLabel self,
        float timeStacker
    ) {
        orig(self, timeStacker);
        if (
            self.owner is SleepScreenKills
                && self.menu is SleepAndDeathScreen screen
                && screen.IsAnyDeath
                && self.symbol?.symbolSprite != null
        ) {
            self.symbol.symbolSprite.color = Color.Lerp(
                Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey),
                Color.red,
                0.5f - 0.5f * Mathf.Sin((timeStacker + Time.time * 40f) / 30f * (float)Math.PI * 2f)
            );
        }
    }
}

public class ShowKillsOnDeathOptions : OptionInterface {
    public readonly Configurable<bool> Enabled;
    private OpTab mainTab;
    private OpCheckBox _enabledCheckbox;

    private const string description = "Show kill counts even when you die";

    public ShowKillsOnDeathOptions() {
        Enabled = config.Bind("enabled", true);
    }

    public override void Initialize() {
        base.Initialize();

        mainTab = new OpTab(this, "Main");
        Tabs = [mainTab];
        _enabledCheckbox = new OpCheckBox(Enabled, 5f, 527f) {
            description = description
        };

        mainTab.AddItems([
            _enabledCheckbox,
            new OpLabel(
                37f,
                530f,
                "Show kills on death"
            ) {
                alignment = FLabelAlignment.Left,
                description = description
            }
        ]);
    }
}
