# CodeIndex

A fast code searching tools based on Lucene.Net

## Demonstrate Site

https://codeindex.qhnetdisk.tk/

## Use It On Your Local

### Config the index file path and the code folder you want to index

Change CodeIndex.Server => appsettings.json => LuceneIndex and MonitorFolder to your local

### Run Server

Run server and doing the searching

#### Test

Main Page
<div><img src="https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/WebServer.png"/></div>

Details Page
<div><img src="https://raw.githubusercontent.com/qiuhaotc/CodeIndex/master/doc/WebServer-Details.png"/></div>

### Run with docker
example

```bash
docker pull qiuhaotc/codeindex
docker run -d --name codeindex -p 8080:80 -v "You index folder":/luceneindex -v "You code folder":/monitorfolder -v "your logs folder":/app/Logs -e MonitorFolderRealPath="You real folder path" --restart=always qiuhaotc/codeindex
```
