{
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";
    nuget-packageslock2nix = {
      url = "github:mdarocha/nuget-packageslock2nix/main";
      inputs.nixpkgs.follows = "nixpkgs";
    };
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils, nuget-packageslock2nix, ... }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };
        cloudy =
          pkgs.buildDotnetModule {
            pname = "Cloudy-Canvas";
            version = "0.0.1";
            src = ./Cloudy-Canvas;
            dotnet-sdk = pkgs.dotnet-sdk_7;
            dotnet-runtime = pkgs.dotnet-runtime_7;
            selfContainedBuild = true;
            buildType = "Release";
            runtimeDeps = with pkgs; [
              zlib
              openssl
              icu
              curl
            ];
            nugetDeps = nuget-packageslock2nix.lib {
              inherit system;
              name = "Cloudy-Canvas";
              lockfiles = [
                ./Cloudy-Canvas/packages.lock.json
              ];
            };
          };
      in
      {
        packages.cloudy-canvas = cloudy;
        packages.default = cloudy;


        formatter = nixpkgs.legacyPackages.${system}.nixpkgs-fmt;

        nixosModules = rec {
          cloudy-canvas = import ./service.nix self;
          default = cloudy-canvas;
        };

        devShells.default =
          let
            pkgs = import nixpkgs { inherit system; };
          in
          pkgs.mkShell {
            buildInputs = with pkgs; [
              dotnet-sdk_7
            ];
          };
      });
}
