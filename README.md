[![Discord](https://img.shields.io/badge/Community-5865f2?style=flat&logo=discord&logoColor=white)](https://discord.gg/n25xj6J6HM)
[![ThorVGPT](https://img.shields.io/badge/ThorVGPT-76A99C?style=flat&logo=openai&logoColor=white)](https://chat.openai.com/g/g-Ht3dYIwLO-thorvgpt)
[![OpenCollective](https://img.shields.io/badge/OpenCollective-84B5FC?style=flat&logo=opencollective&logoColor=white)](https://opencollective.com/thorvg)
[![License](https://img.shields.io/badge/licence-MIT-green.svg?style=flat)](LICENSE)

# ThorVG Unity
<p align="center">
  <img width="800" height="auto" src="https://github.com/thorvg/thorvg.site/blob/main/readme/logo/512/thorvg-banner.png">
</p>
ThorVG Unity supports Lottie animations, enabling seamless integration of vector-based animations in Unity projects.

## Installation

### Via Unity Package Manager

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click the `+` button
3. Select `Add package from git URL...`
4. Enter:

```bash
https://github.com/thorvg/thorvg.unity.git?path=/package
```

## Quick Start

### Using SVG Files

1. **Import**: Drag `.svg` file into Unity
2. **Use**: Drag the SVG onto a GameObject with SpriteRenderer
3. Done! SVG displays as a sprite

### Using Lottie Animations

1. **Import**: Drag `.json` Lottie file into Unity
2. **Add Component**: Add `TvgPlayer` to a GameObject
3. **Assign**: Drag the `.json` file to the `Source` field
4. **Play**: Hit Play to see the animation

## Platform Support

- ✅ Windows (x64)
- ✅ macOS (Apple Silicon)
- ✅ Linux (x64)
- ✅ WebGL
- ✅ Android (armv7a, arm64-v8, x64)
- ✅ iOS (arm64)

## Requirements

- Unity 2021.3 or newer

## Building ThorVG

```bash
cd scripts
python3 build-thorvg.py
```

The built libraries will then be copied to the [Plugins](Plugins) folder

## Communication

For real-time conversations and discussions, please join us on [Discord](https://discord.gg/n25xj6J6HM)
