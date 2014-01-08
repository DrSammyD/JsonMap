###* 
 * Copyright 2013 MacReport Media Publishing Inc.
 * Licensed under MPL-2.0 (see /LICENSE)
 * If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Author: Sam Armstrong
###


MapToList = {
  "EntityJsonMapViewModel": "Entitys"
  "AttributeJsonMapViewModel": "Attributes"
  "ValidationJsonMapViewModel": "Validations"
  "ArgumentJsonMapViewModel": "Arguments"
}

class JsonMapsViewModel extends ko.ModelFactory.ViewModel
  constructor: (initial, data, createAttributeList, createArgumentList) ->
    super(initial, data)

    lastSelected = ko.observable(null)
    @nodeSelected = ko.observable(null)
    @nodeCreated = ko.observable(null)
    @nodeDeleted = ko.observable(null)
    @nodesAdded = ko.observableArray()
    @nodesUnadded = ko.observableArray()
    @error = ko.observable(false)

    @nodeSelectedList = ko.computed () =>
      return if @nodeSelected()? then MapToList[@nodeSelected().constructor.name] else null
    @nodeCreatedList = ko.computed () =>
      return if @nodeCreated()? then MapToList[@nodeCreated().constructor.name] else null
    @nodeDeletedType = ko.computed () =>
      return if @nodeDeleted()? then MapToList[@nodeDeleted().constructor.name]
    @nodesCombindationNoValues = ko.computed () =>
      return $(@nodesAdded()).not([@nodeCreated()]).length == 0  && @nodesUnadded().length == 0
    @nodesCombination = ko.computed () =>
      combination = []
      if @nodeSelected()?
        combination = $.grep(@nodeSelected().childList(), (n) => @nodesUnadded.indexOf(n) == -1 )
      combination = combination.concat @nodesAdded()
      combination


    @nodeSelectedList.subscribe (newValue) =>
      @nodesAdded.removeAll()
      @nodesUnadded.removeAll()
      @nodeCreated(null)

    @nodeCreated.subscribe( (newValue) =>
        for val in $.grep(@nodesAdded(), (n) -> n.Id() == 0)
          @nodesAdded.remove(val)
        if newValue?
          @nodesAdded.push(newValue)
      )

    @createAttributeList = createAttributeList
    @createArgumentListObservable = createArgumentList

    for val in @Entitys()
      @processEntitys(val)

    for val in @Attributes()
      @processAttributes(val)

    for val in @Validations()
      @processValidations(val)

    for val in @Arguments()
      @processArguments(val)


    @createSelectedListDisplay("Entitys")
    @createSelectedListDisplay("Attributes","Entitys")
    @createSelectedListDisplay("Validations","Attributes")
    @createSelectedListDisplay("Arguments","Validations")

    @nodeCreated(null)
    @nodesAdded.removeAll()
    @nodesUnadded.removeAll()

  processNewViewModel: (val) ->
    if !val.Id()?
      val.Id(0)
      val.updateBackup()

  processViewModel: (val) ->
    if $.grep( @[MapToList[val.constructor.name]](), (n) -> n.Id() == val.Id() ).length > 0
      val.Id(0)
      val.updateBackup()

  processNewEntitys: (val) ->
    @processEntitys(val)
    @nodeCreated(val)
    val.GenericOnly(false)

  processNewAttributes: (val) ->
    @processAttributes(val)
    @nodeCreated(val)

  processNewValidations: (val) ->
    @processValidations(val)
    @nodeCreated(val)

  processNewArguments: (val) ->
    @processArguments(val)
    @nodeCreated(val)

  processEntitys: (val) ->
    val.setChildList(@Attributes)
    val.setGlobalSelector(@nodeSelected)

  processAttributes: (val) ->
    val.setChildList(@Validations)
    val.setParentList(@Entitys)
    val.setGlobalSelector(@nodeSelected)
    val.TypeAttributeList = @createAttributeList(val)
    val.TypeAttributeList.subscribe () ->
      val.restoreBackup()

  processValidations: (val) ->
    val.setChildList(@Arguments)
    val.setParentList(@Attributes)
    val.setGlobalSelector(@nodeSelected)

  processArguments: (val) ->
    val.setParentList(@Validations)
    val.setGlobalSelector(@nodeSelected)
    val.ValidArgumentList = @createArgumentListObservable(val)
    val.ValidArgumentList.subscribe () ->
      val.restoreBackup()

  processRemoveViewModel: (val) ->
    if val == @nodeSelected()
      @nodeSelected(null)

  createSelectedListDisplay: (mapList, parentDisplayMapList) ->
    viewModelList = @[mapList]
    list = mapList+"DisplayList"
        

    parentViewModelList = parentDisplayMapList
    parentList = parentDisplayMapList+"DisplayList"

    asynchTimer = ko.observable(10)

    @[mapList+"Total"]= ko.computed( ()=>
      mapArr = viewModelList()
      if @nodeCreated() && @nodeCreated().constructor.name == @model()["_classes"][mapList]["_base"]
        mapArr = [@nodeCreated()].concat(mapArr)
      mapArr
      )
    @[list] = ko.computed( () =>
      mapArr = []
      extraArr = []
      other = null
      if parentDisplayMapList?
        parentDisplayLengthEquality = ko.utils.unwrapObservable(@[parentList]).length == ko.utils.unwrapObservable(@[parentViewModelList]).length

      if @nodeCreated() && @nodeCreated().constructor.name == @model()["_classes"][mapList]["_base"]
        mapArr.push(@nodeCreated())
      for val in viewModelList()
        if val.selected() || val.childSelected() || val.parentSelected()
          mapArr.push(val)
        else if val.constructor.name == "ValidationJsonMapViewModel" 
          extraArr.push(val)
        else if parentDisplayMapList? && mapArr.length == 0 && !parentDisplayLengthEquality && $.grep(ko.utils.unwrapObservable(@[parentList]), (n) -> n.childList().indexOf(val) != -1).length > 0
          other = val
      mapArr = mapArr.concat(extraArr)
      if other != null
        mapArr.push(other)
      if mapArr.length > 0
        if parentDisplayMapList?
          #get alternative JsonMaps that are part of parents that are selected or their alternatives
          alternateMapList = $.grep(viewModelList(), (MapItem) =>
            $.grep(ko.utils.unwrapObservable(@[parentList]), (parentMapItem) =>
              $.grep(parentMapItem[parentMapItem.childVarName](), (childMapItem) =>
                MapItem.Id() == childMapItem.Id()
              ).length > 0
            ).length > 0
          )

          for val in alternateMapList
            if mapArr.indexOf(val) == -1
              mapArr.push(val)
        else
          for val in viewModelList()
            if $.grep(mapArr, (n) -> n.Name() == val.Name()).length > 0
              if mapArr.indexOf(val) == -1
                mapArr.push(val)
        if @nodeCreated() != null && mapArr.indexOf(@nodeCreated()) == -1 && viewModelList().indexOf(@nodeCreated()) != -1
          mapArr.push(@nodeCreated())
        mapArr
      else if parentDisplayLengthEquality != false
        viewModelList()
      else
        []
    ).extend({ throttle: 1000 })

    @[list+"Asynch"] = ko.asynchComputedArray(@[list])

  saveCreated: () ->
    selectedNode = @nodeSelected()
    createdNode = @nodeCreated()
    modelData = createdNode.initialCopy()
    if selectedNode != null
      selectedNode.toggleSelected()
    if selectedNode != null
      Url = "/JsonMap/"+createdNode.postUrl+"Create/"+ko.toJS(selectedNode).Id
    else
      Url = "/JsonMap/"+createdNode.postUrl+"Create/"
    $.ajax {
      type: 'Post',
      url: Url,
      datatype: 'text',
      data: modelData,
      success: (data) =>
        init = ko.utils.parseJson(data)
        if init.Id?
          @nodeCreated(null)
          createdNode.Id(init.Id)
          createdNode.updateBackup()
          @["push"+MapToList[createdNode.constructor.name]](createdNode)
          if selectedNode != null
            needsUpdate = selectedNode.needsUpdate()
            selectedNode["push"+selectedNode.childVarName]({$type: createdNode['$type'],Id: init.Id})
            if !needsUpdate
              selectedNode.updateBackup()
            setTimeout( 
              () => selectedNode.toggleSelected()
              1000)
    }

  saveModified: (updateNode = @nodeSelected()) ->
    selectedNode = @nodeSelected()
    modelData = updateNode.copy()
    newChildren = ko.toJS(@nodesCombination)
    if updateNode == @nodeSelected()
      modelData[modelData.childVarName].removeAll()
      for val in newChildren
        modelData["push"+modelData.childVarName](val)
    $.ajax {
      type: 'Post',
      url: "/JsonMap/"+updateNode.postUrl+"Update",
      datatype: 'text',
      data: modelData.initialCopy()
      success: (data) =>
        if updateNode == selectedNode
          updateNode[updateNode.childVarName].removeAll()
          for val in newChildren
            updateNode["push"+updateNode.childVarName](val)
        updateNode.updateBackup()
        @nodesAdded.removeAll()
        @nodesUnadded.removeAll()
        updateNode.toggleSelected()
      error: (data) =>
        alert(ko.utils.parseJson(data.responseText).message)
        @nodeDeleted(null)
    }

  deleteJsonMap: () ->
    deletedNode = @nodeDeleted()
    url = "/JsonMap/"+@nodeDeleted().postUrl+"Delete/"+ko.toJS(deletedNode).Id
    $.ajax
      type: 'Post'
      url: url
      success: (data) =>
        @nodeDeleted(null)
        if data == "Success"
          @recursiveRemoveJsonMaps(deletedNode, true)
          @nodesAdded.remove(deletedNode)
          @nodesUnadded.remove(deletedNode)
      error: (data) =>
        alert(ko.utils.parseJson(data.responseText).message)
        @nodeDeleted(null)

  saveMaps: () ->
    url = "/JsonMap/SaveMaps"
    $.ajax
      type: 'Post'
      url: url
      success: (data) =>
        console.log("Maps have been saved.")

  recursiveRemoveJsonMaps: (jsonMap, isUpdated) ->
    jsonMap.remove(isUpdated)
    type = jsonMap.constructor.name.split("JsonMap")[0]
    @["remove"+ MapToList[jsonMap.constructor.name]](jsonMap)
    if type == "Entity" || type == "Validation" #|| (type == "Attribute" && ko.utils.unwrapObservable(jsonMap.Name) == "this")
      for val in $.grep(jsonMap.childList(), (n) -> ko.utils.unwrapObservable(n.parentList).length == 0)
        @recursiveRemoveJsonMaps(val)
    jsonMap.remove()

  unaddFromSelected: (jsonMap) ->
    if @nodeSelected().childList?
      if ko.utils.unwrapObservable(@nodeSelected().childList).indexOf(jsonMap) == -1
        @nodesAdded.remove(jsonMap)
      else
        @nodesUnadded.push(jsonMap)

  addToSelected: (jsonMap) ->
    if @nodeSelected().childList?
      if ko.utils.unwrapObservable(@nodeSelected().childList).indexOf(jsonMap) == -1
        @nodesAdded.push(jsonMap)
      else
        @nodesUnadded.remove(jsonMap)

  createNewViewModel: (list) ->
    if @["pushNew"+list]?
      @createPushNewModelToArray(list)
    @["pushNew"+list]()
    @["remove"+list](@nodeCreated())

  removeViewModel: (item, list) ->
    if @["remove"+list]?
      @createRemoveModelFromArray(list)
    @["remove"+list](item)
    if @nodeCreated() == item
      @nodeCreated(null)

  #Display button functions
  showSelect: (item) ->
    item.selected() || (@nodeCreated() == null && @nodesCombindationNoValues())

  showAdd: (item, list) ->
    @nodesCombination().indexOf(item) == -1 && @nodeSelectedList() == list && @nodeCreated() != item

  showUnadd: (item, list) ->
    @nodesCombination().indexOf(item) != -1 && @nodeSelectedList() == list && @nodeCreated() != item

  showUpdate: (item) ->
    ((@nodeSelected() == item && !@nodesCombindationNoValues()) || item.needsUpdate()) && @nodeCreated() == null


