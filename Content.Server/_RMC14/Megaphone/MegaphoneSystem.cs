using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Megaphone;
using Content.Shared.Chat;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Megaphone;

public sealed class MegaphoneSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<MegaphoneSpeakMessage>(OnMegaphoneSpeak);
    }

    private void OnMegaphoneSpeak(MegaphoneSpeakMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession?.AttachedEntity is not { } user)
            return;

        _chat.TrySendInGameICMessage(user, msg.Message, InGameICChatType.Speak, false, checkRadioPrefix: false);
    }
}
