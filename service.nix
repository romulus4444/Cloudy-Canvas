flake: { config, lib, pkgs, ... }:
let
  inherit (lib) mkEnableOption mkOption mkIf types;

  inherit (flake.packages.${pkgs.stdenv.hostPlatform.system}) cloudy-canvas;

  cfg = config.services.cloudy-canvas;
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
    };
  };

  config = mkIf cfg.enable
    {
      systemd.tmpfiles.rules = [
        "d '${cfg.workDir}' 0700 ${cfg.user} ${cfg.group} - -"
      ];
      systemd.services.cloudy-canvas = {
        description = "Cloudy-Canvas";
        after = [ "network.target" ];
        wantedBy = [ "multi-user.target" ];

        serviceConfig = {
          Type = "simple";
          User = cfg.user;
          Group = cfg.group;
          WorkingDirectory = cfg.workDir;
          ExecStart = "${cfg.package}/bin/Cloudy-Canvas";
          Restart = "on-failure";
        } // lib.optionalAttrs (cfg.environmentFile != null) {
          EnvironmentFile = cfg.environmentFile;
        };
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