class JsonMapViewModel extends ko.ModelFactory.ViewModel
  constructor: (initial, data) ->
    super(initial, data)
    @selected = ko.observable(false)
    @childSelected = ko.observable(false)
    @parentSelected = ko.observable(false)
    @childList = () -> []
    @parentList = () -> []
    @childVarName = ""
    @createBackup()

  setParentList: (parentList) ->
    @parentList = ko.computed( () =>
      parentArr = $.grep parentList(), (n) =>
        $.grep(n.childList(), (i) =>
          i == @
        ).length > 0
      if parentArr.length == 0 && @globalSelector? && @globalSelector() != null
        parentArr = [@globalSelector()]
      parentArr
    ).extend({ throttle: 500 })

  setChildList: (childList) ->
    @childList = ko.computed( () =>
      $.grep childList(), (n) =>
        $.grep(@[@childVarName](), (i) =>
          i.Id() == n.Id()
        ).length > 0
    ).extend({ throttle: 500 })

  setGlobalSelector: (parent) ->
    @globalSelector = parent

  toggleSelected: (changeGlobal = false) ->
    if @ != @globalSelector() && !changeGlobal && @globalSelector() != null
      if @globalSelector().selected? && @globalSelector().selected()
        @globalSelector().toggleSelected(true)
    @globalSelector(@)
    @globalSelector().selected(!@selected())
    if @childList?
      for val in @childList()
        val.setParentSelected(@selected())
    if @parentList?
      for val in @parentList()
        val.setChildSelected(@selected())
    if !@selected() && !changeGlobal
      @globalSelector(null)

  setParentSelected: (selected) ->
    @parentSelected(selected)
    if @childList?
      for val in @childList()
        val.setParentSelected(selected)

  setChildSelected: (selected) ->
    @childSelected(selected)
    if @parentList?
      for val in @parentList()
        val.setChildSelected(selected)

  remove: (isUpdated) ->
    if @parentList?
      for val in ko.utils.unwrapObservable(@parentList)
        val.removeChild(@)
        if isUpdated
          val.updateBackup()
    @


  removeChild: (childViewModel) ->
    for val in $.grep( @[@childVarName](), (n) -> n.Id() == ko.utils.unwrapObservable(childViewModel.Id) )
      @["remove"+@childVarName](val)


