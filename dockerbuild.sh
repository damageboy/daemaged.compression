#!/bin/bash

mkdir -p nuget.package

git submodule foreach git clean -fdx && git submodule foreach git reset --hard

RID=$1

docker build -t $RID -f Dockerfile.$RID .

docker run -it -e RID=$RID --name=$RID $RID

docker cp $RID:/nativebinaries/runtimes nuget.package/

docker rm $RID
