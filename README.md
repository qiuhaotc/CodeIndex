# CodeIndex

A fast code searching tools based on Lucene.Net

## Demonstrate Site

[https://codeindex.qhnetdisk.tk/](https://codeindex.qhnetdisk.tk/)

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
docker run -d --name codeindex -p 8080:80 -v "Your index folder":/luceneindex -v "Your code folder":/monitorfolder -v "Your logs folder":/app/Logs -e CodeIndex__MonitorFolderRealPath="Your real folder path" -e CodeIndex__ManagerUsers__0__UserName="Your Management User Name" -e CodeIndex__ManagerUsers__0__Password="Your Management Password" --restart=always qiuhaotc/codeindex
```

##### Example

```bash
docker pull qiuhaotc/codeindex
docker run -d --name codeindex -p 8080:80 -v /home/user/luceneindex:/luceneindex -v /home/user/codefolder:/monitorfolder -v /home/user/logs:/app/Logs -e CodeIndex__MonitorFolderRealPath="/home/user/codefolder" -e CodeIndex__ManagerUsers__0__UserName="Test" -e CodeIndex__ManagerUsers__0__Password="Dummy" --restart=always qiuhaotc/codeindex
```

Notice: in the docker container, when add the index config, the monitor folder should replace the actual path to start with "/monitorfolder", like the actually path is "/home/user/codefolder/mysourceA", the monitor folder should be "/monitorfolder/mysourceA"

### Search Extension For Visual Studio

|Status|Value|
|:----|:---:|
|VS Marketplace|[![VS Marketplace](http://vsmarketplacebadge.apphb.com/version-short/qiuhaotc.CodeIndexExtension.svg)](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)
|VS Marketplace Downloads|[![VS Marketplace Downloads](http://vsmarketplacebadge.apphb.com/downloads/qiuhaotc.CodeIndexExtension.svg)](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)
|VS Marketplace Installs|[![VS Marketplace Installs](http://vsmarketplacebadge.apphb.com/installs-short/qiuhaotc.CodeIndexExtension.svg)](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)

#### Download Url

[Code Index Extension](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)

#### Search Code

1. Open the code index search window under: view => other window => code index search
2. Config the service url to your own service
3. Doing the search

![Code Index Search Extension](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/UseExtension.gif)

## Search Syntax

Refer [http://www.lucenetutorial.com/lucene-query-syntax.html](http://www.lucenetutorial.com/lucene-query-syntax.html)

## Misc

|Status|Value|
|:----|:---:|
|Stars|[![Stars](https://img.shields.io/github/stars/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)
|Forks|[![Forks](https://img.shields.io/github/forks/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)
|License|[![License](https://img.shields.io/github/license/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)
|Issues|[![Issues](https://img.shields.io/github/issues/qiuhaotc/CodeIndex)](https://github.com/qiuhaotc/CodeIndex)
|Docker Pulls|[![Downloads](https://img.shields.io/docker/pulls/qiuhaotc/codeindex.svg)](https://hub.docker.com/r/qiuhaotc/codeindex)
|Release Downloads|[![Downloads](https://img.shields.io/github/downloads/qiuhaotc/CodeIndex/total.svg)](https://github.com/qiuhaotc/CodeIndex/releases)
