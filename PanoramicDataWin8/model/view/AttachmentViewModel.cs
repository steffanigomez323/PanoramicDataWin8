﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PanoramicDataWin8.model.data;
using PanoramicDataWin8.utils;

namespace PanoramicDataWin8.model.view
{
    public class AttachmentViewModel : ExtendedBindableBase
    {
        private VisualizationViewModel _visualizationViewModel;
        public VisualizationViewModel VisualizationViewModel
        {
            get
            {
                return _visualizationViewModel;
            }
            set
            {
                if (_visualizationViewModel != null)
                {
                    _visualizationViewModel.PropertyChanged -= _visualizationViewModel_PropertyChanged;
                }
                this.SetProperty(ref _visualizationViewModel, value);
                if (_visualizationViewModel != null)
                {
                    _visualizationViewModel.PropertyChanged += _visualizationViewModel_PropertyChanged;
                    _visualizationViewModel.QueryModel.PropertyChanged += QueryModel_PropertyChanged;    
                    initialize();
                }
            }
        }


        private ObservableCollection<AttachmentHeaderViewModel> _attachmentHeaderViewModels = new ObservableCollection<AttachmentHeaderViewModel>();
        public ObservableCollection<AttachmentHeaderViewModel> AttachmentHeaderViewModels
        {
            get
            {
                return _attachmentHeaderViewModels;
            }
            set
            {
                this.SetProperty(ref _attachmentHeaderViewModels, value);
            }
        }

        private AttachmentOrientation _attachmentOrientation;
        public AttachmentOrientation AttachmentOrientation
        {
            get
            {
                return _attachmentOrientation;
            }
            set
            {
                this.SetProperty(ref _attachmentOrientation, value);
            }
        }

        private bool _isDisplayed;
        public bool IsDisplayed
        {
            get
            {
                return _isDisplayed;
            }
            set
            {
                this.SetProperty(ref _isDisplayed, value);
            }
        }

        private Stopwatch _activeStopwatch = new Stopwatch();
        public Stopwatch ActiveStopwatch
        {
            get
            {
                return _activeStopwatch;
            }
            set
            {
                this.SetProperty(ref _activeStopwatch, value);
            }
        }

        public MenuViewModel CreateMenuViewModel(AttachmentItemViewModel attachmentItemViewModel)
        {
            MenuViewModel menuViewModel = new MenuViewModel()
            {
                AttachmentViewModel = this,
                AttachmentItemViewModel = attachmentItemViewModel,
                AttachmentOrientation = this.AttachmentOrientation
            };

            // is value InputOperationModel
            if (_visualizationViewModel.QueryModel.GetUsageInputOperationModel(InputUsage.Value).Contains(attachmentItemViewModel.InputOperationModel))
            {
                var aom = attachmentItemViewModel.InputOperationModel;
                if (aom.InputModel.InputDataType == InputDataTypeConstants.INT ||
                    aom.InputModel.InputDataType == InputDataTypeConstants.FLOAT)
                {
                    menuViewModel.NrRows = 3;
                    menuViewModel.NrColumns = 4;

                    List<ToggleMenuItemComponentViewModel> toggles = new List<ToggleMenuItemComponentViewModel>();
                    List<MenuItemViewModel> items = new List<MenuItemViewModel>();

                    int count = 0;
                    foreach (var aggregationFunction in Enum.GetValues(typeof(AggregateFunction)).Cast<AggregateFunction>().Where(af => af != AggregateFunction.None))
                    {
                        var menuItem = new MenuItemViewModel()
                        {
                            MenuViewModel = menuViewModel,
                            Row = count <= 2 ? 0 : 1,
                            RowSpan = count <= 2 ? 1 : 2,
                            Column = count % 3 + 1,
                            Size = new Vec(32, 50),
                            TargetSize = new Vec(32, 50)
                        };
                        menuItem.Position = attachmentItemViewModel.Position;
                        ToggleMenuItemComponentViewModel toggle = new ToggleMenuItemComponentViewModel()
                        {
                            Label = aggregationFunction.ToString(),
                            IsChecked = attachmentItemViewModel.InputOperationModel.AggregateFunction == aggregationFunction
                        };
                        toggles.Add(toggle);
                        menuItem.MenuItemComponentViewModel = toggle;
                        menuItem.MenuItemComponentViewModel.PropertyChanged += (sender, args) =>
                        {
                            var model = (sender as ToggleMenuItemComponentViewModel);
                            if (args.PropertyName == model.GetPropertyName(() => model.IsChecked))
                            {
                                if (model.IsChecked)
                                {
                                    attachmentItemViewModel.InputOperationModel.AggregateFunction = aggregationFunction;
                                    foreach (var tg in model.OtherToggles)
                                    {
                                        tg.IsChecked = false;
                                    }
                                }
                            }
                        };
                        menuViewModel.MenuItemViewModels.Add(menuItem);
                        items.Add(menuItem);
                        count++;
                    }

                    foreach (var mi in items)
                    {
                        (mi.MenuItemComponentViewModel as ToggleMenuItemComponentViewModel).OtherToggles.AddRange(toggles.Where(ti => ti != mi.MenuItemComponentViewModel));
                    }
                }
            }

            return menuViewModel;
        }

