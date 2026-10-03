# A NixOS VM test of service.nix: `nix build .#checks.x86_64-linux.service` (needs KVM).
#
# There is no Discord to connect to in a test, so this checks the parts of the service that can be checked:
#   * the real unit, with no token and with a made-up one, starts, fails the way it is meant to, and is
#     restarted, and is never stopped by its own sandbox (which would show up as a crash, not as a Discord error);
#   * the sandbox does what it says: a probe is run as that same unit and reports what it is allowed to do.
self: pkgs:

let
  workDir = "/var/lib/cloudy-canvas";
  module = self.nixosModules.${pkgs.stdenv.hostPlatform.system}.default;

  # Runs in place of the bot, with the unit's user, sandbox and working directory, and writes down what it finds.
  probe = pkgs.writeShellScript "cloudy-canvas-probe" ''
    out=${workDir}/probe.txt
    : > "$out"
    say() { echo "$1" >> "$out"; }

    say "uid=$(id -u)"
    say "no-new-privs=$(grep '^NoNewPrivs:' /proc/self/status | cut -f2)"
    say "capabilities=$(grep '^CapEff:' /proc/self/status | cut -f2)"
    say "seccomp=$(grep '^Seccomp:' /proc/self/status | cut -f2)"
    say "umask=$(umask)"

    touch ${workDir}/writable-test 2>/dev/null && say "workdir=writable" || say "workdir=read-only"
    touch /etc/cloudy-canvas-probe 2>/dev/null && say "etc=writable" || say "etc=read-only"
    touch /var/lib/cloudy-canvas-probe 2>/dev/null && say "var-lib=writable" || say "var-lib=read-only"
    touch /usr/cloudy-canvas-probe 2>/dev/null && say "usr=writable" || say "usr=read-only"

    # The test put a file in the host's /tmp and a file in /home before starting the service.
    test -e /tmp/host-marker && say "tmp=shared" || say "tmp=private"
    test -e /home/alice/secret && say "home=visible" || say "home=hidden"

    # Other users' processes (systemd itself, for one) are not visible in /proc.
    say "foreign-processes=$(for p in /proc/[0-9]*; do stat -c %u "$p" 2>/dev/null; done | grep -vc "^$(id -u)$")"

    # Devices: only the harmless ones.
    test -e /dev/sda -o -e /dev/vda && say "disks=visible" || say "disks=hidden"
    test -w /dev/null && say "dev-null=usable" || say "dev-null=missing"

    # Things only a privileged process can do.
    hostname cloudy-probe 2>/dev/null && say "hostname=changed" || say "hostname=unchanged"
    date -s '2001-01-01' >/dev/null 2>&1 && say "clock=changed" || say "clock=unchanged"
    echo 1 > /proc/sys/kernel/hostname 2>/dev/null && say "sysctl=writable" || say "sysctl=read-only"
    say "done=yes"
  '';
