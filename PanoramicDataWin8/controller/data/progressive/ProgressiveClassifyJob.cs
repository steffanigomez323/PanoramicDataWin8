﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Composition;
using Windows.UI.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PanoramicDataWin8.controller.data;
using PanoramicDataWin8.controller.view;
using PanoramicDataWin8.model.data;
using PanoramicDataWin8.model.data.common;
using PanoramicDataWin8.model.data.progressive;
using PanoramicDataWin8.model.data.result;
using PanoramicDataWin8.utils;

namespace PanoramicDataWin8.controller.data.progressive
{
    public class ProgressiveClassifyJob : Job
    {
        private Object _lock = new Object();
        private bool _isRunning = false;

        private string _requestUuid = "";
        private int _sampleSize = 0;
        private Stopwatch _stopWatch = new Stopwatch();
        private JObject _query = null;

        private TimeSpan _throttle = TimeSpan.FromMilliseconds(0);
        public QueryModel QueryModel { get; set; }
        public QueryModel QueryModelClone { get; set; }

        public ProgressiveClassifyJob(QueryModel queryModel, QueryModel queryModelClone, TimeSpan throttle, int sampleSize)
        {
            QueryModel = queryModel;
            QueryModelClone = queryModelClone;
            _sampleSize = sampleSize;
            _throttle = throttle;
            var psm = (queryModelClone.SchemaModel as ProgressiveSchemaModel);

            var features = QueryModelClone.GetUsageInputOperationModel(InputUsage.Feature).Select(iom => iom.InputModel.RawName).ToList();

            string filter = "";
            List<FilterModel> filterModels = new List<FilterModel>();
            filter = FilterModel.GetFilterModelsRecursive(QueryModelClone, new List<QueryModel>(), filterModels, true);

            List<string> brushes = new List<string>();
            foreach (var brushQueryModel in QueryModelClone.BrushQueryModels)
            {
                filterModels = new List<FilterModel>();
                var brush = FilterModel.GetFilterModelsRecursive(brushQueryModel, new List<QueryModel>(), filterModels, false);
                brushes.Add(brush);
            }

            _query = new JObject(
                new JProperty("type", "execute"),
                new JProperty("dataset", psm.RootOriginModel.DatasetConfiguration.Schema.RawName),
                new JProperty("task",
                    new JObject(
                        new JProperty("type", "classify"),
                        new JProperty("filter", filter),
                        new JProperty("chunkSize", sampleSize),
                        new JProperty("classifier", QueryModelClone.TaskModel.Name),
                        new JProperty("label", brushes[0]),
                        new JProperty("features", features)
                    ))
                );
        }
        public override void Start()
        {
            _stopWatch.Start();
            Task.Run(() => run());
            lock (_lock)
            {
                _isRunning = true;
            }
        }


