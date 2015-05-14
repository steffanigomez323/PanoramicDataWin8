﻿using System;
using System.Collections.ObjectModel;
using PanoramicDataWin8.utils;

namespace PanoramicDataWin8.model.data.result
{
    public class ResultModel : ExtendedBindableBase
    {
        public delegate void ResultModelUpdatedHandler(object sender, EventArgs e);
        public event ResultModelUpdatedHandler ResultModelUpdated;

        private ObservableCollection<ResultItemModel> _resultItemModels = null;
        public ObservableCollection<ResultItemModel> ResultItemModels
        {
            get
            {
                return _resultItemModels;
            }
            set
            {
                this.SetProperty(ref _resultItemModels, value);
            }
        }

        private ResultDescriptionModel _resultDescriptionModel = null;
        public ResultDescriptionModel ResultDescriptionModel
        {
            get
            {
                return _resultDescriptionModel;
            }
            set
            {
                this.SetProperty(ref _resultDescriptionModel, value);
            }
        }

        public void FireResultModelUpdated()
        {
            if (ResultModelUpdated != null)
            {
                ResultModelUpdated(this, new EventArgs());
            }
        }


        private double _progress = 0;
        public double Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                this.SetProperty(ref _progress, value);
            }
        }
    }
}
