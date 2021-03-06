﻿using System;
using IDEA_common.operations.risk;
using PanoramicDataWin8.model.data.operation;

namespace PanoramicDataWin8.controller.data.progressive
{
    public class RiskOperationJob : OperationJob
    {
        public RiskOperationJob(OperationModel operationModel,
            TimeSpan throttle) : base(operationModel, throttle)
        {
            OperationParameters = new NewModelOperationParameters()
            {
                RiskControlTypes = ((RiskOperationModel)operationModel).RiskControlTypes,
                Alpha = ((RiskOperationModel)operationModel).Alpha
            };
        }
    }
}