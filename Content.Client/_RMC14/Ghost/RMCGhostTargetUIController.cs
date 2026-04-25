using Content.Client.UserInterface.Systems.Ghost;
using Content.Shared._RMC14.Ghost;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._RMC14.Ghost;

[UsedImplicitly]
public sealed class RMCGhostTargetUIController : UIController
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    private RMCGhostTargetWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostWarpsWindowRequestedEvent>(OnGhostWarpsWindowRequested);
        SubscribeNetworkEvent<RMCGhostTargetsResponseEvent>(OnGhostTargetsResponse);
    }

    private void OnGhostWarpsWindowRequested(GhostWarpsWindowRequestedEvent args)
    {
        args.Handled = true;

        var window = EnsureWindow();
        window.SetLoading();

        if (!window.IsOpen)
            window.OpenCentered();

        _net.SendSystemNetworkMessage(new RMCGhostTargetsRequestEvent());
    }

    private void OnGhostTargetsResponse(RMCGhostTargetsResponseEvent msg, EntitySessionEventArgs args)
    {
        _window?.UpdateSections(msg.Sections);
    }

    private RMCGhostTargetWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        _window = UIManager.CreateWindow<RMCGhostTargetWindow>();
        _window.TargetPressed += OnTargetPressed;
        _window.RefreshPressed += OnRefreshPressed;
        _window.MostFollowedPressed += OnMostFollowedPressed;
        _window.OnClose += OnWindowClosed;
        return _window;
    }

    private void OnWindowClosed()
    {
        if (_window == null)
            return;

        _window.TargetPressed -= OnTargetPressed;
        _window.RefreshPressed -= OnRefreshPressed;
        _window.MostFollowedPressed -= OnMostFollowedPressed;
        _window.OnClose -= OnWindowClosed;
        _window = null;
    }

    private void OnTargetPressed(NetEntity target)
    {
        _net.SendSystemNetworkMessage(new RMCGhostTargetWarpRequestEvent(target));
    }

    private void OnRefreshPressed()
    {
        _window?.SetLoading();
        _net.SendSystemNetworkMessage(new RMCGhostTargetsRequestEvent());
    }

    private void OnMostFollowedPressed()
    {
        _net.SendSystemNetworkMessage(new RMCGhostTargetMostFollowedRequestEvent());
    }
}
