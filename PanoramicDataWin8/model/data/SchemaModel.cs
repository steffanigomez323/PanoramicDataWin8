﻿using System.Collections.Generic;
using Microsoft.Practices.Prism.Mvvm;
using PanoramicDataWin8.controller.data;

namespace PanoramicDataWin8.model.data
{
    public abstract class SchemaModel : BindableBase
    {
        public abstract List<OriginModel> OriginModels { get; }
    }
}