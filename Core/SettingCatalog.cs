using System.Collections.Generic;

namespace StarsectorConfigTool.Core
{
    /// <summary>一条可修改的配置定义。</summary>
    public class SettingDef
    {
        /// <summary>settings.json 里的键名。</summary>
        public string Key;

        /// <summary>界面上显示的名字。</summary>
        public string Label;

        /// <summary>鼠标悬停的说明。</summary>
        public string Tip;

        public double Min;
        public double Max;
        public double Step;
        public double Default;

        /// <summary>true 表示整数（如等级），false 表示小数（如倍率）。</summary>
        public bool IsInt;

        public int Decimals { get { return IsInt ? 0 : 2; } }
    }

    /// <summary>所有可修改项的清单。</summary>
    public static class SettingCatalog
    {
        /// <summary>技能与等级组。</summary>
        public static List<SettingDef> SkillGroup()
        {
            return new List<SettingDef>
            {
                new SettingDef {
                    Key = "playerMaxLevel", Label = "玩家等级上限",
                    Tip = "角色能达到的最高等级，原版 50。改高后可以继续升级拿技能点。",
                    Min = 1, Max = 200, Step = 1, Default = 50, IsInt = true },

                new SettingDef {
                    Key = "skillPointsPerLevel", Label = "每级技能点",
                    Tip = "每次升级获得的技能点数，原版 2。",
                    Min = 1, Max = 30, Step = 1, Default = 2, IsInt = true },

                new SettingDef {
                    Key = "storyPointsPerLevel", Label = "每级故事点",
                    Tip = "每次升级获得的故事点数，原版 16。",
                    Min = 1, Max = 200, Step = 1, Default = 16, IsInt = true },

                new SettingDef {
                    Key = "xpGainMult", Label = "经验获取倍率",
                    Tip = "全局经验倍率，原版 1。调高升级更快。",
                    Min = 0.1, Max = 50, Step = 0.5, Default = 1, IsInt = false },

                new SettingDef {
                    Key = "officerMaxLevel", Label = "军官等级上限",
                    Tip = "手下的军官能升到的最高等级，原版 5。",
                    Min = 1, Max = 20, Step = 1, Default = 5, IsInt = true },

                new SettingDef {
                    Key = "officerXPRequiredMult", Label = "军官升级经验倍率",
                    Tip = "军官升级所需经验，原版 4。调低军官升级更快。",
                    Min = 0.05, Max = 20, Step = 0.5, Default = 4, IsInt = false },
            };
        }

        /// <summary>舰队与战斗组。</summary>
        public static List<SettingDef> FleetGroup()
        {
            return new List<SettingDef>
            {
                new SettingDef {
                    Key = "baseNumOfficers", Label = "基础军官上限",
                    Tip = "不点军官管理类技能时的军官数量上限，原版 8。",
                    Min = 0, Max = 200, Step = 1, Default = 8, IsInt = true },

                new SettingDef {
                    Key = "maxShipsInFleet", Label = "舰队舰船上限",
                    Tip = "舰队最多能带多少艘船，原版 30。超出后维护费大幅上升。",
                    Min = 1, Max = 200, Step = 1, Default = 30, IsInt = true },

                new SettingDef {
                    Key = "maxBattleSize", Label = "战斗规模上限",
                    Tip = "设置里战斗规模滑条的最大值，原版 600。会显著影响性能。",
                    Min = 100, Max = 4000, Step = 50, Default = 600, IsInt = true },

                new SettingDef {
                    Key = "defaultBattleSize", Label = "默认战斗规模",
                    Tip = "战斗规模的初始值，原版 400。不能超过上面的上限。",
                    Min = 50, Max = 4000, Step = 50, Default = 400, IsInt = true },

                new SettingDef {
                    Key = "maxPermanentHullmods", Label = "永久插件上限",
                    Tip = "每艘船能安装几个永久化（S-mod）插件，原版 2。",
                    Min = 0, Max = 50, Step = 1, Default = 2, IsInt = true },

                new SettingDef {
                    Key = "fluxPerCapacitor", Label = "每点幅能容量",
                    Tip = "每装一点幅能容量获得多少幅能，原版 200。",
                    Min = 50, Max = 2000, Step = 10, Default = 200, IsInt = true },
            };
        }
    }
}
