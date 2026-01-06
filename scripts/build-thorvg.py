#!/usr/bin/env python3
"""
ThorVG Build Script for Unity
Builds ThorVG native libraries for different platforms

Usage:
    python build-thorvg.py <tag> desktop      # Build for desktop (current platform)
    python build-thorvg.py <tag> ios          # Build for iOS
    python build-thorvg.py <tag> android      # Build for Android
    python build-thorvg.py <tag> wasm         # Build for WebGL

Examples:
    python build-thorvg.py v1.0-pre34 desktop
"""

import argparse
import os
import platform
import shutil
import subprocess
import sys
from pathlib import Path

# Configuration
THORVG_REPO = "https://github.com/thorvg/thorvg.git"
UNITY_PLUGINS = Path(__file__).parent.parent / "package" / "Plugins"

# These will be set based on CLI arguments
THORVG_TAG = ""
THORVG_DIR = Path()

# Common meson options
COMMON_OPTIONS = [
    "-Dbindings=capi",
    "-Dloaders=lottie,svg,png,jpg,webp",
    "-Dengines=sw",
    "-Dsavers=",
    "-Dthreads=false",
    "-Dfile=false",
    "-Dpartial=false",
    "-Dextra=",
    "-Dstatic=true",
    "-Dbuildtype=release",
]


def check_dependencies():
    """Check if required build tools are installed"""
    # Check for meson
    try:
        subprocess.run(["meson", "--version"], capture_output=True, check=True)
        print("[OK] Meson found")
    except (subprocess.CalledProcessError, FileNotFoundError):
        print("[ERROR] Meson not found!")
        print("\nInstall Meson:")
        system = platform.system()
        if system == "Darwin":
            print("  macOS:   brew install meson")
        elif system == "Linux":
            print("  Linux:   sudo apt install meson")
            print("           or: pip3 install meson")
        elif system == "Windows":
            print("  Windows: pip install meson")
            print("           or: choco install meson")
        else:
            print("  pip3 install meson")
        sys.exit(1)

    # Check for ninja (usually comes with meson)
    try:
        subprocess.run(["ninja", "--version"], capture_output=True, check=True)
        print("[OK] Ninja found")
    except (subprocess.CalledProcessError, FileNotFoundError):
        print("[WARN] Ninja not found (optional but recommended)")
        print("  Install: brew install ninja")

    if not THORVG_DIR.exists():
        print("Cloning ThorVG repository...")
        run_command(
            [
                "git",
                "clone",
                THORVG_REPO,
                "--depth=1",
                f"--branch={THORVG_TAG}",
                str(THORVG_DIR),
            ]
        )
    else:
        print("[OK] ThorVG found")


def run_command(cmd, cwd=None):
    """Run a command and print output"""
    print(f"Running: {' '.join(cmd)}")
    result = subprocess.run(cmd, cwd=cwd, capture_output=False, text=True)
    if result.returncode != 0:
        print(f"Error: Command failed with code {result.returncode}")
        sys.exit(1)


def find_android_ndk():
    """Auto-detect Android NDK location"""
    possible_locations = [
        # Environment variables (check first)
        Path(os.environ.get("ANDROID_NDK_HOME", ""))
        if os.environ.get("ANDROID_NDK_HOME")
        else None,
        Path(os.environ.get("ANDROID_NDK", ""))
        if os.environ.get("ANDROID_NDK")
        else None,
        Path(os.environ.get("ANDROID_HOME", "")) / "ndk"
        if os.environ.get("ANDROID_HOME")
        else None,
        # Homebrew (macOS) - direct install
        Path("/opt/homebrew/share/android-ndk"),
        Path("/usr/local/share/android-ndk"),
        # Homebrew (macOS) - cask install
        Path("/opt/homebrew/Caskroom/android-ndk"),
        Path("/usr/local/Caskroom/android-ndk"),
        # Android Studio (macOS)
        Path.home() / "Library/Android/sdk/ndk",
        # Android Studio (Linux)
        Path.home() / "Android/Sdk/ndk",
        # Android Studio (Windows)
        Path.home() / "AppData/Local/Android/Sdk/ndk",
    ]

    for location in possible_locations:
        if location and location.exists():
            # Direct NDK path (Homebrew share)
            if (location / "toolchains").exists():
                print(f"[OK] Found Android NDK: {location}")
                return location

            # Versioned NDK path (Android Studio or Caskroom)
            versions = sorted(
                [d for d in location.iterdir() if d.is_dir()], reverse=True
            )
            if versions:
                # Handle Homebrew's .app wrapper
                ndk_path = versions[0]
                if (ndk_path / "AndroidNDK14206865.app").exists():
                    ndk_path = ndk_path / "AndroidNDK14206865.app/Contents/NDK"

                if (ndk_path / "toolchains").exists():
                    print(f"[OK] Found Android NDK: {ndk_path}")
                    return ndk_path

    print("[ERROR] Android NDK not found!")
    print("\nInstall Android NDK:")
    print("  macOS:    brew install android-ndk")
    print("  Or set:   export ANDROID_NDK_HOME=/path/to/ndk")
    print("  Download: https://developer.android.com/ndk/downloads")
    return None