        private async void run()
        {
            try
            {
                string response = null;//await ProgressiveGateway.Request(_query);
                JObject dict = JObject.Parse(response);
                _requestUuid = dict["uuid"].ToString();

                // starting looping for updates
                while (_isRunning)
                {

                    // starting looping for updates
                    while (_isRunning)
                    {
                        JObject lookupData = new JObject(
                            new JProperty("type", "lookup"),
                            new JProperty("uuid", _requestUuid));
                        string message = null;;//await ProgressiveGateway.Request(lookupData);

                        if (message != "None" && message != "null" && message != "\"None\"")
                        {
                            List<string> brushes = new List<string>();
                            foreach (var brushQueryModel in QueryModelClone.BrushQueryModels)
                            {
                                List<FilterModel> filterModels = new List<FilterModel>();
                                var brush = FilterModel.GetFilterModelsRecursive(brushQueryModel, new List<QueryModel>(), filterModels, false);
                                brushes.Add(brush);
                            }
                            var label = brushes[0];

                            ClassfierResultDescriptionModel resultDescriptionModel = new ClassfierResultDescriptionModel();
                            resultDescriptionModel.Uuid = _requestUuid;
                            JObject result = JObject.Parse(message);
                            double progress = (double)result["progress"];

                            var features = QueryModelClone.GetUsageInputOperationModel(InputUsage.Feature).ToList();
                            foreach (var feature in features)
                            {
                                //['actual and predicted', 'not actual and predicted', 'not actual and not predicted', 'actual and not predicted']
                                List<string> visBrushes = new List<string>() { "0", "1", "2", "3" };

                                JObject token = (JObject)result["histograms"][feature.InputModel.RawName];
                                VisualizationResultDescriptionModel visResultDescriptionModel = new VisualizationResultDescriptionModel();
                                List<ResultItemModel> resultItemModels = ProgressiveVisualizationJob.UpdateVisualizationResultDescriptionModel(visResultDescriptionModel, token, visBrushes,
                                    new List<InputOperationModel>()
                                    {
                                new InputOperationModel(feature.InputModel)
                                {
                                    AggregateFunction = AggregateFunction.None
                                },
                                new InputOperationModel(feature.InputModel)
                                {
                                    AggregateFunction = AggregateFunction.Count
                                }
                                    }, new List<AxisType>() { AxisType.Quantitative, AxisType.Quantitative }, new List<InputOperationModel>()
                                    {
                                new InputOperationModel(feature.InputModel)
                                {
                                    AggregateFunction = AggregateFunction.Count
                                }
                                    });
                                resultDescriptionModel.VisualizationResultModel.Add(new ResultModel()
                                {
                                    Progress = progress,
                                    ResultDescriptionModel = visResultDescriptionModel,
                                    ResultItemModels = new ObservableCollection<ResultItemModel>(resultItemModels)
                                });
                            }

                            /*var classifyResult = JsonConvert.DeserializeObject<ClassifyResult>(result[label].ToString());


                            resultDescriptionModel.ConfusionMatrices.Add(new List<double>());
                            resultDescriptionModel.ConfusionMatrices[0].Add((double)classifyResult.tp);
                            resultDescriptionModel.ConfusionMatrices[0].Add((double)classifyResult.fn);

                            resultDescriptionModel.ConfusionMatrices.Add(new List<double>());
                            resultDescriptionModel.ConfusionMatrices[1].Add((double)classifyResult.fp);
                            resultDescriptionModel.ConfusionMatrices[1].Add((double)classifyResult.tn);

                            var xs = classifyResult.fpr;
                            var ys = classifyResult.tpr;
                            resultDescriptionModel.RocCurve = new List<Pt>();
                            resultDescriptionModel.RocCurve.Add(new Pt(0, 0));
                            var step = 1; //ys.Count() > 300 ? 50 : 1;  

                            if (xs != null && ys != null)
                            {
                                for (int i = 0; i < xs.Count(); i += step)
                                {
                                    resultDescriptionModel.RocCurve.Add(new Pt((double)xs[i], (double)ys[i]));
                                }
                                resultDescriptionModel.RocCurve.Add(new Pt(1, 1));
                            }

                            resultDescriptionModel.Precision = classifyResult.precision;
                            resultDescriptionModel.Recall = classifyResult.recall;
                            resultDescriptionModel.AUC = classifyResult.auc;
                            resultDescriptionModel.F1s = classifyResult.f1;
                            resultDescriptionModel.Progresses = classifyResult.progress;


                            await fireUpdated(new List<ResultItemModel>(), progress, resultDescriptionModel);*/

                            if (progress >= 1.0)
                            {
                                Stop();
                                await fireCompleted();
                            }
                        }

                        await Task.Delay(_throttle);
                    }

                }
            }
            catch (Exception exc)
            {
                ErrorHandler.HandleError(exc.Message);
            }
        }
        
        private async Task fireUpdated(List<ResultItemModel> samples, double progress, ResultDescriptionModel resultDescriptionModel)
        {
            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FireJobUpdated(new JobEventArgs()
                {
                    Samples = samples,
                    Progress = progress,
                    ResultDescriptionModel = resultDescriptionModel
                });
            });
        }

        private async Task fireCompleted()
        {
            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FireJobCompleted(new EventArgs());
            });
        }


        public override void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;

                JObject lookupData = new JObject(
                               new JProperty("type", "halt"),
                               new JProperty("uuid", _requestUuid));
                //ProgressiveGateway.Request(lookupData);
            }
        }
    }
}