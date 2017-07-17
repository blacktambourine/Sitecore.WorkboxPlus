var corpApp = angular.module('wbpApp', [
    'ngRoute',
    'ngMessages',
    'ngResource',
    'ngAnimate',
    'ui.bootstrap',
    'wbpApp.wbpModule'
]);

var wbpModule = angular.module('wbpApp.wbpModule', ['ngRoute']);

wbpModule.filter('trustHtml', ['$sce', function ($sce) {
    return function (html) {
        return $sce.trustAsHtml(html);
    };
}]);

wbpModule.filter('parseUserUrl', ['$sce', function ($sce) {
    return function (html) {
        var elements = $(html);
        var a = $('.user-link', elements);
        if (a === undefined)
            return '';

        return $sce.trustAsHtml(a[0].outerHTML);
    };
}]);

wbpModule.filter('parseWorkflowActions', ['$sce', function ($sce) {
    return function (html) {
        var elements = $(html);
        var a = elements[3];
        if (a === undefined)
            return '';

        return $sce.trustAsHtml(a.outerHTML);
    };
}]);

wbpModule.filter('parseComments', ['$sce', function ($sce) {
    return function (html) {
        var elements = $(html);
        var a = elements[0];
        if (a === undefined)
            return '';

        return $sce.trustAsHtml(a.outerHTML);
    };
}]);

wbpModule.filter('parsePreview', ['$sce', function ($sce) {
    return function (html) {
        var result = undefined;
        var elements = $(html);
        for (var i = 0; i <= elements.length; i++) {
            var b = elements[i];
            if (b.childNodes.length === 1 && b.childNodes[0].innerText === 'Preview the webpage') {
                result = b.childNodes[0];
                break;
            }
        }

        if (result === undefined)
            return '';

        return $sce.trustAsHtml(result.outerHTML);
    };
}]);


wbpModule.controller('WorkboxPlusCtrl', ['$scope', '$controller', '$http', function ($scope, $controller, $http) {

    $scope.isDraftUser = function (val) {
        var elements = $(val);
        var a = $('.user-link', elements);
        if (a === undefined)
            return false;

        if (a[0] !== undefined && $scope.currentUserName.replace(" ", "").toLowerCase() === a[0].innerText.replace(" ", "").toLowerCase()) {
            return true;
        }
        else {
            return false;
        }
    }

    //rejected items need to be filtered differently; i.e. check the workflow history field. i.e. get last table element and check it contains user email
    $scope.isRejectedUser = function (val) {
        var elements = $(val);
        var lastElement = elements.slice(-1)[0];
        if (lastElement === undefined)
            return false;

        if (lastElement.innerText.includes($scope.currentUserEmail.replace(" ", "").toLowerCase())) {
            return true;
        }
        else {
            return false;
        }
    }


    $scope.ParseXML = function (val) {
        if (window.DOMParser) {
            parser = new DOMParser();
            xmlDoc = parser.parseFromString(val, "text/xml");
        }
        else // Internet Explorer
        {
            xmlDoc = new ActiveXObject("Microsoft.XMLDOM"); xmlDoc.loadXML(val);
        }
        return xmlDoc;
    }

    $scope.getDraftData = function (scUrl) {
        $http.get(scUrl)
        .success(function (xmlDoc) {
            var x2js = new X2JS();
            var jsonObj = x2js.xml2json($scope.ParseXML(xmlDoc));
            $scope.draftData = jsonObj.rss.channel.item;
            return true;
        })
        .error(function (error) {
            console.log('an error occurred.');
        });
    }

    $scope.getRejectedData = function (scUrl) {
        $http.get(scUrl)
        .success(function (xmlDoc) {
            var x2js = new X2JS();
            var jsonObj = x2js.xml2json($scope.ParseXML(xmlDoc));
            $scope.rejectedData = jsonObj.rss.channel.item;
            return true;
        })
        .error(function (error) {
            console.log('an error occurred.');
        });
    }

    //initialise page
    $scope.init = function (workFlowId, userName, draftId, rejectedId, userEmail) {
        $scope.currentUserName = userName.replace("sitecore", "").replace(" ", "").toLowerCase();
        $scope.currentUserEmail = userEmail;
        $scope.workflowGuid = workFlowId;

        /* Draft WF */
        $scope.draftGuid = draftId;
        var scDraftUrl = '/sitecore/shell/~/feed/workflowstate.aspx?wf=%7B' + $scope.workflowGuid + '%7D&st=%7B' + $scope.draftGuid + '%7D';
        $scope.draftData = [];
        $scope.getDraftData(scDraftUrl);
        $scope.draftIsOpen = true;

        /* Rejected WF */
        $scope.rejectedGuid = rejectedId;
        var scRejectedUrl = '/sitecore/shell/~/feed/workflowstate.aspx?wf=%7B' + $scope.workflowGuid + '%7D&st=%7B' + $scope.rejectedGuid + '%7D';
        $scope.rejectedData = [];
        $scope.getRejectedData(scRejectedUrl);
        $scope.rejectedIsOpen = false;
    }
}]);
