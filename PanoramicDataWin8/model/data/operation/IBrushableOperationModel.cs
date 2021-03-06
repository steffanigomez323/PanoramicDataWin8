﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI;

namespace PanoramicDataWin8.model.data.operation
{
    public interface IBrushableOperationModel : IOperationModel
    {
        ObservableCollection<IBrushableOperationModel> BrushOperationModels { get; set; }
        List<Color> BrushColors { get; set; }
    }
}