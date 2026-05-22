using BepInEx.Configuration;
using System.IO;
using UnityEngine;

namespace OmegasGodTweaks;

internal static class OmegasGodTweaksConfig
{
    internal static ConfigEntry<bool> AllowJoiningMultipleReligions = null!;
    internal static ConfigEntry<bool> RemoveConversionPunishment = null!;
    internal static ConfigEntry<bool> RemoveAltarTakeoverPunishment = null!;
    internal static ConfigEntry<bool> AllowOfferingsForJoinedNonCurrentGods = null!;
    internal static ConfigEntry<bool> RemoveOfferingCategoryRestrictions = null!;
    internal static ConfigEntry<bool> RemovePietyCapFromOfferings = null!;
    internal static ConfigEntry<bool> RemoveOfferingWeightValueCap = null!;
    internal static ConfigEntry<bool> RemoveOfferingLevelBonusCap = null!;
    internal static ConfigEntry<bool> RemoveOfferingOverflowWaste = null!;
    internal static ConfigEntry<bool> DisableHarvestQuestOfferingKarmaLoss = null!;
    internal static ConfigEntry<bool> AddPietyGainFromPrayer = null!;
    internal static ConfigEntry<bool> AllowMultiplePrayersPerDay = null!;
    internal static ConfigEntry<bool> ApplyPrayerPietyToJoinedGods = null!;
    internal static ConfigEntry<bool> AllowPassivePrayerPietyGain = null!;
    internal static ConfigEntry<bool> AllowPrayerRewardChecksForJoinedGods = null!;
    internal static ConfigEntry<bool> RepeatApostleRewards = null!;
    internal static ConfigEntry<bool> RepeatArtifactRewards = null!;
    internal static ConfigEntry<bool> ApplyJoinedGodBonuses = null!;
    internal static ConfigEntry<bool> RemoveFaithResistanceBonusCap = null!;
    internal static ConfigEntry<bool> UnlockGodArtifactFactionEffects = null!;
    internal static ConfigEntry<bool> AllowDuplicateGodArtifacts = null!;
    internal static ConfigEntry<bool> DisableEythSingleArtifactPurge = null!;
    internal static ConfigEntry<bool> DisableApostleInfighting = null!;
    internal static ConfigEntry<bool> EnableJoinedGodRevelationRouting = null!;
    internal static ConfigEntry<bool> ShowPietyFaithAfterOffering = null!;
    internal static ConfigEntry<bool> ShowPietyFaithAfterPrayer = null!;
    internal static ConfigEntry<int> PrayerPietyGain = null!;
    internal static ConfigEntry<int> JoinedGodRevelationChance = null!;
    internal static ConfigEntry<RevelationMode> RevelationMode = null!;
    internal static ConfigEntry<string> SelectedRevelationGod = null!;
    internal static string XmlPath { get; private set; } = string.Empty;
    internal static string TranslationXlsxPath { get; private set; } = string.Empty;

    private const string Section = ModInfo.Name;

