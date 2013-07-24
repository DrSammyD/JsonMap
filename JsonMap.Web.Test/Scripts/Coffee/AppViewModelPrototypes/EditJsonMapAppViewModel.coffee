class EditJsonMapAppViewModel extends ko.ModelFactory.AppViewModel
  constructor: (initial) ->
    JsonMapsViewModel = initial["JsonMapsViewModel"]
    delete initial.JsonMapsViewModel
    super(initial)
    @JsonMapsViewModel = new ko.ModelFactory.JsonMapsViewModel(@_stubs["JsonMapsViewModel"], JsonMapsViewModel, @FormViewModel.TypeListViewModel.createAttributesListObservable, @FormViewModel.ValidationMethodListViewModel.createArgumentsListObservable)

ko.ModelFactory.EditJsonMapAppViewModel = EditJsonMapAppViewModel