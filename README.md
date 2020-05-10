# CodeIndex

A fast code searching tools based on Lucene.Net

## Demonstrate Site

[https://codeindex.qhnetdisk.tk/](https://codeindex.qhnetdisk.tk/)

## Guide

### Run On Your Local

#### Config

Change CodeIndex.Server => appsettings.json => LuceneIndex and MonitorFolder to your local

#### Run Server

Run server and doing the searching

```bash
dotnet CodeIndex.Server.dll --urls "http://:5000;https://:5001"
```

#### Doing Search

##### Search By Files

It will return the matched infos with highlight in the whole file

![Search By Files](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/SearchByFiles.gif)

##### Search By Lines

It will return the matched infos with highlight and matched line number

![Search By Lines](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/SearchByLines.gif)

### Run With Docker

Support docker container

#### Example

```bash
docker pull qiuhaotc/codeindex
docker run -d --name codeindex -p 8080:80 -v "You index folder":/luceneindex -v "You code folder":/monitorfolder -v "your logs folder":/app/Logs -e CodeIndex__MonitorFolderRealPath="You real folder path" --restart=always qiuhaotc/codeindex
```

### Search Extension For Visual Studio

Download Url [Code Index Extension](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)

It's a pre-release for now.

|Status|Value|
|:----|:---:|
|VS Marketplace|[![VS Marketplace](http://vsmarketplacebadge.apphb.com/version-short/qiuhaotc.CodeIndexExtension.svg)](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)
|VS Marketplace Downloads|[![VS Marketplace Downloads](http://vsmarketplacebadge.apphb.com/downloads/qiuhaotc.CodeIndexExtension.svg)](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)
|VS Marketplace Installs|[![VS Marketplace Installs](http://vsmarketplacebadge.apphb.com/installs-short/qiuhaotc.CodeIndexExtension.svg)](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)

Open Search Window

![Open Search Window](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/VSExtension-1.png)

Code Index Search Window

![Code Index Search Window](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/VSExtension-2.png)

Config the service url to your own service and doing the searching

## Search Syntax

Refer [http://www.lucenetutorial.com/lucene-query-syntax.html](http://www.lucenetutorial.com/lucene-query-syntax.html)

## Misc

|Status|Value|
|:----|:---:|
|Stars|[![Stars](https://img.shields.io/github/stars/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)
|Forks|[![Forks](https://img.shields.io/github/forks/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)
|License|[![License](https://img.shields.io/github/license/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)
|Issues|[![Issues](https://img.shields.io/github/issues/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)
|Release Downloads|[![Downloads](https://img.shields.io/github/downloads/qiuhaotc/CodeIndex/total.svg)](https://github.com/qiuhaotc/CodeIndex/releases)