def get_ndk_host_tag():
    """Get the NDK host tag for current platform"""
    system = platform.system()
    if system == "Darwin":
        return "darwin-x86_64"
    elif system == "Linux":
        return "linux-x86_64"
    elif system == "Windows":
        return "windows-x86_64"
    return "darwin-x86_64"


def create_android_cross_file(arch, ndk_path, api_level=24):
    """Dynamically create Android cross-compilation file"""
    host_tag = get_ndk_host_tag()
    toolchain = ndk_path / "toolchains/llvm/prebuilt" / host_tag

    # Architecture-specific compiler prefix
    arch_map = {
        "arm64-v8a": ("aarch64-linux-android", "aarch64", "aarch64"),
        "armeabi-v7a": ("armv7a-linux-androideabi", "arm", "armv7a"),
        "x86_64": ("x86_64-linux-android", "x86_64", "x86_64"),
    }

    if arch not in arch_map:
        raise ValueError(f"Unknown architecture: {arch}")

    compiler_prefix, cpu_family, cpu = arch_map[arch]

    # Add .cmd suffix for Windows
    ext = ".cmd" if platform.system() == "Windows" else ""

    content = f"""# Auto-generated Android {arch} cross-file

[binaries]
cpp     = '{toolchain}/bin/{compiler_prefix}{api_level}-clang++{ext}'
c       = '{toolchain}/bin/{compiler_prefix}{api_level}-clang{ext}'
ar      = '{toolchain}/bin/llvm-ar'
as      = '{toolchain}/bin/llvm-as'
ranlib  = '{toolchain}/bin/llvm-ranlib'
ld      = '{toolchain}/bin/ld'
strip   = '{toolchain}/bin/llvm-strip'

[properties]
sys_root = '{toolchain}/sysroot'

[built-in options]
cpp_link_args = ['-static-libstdc++']

[host_machine]
system = 'android'
cpu_family = '{cpu_family}'
cpu = '{cpu}'
endian = 'little'
"""

    # Write to temp file
    cross_file = Path(f".android-{arch}.cross")
    cross_file.write_text(content)
    print(f"[OK] Generated cross-file: {cross_file}")
    return cross_file


def build_desktop():
    """Build for current desktop platform (macOS, Windows, Linux)"""
    print("\n=== Building for Desktop ===")

    system = platform.system()
    build_dir = Path(f"build/desktop-{system}")

    # Setup
    run_command(
        ["meson", "setup", str(build_dir), str(THORVG_DIR)]
        + COMMON_OPTIONS
        + ["--wipe"]
    )

    # Compile
    run_command(["meson", "compile", "-C", str(build_dir)])

    # Copy to appropriate plugin folder
    if system == "Darwin":
        # macOS
        output_dir = UNITY_PLUGINS / "macOS"
        output_file = "libthorvg.dylib"
        source = build_dir / "src" / output_file
    elif system == "Windows":
        # Windows
        output_dir = UNITY_PLUGINS / "x86_64"
        output_file = "libthorvg.dll"
        source = build_dir / "src" / "libthorvg-1.dll"
    else:
        # Linux
        output_dir = UNITY_PLUGINS / "x86_64"
        output_file = "libthorvg.so"
        source = build_dir / "src" / output_file

    output_dir.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, output_dir / output_file)
    print(f"[OK] Desktop build copied to {output_dir / output_file}")


def build_ios():
    """Build for iOS (ARM64)"""
    print("\n=== Building for iOS ===")

    if platform.system() != "Darwin":
        print("[WARN]  iOS builds require macOS")
        return

    build_dir = Path("build/ios")
    cross_file = THORVG_DIR / "cross" / "ios_arm64.txt"

    # Setup
    run_command(
        [
            "meson",
            "setup",
            str(build_dir),
            str(THORVG_DIR),
            f"--cross-file={cross_file}",
            "-Ddefault_library=static",
        ]
        + COMMON_OPTIONS
        + ["--wipe"]
    )

    # Compile
    run_command(["meson", "compile", "-C", str(build_dir)])

    # Copy to Unity plugins
    output_dir = UNITY_PLUGINS / "iOS" / "arm64"
    output_dir.mkdir(parents=True, exist_ok=True)

    source = build_dir / "src" / "libthorvg.a"
    shutil.copy2(source, output_dir / "libthorvg.a")
    print(f"[OK] iOS build copied to {output_dir / 'libthorvg.a'}")


