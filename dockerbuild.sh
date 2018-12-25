#!/bin/bash

RID=$1

docker build -t $RID -f Dockerfile.$RID .

docker run -it -e RID=$RID --name=$RID $RID

docker cp $RID:/nativebinaries/runtimes nuget.package/

docker rm $RID
