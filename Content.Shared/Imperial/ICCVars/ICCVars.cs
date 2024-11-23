using Robust.Shared.Configuration;

namespace Content.Shared.Imperial.ICCVar;

[CVarDefs]
// ReSharper disable once InconsistentNaming
public sealed class ICCVars
{
    /// <summary>
    /// Конфигурабельное окошко при попадании в лобби
    /// </summary>
    [CVarDefs]
    public static class ShowPopupOnJoin
    {
        /// <summary>
        /// Показывать ли окошко (на клиенте)
        /// </summary>
        public static readonly CVarDef<bool>
            Enabled = CVarDef.Create("imperial.show_popup_on_join.enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        // MAYBE: Просто синхронизировать данные CVar-ы и не обрабатывать запрос с реквестом popup данных

        public static readonly CVarDef<string>
            Title = CVarDef.Create("imperial.show_popup_on_join.title", "popup-title", CVar.SERVERONLY | CVar.ARCHIVE);

        public static readonly CVarDef<string>
            Content = CVarDef.Create("imperial.show_popup_on_join.content", "popup-content", CVar.SERVERONLY | CVar.ARCHIVE);

        public static readonly CVarDef<string>
            Link = CVarDef.Create("imperial.show_popup_on_join.link", "popup-link", CVar.SERVERONLY | CVar.ARCHIVE);

        /// <summary>
        /// Изображение qr кода формата: "1111|1001|1011|1111"
        /// </summary>
        public static readonly CVarDef<string>
            QR = CVarDef.Create("imperial.show_popup_on_join.qr", "", CVar.SERVERONLY | CVar.ARCHIVE);
    }
    /// Enables footprints
    /// </summary>
    public static readonly CVarDef<bool>
        FootPrintsEnabled = CVarDef.Create("imperial.footprints_enabled", true, CVar.SERVERONLY);
    /// <summary>
    /// Enables autovote for map and preset in lobby
    /// </summary>
    public static readonly CVarDef<bool>
        VoteAutoStartInLobby = CVarDef.Create("vote.autostartinlobby", true, CVar.SERVERONLY);
    /// <summary>
    /// Timer for end round
    /// </summary>
    public static readonly CVarDef<int>
        GameEndRoundDuration = CVarDef.Create("game.endroundduration", 40, CVar.SERVERONLY);
    /// SpacingEscapeRatio Moved to CVars from SpacingEscapeRatio
    /// <summary>
    ///     What fraction of air from a spaced tile escapes every tick.
    ///     1.0 for instant spacing, 0.2 means 20% of remaining air lost each time
    /// </summary>
    public static readonly CVarDef<float>
        SpacingEscapeRatio = CVarDef.Create("atmos.spacingescaperatio", 1f, CVar.SERVERONLY);

    public static readonly CVarDef<bool>
        PsychosisEnabled = CVarDef.Create("psychosis.enabled", true, CVar.REPLICATED);
}