    internal static void LoadConfig(ConfigFile config)
    {
        AllowJoiningMultipleReligions = config.Bind(
            section: Section,
            key: "Allow Joining Multiple Religions",
            defaultValue: false,
            description: "Lets previous gods stay joined when converting. Each joined god keeps its own saved piety, days with god, and reward history in the current save, and that progress is restored when switching back while this setting is enabled. This can only preserve progress after the mod has seen that god in this save; vanilla does not keep recoverable piety for gods left before installing the mod. When this is off, previous gods do not count as joined and their saved progress is ignored for gameplay, but saved progress is not deleted or reduced.\n" +
                         "改宗時に以前の神との加入状態を残します。加入済みの神ごとの信仰心、信仰日数、報酬履歴は現在のセーブに保存され、この設定が有効な間は戻った時に復元されます。このMODがそのセーブ内で確認した後の進行だけを保持できます。導入前に離れた神の信仰心は、バニラ側に復元できる形では残りません。オフの間、以前の神は加入済みとして扱われず、保存済み進行はゲームプレイでは無視されますが、削除されたり減らされたりはしません。\n" +
                         "允许改宗后保留已加入神明。每位已加入神明的虔诚、信仰天数和奖励记录会保存在当前存档中，并在此项启用时切换回来后恢复。只能保留本MOD在该存档中见过该神明之后的进度；原版不会保存安装前已离开神明的可恢复虔诚。关闭此项时，之前的神明不会被视为已加入，其保存进度会在玩法中被忽略，但不会被删除或降低。"
        );

        RemoveConversionPunishment = config.Bind(
            section: Section,
            key: "Remove Conversion Punishment",
            defaultValue: false,
            description: "Removes wrath punishment when changing faith outside campaign conversion.\n" +
                         "キャンペーン以外で改宗した時の神罰をなくします。\n" +
                         "移除非剧情改宗时的神罚。"
        );

        RemoveAltarTakeoverPunishment = config.Bind(
            section: Section,
            key: "Remove Altar Takeover Punishment",
            defaultValue: false,
            description: "Removes wrath punishment when an altar takeover fails.\n" +
                         "祭壇の乗っ取りに失敗した時の神罰をなくします。\n" +
                         "移除祭坛争夺失败时的神罚。"
        );

        AllowOfferingsForJoinedNonCurrentGods = config.Bind(
            section: Section,
            key: "Allow Offerings For Joined Non-Current Gods",
            defaultValue: false,
            description: "Lets offerings at a joined god's own altar increase that god's piety, even when that god is not the current faith. Non-current gods require Allow Joining Multiple Religions and saved joined state.\n" +
                         "現在信仰していない加入済みの神でも、その神自身の祭壇で捧げ物をするとその神の信仰心を増やせます。現在信仰ではない神には Allow Joining Multiple Religions と保存済み加入状態が必要です。\n" +
                         "允许在已加入神明自己的祭坛献祭并增加该神明的虔诚，即使该神明不是当前信仰。非当前神明需要启用 Allow Joining Multiple Religions 并已有保存的加入状态。"
        );

        RemoveOfferingCategoryRestrictions = config.Bind(
            section: Section,
            key: "Remove Offering Category Restrictions",
            defaultValue: false,
            description: "Lets normal offerings from any item category count, using that item category's offering value. Vanilla accepted offerings are unchanged.\n" +
                         "通常の捧げ物がどのアイテムカテゴリでも有効になり、そのカテゴリの捧げ物価値を使います。バニラで有効な捧げ物は変わりません。\n" +
                         "普通献祭可使用任意物品类别，并按该类别的献祭价值计算。原版已接受的献祭物不变。"
        );

        RemovePietyCapFromOfferings = config.Bind(
            section: Section,
            key: "Remove Piety Cap From Offerings",
            defaultValue: false,
            description: "Lets offerings keep increasing piety beyond the normal faith-skill cap. This does not add multi-level piety gains by itself; NoGainExpLimit can handle multiple level-ups from one large offering if installed.\n" +
                         "捧げ物による信仰心が通常の信仰スキル上限を超えて増えるようにします。この項目だけでは1回の捧げ物で複数レベル上がる処理は追加しません。NoGainExpLimit が導入されている場合、大きな捧げ物による複数レベル上昇はそちらが処理できます。\n" +
                         "允许献祭获得的虔诚超过通常信仰技能上限。此项本身不会添加单次献祭多级提升；如果安装了 NoGainExpLimit，超大献祭带来的多级提升可由该MOD处理。"
        );

        RemoveOfferingWeightValueCap = config.Bind(
            section: Section,
            key: "Remove Offering Weight Value Cap",
            defaultValue: false,
            description: "Removes the vanilla per-item offering value cap based on item weight. Very heavy offerings can be worth more than the normal 1000 value cap.\n" +
                         "アイテム重量に基づく捧げ物価値のバニラ上限を解除します。非常に重い捧げ物は通常の価値1000上限を超えることがあります。\n" +
                         "移除原版基于物品重量的单件献祭价值上限。极重的献祭物可超过通常1000价值上限。"
        );

        RemoveOfferingLevelBonusCap = config.Bind(
            section: Section,
            key: "Remove Offering Level Bonus Cap",
            defaultValue: false,
            description: "Removes the vanilla +100% item level bonus cap from offering value. High-level offerings can receive their full level-based bonus, while the final vanilla safety clamp still remains.\n" +
                         "高レベルの捧げ物が、バニラの+100%上限で止まらずアイテムレベルボーナスを全て受けられるようにします。最終的なバニラの安全上限は残ります。\n" +
                         "高等级献祭物可获得完整的物品等级奖励，不再停在原版 +100% 上限。最终的原版安全上限仍会保留。"
        );

        RemoveOfferingOverflowWaste = config.Bind(
            section: Section,
            key: "Remove Offering Overflow Waste",
            defaultValue: false,
            description: "When the piety cap is still used, applies piety overflow instead of discarding it.\n" +
                         "信仰心上限を使う場合でも、あふれた分を無駄にしないようにします。\n" +
                         "启用虔诚上限时，也会尽量保留溢出的献祭收益。"
        );

        DisableHarvestQuestOfferingKarmaLoss = config.Bind(
            section: Section,
            key: "Disable Harvest Quest Offering Karma Loss",
            defaultValue: false,
            description: "Prevents offerings marked by vanilla's harvest quest crop flag from reducing karma.\n" +
                         "バニラの収穫依頼作物フラグが付いた捧げ物でカルマが下がらないようにします。\n" +
                         "防止献祭带有原版收获任务作物标记的物品时降低业力。"
        );

        AddPietyGainFromPrayer = config.Bind(
            section: Section,
            key: "Add Piety Gain From Prayer",
            defaultValue: false,
            description: "Adds direct piety gain when the player prays.\n" +
                         "プレイヤーが祈った時に信仰心も増えるようにします。\n" +
                         "玩家祈祷时额外获得虔诚。"
        );

        AllowMultiplePrayersPerDay = config.Bind(
            section: Section,
            key: "Allow Multiple Prayers Per Day",
            defaultValue: false,
            description: "Lets active player prayers use the normal prayer answer, heal, revelation, piety, and reward paths even after the daily prayer flag is already set.\n" +
                         "1日の祈り済みフラグが立っていても、プレイヤーの能動的な祈りで通常の応答、回復、啓示、信仰心、報酬処理を使えるようにします。\n" +
                         "即使当天已祈祷，玩家主动祈祷仍可使用通常的回应、治疗、启示、虔诚和奖励流程。"
        );

        ApplyPrayerPietyToJoinedGods = config.Bind(
            section: Section,
            key: "Apply Prayer Piety To Joined Gods",
            defaultValue: false,
            description: "Also applies prayer piety gain to joined non-current gods. Non-current gods require Allow Joining Multiple Religions and saved joined state.\n" +
                         "祈りで得る信仰心を、現在信仰していない加入済みの神にも適用します。現在信仰ではない神には Allow Joining Multiple Religions と保存済み加入状態が必要です。\n" +
                         "也将祈祷获得的虔诚应用到已加入但非当前信仰的神明。非当前神明需要启用 Allow Joining Multiple Religions 并已有保存的加入状态。"
        );

        AllowPassivePrayerPietyGain = config.Bind(
            section: Section,
            key: "Allow Passive Prayer Piety Gain",
            defaultValue: false,
            description: "Allows automatic passive prayer to gain piety too.\n" +
                         "自動の受動祈りでも信仰心を得られるようにします。\n" +
                         "允许自动被动祈祷也获得虔诚。"
        );

        AllowPrayerRewardChecksForJoinedGods = config.Bind(
            section: Section,
            key: "Allow Prayer Reward Checks For Joined Gods",
            defaultValue: false,
            description: "Allows prayer to check rewards for joined gods, not only the current faith. Non-current gods require Allow Joining Multiple Religions and saved joined state.\n" +
                         "祈りで現在の信仰だけでなく加入済みの神の報酬も確認します。現在信仰ではない神には Allow Joining Multiple Religions と保存済み加入状態が必要です。\n" +
                         "祈祷时也会检查已加入神明的奖励。非当前神明需要启用 Allow Joining Multiple Religions 并已有保存的加入状态。"
        );

        RepeatApostleRewards = config.Bind(
            section: Section,
            key: "Repeat Apostle Rewards",
            defaultValue: false,
            description: "Allows apostle rewards to repeat at piety 15, 45, 75, and so on. Already-paid steps are tracked per god in the save.\n" +
                         "信仰心15、45、75…の段階で使徒報酬を再入手できるようにします。受け取り済みの段階は神ごとにセーブへ記録されます。\n" +
                         "在虔诚15、45、75等阶段可再次获得使徒奖励。已领取阶段会按神明分别保存在存档中。"
        );

        RepeatArtifactRewards = config.Bind(
            section: Section,
            key: "Repeat Artifact Rewards",
            defaultValue: false,
            description: "Allows god artifact rewards to repeat at piety 30, 60, 90, and so on. Already-paid steps are tracked per god in the save.\n" +
                         "信仰心30、60、90…の段階で神のアーティファクト報酬を再入手できるようにします。受け取り済みの段階は神ごとにセーブへ記録されます。\n" +
                         "在虔诚30、60、90等阶段可再次获得神明神器奖励。已领取阶段会按神明分别保存在存档中。"
        );

        ApplyJoinedGodBonuses = config.Bind(
            section: Section,
            key: "Apply Joined God Bonuses",
            defaultValue: false,
            description: "Adds faith bonus elements from joined gods. Non-current gods count as joined only when Allow Joining Multiple Religions is enabled and the god has saved joined state.\n" +
                         "加入済みの神の信仰ボーナスも追加します。現在信仰ではない神は、Allow Joining Multiple Religions が有効で、その神の加入状態が保存されている場合だけ加入済みとして扱われます。\n" +
                         "添加已加入神明的信仰加成。非当前信仰的神明只有在启用 Allow Joining Multiple Religions 且已保存加入状态时，才会被视为已加入。"
        );

        RemoveFaithResistanceBonusCap = config.Bind(
            section: Section,
            key: "Remove Faith Resistance Bonus Cap",
            defaultValue: false,
            description: "Allows faith resistance bonuses to scale beyond the vanilla cap of 20. Affects current-faith and joined-god faith bonuses only; normal resistance sources are unchanged.\n" +
                         "信仰による耐性ボーナスがバニラの20上限を超えて伸びるようにします。現在信仰と加入済み神の信仰ボーナスだけに影響し、通常の耐性源は変わりません。\n" +
                         "允许信仰抗性加成超过原版20上限继续增长。仅影响当前信仰和已加入神明的信仰加成；普通抗性来源不变。"
        );

        UnlockGodArtifactFactionEffects = config.Bind(
            section: Section,
            key: "Unlock God Artifact Faction Effects",
            defaultValue: false,
            description: "Allows joined god artifact faction effects to work even when that god is not the current faith. For non-current gods, this requires Allow Joining Multiple Religions and saved joined state for that god.\n" +
                         "加入済みの神の神アーティファクト陣営効果を、現在信仰中でなくても有効にします。現在信仰ではない神で有効にするには、Allow Joining Multiple Religions と、その神の保存済み加入状態が必要です。\n" +
                         "允许已加入神明的神器阵营效果在非当前信仰时生效。对非当前信仰的神明，需要启用 Allow Joining Multiple Religions，并且该神明已有保存的加入状态。"
        );

        AllowDuplicateGodArtifacts = config.Bind(
            section: Section,
            key: "Allow Duplicate God Artifacts",
            defaultValue: false,
            description: "Stops vanilla from destroying duplicate god artifacts.\n" +
                         "神アーティファクトの重複所持をバニラが破壊しないようにします。\n" +
                         "阻止原版销毁重复的神明神器。"
        );

        DisableEythSingleArtifactPurge = config.Bind(
            section: Section,
            key: "Disable Eyth Single Artifact Purge",
            defaultValue: false,
            description: "Stops Eyth's single-artifact cleanup from stripping other god artifacts.\n" +
                         "エイス信仰時の単一アーティファクト整理で他の神アーティファクトが外されないようにします。\n" +
                         "阻止艾斯单神器清理移除其他神明神器。"
        );

        DisableApostleInfighting = config.Bind(
            section: Section,
            key: "Disable Apostle Infighting",
            defaultValue: false,
            description: "Stops apostles in the player party from starting vanilla apostle infighting.\n" +
                         "プレイヤーパーティ内の使徒同士がバニラの使徒争いを始めないようにします。\n" +
                         "阻止玩家队伍中的使徒触发原版内斗。"
        );

        EnableJoinedGodRevelationRouting = config.Bind(
            section: Section,
            key: "Enable Joined God Revelation Routing",
            defaultValue: false,
            description: "Lets joined gods occasionally answer prayer revelations when Revelation Mode allows it. Non-current gods require Allow Joining Multiple Religions and saved joined state.\n" +
                         "Revelation Mode が許可する場合、加入済みの神も祈りの啓示に応じることがあります。現在信仰ではない神には Allow Joining Multiple Religions と保存済み加入状態が必要です。\n" +
                         "Revelation Mode 允许时，已加入神明也可能回应祈祷启示。非当前神明需要启用 Allow Joining Multiple Religions 并已有保存的加入状态。"
        );

        JoinedGodRevelationChance = config.Bind(
            section: Section,
            key: "Joined God Revelation Chance",
            defaultValue: 25,
            description: "Chance from 0 to 100 for each extra joined-god revelation check. Values below 0 are treated as 0, and values above 100 are treated as 100. Setting this to 0 disables extra joined-god revelations.\n" +
                         "加入済み神による追加の啓示判定ごとの確率です。0未満は0、100を超える値は100として扱われます。0にすると加入済み神の追加啓示を無効にします。\n" +
                         "每次额外已加入神明启示检查的几率，范围为0到100。低于0按0处理，高于100按100处理。设为0会禁用额外已加入神明启示。"
        );

        ShowPietyFaithAfterOffering = config.Bind(
            section: Section,
            key: "Show Piety Faith After Offering",
            defaultValue: false,
            description: "Shows current piety and faith skill after an offering changes them.\n" +
                         "捧げ物で変化した後の信仰心と信仰スキルを表示します。\n" +
                         "献祭变化后显示当前虔诚和信仰技能。"
        );

        ShowPietyFaithAfterPrayer = config.Bind(
            section: Section,
            key: "Show Piety Faith After Prayer",
            defaultValue: false,
            description: "Shows current piety and faith skill after prayer piety is applied.\n" +
                         "祈りで信仰心が適用された後、現在の信仰心と信仰スキルを表示します。\n" +
                         "祈祷虔诚生效后显示当前虔诚和信仰技能。"
        );

        PrayerPietyGain = config.Bind(
            section: Section,
            key: "Prayer Piety Gain",
            defaultValue: 25,
            description: "Raw piety EXP added by prayer when Add Piety Gain From Prayer is enabled. Values below 0 are treated as 0.\n" +
                         "祈りで追加される信仰心EXPです。0未満は0として扱われます。\n" +
                         "启用祈祷虔诚时，祈祷增加的原始虔诚EXP。低于0按0处理。"
        );

        RevelationMode = config.Bind(
            section: Section,
            key: "Revelation Mode",
            defaultValue: global::OmegasGodTweaks.RevelationMode.SelectedJoinedGod,
            description: "Controls extra joined-god prayer revelations. Vanilla keeps only the current faith. SelectedJoinedGod checks the selected joined god. AllJoinedGods checks every joined non-current god. Non-current gods require Allow Joining Multiple Religions and saved joined state.\n" +
                         "加入済みの神による追加の祈りの啓示を設定します。Vanilla は現在信仰のみです。SelectedJoinedGod は選択した加入済みの神を確認します。AllJoinedGods は加入済みで現在信仰していない全ての神を確認します。現在信仰ではない神には Allow Joining Multiple Religions と保存済み加入状態が必要です。\n" +
                         "控制已加入神明的额外祈祷启示。Vanilla 仅保留当前信仰。SelectedJoinedGod 检查选定的已加入神明。AllJoinedGods 检查所有已加入但非当前信仰的神明。非当前神明需要启用 Allow Joining Multiple Religions 并已有保存的加入状态。"
        );

        SelectedRevelationGod = config.Bind(
            section: Section,
            key: "Selected Revelation God",
            defaultValue: "earth",
            description: "Religion id used by Revelation Mode = SelectedJoinedGod.\n" +
                         "Revelation Mode = SelectedJoinedGod で使う宗教IDです。\n" +
                         "Revelation Mode = SelectedJoinedGod 使用的宗教ID。"
        );

        AllowJoiningMultipleReligions.SettingChanged += (_, _) => RefreshJoinedRuntimeState();
        ApplyJoinedGodBonuses.SettingChanged += (_, _) => RefreshPlayerFaithElements();
        RemoveFaithResistanceBonusCap.SettingChanged += (_, _) => RefreshPlayerFaithElements();
        UnlockGodArtifactFactionEffects.SettingChanged += (_, _) => ElementContainerPatch.RefreshAppliedArtifactEffects();
    }

    internal static int ClampNonNegative(int value)
    {
        return Mathf.Max(a: value, b: 0);
    }

    internal static int ClampPercent(int value)
    {
        return Mathf.Clamp(value: value, min: 0, max: 100);
    }

    internal static void InitializeXmlPath(string xmlPath)
    {
        if (File.Exists(path: xmlPath))
        {
            XmlPath = xmlPath;
        }
        else
        {
            XmlPath = string.Empty;
        }
    }

    internal static void InitializeTranslationXlsxPath(string xlsxPath)
    {
        if (File.Exists(path: xlsxPath))
        {
            TranslationXlsxPath = xlsxPath;
        }
        else
        {
            TranslationXlsxPath = string.Empty;
        }
    }

    private static void RefreshJoinedRuntimeState()
    {
        RefreshPlayerFaithElements();
        ElementContainerPatch.RefreshAppliedArtifactEffects();
    }

    private static void RefreshPlayerFaithElements()
    {
        Chara? player = EClass.pc;
        if (player == null)
        {
            return;
        }

        player.RefreshFaithElement();
    }
}

internal enum RevelationMode
{
    Vanilla,
    SelectedJoinedGod,
    AllJoinedGods
}