class EntityJsonMapViewModel extends JsonMapViewModel
  constructor: (initial, data) ->
    super(initial, data)
    @childVarName = "Attributes"
    @postUrl = "Entity"

class AttributeJsonMapViewModel extends JsonMapViewModel
  constructor: (initial, data) ->
    super(initial, data)
    @childVarName = "Validations"
    @postUrl = "Attribute"

class ValidationJsonMapViewModel extends JsonMapViewModel
  constructor: (initial, data) ->
    super(initial, data)
    @childVarName = "Arguments"
    @postUrl = "Validation"

class ArgumentJsonMapViewModel extends JsonMapViewModel
  constructor: (initial, data) ->
    super(initial, data)
    @childVarName = "JsonMap"
    @postUrl = "Argument"
    @[@childVarName] = ko.observableArray([])

class TypeListViewModel extends ko.ModelFactory.ViewModel
  constructor: (initial, data) ->
    super(initial, data)
    @TypeList = ko.computed( () =>
      pairs = _.pairs(@TypeAttributeList)
      filtered = _.filter(pairs, (pair) -> ko.isObservable((pair[1])))
      _.map(filtered, (pair)-> pair[0])
      ).extend({throttle: 500})

  createAttributesListObservable: (attribute) =>
    ko.computed( () =>
      keyArr = $.grep(@TypeList(), (n) ->
        $.grep(attribute.parentList(), (i) ->
          n == i.Name()
          ).length > 0
        )
      attributeArray = []
      for key in keyArr
        attributeArray.push(_.map(@TypeAttributeList[key](), (name) -> ko.utils.unwrapObservable(name.Name)))
      _.reduce(attributeArray, (sum,num) -> _.intersection(sum,num) )
    ).extend({throttle: 500 })

