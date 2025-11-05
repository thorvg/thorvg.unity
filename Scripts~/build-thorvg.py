#!/usr/bin/env python3
"""
ThorVG Build Script for Unity
Builds ThorVG native libraries for different platforms

Usage:
    python build-thorvg.py              # Build for desktop (current platform)
    python build-thorvg.py desktop      # Build for desktop (current platform)
    python build-thorvg.py ios          # Build for iOS
    python build-thorvg.py android      # Build for Android
    python build-thorvg.py wasm         # Build for WebGL
    python build-thorvg.py all          # Build for all platforms
"""

import os
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

def check_dependencies(need_emsdk=False):
    """Check if required build tools are installed"""
    # Check for meson
    try:
        subprocess.run(["meson", "--version"], capture_output=True, check=True)
        print("✅ Meson found")
    except (subprocess.CalledProcessError, FileNotFoundError):
        print("❌ Meson not found!")
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
        print("✅ Ninja found")
    except (subprocess.CalledProcessError, FileNotFoundError):
        print("⚠️  Ninja not found (optional but recommended)")
        print("  Install: brew install ninja")

    if not THORVG_DIR.exists():
        print("Cloning ThorVG repository...")
        run_command(["git", "clone", THORVG_REPO, '--depth=1', f'--branch={THORVG_TAG}', str(THORVG_DIR)])
    else:
        print("✅ ThorVG found")
    
    # Setup emsdk if needed for WASM builds
    if need_emsdk:
        emsdk_dir = Path("emsdk")
        if not emsdk_dir.exists():
            print("Downloading emsdk...")
            run_command(["git", "clone", "https://github.com/emscripten-core/emsdk.git", str(emsdk_dir)])
        else:
            print("✅ emsdk found")
        
        # Install and activate Emscripten 4.0.0
        emsdk_script = "emsdk.bat" if platform.system() == "Windows" else "./emsdk"
        print("Installing Emscripten 4.0.0...")
        run_command([emsdk_script, "install", "4.0.0"], cwd=emsdk_dir)
        run_command([emsdk_script, "activate", "4.0.0"], cwd=emsdk_dir)
        print("✅ Emscripten 4.0.0 activated")

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
        Path(os.environ.get("ANDROID_NDK_HOME", "")) if os.environ.get("ANDROID_NDK_HOME") else None,
        Path(os.environ.get("ANDROID_NDK", "")) if os.environ.get("ANDROID_NDK") else None,
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
                print(f"✅ Found Android NDK: {location}")
                return location
            
            # Versioned NDK path (Android Studio or Caskroom)
            versions = sorted([d for d in location.iterdir() if d.is_dir()], reverse=True)
            if versions:
                # Handle Homebrew's .app wrapper
                ndk_path = versions[0]
                if (ndk_path / "AndroidNDK14206865.app").exists():
                    ndk_path = ndk_path / "AndroidNDK14206865.app/Contents/NDK"
                
                if (ndk_path / "toolchains").exists():
                    print(f"✅ Found Android NDK: {ndk_path}")
                    return ndk_path
    
    print("❌ Android NDK not found!")
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
        "x86_64": ("x86_64-linux-android", "x86_64", "x86_64")
    }
    
    if arch not in arch_map:
        raise ValueError(f"Unknown architecture: {arch}")
    
    compiler_prefix, cpu_family, cpu = arch_map[arch]
    
    content = f"""# Auto-generated Android {arch} cross-file

[binaries]
cpp     = '{toolchain}/bin/{compiler_prefix}{api_level}-clang++'
c       = '{toolchain}/bin/{compiler_prefix}{api_level}-clang'
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
    print(f"✅ Generated cross-file: {cross_file}")
    return cross_file


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
    output_dir = UNITY_PLUGINS / "iOS" / "arm64"
    output_dir.mkdir(parents=True, exist_ok=True)
    
    source = build_dir / "src" / "libthorvg.a"
    shutil.copy2(source, output_dir / "libthorvg.a")
    print(f"✅ iOS build copied to {output_dir / 'libthorvg.a'}")

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
            print(f"⚠️  {e}, skipping {arch}")
            continue
        
        build_dir = Path(f"build/android-{arch}")
        
        # Setup
        cmd = ["meson", "setup", str(build_dir), str(THORVG_DIR),
               f"--cross-file={cross_file}"] + COMMON_OPTIONS + ["--wipe"]
        
        try:
            run_command(cmd)
            
            # Compile
            run_command(["meson", "compile", "-C", str(build_dir)])
            
            # Copy to Unity plugins
            output_dir = UNITY_PLUGINS / "Android" / "libs" / arch
            output_dir.mkdir(parents=True, exist_ok=True)
            
            source = build_dir / "src" / "libthorvg.so"
            shutil.copy2(source, output_dir / "libthorvg.so")
            print(f"✅ {arch} build copied to {output_dir / 'libthorvg.so'}")
        finally:
            # Clean up generated cross-file
            if cross_file.exists():
                cross_file.unlink()
    
    print("\n✅ All Android architectures built")

def setup_emsdk():
    """Setup Emscripten SDK"""
    emsdk_dir = Path("emsdk")
    
    if not emsdk_dir.exists():
        print("Downloading emsdk...")
        run_command(["git", "clone", "https://github.com/emscripten-core/emsdk.git", str(emsdk_dir)])
    else:
        print("✅ emsdk already exists")
    
    return emsdk_dir

def create_wasm_cross_file(emsdk_dir):
    """Create WASM cross-file with correct emsdk paths"""
    emsdk_abs = emsdk_dir.resolve()
    
    content = f"""# Auto-generated WASM cross-file

