# DPI bypass controls for Windows

The toolbar offers separate SNI, Serverless and CF controls alongside Psiphon. Only one of these bypass modes runs at a time. Enabling a mode turns the other bypass modes off while preserving your selected profile and current TUN setting. Psiphon still enables TUN according to its existing behavior. Turning SNI, Serverless or CF off reloads the selected profile. If a bypass process exits or cannot start, the app turns that mode off and reloads the selected profile, keeping the current TUN setting.

## SNI spoofing

Set a VLESS or Trojan TLS/REALITY profile as active, open **SNI → SNI spoofing settings**, and choose a decoy SNI, TLS fingerprint and active/passive injector. An optional host:port override changes the injection destination. The original TLS server name, WebSocket headers and credentials are retained. The helper supports IPv4 destinations. Custom configurations and subscription proxy chains are not supported by this overlay.

Enable **SNI → Enable SNI spoofing for active config**. Windows requests administrator privileges for packet injection. The app uses [SNI-Spoofing-Go v0.7.2](https://github.com/aleskxyz/SNI-Spoofing-Go/tree/v0.7.2), the Windows version of the helper used in the [IranTSR Android release](https://github.com/IranTSR/v2rayNG/releases/tag/v2.3.10-sni-spoofing). Blue **SNI: On** indicates it is enabled; gray **SNI: Off** indicates it is disabled.

## Cloudflare Fragment preset

Set a Cloudflare Worker VLESS/Trojan TLS profile as active and enable **CF → Enable Cloudflare Fragment preset**. This applies the supplied PattN/PattNG recipe to the generated runtime config:

- Finalmask: TLS ClientHello lengths `0,104,1`, delay `0`, maxSplit `0`, followed by packet `1-1` lengths `114,1`, delay `1`, maxSplit `11`.
- TLS fingerprint `unsafe`, ALPN `http/1.1`, and the complete cipher suite list in `CloudflareFragmentService.CipherSuites`.
- The original real SNI and certificate verification setting are preserved. The fingerprint name does not turn certificate verification off.

The settings dialog allows an optional clean IPv4 address; blank keeps the existing address. The suggested `188.114.97.6` is not automatically applied or guaranteed to work on every network. Orange **CF: On** indicates the preset is active. Profiles are not modified, and turning it off restores their normal generated settings. Supports WebSocket, HTTPUpgrade, XHTTP and raw TCP; gRPC requires HTTP/2 and is excluded from this HTTP/1.1 preset.

## Serverless direct access

Choose **Serverless → Direct · Fragment A** or **B**. These are embedded snapshots of [patterniha/Serverless-for-Iran](https://github.com/patterniha/Serverless-for-Iran), credited to @patterniha under GPL-3.0. The mixed proxy binds to loopback using the configured local port, or a separate internal port when TUN is enabled. It can work without selecting a remote profile. The selected profile remains available when you turn Serverless off.

Serverless provides direct access with fragmentation, DNS and routing rules. Websites see your normal connection IP, and this method cannot reach destinations whose IPs are blocked or which deny access from Iran. Its project advises against judging functionality from a profile delay test, so this mode does not overwrite the selected server's delay result.

## Installing companion runtimes

From a source checkout, after publishing the Windows app, run:

```powershell
./scripts/Install-DpiRuntimes.ps1 -Destination 'C:\path\to\v2rayN' -Architecture x64
```

Use `arm64` for a Windows ARM64 build. The script verifies pinned SHA-256 hashes for SNI-Spoofing-Go v0.7.2 and Xray v26.9.30. The latter is an upstream prerelease that supports Finalmask and the required TLS settings. It is installed separately in `bin/Serverless`, with its geodata and license files, and is used only by Serverless and CF. Your regular Xray and sing-box binaries stay unchanged. SNI runs from `bin/SniSpoofing` and includes the upstream WinDivert integration. Release packaging includes these companions for Windows WPF builds.

Successful local startup and configuration validation do not guarantee that a DPI method works on a particular ISP. Test the mode against the sites you use on your connection.

While SNI or CF is enabled, **Test real delay** measures the live active connection through its helper or preset. It does not launch independent normal cores or overwrite other profiles' delay results. Turn the mode off before testing saved profiles independently. Serverless does not run profile tests.

Changing modes or reloading cancels pending connection checks and discards results from older batch tests. This prevents a test from the previous connection from overwriting the new connection's delay. SNI helper messages, including injection/ACK failures and shutdown, are saved in the dated file under `guiLogs` when application logging is enabled. A blue button confirms the mode is enabled; a successful real delay or website request confirms connectivity.

## Sources and licenses

- [SNI-Spoofing-Go](https://github.com/aleskxyz/SNI-Spoofing-Go) — GPL-3.0, release v0.7.2; binary license included by the installer.
- [Serverless-for-Iran](https://github.com/patterniha/Serverless-for-Iran) — GPL-3.0, embedded `Serverless-v51-fragA/B` snapshots; original credits retained.
- [PattN](https://github.com/patterniha/PattN) and the user's supplied guide — source of the Cloudflare preset values.
- [Xray-core](https://github.com/XTLS/Xray-core/releases/tag/v26.9.30) — separate runtime, upstream license and geodata included.
