define(() => FrontendAPI);

function FrontendAPI()
{		
	var funcs = [];
	function callRPC(namespace, funcName, funcArgVals)
	{
		if (!!funcs[funcName] === false)
		{
			funcs[funcName] = { FunctionName: funcName, OnReturn: null, OnError: null /*used by Promise*/ };
		}

		var promise = new Promise((resolve, reject) =>
		{
			funcs[funcName].OnReturn = resolve;
			funcs[funcName].OnError = reject;
		});

		var msg = JSON.stringify({ FunctionName: funcName, Arguments: funcArgVals });
		ajax(`/${namespace}/`, msg, p => funcs[funcName].uploadProgress = p);
		return promise;
	}

	function ajax(url, msg, onProgress)
	{
		onProgress = onProgress || null;

		var xhttp = new XMLHttpRequest();
		xhttp.onProgress = onProgress;

		xhttp.onreadystatechange = () => 
		{
			if (xhttp.readyState != 4) return;
			onRPCResponse(xhttp.responseText);
		};

		xhttp.open("POST", url, true);
		xhttp.send(msg);
	}

	function onRPCResponse(msg)
	{
		var data = null;
		try { data = JSON.parse(msg); } catch (e) { }
		var isRPCResponse = (data !== null) && !!funcs[data.FunctionName] && (data.ReturnValue !== undefined || !!data.Error);
		
		if(!isRPCResponse)
			return;
		
		if (!!data.Error && data.Error)
			funcs[data.FunctionName].OnError(data.Error);
		else
			funcs[data.FunctionName].OnReturn(data.ReturnValue);
	}
		
	this.uploadProgressOf = function(funcName){
		funcName = funcName.charAt(0).toUpperCase() + funcName.slice(1);
		return funcs[funcName].uploadProgress;
	}
		
	/***************************************** API ********************************************/
	
	this.activeActions = function() {
		return callRPC("FrontendAPI", "ActiveActions", Object.values(this.activeActions.arguments));
	}
	
							//listing
	
    this.getProjectInfos = function (onlyAvailable) {
        return callRPC("FrontendAPI", "GetProjectInfos", Object.values(this.getProjectInfos.arguments));
    };
	

    this.getProjectSyncInfos = function (templateIds) {
		return callRPC("FrontendAPI", "GetProjectSyncInfos", Object.values(this.getProjectSyncInfos.arguments));
    };
	

    this.getProjectUserPairs = function() {
		return callRPC("FrontendAPI", "GetProjectUserPairs", Object.values(this.getProjectUserPairs.arguments));
	}
	
							//details
	
    this.getProjectInfo = function (templateId) {
        return callRPC("FrontendAPI", "GetProjectInfo", Object.values(this.getProjectInfo.arguments));
    };
		
    this.updateTemplate = function(templateId, name, description, releaseOn) {
		return callRPC("FrontendAPI", "GetProjectInfo", Object.values(this.getProjectInfo.arguments));
	};	
	
							//shared
	
    this.getCacheInfos = function (templateIds) {
		return callRPC("FrontendAPI", "GetCacheInfos", Object.values(this.getCacheInfos.arguments));
    };

    this.getMediaInfos = function (templateIds) {	
		return callRPC("FrontendAPI", "GetMediaInfos", Object.values(this.getMediaInfos.arguments));
    };
	
							//create and clone
							
    this.createNewFromFile = function (templateId, fileData) {
       return callRPC("FrontendAPI", "CreateNewFromFile", Object.values(this.createNewFromFile.arguments));
    };
	
	this.getAvailableTemplates = function () {
	   return callRPC("FrontendAPI", "GetAvailableTemplates", Object.values(this.getAvailableTemplates.arguments));
	};

    this.cloneProject = function (sourceTemplateId, targetTemplateId) {
       return callRPC("FrontendAPI", "CloneProject", Object.values(this.cloneProject.arguments));
    };
	

    this.getProjectGameTemplatePairInfos = function () {
	   return callRPC("FrontendAPI", "GetProjectGameTemplatePairInfos", Object.values(this.getProjectGameTemplatePairInfos.arguments));
	};

	this.createNewTemplate = function (gameId, name, description){
	   return callRPC("FrontendAPI", "CreateNewTemplate", Object.values(this.createNewTemplate.arguments));
	};
}
