var app = angular.module("MyApp", []);

app.controller('myCtrl', ['$scope', '$http', function ($scope, $http) {

    $scope.uploadedCount = 0;
    $scope.TotalParts = 0;
    $scope.starttime = 0;
    $scope.endtime = 0;



    $scope.myFunc = function () {
        $scope.UploadFile($('#uploadFile')[0].files);
    };

    $scope.UploadFile = function (TargetFile) {
        var startdate = new Date();
        starttime = startdate.getTime();

        var FileChunk = [];
        var file = TargetFile[0];
        var MaxFileSizeMB = 1;
        var BufferChunkSize = 1048575;// mulpiple of 3

        var ReadBuffer_Size = 1024;
        var FileStreamPos = 0;
        var EndPos = BufferChunkSize;
        var Size = file.size;

        while (FileStreamPos < Size) {
            FileChunk.push(file.slice(FileStreamPos, EndPos));
            FileStreamPos = EndPos;
            EndPos = FileStreamPos + BufferChunkSize;
        }
        TotalParts = FileChunk.length;
        var PartCount = 0;

        uploadedCount = 0;
        var userId = "101";
        var dateTime = Date.now().toString();
        var promises = [];
        var fileName;
        while (chunk = FileChunk.shift()) {
            PartCount++;
            fileName = userId + "_" + dateTime + "_" + file.name + ".part_" + PartCount + "." + TotalParts;;
            promises.push($scope.UploadFileChunk(chunk, fileName, "UPLOAD"));
        }

        FileChunk = [];

        Promise.allSettled(promises)
            .then(results => {
                if (results.find(p => p.status === "rejected")) {
                    $scope.UploadFileChunk("", fileName, "DELETE")
                    console.log("Some of the promise got errored.. So need to delete the files");
                }
                else {
                    $scope.UploadFileChunk("", fileName, "MERGE")
                        .then(() => {
                            console.log("All sucess");
                            var enddate = new Date();
                            endtime = enddate.getTime();
                            console.log("Total time: " + (endtime - starttime));
                            alert("Total time: " + (endtime - starttime) + "ms");
                        }).catch(() => {
                            $scope.UploadFileChunk("", fileName, "DELETE")
                        });;

                }


            })
    }

    $scope.UploadFileChunk = function (Chunk, fileName, mode) {
        var FD = new FormData();
        if (mode === "UPLOAD")
            FD.append('file', Chunk, fileName);
        else
            FD.append('fileName', fileName);

        FD.append('mode', mode);

        return $http.post('/Home/UploadFile', FD, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        })
    }



}]);