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

Main Page

![Main Page](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/WebServer.png)

Details Page

![Details Page](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/WebServer-Details.png)

### Run With Docker

Support docker container

#### Example

```bash
docker pull qiuhaotc/codeindex
docker run -d --name codeindex -p 8080:80 -v "You index folder":/luceneindex -v "You code folder":/monitorfolder -v "your logs folder":/app/Logs -e CodeIndex__MonitorFolderRealPath="You real folder path" --restart=always qiuhaotc/codeindex
```

### Search Extension For Visual Studio

Download Url [Code Index Extension](https://marketplace.visualstudio.com/items?itemName=qiuhaotc.CodeIndexExtension)

Open Search Window

![Open Search Window](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/VSExtension-1.png)

Code Index Search Window

![Code Index Search Window](https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/VSExtension-2.png)

Config the service url to your own service and doing the searching

## Search Syntax

Refer [http://www.lucenetutorial.com/lucene-query-syntax.html](http://www.lucenetutorial.com/lucene-query-syntax.html)
