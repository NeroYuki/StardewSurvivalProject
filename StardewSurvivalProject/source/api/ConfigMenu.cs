using System;
using StardewModdingAPI;

namespace StardewSurvivalProject.source.api
{
    public class ConfigMenu
    {
        public static void Init(Mod context)
        {
            var api = context.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (api is null)
                return;

            // register mod configuration
            api.RegisterModConfig(
                mod: context.ModManifest,
                revertToDefault: () => ModConfig.GetInstance().SetConfig(new ModConfig()),
                saveToFile: () => context.Helper.WriteConfig(ModConfig.GetInstance())
            );

            // let players configure your mod in-game (instead of just from the title screen)
            api.SetDefaultIngameOptinValue(context.ModManifest, true);

            api.RegisterPageLabel(
                context.ModManifest,
                "Toggle Feature",
                "Options to enable / disable certain mod feature",
                "Toggle Feature"
            );
            api.RegisterPageLabel(
                context.ModManifest,
                "UI Configuration",
                "Options to adjust how the mod's UI is displayed",
                "UI Configuration"
            );
            api.RegisterPageLabel(
                context.ModManifest,
                "Difficulty Setting",
                "Options to tweak various aspect of the mod's mechanic for a harder or easier experience, some options is fairly advanced",
                "Difficulty Setting"
            );

            api.StartNewPage(context.ModManifest, "Toggle Feature");
            api.RegisterParagraph(context.ModManifest, "Options to enable / disable certain mod feature");

            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "Drain on running",
                optionDesc: "Drain hunger and thirst on running (Default: Checked)",
                optionGet: () => ModConfig.GetInstance().UseOnRunningDrain,
                optionSet: value => ModConfig.GetInstance().UseOnRunningDrain = value
            ) ;
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "Passive drain",
                optionDesc: "Enable hunger and thirst drain overtime - trigger every 10 minutes in-game (Default: Checked)",
                optionGet: () => ModConfig.GetInstance().UsePassiveDrain,
                optionSet: value => ModConfig.GetInstance().UsePassiveDrain = value
            );
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "Drain on running",
                optionDesc: "Drain hunger and thirst on running (Default: Checked)",
                optionGet: () => ModConfig.GetInstance().UseOnRunningDrain,
                optionSet: value => ModConfig.GetInstance().UseOnRunningDrain = value
            );
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "Drain on tools used",
                optionDesc: "Drain hunger and thirst after using tools - Further adjustment can be made in Difficulty Setting (Default: Checked)",
                optionGet: () => ModConfig.GetInstance().UseOnToolUseDrain,
                optionSet: value => ModConfig.GetInstance().UseOnToolUseDrain = value
            );
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "Disable Item HP Regen.",
                optionDesc: "Disable HP Regeneration from all in-game item except item from modded and some whitelisted items (Default: Checked)",
                optionGet: () => ModConfig.GetInstance().DisableHPHealingOnEatingFood,
                optionSet: value => ModConfig.GetInstance().DisableHPHealingOnEatingFood = value
            );
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "Overnight drain",
                optionDesc: "Drain hunger and thirst when sleeping - Equivalent to 4 Hours of passive drain (Default: Checked)",
                optionGet: () => ModConfig.GetInstance().UsePassiveDrain,
                optionSet: value => ModConfig.GetInstance().UsePassiveDrain = value
            );

            api.StartNewPage(context.ModManifest, "UI Configuration");
            api.RegisterParagraph(context.ModManifest, "Options to adjust how the mod's UI is displayed");

            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "UI Offset (X-axis)",
                optionDesc: "Offset of the mod vitality UI by X-axis (Default: 10)",
                optionGet: () => ModConfig.GetInstance().UIOffsetX,
                optionSet: value => ModConfig.GetInstance().UIOffsetX = value
            );
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "UI Offset (Y-axis)",
                optionDesc: "Offset of the mod vitality UI by Y-axis (Default: 10)",
                optionGet: () => ModConfig.GetInstance().UIOffsetY,
                optionSet: value => ModConfig.GetInstance().UIOffsetY = value
            );
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "UI Scale",
                optionDesc: "Scale of the mod vitality UI (Default: 4.0)",
                optionGet: () => ModConfig.GetInstance().UIScale,
                optionSet: value => ModConfig.GetInstance().UIScale = value
            );
            api.RegisterChoiceOption(
                mod: context.ModManifest,
                optionName: "Temperature Unit",
                optionDesc: "Change the temperature Unit (Default: Celcius)",
                optionGet: () => ModConfig.GetInstance().TemperatureUnit,
                optionSet: value => ModConfig.GetInstance().TemperatureUnit = value,
                choices: new string[] { "Celcius", "Kelvin", "Fahrenheit" }
            );
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "Env. Temperature Lower Bound",
                optionDesc: "Lowest Temperature value in the environment temperature bar (Default: -10C)",
                optionGet: () => (float)ModConfig.GetInstance().EnvironmentTemperatureDisplayLowerBound,
                optionSet: value => ModConfig.GetInstance().EnvironmentTemperatureDisplayLowerBound = (double)value
            ) ;
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "Env. Temperature Higher Bound",
                optionDesc: "Highest Temperature value in the environment temperature bar (Default: 50C)",
                optionGet: () => (float)ModConfig.GetInstance().EnvironmentTemperatureDisplayHigherBound,
                optionSet: value => ModConfig.GetInstance().EnvironmentTemperatureDisplayHigherBound = (double)value
            );
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "Body Temperature Lower Bound",
                optionDesc: "Lowest Temperature value in the body temperature bar (Default: 28C)",
                optionGet: () => (float)ModConfig.GetInstance().BodyTemperatureDisplayLowerBound,
                optionSet: value => ModConfig.GetInstance().BodyTemperatureDisplayLowerBound = (double)value
            );
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "Body Temperature Higher Bound",
                optionDesc: "Lowest Temperature value in the body temperature bar (Default: 45C)",
                optionGet: () => (float)ModConfig.GetInstance().EnvironmentTemperatureDisplayLowerBound,
                optionSet: value => ModConfig.GetInstance().EnvironmentTemperatureDisplayLowerBound = (double)value
            );
            api.RegisterSimpleOption(
                mod: context.ModManifest,
                optionName: "Disable Hover Info",
                optionDesc: "Disable All Mod-related info when hover on items. Not recommended (Default: Unchecked)",
                optionGet: () => ModConfig.GetInstance().DisableModItemInfo,
                optionSet: value => ModConfig.GetInstance().DisableModItemInfo = value
            );


            api.StartNewPage(context.ModManifest, "Difficulty Setting");
            api.RegisterParagraph(context.ModManifest, "Options to tweak various aspect of the mod's mechanic for a harder or easier experience, some options is fairly advanced");

            api.RegisterPageLabel(
                context.ModManifest,
                "Thirst and Hunger",
                "Options for Thirst and Hunger mechanic",
                "Thirst and Hunger"
            );
            api.RegisterPageLabel(
                context.ModManifest,
                "Environmental Temperature",
                "Options to tweak how environmental temperature is calculated",
                "Environmental Temperature"
            );
            api.RegisterPageLabel(
                context.ModManifest,
                "Body Temperature",
                "Options to tweak how body temperature is calculated",
                "Body Temperature"
            );
            api.RegisterPageLabel(
                context.ModManifest,
                "Custom Buff / Debuff",
                "Options for mod's custom buff and debuff conditions and mechanics",
                "Custom Buff / Debuff"
            );

            api.StartNewPage(context.ModManifest, "Thirst and Hunger");



            api.StartNewPage(context.ModManifest, "Environmental Temperature");



            api.StartNewPage(context.ModManifest, "Body Temperature");



            api.StartNewPage(context.ModManifest, "Difficulty Setting");




        }
    }
}
