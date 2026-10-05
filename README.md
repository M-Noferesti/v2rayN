# v2rayN

## v2rayN Psiphon Support

This fork adds built-in Psiphon support to v2rayN on Windows, including direct Psiphon and Psiphon-after-config modes, region selection, and integration with TUN mode.

Psiphon and TUN are linked: disabling TUN turns Psiphon off, and enabling a Psiphon mode enables TUN.

Windows also has separate **SNI Spoofing**, **Serverless A/B** and **Cloudflare Fragment** controls. They apply to the running connection without replacing your selected profile. See [DPI bypass setup and usage](docs/DPI-BYPASS.md), including companion runtime installation and the PattN/PattNG Cloudflare preset.

### Download this fork for Windows

Download **v2rayN-Psiphon-Windows-x64.zip** from [this fork's latest release](https://github.com/M-Noferesti/v2rayN/releases/latest). This package includes `v2rayN.exe`, the .NET runtime, Xray, sing-box, Psiphon and the SNI/Serverless/Cloudflare companions. It is for Windows x64; ARM64 and x86 packages are not provided by this fork yet.

1. Extract the **entire ZIP** to a writable folder, then run `v2rayN.exe`. GitHub's **Code → Download ZIP** contains source code, not the application.
2. Import your own subscription or server configuration and select a profile as active. No personal profiles are bundled.
3. Enable TUN for device routing and approve Windows administrator access when requested. Use the **P** menu for **Psiphon only**, **Psiphon after active config**, country selection, or **Off**.
4. Use the separate **SNI**, **Serverless** or **CF** menus as needed. SNI requires administrator access and a compatible profile. Only one bypass mode runs at a time.

Turning TUN off turns all bypass modes off and restores the selected normal profile. For an update, close the old app before launching the new copy. Keep your existing `guiConfigs` folder locally if you want to transfer saved profiles.

These download links are for this modified fork. The upstream links below document the original project and do not include these additions.

### A GUI client for Windows, Linux and macOS. Support [Xray](https://github.com/XTLS/Xray-core) and [sing-box](https://github.com/SagerNet/sing-box) and [others](https://github.com/2dust/v2rayN/wiki/List-of-supported-cores)

[![CodeFactor](https://www.codefactor.io/repository/github/2dust/v2rayn/badge)](https://www.codefactor.io/repository/github/2dust/v2rayn)
[![Release](https://img.shields.io/github/v/release/M-Noferesti/v2rayN?include_prereleases&logo=github&label=Release)](https://github.com/M-Noferesti/v2rayN/releases)
[![Downloads](https://img.shields.io/github/downloads/M-Noferesti/v2rayN/total?logo=github&label=Downloads)](https://github.com/M-Noferesti/v2rayN/releases)
[![Telegram](https://img.shields.io/badge/Telegram-Chat-26A5E4?logo=telegram)](https://t.me/v2rayn)
 
[![Windows](https://img.shields.io/badge/Windows-supported-0078D6?logo=windows)](https://github.com/2dust/v2rayN) 
[![Linux](https://img.shields.io/badge/Linux-supported-FCC624?logo=linux&logoColor=000)](https://github.com/2dust/v2rayN) 
[![macOS](https://img.shields.io/badge/macOS-supported-000000?logo=apple)](https://github.com/2dust/v2rayN) 
Release assets include SHA-256 checksums. This fork's Windows package is not GPG-signed.


---

## Download / 下载

Download the latest release here:

在这里下载最新版本：

[Download v2rayN with Psiphon support for Windows](https://github.com/M-Noferesti/v2rayN/releases/latest)


> [!TIP]
> v2rayN is the desktop version. For the mobile version, please visit the v2rayNG \
> v2rayN 是电脑版，手机版请访问 v2rayNG
>
> https://github.com/2dust/v2rayNG

---

## Documentation / 使用文档

Read the Wiki for usage guides and configuration details.

请阅读 Wiki 获取使用说明和配置教程。

[https://github.com/2dust/v2rayN/wiki](https://github.com/2dust/v2rayN/wiki)

---

## Supported Platforms / 支持平台

| Platform / 平台 | x64 | x86 | arm64 | riscv64 | loong64 |
| --- | --- | --- | --- | --- | --- |
| Windows | ✅ | ✅ | ✅ | - | - |
| Linux | ✅ | - | ✅ | ✅ | ✅ |
| macOS | ✅ | - | ✅ | - | - |

Minimum OS requirements: [Release files introduction](https://github.com/2dust/v2rayN/wiki/Release-files-introduction) / 最低系统要求：[发布文件介绍](https://github.com/2dust/v2rayN/wiki/Release-files-introduction)

---

## GPG Verification / GPG 签名校验

Release files are signed with GPG to verify authenticity and integrity, helping prevent mirror, ISP, or CDN hijacking.

发布文件已使用 GPG 签名，可用于校验文件真实性与完整性，预防镜像站、运营商或 CDN 劫持。

### Fingerprint / 公钥指纹

```text
7694 5E9F 3E9A 168F 8070 F195 805D 661C
134D FAF6 8903 C199 463C 31E5 AE90 3AE0
```

---

## Community / 社区

Telegram Group / Telegram 群组：

[https://t.me/v2rayN](https://t.me/v2rayN)

Telegram Channel / Telegram 频道：

[https://t.me/github_2dust](https://t.me/github_2dust)
