<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="wbp.aspx.cs" Inherits="BlackTambourine.Sitecore8.WorkboxPlus.V2.sitecore.shell.Applications.WorkboxPlusV2.wbp" %>

<!DOCTYPE html>
<html ng-app="wbpApp">
<head>
    <title>Sitecore - Workbox Plus</title>
    <meta charset="utf-8"/>
    <script src="http://code.jquery.com/jquery-1.12.4.min.js" integrity="sha256-ZosEbRLbNQzLpnKIkEdrPv7lOy9C27hHQ+Xp8a4MxAQ=" crossorigin="anonymous"></script>
    <script src="//ajax.googleapis.com/ajax/libs/angularjs/1.5.3/angular.js"></script>
    <script src="//ajax.googleapis.com/ajax/libs/angularjs/1.5.3/angular-touch.min.js"></script>
    <script src="//ajax.googleapis.com/ajax/libs/angularjs/1.5.3/angular-sanitize.js"></script>
    <script src="//ajax.googleapis.com/ajax/libs/angularjs/1.5.3/angular-resource.js"></script>
    <script src="//ajax.googleapis.com/ajax/libs/angularjs/1.5.3/angular-route.js"></script>
    <script src="//ajax.googleapis.com/ajax/libs/angularjs/1.5.3/angular-messages.js"></script>
    <script src="//ajax.googleapis.com/ajax/libs/angularjs/1.5.3/angular-animate.js"></script>
    <script src="//netdna.bootstrapcdn.com/bootstrap/3.3.6/js/bootstrap.js"></script>	
    <script src="https://cdn.rawgit.com/abdmob/x2js/master/xml2json.js"></script>
	
    <script src="wbp.js"></script>
	<link href="wbp.css" rel="stylesheet"/>

    <script src="//angular-ui.github.io/bootstrap/ui-bootstrap-tpls-1.3.3.min.js"></script>
    <link href="//netdna.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css" rel="stylesheet"/>

</head>
<body>
       		 
<div data-ng-controller="WorkboxPlusCtrl" data-ng-init="init('<%= this._workflowGuid %>', '<%= this._currentUserName %>', '<%= this._draftGuid %>', '<%= this._rejGuid %>', '<%= this._currentUserEmail %>')" data-ng-cloak>	
  
  <uib-accordion close-others="true">
    
	<!-- drafts section -->
	<div uib-accordion-group class="panel-default" heading="Draft" is-open="draftIsOpen">
		<uib-accordion-heading >
			<div style="font-weight: 600; font-size: 12px!important;">
			Draft <i class="pull-right glyphicon" ng-class="{'glyphicon-chevron-up': draftIsOpen, 'glyphicon-chevron-down': !draftIsOpen}"></i>
			</div>
		</uib-accordion-heading>
				
	    <div>			
		   <div data-ng-repeat="i in draftData" class="itemRow" data-ng-if="isDraftUser(i.description)">	        

				<div class="col-xs-1">
					<img style="margin:0 10px; float: right;" src="/temp/iconcache/applications/32x32/document.png" border="0" alt="" width="32px" height="32px">
				</div>
				<div class="col-xs-11">							
					<div>				
						<span><a href='{{i.link}}' class="itemTitle">{{i.title}}</a></span>
					</div>
					
					<div class="wfComments">
						<span data-ng-bind-html="i.description | parseComments"></span>
					</div>							
					
					<div class="wfActions">
						<span data-ng-bind-html="i.description | parseWorkflowActions"></span>
					</div>	
					
					<div class="wfPreview">
						<span data-ng-bind-html="i.description | parsePreview"></span>
					</div>
				</div>
		   </div>		   
	    </div>
    </div>
	
	
	<!-- rejected section -->
	<div uib-accordion-group class="panel-default" heading="Rejected" is-open="rejectedIsOpen">
			
			<uib-accordion-heading >
				<div style="font-weight: 600; font-size: 12px!important;">
				Rejected <i class="pull-right glyphicon" ng-class="{'glyphicon-chevron-up': rejectedIsOpen, 'glyphicon-chevron-down': !rejectedIsOpen}"></i>
				</div>
			</uib-accordion-heading>		
		
		  <div>
			   <div data-ng-repeat="i in rejectedData" class="itemRow" data-ng-if="isRejectedUser(i.description)">	        
			   
					<div>
						<span ><a href='{{i.link}}' class="itemTitle">{{i.title}}</a></span>
					</div>
					
					<div class="wfComments">
						<span data-ng-bind-html="i.description | parseComments"></span>
					</div>								
										
					<div class="wfActions">
						<span data-ng-bind-html="i.description | parseWorkflowActions"></span>
					</div>	
									
					<div class="wfPreview">
						<span data-ng-bind-html="i.description | parsePreview"></span>
					</div>

			   </div>
			   
		  </div>
    </div>
	
  </uib-accordion>
  

</div>
	   
</body>
</html>