[binaries]
cpp = '{emsdk_abs}/upstream/emscripten/em++.py'
ar = '{emsdk_abs}/upstream/emscripten/emar.py'
strip = '-strip'

[properties]
root = '{emsdk_abs}/upstream/emscripten/system'
shared_lib_suffix = 'js'
static_lib_suffix = 'js'
shared_module_suffix = 'js'
exe_suffix = 'js'

[built-in options]
cpp_args = ['-Wshift-negative-value', '-flto', '-Oz', '-fno-exceptions']
cpp_link_args = ['-Wshift-negative-value', '-flto', '-Oz', '-fno-exceptions', '--bind', '--closure=1', '-sWASM=1', '-sALLOW_MEMORY_GROWTH=1', '-sEXPORT_ES6=1', '-sFORCE_FILESYSTEM=1', '-sMODULARIZE=1', '-sEXPORTED_RUNTIME_METHODS=FS']

[host_machine]
system = 'emscripten'
cpu_family = 'wasm32'
cpu = 'wasm32'
endian = 'little'
"""
    
    cross_file = Path(".wasm32.cross")
    cross_file.write_text(content)
    print(f"✅ Generated WASM cross-file: {cross_file}")
    return cross_file

def build_wasm():
    """Build WASM module using Emscripten"""
    print("\n=== Building for WebGL ===")
    
    build_dir = Path("build/wasm")
    
    # Generate WASM cross-file
    emsdk_dir = Path("emsdk")
    cross_file = create_wasm_cross_file(emsdk_dir)
    emsdk_env_path = emsdk_dir / "emsdk_env.sh"
    
    try:
        # Build command matching ThorVG's wasm_build.sh
        meson_cmd = f"meson setup {build_dir} {THORVG_DIR} " \
                   f"--cross-file={cross_file} " \
                   f"-Db_lto=true -Ddefault_library=static -Dstatic=true " \
                   f"-Dloaders=all -Dthreads=false " \
                   f"-Dbindings=wasm_beta -Dpartial=false -Dfile=false " \
                   f"-Dbuildtype=release --wipe"
        
        # Source emsdk and run meson
        full_cmd = f"source {emsdk_env_path} && {meson_cmd}"
        print(f"Running with Emscripten environment...")
        result = subprocess.run(full_cmd, shell=True, cwd=Path.cwd())
        if result.returncode != 0:
            raise subprocess.CalledProcessError(result.returncode, full_cmd)
        
        # Compile
        compile_cmd = f"source {emsdk_env_path} && meson compile -C {build_dir}"
        result = subprocess.run(compile_cmd, shell=True, cwd=Path.cwd())
        if result.returncode != 0:
            raise subprocess.CalledProcessError(result.returncode, compile_cmd)
        
        # Copy WASM module files to package StreamingAssets
        # Unity will copy these to Build/StreamingAssets/Packages/com.thorvg.unity/WebGL/
        output_dir = Path("../StreamingAssets/WebGL")
        output_dir.mkdir(parents=True, exist_ok=True)
        
        wasm_output = build_dir / "src" / "bindings" / "wasm"
        shutil.copy2(wasm_output / "thorvg.js", output_dir / "thorvg.js")
        shutil.copy2(wasm_output / "thorvg.wasm", output_dir / "thorvg.wasm")
        print(f"✅ WebGL module copied to {output_dir}")
    finally:
        # Clean up
        if cross_file.exists():
            cross_file.unlink()

def main():
    """Main entry point"""
    # Parse arguments
    if len(sys.argv) < 2:
        target = "desktop"
    else:
        target = sys.argv[1].lower()
    
    # Check dependencies (with emsdk for WASM builds)
    need_emsdk = target in ["wasm", "webgl", "all"]
    check_dependencies(need_emsdk=need_emsdk)
    
    # Build based on target
    if target == "desktop":
        build_desktop()
    elif target == "ios":
        build_ios()
    elif target == "android":
        build_android()
    elif target == "wasm" or target == "webgl":
        build_wasm()
    elif target == "all":
        build_desktop()
        build_ios()
        build_android()
        build_wasm()
    else:
        print(f"Unknown target: {target}")
        print(__doc__)
        sys.exit(1)
    
    print("\n✅ Build complete!")

if __name__ == "__main__":
    main()

