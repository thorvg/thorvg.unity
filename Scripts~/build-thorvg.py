#!/usr/bin/env python3
"""
ThorVG Build Script for Unity
Builds ThorVG native libraries for different platforms

Usage:
    python build-thorvg.py              # Build for desktop (current platform)
    python build-thorvg.py desktop      # Build for desktop (current platform)
    python build-thorvg.py ios          # Build for iOS
    python build-thorvg.py android      # Build for Android
    python build-thorvg.py all          # Build for all platforms
"""

import sys
import subprocess
import platform
import shutil
from pathlib import Path

# Configuration
THORVG_TAG = "v1.0-pre31"
THORVG_REPO = "https://github.com/thorvg/thorvg.git"
THORVG_DIR = Path(f'thorvg-{THORVG_TAG}')
UNITY_PLUGINS = Path("../Plugins")

# Common meson options
COMMON_OPTIONS = [
    "-Dbindings=capi",
    "-Dloaders=lottie,svg,png,jpg,webp",
    "-Dthreads=false",
    "-Dfile=false",
    "-Dpartial=false",
    "-Dextra=",
    "-Dbuildtype=release"
]

def run_command(cmd, cwd=None):
    """Run a command and print output"""
    print(f"Running: {' '.join(cmd)}")
    result = subprocess.run(cmd, cwd=cwd, capture_output=False, text=True)
    if result.returncode != 0:
        print(f"Error: Command failed with code {result.returncode}")
        sys.exit(1)

def clone_thorvg():
    """Clone ThorVG repository if it doesn't exist"""
    if not THORVG_DIR.exists():
        print("Cloning ThorVG repository...")
        run_command(["git", "clone", THORVG_REPO, '--depth=1', f'--branch={THORVG_TAG}', str(THORVG_DIR)])
    else:
        print("ThorVG repository already exists")

def build_desktop():
    """Build for current desktop platform (macOS, Windows, Linux)"""
    print("\n=== Building for Desktop ===")
    
    build_dir = Path("build/desktop")
    
    # Setup
    cmd = ["meson", "setup", str(build_dir), str(THORVG_DIR)] + COMMON_OPTIONS + ["--wipe"]
    run_command(cmd)
    
    # Compile
    run_command(["meson", "compile", "-C", str(build_dir)])
    
    # Copy to appropriate plugin folder
    system = platform.system()
    if system == "Darwin":
        # macOS
        output_dir = UNITY_PLUGINS / "arm64"
        output_file = "libthorvg.dylib"
        source = build_dir / "src" / output_file
    elif system == "Windows":
        # Windows
        output_dir = UNITY_PLUGINS / "x86_64"
        output_file = "libthorvg.dll"
        source = build_dir / "src" / output_file
    else:
        # Linux
        output_dir = UNITY_PLUGINS / "x86_64"
        output_file = "libthorvg.so"
        source = build_dir / "src" / output_file
    
    output_dir.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, output_dir / output_file)
    print(f"✅ Desktop build copied to {output_dir / output_file}")

def build_ios():
    """Build for iOS (ARM64)"""
    print("\n=== Building for iOS ===")
    
    if platform.system() != "Darwin":
        print("⚠️  iOS builds require macOS")
        return
    
    build_dir = Path("build/ios")
    cross_file = THORVG_DIR / "cross" / "ios_arm64.txt"
    
    # Setup
    cmd = ["meson", "setup", str(build_dir), str(THORVG_DIR), 
           f"--cross-file={cross_file}", '-Dstatic=true', '-Ddefault_library=static'] + COMMON_OPTIONS + ["--wipe"]
    run_command(cmd)
    
    # Compile
    run_command(["meson", "compile", "-C", str(build_dir)])
    
    # Copy to Unity plugins
    output_dir = UNITY_PLUGINS / "iOS"
    output_dir.mkdir(parents=True, exist_ok=True)
    
    source = build_dir / "src" / "libthorvg.a"
    shutil.copy2(source, output_dir / "libthorvg.a")
    print(f"✅ iOS build copied to {output_dir / 'libthorvg.a'}")

def build_android():
    """Build for Android (all architectures)"""
    print("\n=== Building for Android ===")
    
    architectures = [
        ("arm64-v8a", "android-arm64.cross"),      # Modern 64-bit (required)
        ("armeabi-v7a", "android-armv7.cross"),    # Legacy 32-bit (optional)
        ("x86_64", "android-x86_64.cross")         # Emulator (optional)
    ]
    
    scripts_dir = Path(__file__).parent
    
    for arch, cross_file_name in architectures:
        print(f"\n--- Building {arch} ---")
        
        build_dir = Path(f"build/android-{arch}")
        cross_file = scripts_dir / "cross" / cross_file_name
        
        if not cross_file.exists():
            print(f"⚠️  Cross-file not found: {cross_file}, skipping {arch}")
            continue
        
        # Setup
        cmd = ["meson", "setup", str(build_dir), str(THORVG_DIR),
               f"--cross-file={cross_file}"] + COMMON_OPTIONS + ["--wipe"]
        run_command(cmd)
        
        # Compile
        run_command(["meson", "compile", "-C", str(build_dir)])
        
        # Copy to Unity plugins
        output_dir = UNITY_PLUGINS / "Android" / "libs" / arch
        output_dir.mkdir(parents=True, exist_ok=True)
        
        source = build_dir / "src" / "libthorvg.so"
        shutil.copy2(source, output_dir / "libthorvg.so")
        print(f"✅ {arch} build copied to {output_dir / 'libthorvg.so'}")
    
    print("\n✅ All Android architectures built")

def main():
    """Main entry point"""
    # Parse arguments
    if len(sys.argv) < 2:
        target = "desktop"
    else:
        target = sys.argv[1].lower()
    
    # Ensure ThorVG is cloned
    clone_thorvg()
    
    # Build based on target
    if target == "desktop":
        build_desktop()
    elif target == "ios":
        build_ios()
    elif target == "android":
        build_android()
    elif target == "all":
        build_desktop()
        build_ios()
        build_android()
    else:
        print(f"Unknown target: {target}")
        print(__doc__)
        sys.exit(1)
    
    print("\n✅ Build complete!")

if __name__ == "__main__":
    main()