class ValidationMethodListViewModel extends ko.ModelFactory.ViewModel
  constructor: (initial, data) ->
    super(initial, data)
    @ValidMethodList = ko.computed( () =>
      pairs = _.pairs(@ValidationMethodList)
      filtered = _.filter(pairs, (pair) -> ko.isObservable((pair[1])))
      _.map(filtered, (pair)-> pair[0])
      ).extend({throttle: 500})

  createArgumentsListObservable: (Argument) =>
    ko.computed( () =>
      keyArr = $.grep(@ValidMethodList(), (n) ->
        $.grep(Argument.parentList(), (i) ->
          n == i.Name()
          ).length > 0
        )
      argumentArray = []
      for key in keyArr
        argumentArray.push(_.map(@ValidationMethodList[key](), (name) -> ko.utils.unwrapObservable(name.Name)))
      _.reduce(argumentArray, (sum,num) -> _.intersection(sum,num) )
      _.flatten(argumentArray)
    ).extend({throttle: 500 })



ko.ModelFactory.JsonMapViewModel = JsonMapViewModel
ko.ModelFactory.JsonMapsViewModel = JsonMapsViewModel
ko.ModelFactory.EntityJsonMapViewModel = EntityJsonMapViewModel
ko.ModelFactory.AttributeJsonMapViewModel = AttributeJsonMapViewModel
ko.ModelFactory.ValidationJsonMapViewModel = ValidationJsonMapViewModel
ko.ModelFactory.ArgumentJsonMapViewModel = ArgumentJsonMapViewModel

ko.ModelFactory.TypeListViewModel = TypeListViewModel
ko.ModelFactory.ValidationMethodListViewModel = ValidationMethodListViewModel
