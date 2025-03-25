using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Megaphone;

[Serializable, NetSerializable]
public enum MegaphoneUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MegaphoneSpeakMessage(string message) : BoundUserInterfaceMessage
{
    public readonly string Message = message;
}
