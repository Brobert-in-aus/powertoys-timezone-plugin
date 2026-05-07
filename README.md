# Time Zone Converter PowerToys Run Plugin

PowerToys Run plugin that converts a typed time to a requested destination timezone.

## Usage

Use the `tz` action keyword in PowerToys Run:

```text
tz 10:30 AM CST to +10
tz 10:30 AM CET
tz ten thirty next Friday to AEDT
tz 22:30 CET to UTC
tz May 10 10:00 AM UTC to Central European Time
tz noon GMT-0530 to AEST
```

Without `to`, the final timezone is treated as the source timezone and the time is converted to your local Windows timezone. With `to`, the timezone after `to` is the destination; if the source timezone is omitted in that form, the source time is interpreted in your local Windows timezone.

## Supported Input

- Numeric times: `10:30 AM`, `22:30`, `1030pm`
- Word times: `ten thirty`, `quarter past ten`, `quarter to eleven`, `noon`, `midnight`
- Dates: `May 10`, `10 May`, `2026-05-10`, `10/05/2026`
- Relative dates: `today`, `tomorrow`, `yesterday`, `Friday`, `next Friday`, `last Friday`
- Destination/source zones: common North American, European, UK, African, Indian, Asian, Australian, and New Zealand abbreviations/names
- Offsets: `+10`, `+10:00`, `UTC+2`, `GMT-0530`

Region names such as `ET`, `Central European Time`, `UK Time`, `India Time`, and `New Zealand Time` use Windows timezone rules where applicable. Abbreviations such as `EST`, `CST`, `CET`, `CEST`, and `AEDT` are treated as fixed offsets.

## Build

```powershell
dotnet build .\TimeZoneConverter.slnx -c Release /p:Platform=x64
dotnet test .\TimeZoneConverter.slnx /p:Platform=x64
```

## Deploy

From PowerShell:

```powershell
.\deploy.ps1 -Platform x64
```