in
pkgs.testers.runNixOSTest {
  name = "cloudy-canvas-service";

  nodes = {
    # The real service, with nothing configured.
    notoken = { ... }: {
      imports = [ module ];
      services.cloudy-canvas.enable = true;
    };

    # The real service, with a token from an environment file that Discord will never accept (there is no network).
    token = { ... }: {
      imports = [ module ];
      services.cloudy-canvas = {
        enable = true;
        environmentFile = "/etc/cloudy-canvas.env";
      };
      environment.etc."cloudy-canvas.env".text = "DiscordSettings__token=not-a-real-token\n";
    };

    # The same unit with its command swapped for the probe above.
    probe = { lib, ... }: {
      imports = [ module ];
      services.cloudy-canvas.enable = true;
      systemd.services.cloudy-canvas.serviceConfig = {
        Type = lib.mkForce "oneshot";
        Restart = lib.mkForce "no";
        ExecStart = lib.mkForce probe;
      };
      users.users.alice = { isNormalUser = true; home = "/home/alice"; };
    };
  };

  testScript = ''
    import re

    # What a service killed by its own sandbox leaves in the journal: a seccomp kill, a refused syscall, a runtime that
    # could not start. A bot that is merely refused by Discord leaves none of these.
    SANDBOX_DAMAGE = r"SIGSYS|status=31|Operation not permitted|Permission denied|Failed to create CoreCLR|Failed to load|Aborted|SIGABRT|core-dump"

    start_all()

    # ---- no token: it says so, exits with an error, and systemd starts it again ---------------------------------------
    notoken.wait_for_unit("multi-user.target")
    notoken.wait_until_succeeds("journalctl -u cloudy-canvas --no-pager | grep -q 'No Discord bot token is configured'")
    notoken.wait_until_succeeds("journalctl -u cloudy-canvas --no-pager | grep -q 'status=1/FAILURE'")
    notoken.wait_until_succeeds("test $(systemctl show -p NRestarts --value cloudy-canvas) -ge 1")
    journal = notoken.succeed("journalctl -u cloudy-canvas --no-pager")
    assert not re.search(SANDBOX_DAMAGE, journal), "the sandbox got in the way:\n" + journal

    # It is restarted after the delay, not at once, and never abandoned.
    notoken.succeed("systemctl show -p RestartUSec --value cloudy-canvas | grep -q '^10s$'")
    notoken.succeed("systemctl show -p StartLimitIntervalUSec --value cloudy-canvas | grep -q '^0$'")
    notoken.succeed("systemctl show -p After --value cloudy-canvas | grep -q network-online.target")

    # It runs as its own user, with its working directory owned by it and closed to everyone else.
    notoken.succeed("systemctl show -p User --value cloudy-canvas | grep -qx cloudy-canvas")
    notoken.succeed("test $(stat -c %U:%G:%a ${workDir}) = cloudy-canvas:cloudy-canvas:700")

    # ---- a token from the environment file: it is picked up, the bot starts and then Discord can't be reached ---------
    token.wait_for_unit("multi-user.target")
    token.wait_until_succeeds("journalctl -u cloudy-canvas --no-pager | grep -q 'Starting up'")
    token.sleep(20)
    journal = token.succeed("journalctl -u cloudy-canvas --no-pager")
    assert "No Discord bot token is configured" not in journal, "the token in the environment file was not used:\n" + journal
    assert not re.search(SANDBOX_DAMAGE, journal), "the sandbox got in the way:\n" + journal

    # ---- what the sandbox allows ---------------------------------------------------------------------------------------
    probe.wait_for_unit("multi-user.target")
    probe.succeed("touch /tmp/host-marker")
    probe.succeed("mkdir -p /home/alice && echo hidden > /home/alice/secret")
    probe.succeed("systemctl start cloudy-canvas")
    results = dict(line.split("=", 1) for line in probe.succeed("cat ${workDir}/probe.txt").split())
    print(results)

    expected = {
        "done": "yes",
        "no-new-privs": "1",
        "capabilities": "0000000000000000",
        "seccomp": "2",
        "umask": "0077",
        "workdir": "writable",
        "etc": "read-only",
        "var-lib": "read-only",
        "usr": "read-only",
        "tmp": "private",
        "home": "hidden",
        "disks": "hidden",
        "dev-null": "usable",
        "hostname": "unchanged",
        "clock": "unchanged",
        "sysctl": "read-only",
        "foreign-processes": "0",
    }
    for key, value in expected.items():
        assert results.get(key) == value, f"{key}: expected {value}, got {results.get(key)}\n{results}"
    assert results["uid"] != "0", "the service must not run as root"

    # systemd's own opinion of how exposed the unit is (0 is a service that can do nothing, 10 one that can do anything).
    analysis = probe.succeed("systemd-analyze security cloudy-canvas.service --no-pager")
    print(analysis)
    match = re.search(r"Overall exposure level for cloudy-canvas.service: ([0-9.]+)", analysis)
    assert match, analysis
    assert float(match.group(1)) < 3.0, f"the service is more exposed than it should be: {match.group(1)}"
  '';
}