def build_android():
    """Build for Android (all architectures)"""
    print("\n=== Building for Android ===")

    # Find Android NDK (checks ANDROID_NDK_HOME first)
    ndk_path = find_android_ndk()
    if not ndk_path:
        return

    architectures = ["arm64-v8a", "armeabi-v7a", "x86_64"]

    for arch in architectures:
        print(f"\n--- Building {arch} ---")

        # Generate cross-file dynamically based on detected NDK
        try:
            cross_file = create_android_cross_file(arch, ndk_path)
        except ValueError as e:
            print(f"[WARN]  {e}, skipping {arch}")
            continue

        build_dir = Path(f"build/android-{arch}")

        # Setup
        try:
            run_command(
                [
                    "meson",
                    "setup",
                    str(build_dir),
                    str(THORVG_DIR),
                    f"--cross-file={cross_file}",
                ]
                + COMMON_OPTIONS
                + ["--wipe"]
            )

            # Compile
            run_command(["meson", "compile", "-C", str(build_dir)])

            # Copy to Unity plugins
            output_dir = UNITY_PLUGINS / "Android" / "libs" / arch
            output_dir.mkdir(parents=True, exist_ok=True)

            source = build_dir / "src" / "libthorvg.so"
            shutil.copy2(source, output_dir / "libthorvg.so")
            print(f"[OK] {arch} build copied to {output_dir / 'libthorvg.so'}")
        finally:
            # Clean up generated cross-file
            if cross_file.exists():
                cross_file.unlink()

    print("\n[OK] All Android architectures built")


def create_wasm_cross_file():
    """Create WASM cross-file by replacing EMSDK: placeholder in ThorVG's template"""
    emsdk_root_env = os.environ.get("EMSDK")
    if not emsdk_root_env:
        print("[ERROR] EMSDK not found! Set the EMSDK variable")
        return None

    emsdk_root = Path(emsdk_root_env)
    if not emsdk_root.exists():
        print("[ERROR] EMSDK not found! Set the EMSDK variable")
        return None

    # Read ThorVG's wasm32_sw.txt template
    template_file = THORVG_DIR / "cross" / "wasm32_sw.txt"
    content = template_file.read_text()

    # Replace EMSDK: placeholder with actual path (matching ThorVG's wasm_build.sh)
    content = content.replace("EMSDK:", str(emsdk_root) + "/")

    # Write to temp file
    cross_file = Path(".wasm32_sw.cross")
    cross_file.write_text(content)
    print("[OK] Generated WASM cross-file")
    return cross_file


def build_wasm():
    """Build WASM module using Emscripten"""
    print("\n=== Building for WebGL ===")

    build_dir = Path("build/wasm")

    # Generate WASM cross-file
    cross_file = create_wasm_cross_file()
    if cross_file is None:
        return

    wasm_commands = COMMON_OPTIONS.copy()
    wasm_commands[0] = "-Dbindings=wasm_beta"
    wasm_commands[1] = "-Dloaders=all"

    try:
        run_command(
            [
                "meson",
                "setup",
                str(build_dir),
                str(THORVG_DIR),
                f"--cross-file={cross_file}",
                "-Ddefault_library=static",
            ]
            + wasm_commands
            + ["--wipe"],
        )

        # Compile
        run_command(["meson", "compile", "-C", str(build_dir)])

        # Copy WASM module files to package StreamingAssets
        # Unity will copy these to Build/StreamingAssets/Packages/com.thorvg.unity/WebGL/
        output_dir = Path(__file__).parent.parent / "package" / "StreamingAssets" / "WebGL"
        output_dir.mkdir(parents=True, exist_ok=True)

        wasm_output = build_dir / "src" / "bindings" / "wasm"
        shutil.copy2(wasm_output / "thorvg.js", output_dir / "thorvg.js")
        shutil.copy2(wasm_output / "thorvg.wasm", output_dir / "thorvg.wasm")
        print(f"[OK] WebGL module copied to {output_dir}")
    finally:
        # Clean up
        if cross_file.exists():
            cross_file.unlink()


def main():
    """Main entry point"""
    global THORVG_TAG, THORVG_DIR

    parser = argparse.ArgumentParser(
        description="Build ThorVG native libraries for Unity",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
    python build-thorvg.py v1.0-pre34 desktop
    python build-thorvg.py v1.0-pre34 android ios
        """,
    )
    parser.add_argument(
        "tag",
        help="ThorVG git tag to build",
    )
    parser.add_argument(
        "targets",
        nargs="+",
        choices=["desktop", "ios", "android", "wasm", "webgl", "web"],
        help="Build target(s)",
    )

    args = parser.parse_args()

    # Set global configuration based on tag
    THORVG_TAG = args.tag
    THORVG_DIR = Path(__file__).parent / f"thorvg-{THORVG_TAG}"

    # Normalize targets
    targets = set()
    for t in args.targets:
        t = t.lower()
        if t in ["wasm", "webgl", "web"]:
            targets.add("wasm")
        else:
            targets.add(t)

    # Check dependencies
    check_dependencies()

    # Build each target
    for target in targets:
        if target == "desktop":
            build_desktop()
        elif target == "ios":
            build_ios()
        elif target == "android":
            build_android()
        elif target == "wasm":
            build_wasm()

    print("\n[OK] Build complete!")


if __name__ == "__main__":
    main()
