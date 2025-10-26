# Code Index

A fast full-text searching tools based on Lucene.Net

## Feature

1. Support multiple indexes
2. Auto monitoring indexed files changes
3. Support various search (etc: fuzzy, wildcard, case-sensitive) on file content, name, location, extension
4. Support Docker
5. Support Visual Studio

## Demonstrate Site

[https://coderindex.azurewebsites.net/](https://coderindex.azurewebsites.net/)

## Guide

### Run On Your Local

#### Config

Go to CodeIndex.Server => appsettings.json

LuceneIndex => To your local empty folder, this folder will be going to store the index files and configuration files.

ManagerUsers => Config the users name, id, password that can management the indexes.

```json
"CodeIndex": {
  "LuceneIndex": "D:\\TestFolder\\Index",
  "IsInLinux": "false",
  "MaximumResults": 10000,
  "ManagerUsers": [
    {
      "Id": 1,
      "UserName": "Admin",
      "Password": "CodeIndex"
    }
  ]
}
```

#### Run Server

Set CodeIndex.Server as the start up project, compile the project

Run it via visual studio or bash

```bash
dotnet CodeIndex.Server.dll --urls "http://:5000;https://:5001"
```

#### Config Indexes

In this page, you can add/remove/delete and config the index folder you want to monitoring and searching.

![Config Indexes](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/ConfigAndSearching.gif)

#### Doing Search

##### Search By Files

It will return the matched infos with highlight in the whole file

![Search By Files](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/SearchByFiles.gif)

##### Search By Lines

It will return the matched infos with highlight and matched line number

![Search By Lines](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/SearchByLines.gif)

### Run With Docker

Support docker container, [Docker hub](https://hub.docker.com/r/qiuhaotc/codeindex)

#### Docker Command Format

```bash
docker pull qiuhaotc/codeindex
docker run -d --name codeindex -p 8080:8080 -v "Your index folder":/luceneindex -v "Your code folder":/monitorfolder -v "Your logs folder":/app/Logs -e CodeIndex__MonitorFolderRealPath="Your real folder path" -e CodeIndex__ManagerUsers__0__UserName="Your Management User Name" -e CodeIndex__ManagerUsers__0__Password="Your Management Password" --restart=always qiuhaotc/codeindex
```

##### Example

```bash
docker pull qiuhaotc/codeindex
docker run -d --name codeindex -p 8080:8080 -v /home/user/luceneindex:/luceneindex -v /home/user/codefolder:/monitorfolder -v /home/user/logs:/app/Logs -e CodeIndex__MonitorFolderRealPath="/home/user/codefolder" -e CodeIndex__ManagerUsers__0__UserName="Test" -e CodeIndex__ManagerUsers__0__Password="Dummy" --restart=always qiuhaotc/codeindex
```

Notice: in the docker container, when add the index config, the monitor folder should replace the actual path to start with "/monitorfolder", like the actually path is "/home/user/codefolder/mysourceA", the monitor folder should be "/monitorfolder/mysourceA"

### Search Extension For Visual Studio

Current icon used in listing:

![Marketplace Icon](doc/Extension-Icon.png)

|Status|Badge|
|:----|:---:|
|VS Marketplace Version|[![VS Marketplace Version](https://img.shields.io/visual-studio-marketplace/v/qiuhaotc.CodeIndexExtension?label=version&logo=visualstudio&color=blueviolet)](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)|
|VS Marketplace Downloads|[![VS Marketplace Downloads](https://img.shields.io/visual-studio-marketplace/d/qiuhaotc.CodeIndexExtension?label=downloads&logo=visualstudio)](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)|
|VS Marketplace Installs|[![VS Marketplace Installs](https://img.shields.io/visual-studio-marketplace/i/qiuhaotc.CodeIndexExtension?label=installs&logo=visualstudio)](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)|
|VS Marketplace Rating|[![VS Marketplace Rating](https://img.shields.io/visual-studio-marketplace/r/qiuhaotc.CodeIndexExtension?label=rating&logo=visualstudio)](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)|

#### Recent Updates

Latest improvements to the Visual Studio extension

- Dual Server Modes
  - Seamlessly switch between Local and Remote server modes with persistent settings.
  - Automatic health probe on settings window open (local mode) + manual Check button.
- Resilient Local Server Lifecycle
  - Single-instance control (global mutex + per-process lock files + PID file).
  - Auto restart if external termination detected (mutex loss recovery).
  - Deferred index loading until server health passes.
- Download / Update Workflow
  - One-click Download / Update (latest tag scraped from Releases page).
  - Streamed download with real-time percentage progress.
  - Temp ZIP cleanup after successful extraction; validates core binary presence.
- Smart Default Paths
  - Auto install path: `%LOCALAPPDATA%/CodeIndex.VisualStudioExtension/CodeIndex.Server` when empty.
  - Auto data path: `%LOCALAPPDATA%/CodeIndex.VisualStudioExtension/CodeIndex.Server.Data` when first selecting install path and data path empty.
- Modern Folder Picker
  - Replaced WinForms dialog with Vista IFileOpenDialog (better UX); removed System.Windows.Forms dependency.
- Theme-Aware UI
  - Buttons/styles now use Visual Studio dynamic theme brushes (light/dark/HC) instead of hardcoded colors.
- Internationalization (i18n)
  - Full multi-language support with automatic VS language detection.
  - Currently supports English and Simplified Chinese (易于扩展其他语言).
  - All UI elements (windows, buttons, messages) fully localized.
- Quick Navigation Buttons
  - Open buttons beside Local & Remote URLs (auto prepend http:// when missing).
- Responsive Async Commands
  - Instant button enable/disable; removed unsafe async void patterns.
- Embedded Log Viewer
  - Displays latest 100 log lines with refresh.
- Packaging & Manifest Reliability
  - Pre-build sync of `source.extension.vsixmanifest` prevents stale version drift.
  - Architecture targeting + ProductArchitecture resolves VSSDK1311 warning.
- Settings & Migration
  - JSON settings, legacy URL migration, normalized trailing slashes.
- Additional Hardening
  - Clear health states (Started / Stopped / Error / Unknown) drive UI state.
  - Improved error messages for download / extraction / URL opening.

#### Download Url

[Code Index Extension](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)

#### Search Code

1. Open the code index search window under: view => other window => code index search
2. Config the service url to your own service
3. Doing the search

![Code Index Search Extension](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/UseExtension.gif)

### Search Syntax

#### Phase Query

When Phase Query been ticked, we can search the content via the query like: str*ng abc, it will give the result such as "string abc", "strdummyng abc" as the search results, the results like "abc string" or "stng abc" won't return.

In Phase Query mode, currently only support wildcard matching for word like stri*, organi*tion

![Phase Query Search](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/PhaseQuerySearch.gif)

When Phase Quuery not been ticked, you can follow the sytax under [http://www.lucenetutorial.com/lucene-query-syntax.html](http://www.lucenetutorial.com/lucene-query-syntax.html) to doing the search

#### Case-Sensitive

When Case-Sensitive been ticked, we can search the content in case-sensitive mode. When search the content like String, it won't return the content that contains string

## Extension Compile

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe" src\CodeIndex.VisualStudioExtension\CodeIndex.VisualStudioExtension.csproj /t:Build /p:Configuration=Debug /nologo
```

## Misc

|Status|Value|
|:----|:---:|
|Stars|[![Stars](https://img.shields.io/github/stars/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)|
|Forks|[![Forks](https://img.shields.io/github/forks/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)|
|License|[![License](https://img.shields.io/github/license/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)|
|Issues|[![Issues](https://img.shields.io/github/issues/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)|
|Docker Pulls|[![Downloads](https://img.shields.io/docker/pulls/qiuhaotc/codeindex.svg)](https://hub.docker.com/r/qiuhaotc/codeindex)|
|Release Downloads|[![Downloads](https://img.shields.io/github/downloads/qiuhaotc/CodeIndex/total.svg)](https://github.com/qiuhaotc/CodeIndex/releases)|
