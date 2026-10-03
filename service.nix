flake: { config, lib, pkgs, ... }:
let
  inherit (lib) mkEnableOption mkOption mkIf types;

  inherit (flake.packages.${pkgs.stdenv.hostPlatform.system}) cloudy-canvas;

  cfg = config.services.cloudy-canvas;

  # What the bot may do. It needs the network (Discord, Manebooru) and its own working directory, and nothing else:
  # no other files, no devices, no privileges, no other users' processes. MemoryDenyWriteExecute is left off on
  # purpose: the .NET JIT compiler needs memory that is both writable and executable.
  sandbox = {
    NoNewPrivileges = true;
    CapabilityBoundingSet = "";
    AmbientCapabilities = "";
    UMask = "0077";
    RemoveIPC = true;
    LockPersonality = true;
    SystemCallArchitectures = "native";
    SystemCallFilter = [ "@system-service" "~@privileged" ];
    RestrictAddressFamilies = [ "AF_INET" "AF_INET6" "AF_UNIX" ];
    RestrictNamespaces = true;
    RestrictRealtime = true;
    RestrictSUIDSGID = true;
    PrivateTmp = true;
    PrivateDevices = true;
    ProtectSystem = "strict";
    ProtectHome = true;
    ProtectProc = "invisible";
    ProtectKernelTunables = true;
    ProtectKernelModules = true;
    ProtectKernelLogs = true;
    ProtectControlGroups = true;
    ProtectClock = true;
    ProtectHostname = true;
    ReadWritePaths = [ (toString cfg.workDir) ];
  };
in
{
  options = {
    services.cloudy-canvas = {
      enable = mkEnableOption "Cloudy-Canvas Discord Bot";
      package = mkOption {
        type = types.package;
        default = flake.packages.${pkgs.stdenv.hostPlatform.system}.default;
        description = "Cloudy-Canvas Package to use";
      };
      workDir = mkOption {
        type = types.path;
        default = "/var/lib/cloudy-canvas";
        description = "Working Directory to use";
      };
      user = mkOption {
        type = types.str;
        default = "cloudy-canvas";
        description = "User Account to run Cloudy-Canvas under";
      };
      group = mkOption {
        type = types.str;
        default = "cloudy-canvas";
        description = "Group to run Cloudy-Canvas under";
      };
      environmentFile = mkOption {
        type = types.nullOr types.path;
        default = null;
        example = "/run/secrets/cloudy-canvas.env";
        description = ''
          File of KEY=value lines loaded as environment variables, for secrets such as
          DiscordSettings__token and ManebooruSettings__token. Keep it outside the Nix store
          (e.g. managed by agenix or sops-nix) and readable only by root.
        '';
      };
      sandbox = mkOption {
        type = types.bool;
        default = true;
        description = ''
          Run the service with systemd's sandboxing: it can use the network and write to
          its working directory, and nothing else. Turn this off only if it stops
          something you need working, and please report what.
        '';
      };
    };
  };

  config = mkIf cfg.enable
    {
      systemd.tmpfiles.rules = [
        "d '${cfg.workDir}' 0700 ${cfg.user} ${cfg.group} - -"
      ];
      systemd.services.cloudy-canvas = {
        description = "Cloudy-Canvas";
        after = [ "network-online.target" ];
        wants = [ "network-online.target" ];
        wantedBy = [ "multi-user.target" ];

        # Never give up restarting (a Discord or network outage ends when it ends), but back off so a
        # bad token or a long outage doesn't mean a login attempt every few seconds.
        unitConfig.StartLimitIntervalSec = 0;

        serviceConfig = {
          Type = "simple";
          User = cfg.user;
          Group = cfg.group;
          WorkingDirectory = cfg.workDir;
          ExecStart = "${cfg.package}/bin/Cloudy-Canvas";
          Restart = "on-failure";
          RestartSec = "10s";
          RestartSteps = 5;
          RestartMaxDelaySec = "10min";
        } // lib.optionalAttrs (cfg.environmentFile != null) {
          EnvironmentFile = cfg.environmentFile;
        } // lib.optionalAttrs cfg.sandbox sandbox;
      };
      users.users = mkIf (cfg.user == "cloudy-canvas") {
        cloudy-canvas = {
          isSystemUser = true; # This is not a log-in user
          group = cfg.group;
          home = cfg.workDir;
        };
      };
      users.groups = mkIf (cfg.group == "cloudy-canvas") {
        cloudy-canvas = { };
      };
    };
}