        void _visualizationViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
        }


        void QueryModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _visualizationViewModel.QueryModel.GetPropertyName(() => _visualizationViewModel.QueryModel.JobType) |
                e.PropertyName == _visualizationViewModel.QueryModel.GetPropertyName(() => _visualizationViewModel.QueryModel.VisualizationType))
            {
                initialize();
            }
        }

        void initialize()
        {
            AttachmentHeaderViewModels.Clear();
            if (_visualizationViewModel.QueryModel.JobType == JobType.DB)
            {
                if (_attachmentOrientation == AttachmentOrientation.Bottom)
                {
                    createDbBottom();
                }
            }
            else if (_visualizationViewModel.QueryModel.JobType == JobType.Kmeans)
            {
                if (_attachmentOrientation == AttachmentOrientation.Bottom)
                {
                    createKMeansBottom();
                }
            }
        }

        void createKMeansBottom()
        {
            AttachmentHeaderViewModel header = new AttachmentHeaderViewModel()
            {
                InputUsage = InputUsage.JobInput
            };
            // initialize items
            foreach (var item in _visualizationViewModel.QueryModel.GetUsageInputOperationModel(InputUsage.JobInput))
            {
                header.AttachmentItemViewModels.Add(new AttachmentItemViewModel()
                {
                    InputOperationModel = item,
                    AttachmentHeaderViewModel = header
                });
            }

            // handle added
            header.AddedTriggered = (inputOperationModel) =>
            {
                QueryModel queryModel = this.VisualizationViewModel.QueryModel;
                if (!queryModel.GetUsageInputOperationModel(InputUsage.JobInput).Contains(inputOperationModel))
                {
                    queryModel.AddUsageInputOperationModel(InputUsage.JobInput, inputOperationModel);
                }
            };
            // handle removed
            header.RemovedTriggered = (attachmentItemViewModel) =>
            {
                QueryModel queryModel = this.VisualizationViewModel.QueryModel;
                if (queryModel.GetUsageInputOperationModel(InputUsage.JobInput).Contains(attachmentItemViewModel.InputOperationModel))
                {
                    queryModel.RemoveUsageInputOperationModel(InputUsage.JobInput, attachmentItemViewModel.InputOperationModel);
                }
            };

            header.AddAttachmentItemViewModel = new AddAttachmentItemViewModel()
            {
                AttachmentHeaderViewModel = header,
                //Size = new Vec(25,25),
                //TargetSize = new Vec(25, 25),
                Label = "input"
            };

            // handle updates
            _visualizationViewModel.QueryModel.GetUsageInputOperationModel(InputUsage.JobInput).CollectionChanged += (sender, args) =>
            {
                if (args.OldItems != null)
                {
                    foreach (var item in args.OldItems)
                    {
                        if (header.AttachmentItemViewModels.Any(aiv => aiv.InputOperationModel == item))
                        {
                            header.AttachmentItemViewModels.Remove(header.AttachmentItemViewModels.First(aiv => aiv.InputOperationModel == item));
                        }
                    }
                }
                if (args.NewItems != null)
                {
                    foreach (var item in args.NewItems)
                    {
                        header.AttachmentItemViewModels.Add(new AttachmentItemViewModel()
                        {
                            InputOperationModel = item as InputOperationModel,
                            SubLabel = (item as InputOperationModel).InputModel.Name,
                            MainLabel = "input",
                            AttachmentHeaderViewModel = header
                        });
                    }
                }
            };
            AttachmentHeaderViewModels.Add(header);
        }

        void createDbBottom()
        {
            // value
            var intensityHeader = createValueAttachmentHeader();
            AttachmentHeaderViewModels.Add(intensityHeader);

            // grouping
            var groupHeader = createGroupingAttachmentHeader();
            AttachmentHeaderViewModels.Add(groupHeader);
        }

        AttachmentHeaderViewModel createValueAttachmentHeader()
        {
            var groupHeader = createInputFieldUsageAttachmentHeader(InputUsage.Value);

            // handle added
            groupHeader.AddedTriggered = (inputOperationModel) =>
            {
                QueryModel queryModel = this.VisualizationViewModel.QueryModel;
                if (inputOperationModel.InputModel.InputDataType == InputDataTypeConstants.INT ||
                    inputOperationModel.InputModel.InputDataType == InputDataTypeConstants.FLOAT)
                {
                    inputOperationModel.AggregateFunction = AggregateFunction.Avg;
                }
                else
                {
                    inputOperationModel.AggregateFunction = AggregateFunction.Count;
                }
                if (!queryModel.GetUsageInputOperationModel(InputUsage.Value).Contains(inputOperationModel))
                {
                    queryModel.AddUsageInputOperationModel(InputUsage.Value, inputOperationModel);
                }
            };
            // handle removed
            groupHeader.RemovedTriggered = (attachmentItemViewModel) =>
            {
                QueryModel queryModel = this.VisualizationViewModel.QueryModel;
                if (queryModel.GetUsageInputOperationModel(InputUsage.Value).Contains(attachmentItemViewModel.InputOperationModel))
                {
                    queryModel.RemoveUsageInputOperationModel(InputUsage.Value, attachmentItemViewModel.InputOperationModel);
                }
            };
            return groupHeader;
        }

        AttachmentHeaderViewModel createGroupingAttachmentHeader()
        {
             var groupHeader = createInputFieldUsageAttachmentHeader(InputUsage.Group);

            // handle added
            groupHeader.AddedTriggered = (inputOperationModel) =>
            {
                QueryModel queryModel = this.VisualizationViewModel.QueryModel;
                if (!queryModel.GetUsageInputOperationModel(InputUsage.Group).Contains(inputOperationModel))
                {
                    queryModel.AddUsageInputOperationModel(InputUsage.Group, inputOperationModel);
                }
            };
            // handle removed
            groupHeader.RemovedTriggered = (attachmentItemViewModel) =>
            {
                QueryModel queryModel = this.VisualizationViewModel.QueryModel;
                if (queryModel.GetUsageInputOperationModel(InputUsage.Group).Contains(attachmentItemViewModel.InputOperationModel))
                {
                    queryModel.RemoveUsageInputOperationModel(InputUsage.Group, attachmentItemViewModel.InputOperationModel);
                }
            };
            return groupHeader;
        }

        AttachmentHeaderViewModel createInputFieldUsageAttachmentHeader(InputUsage inputUsage)
        {
            AttachmentHeaderViewModel header = new AttachmentHeaderViewModel()
            {
                InputUsage = inputUsage
            };
            // initialize items
            foreach (var item in _visualizationViewModel.QueryModel.GetUsageInputOperationModel(inputUsage))
            {
                header.AttachmentItemViewModels.Add(new AttachmentItemViewModel()
                {
                    InputOperationModel = item,
                    AttachmentHeaderViewModel = header
                });
            }

            header.AddAttachmentItemViewModel = new AddAttachmentItemViewModel()
            {
                AttachmentHeaderViewModel = header,
                Label = inputUsage.ToString().ToLower()
            };

            // handle updates
            _visualizationViewModel.QueryModel.GetUsageInputOperationModel(inputUsage).CollectionChanged += (sender, args) =>
            {
                if (args.OldItems != null)
                {
                    foreach (var item in args.OldItems)
                    {
                        if (header.AttachmentItemViewModels.Any(aiv => aiv.InputOperationModel == item))
                        {
                            header.AttachmentItemViewModels.Remove(header.AttachmentItemViewModels.First(aiv => aiv.InputOperationModel == item));
                        }
                    }
                }
                if (args.NewItems != null)
                {
                    foreach (var item in args.NewItems)
                    {
                        header.AttachmentItemViewModels.Add(new AttachmentItemViewModel()
                        {
                            InputOperationModel = item as InputOperationModel,
                            SubLabel = (item as InputOperationModel).InputModel.Name,
                            MainLabel = inputUsage.ToString().ToLower(),
                            AttachmentHeaderViewModel = header
                        });
                    }
                }
            };
            return header;
        }
    }

    public enum AttachmentOrientation { Left, Top, Bottom, Right }
}
